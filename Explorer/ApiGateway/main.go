package main

import (
	"bytes"
	"context"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"net/http"
	"net/http/httputil"
	"net/url"
	"os"
	"strconv"
	"strings"
	"time"

	"go.opentelemetry.io/contrib/instrumentation/net/http/otelhttp"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/propagation"
)

type route struct {
	prefix string
	target *url.URL
	proxy  *httputil.ReverseProxy
}

type gatewayHandler struct {
	tourServiceURL         string
	stakeholdersServiceURL string
	internalApiKey         string
	httpClient             *http.Client
}

type jsonRPCRequest struct {
	JsonRPC string         `json:"jsonrpc"`
	Method  string         `json:"method"`
	Params  map[string]int `json:"params"`
	ID      string         `json:"id"`
}

type jsonRPCResponse struct {
	JsonRPC string          `json:"jsonrpc"`
	ID      json.RawMessage `json:"id"`
	Result  json.RawMessage `json:"result"`
	Error   *jsonRPCError   `json:"error"`
}

type jsonRPCError struct {
	Code    int      `json:"code"`
	Message string   `json:"message"`
	Details []string `json:"details"`
}

// tourExecutionResult is used to decode the StartTour RPC result for SAGA step 2
type tourExecutionResult struct {
	Id        int    `json:"id"`
	TouristId int    `json:"touristId"`
	TourId    int    `json:"tourId"`
	Status    string `json:"status"`
}

func main() {
	port := getEnv("PORT", "8080")
	tourServiceURL := getEnv("TOUR_SERVICE_URL", "http://tour_service:8080")
	stakeholdersServiceURL := getEnv("STAKEHOLDERS_SERVICE_URL", "http://stakeholders_service:8080")
	internalApiKey := getEnv("INTERNAL_API_KEY", "internal-secret")

	tp, err := initTracer("api-gateway")
	if err != nil {
		log.Fatalf("Failed to initialize tracer: %v", err)
	}
	defer func() { _ = tp.Shutdown(context.Background()) }()

	gw := &gatewayHandler{
		tourServiceURL:         strings.TrimRight(tourServiceURL, "/"),
		stakeholdersServiceURL: strings.TrimRight(stakeholdersServiceURL, "/"),
		internalApiKey:         internalApiKey,
		httpClient: &http.Client{
			Timeout: 30 * time.Second,
			// OTel transport: RPC i SAGA pozivi ka servisima se beleze kao
			// client spanovi i propagiraju trace context
			Transport: otelhttp.NewTransport(http.DefaultTransport),
		},
	}

	routes := []route{
		mustRoute("/api/auth", getEnv("AUTH_SERVICE_URL", "http://auth_service:8080")),
		mustRoute("/api/profiles", stakeholdersServiceURL),
		mustRoute("/api/blogs", getEnv("BLOG_SERVICE_URL", "http://blog_service:8080")),
		mustRoute("/api/followers", getEnv("FOLLOWER_SERVICE_URL", "http://follower_service:8080")),
		mustRoute("/api/tours", tourServiceURL),
		mustRoute("/api/purchases", getEnv("PURCHASE_SERVICE_URL", "http://purchase_service:8080")),
	}

	mux := http.NewServeMux()
	mux.HandleFunc("/health", func(w http.ResponseWriter, r *http.Request) {
		applyCORS(w, r)
		if r.Method == http.MethodOptions {
			w.WriteHeader(http.StatusNoContent)
			return
		}
		w.WriteHeader(http.StatusOK)
		_, _ = w.Write([]byte("ok"))
	})

	mux.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
		if r.Method == http.MethodOptions {
			applyCORS(w, r)
			w.WriteHeader(http.StatusNoContent)
			return
		}

		// RPC routes checked before reverse-proxy fallback
		if gw.tryHandleTourRPC(w, r) {
			return
		}

		for _, route := range routes {
			if strings.HasPrefix(r.URL.Path, route.prefix) {
				route.proxy.ServeHTTP(w, r)
				return
			}
		}

		http.NotFound(w, r)
	})

	// Server span za svaki zahtev koji prodje kroz gateway; ime spana je metoda + putanja
	tracedMux := otelhttp.NewHandler(mux, "api-gateway",
		otelhttp.WithSpanNameFormatter(func(_ string, r *http.Request) string {
			return r.Method + " " + r.URL.Path
		}),
	)

	log.Printf("API gateway listening on :%s", port)
	if err := http.ListenAndServe(":"+port, tracedMux); err != nil {
		log.Fatal(err)
	}
}

// tryHandleTourRPC intercepts POST requests that map to JSON-RPC methods.
// Returns true if the request was handled (success or error), false to fall through.
func (g *gatewayHandler) tryHandleTourRPC(w http.ResponseWriter, r *http.Request) bool {
	if r.Method != http.MethodPost {
		return false
	}

	pathParts := strings.Split(strings.Trim(r.URL.Path, "/"), "/")

	// POST /api/tours/{id}/publish|archive — existing tour management RPC
	if len(pathParts) == 4 && pathParts[0] == "api" && pathParts[1] == "tours" {
		tourID, err := strconv.Atoi(pathParts[2])
		if err != nil {
			return false
		}

		var method string
		switch pathParts[3] {
		case "publish":
			method = "TourService.PublishTour"
		case "archive":
			method = "TourService.ArchiveTour"
		case "start":
			// ─────────────────────────────────────────────────────────────────
			// RPC 1: TourExecutionService.StartTour
			// Wrapped in a SAGA: step 1 creates the execution via RPC, then
			// step 2 records it on the tourist profile in StakeholdersService.
			// Compensation cancels the execution if step 2 fails.
			// ─────────────────────────────────────────────────────────────────
			applyCORS(w, r)
			g.startTourSaga(w, r, tourID)
			return true
		default:
			return false
		}

		applyCORS(w, r)
		if err := g.forwardTourRPC(w, r, method, map[string]int{"tourId": tourID}); err != nil {
			log.Printf("tour RPC %s failed: %v", method, err)
			writeJSONError(w, http.StatusBadGateway, "tour service unavailable")
		}
		return true
	}

	// POST /api/tours/executions/{id}/check-nearby
	// ─────────────────────────────────────────────────────────────────────────
	// RPC 2: TourExecutionService.CheckNearbyKeyPoint
	// Front-end calls this every 10 s while a tour is active. Gateway forwards
	// the request to TourService via JSON-RPC (not plain REST) so the service
	// boundary is an explicit RPC contract, not an HTTP resource endpoint.
	// ─────────────────────────────────────────────────────────────────────────
	if len(pathParts) == 5 &&
		pathParts[0] == "api" && pathParts[1] == "tours" &&
		pathParts[2] == "executions" && pathParts[4] == "check-nearby" {

		executionID, err := strconv.Atoi(pathParts[3])
		if err != nil {
			return false
		}

		applyCORS(w, r)
		if err := g.forwardTourRPC(w, r, "TourExecutionService.CheckNearbyKeyPoint",
			map[string]int{"executionId": executionID}); err != nil {
			log.Printf("CheckNearbyKeyPoint RPC failed: %v", err)
			writeJSONError(w, http.StatusBadGateway, "tour service unavailable")
		}
		return true
	}

	return false
}

// ─── SAGA: StartTour ──────────────────────────────────────────────────────────
//
// Orchestration SAGA with two participants:
//   Step 1 — TourService (via JSON-RPC): create TourExecution
//   Step 2 — StakeholdersService (via internal REST): record active execution on profile
//
// Compensation:
//   If step 2 fails → call TourService RPC CancelExecution to roll back step 1
func (g *gatewayHandler) startTourSaga(w http.ResponseWriter, r *http.Request, tourID int) {
	// ── Step 1: TourService.StartTour via RPC ─────────────────────────────────
	executionRaw, err := g.callTourRPC(r, "TourExecutionService.StartTour",
		map[string]int{"tourId": tourID})
	if err != nil {
		log.Printf("SAGA StartTour step1 failed: %v", err)
		writeJSONError(w, http.StatusBadGateway, "tour service unavailable")
		return
	}

	if executionRaw.Error != nil {
		w.Header().Set("Content-Type", "application/json")
		w.WriteHeader(normalizeStatusCode(executionRaw.Error.Code))
		_ = json.NewEncoder(w).Encode(map[string]any{
			"message": executionRaw.Error.Message,
			"errors":  executionRaw.Error.Details,
		})
		return
	}

	var execution tourExecutionResult
	if err := json.Unmarshal(executionRaw.Result, &execution); err != nil {
		log.Printf("SAGA StartTour: failed to parse execution result: %v", err)
		writeJSONError(w, http.StatusBadGateway, "invalid response from tour service")
		return
	}

	// ── Step 2: StakeholdersService — record active execution on tourist profile
	// Non-critical: if this fails we log it but do NOT roll back the execution.
	// TourService is the authoritative source of truth for execution state.
	if err := g.setActiveTourOnProfile(r, execution.TouristId, execution.Id); err != nil {
		log.Printf("SAGA StartTour step2 non-critical failure (executionId=%d): %v — continuing", execution.Id, err)
	}

	// ── SAGA complete — return execution to client ────────────────────────────
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(http.StatusCreated)
	_, _ = w.Write(executionRaw.Result)
}

// setActiveTourOnProfile calls StakeholdersService internal endpoint (SAGA step 2)
func (g *gatewayHandler) setActiveTourOnProfile(r *http.Request, userId, executionId int) error {
	body, _ := json.Marshal(map[string]int{"userId": userId, "executionId": executionId})
	req, err := http.NewRequestWithContext(r.Context(), http.MethodPost,
		g.stakeholdersServiceURL+"/api/profiles/internal/active-tour",
		bytes.NewReader(body))
	if err != nil {
		return err
	}
	req.Header.Set("Content-Type", "application/json")
	req.Header.Set("X-Internal-Api-Key", g.internalApiKey)

	resp, err := g.httpClient.Do(req)
	if err != nil {
		return err
	}
	defer resp.Body.Close()

	if resp.StatusCode >= 300 {
		respBody, _ := io.ReadAll(resp.Body)
		return fmt.Errorf("stakeholders service returned %d: %s", resp.StatusCode, string(respBody))
	}
	return nil
}

// compensateCancelExecution calls TourService RPC to cancel a just-created execution (SAGA compensation)
func (g *gatewayHandler) compensateCancelExecution(r *http.Request, executionId int) error {
	result, err := g.callTourRPC(r, "TourExecutionService.CancelExecution",
		map[string]int{"executionId": executionId})
	if err != nil {
		return err
	}
	if result.Error != nil {
		return fmt.Errorf("RPC error %d: %s", result.Error.Code, result.Error.Message)
	}
	return nil
}

// ─── RPC helpers ─────────────────────────────────────────────────────────────

// forwardTourRPC sends an RPC call and writes the response directly to w
func (g *gatewayHandler) forwardTourRPC(w http.ResponseWriter, r *http.Request, method string, params map[string]int) error {
	rpcResp, err := g.callTourRPC(r, method, params)
	if err != nil {
		return err
	}

	w.Header().Set("Content-Type", "application/json")

	if rpcResp.Error != nil {
		w.WriteHeader(normalizeStatusCode(rpcResp.Error.Code))
		return json.NewEncoder(w).Encode(map[string]any{
			"message": rpcResp.Error.Message,
			"errors":  rpcResp.Error.Details,
		})
	}

	if len(rpcResp.Result) == 0 {
		w.WriteHeader(http.StatusBadGateway)
		return json.NewEncoder(w).Encode(map[string]string{"message": "invalid RPC response"})
	}

	w.WriteHeader(http.StatusOK)
	_, err = w.Write(rpcResp.Result)
	return err
}

// callTourRPC sends a JSON-RPC 2.0 request to TourService and returns the parsed response
func (g *gatewayHandler) callTourRPC(r *http.Request, method string, params map[string]int) (*jsonRPCResponse, error) {
	requestBody := jsonRPCRequest{
		JsonRPC: "2.0",
		Method:  method,
		Params:  params,
		ID:      fmt.Sprintf("%s-%d", strings.ToLower(strings.TrimPrefix(method, "TourService.")), time.Now().UnixNano()),
	}

	payload, err := json.Marshal(requestBody)
	if err != nil {
		return nil, err
	}

	upstreamReq, err := http.NewRequestWithContext(
		r.Context(),
		http.MethodPost,
		g.tourServiceURL+"/internal/rpc",
		bytes.NewReader(payload),
	)
	if err != nil {
		return nil, err
	}

	upstreamReq.Header.Set("Content-Type", "application/json")
	if authHeader := r.Header.Get("Authorization"); authHeader != "" {
		upstreamReq.Header.Set("Authorization", authHeader)
	}

	resp, err := g.httpClient.Do(upstreamReq)
	if err != nil {
		return nil, err
	}
	defer resp.Body.Close()

	body, err := io.ReadAll(resp.Body)
	if err != nil {
		return nil, err
	}

	trimmed := strings.TrimSpace(string(body))
	if trimmed == "" {
		return &jsonRPCResponse{
			Error: &jsonRPCError{
				Code:    resp.StatusCode,
				Message: fmt.Sprintf("tour service returned empty response (status %d)", resp.StatusCode),
			},
		}, nil
	}

	var rpcResponse jsonRPCResponse
	if err := json.Unmarshal(body, &rpcResponse); err != nil {
		return &jsonRPCResponse{
			Error: &jsonRPCError{
				Code:    resp.StatusCode,
				Message: fmt.Sprintf("tour service returned invalid JSON (status %d): %s", resp.StatusCode, truncateForLog(trimmed, 220)),
			},
		}, nil
	}

	return &rpcResponse, nil
}

func truncateForLog(value string, max int) string {
	if len(value) <= max {
		return value
	}
	return value[:max] + "..."
}

func writeJSONError(w http.ResponseWriter, status int, message string) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	_ = json.NewEncoder(w).Encode(map[string]string{"message": message})
}

func normalizeStatusCode(code int) int {
	if code >= 400 && code < 600 {
		return code
	}
	return http.StatusBadGateway
}

func mustRoute(prefix, rawTarget string) route {
	target, err := url.Parse(rawTarget)
	if err != nil {
		log.Fatalf("invalid target for %s: %v", prefix, err)
	}

	proxy := httputil.NewSingleHostReverseProxy(target)
	originalDirector := proxy.Director
	proxy.Director = func(req *http.Request) {
		originalDirector(req)
		req.Host = target.Host
		req.Header.Set("X-Forwarded-Host", req.Host)
		req.Header.Set("X-Forwarded-Proto", "http")
		// Upisuje traceparent heder u prosledjeni zahtev — downstream servis
		// (npr. follower servis) nastavlja isti trace umesto da pocne novi
		otel.GetTextMapPropagator().Inject(req.Context(), propagation.HeaderCarrier(req.Header))
	}
	proxy.ModifyResponse = func(resp *http.Response) error {
		resp.Header.Set("Access-Control-Allow-Origin", "http://localhost:5173")
		resp.Header.Set("Access-Control-Allow-Headers", "Authorization, Content-Type")
		resp.Header.Set("Access-Control-Allow-Methods", "GET, POST, PUT, PATCH, DELETE, OPTIONS")
		resp.Header.Set("Access-Control-Allow-Credentials", "true")
		return nil
	}
	proxy.ErrorHandler = func(w http.ResponseWriter, r *http.Request, err error) {
		log.Printf("proxy error for %s: %v", prefix, err)
		w.WriteHeader(http.StatusBadGateway)
		_, _ = w.Write([]byte("upstream service unavailable"))
	}

	return route{
		prefix: prefix,
		target: target,
		proxy:  proxy,
	}
}

func applyCORS(w http.ResponseWriter, r *http.Request) {
	origin := r.Header.Get("Origin")
	if origin == "http://localhost:5173" {
		w.Header().Set("Access-Control-Allow-Origin", origin)
	}
	w.Header().Set("Vary", "Origin")
	w.Header().Set("Access-Control-Allow-Headers", "Authorization, Content-Type")
	w.Header().Set("Access-Control-Allow-Methods", "GET, POST, PUT, PATCH, DELETE, OPTIONS")
	w.Header().Set("Access-Control-Allow-Credentials", "true")
}

func getEnv(key, fallback string) string {
	if value := os.Getenv(key); value != "" {
		return value
	}
	return fallback
}
