package main

import (
	"bytes"
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
)

type route struct {
	prefix string
	target *url.URL
	proxy  *httputil.ReverseProxy
}

type rpcGateway struct {
	tourServiceURL string
	httpClient     *http.Client
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

func main() {
	port := getEnv("PORT", "8080")
	tourServiceURL := getEnv("TOUR_SERVICE_URL", "http://tour_service:8080")

	rpcClient := rpcGateway{
		tourServiceURL: strings.TrimRight(tourServiceURL, "/"),
		httpClient: &http.Client{
			Timeout: 30 * time.Second,
		},
	}

	routes := []route{
		mustRoute("/api/auth", getEnv("AUTH_SERVICE_URL", "http://auth_service:8080")),
		mustRoute("/api/profiles", getEnv("STAKEHOLDERS_SERVICE_URL", "http://stakeholders_service:8080")),
		mustRoute("/api/blogs", getEnv("BLOG_SERVICE_URL", "http://blog_service:8080")),
		mustRoute("/api/followers", getEnv("FOLLOWER_SERVICE_URL", "http://follower_service:8080")),
		mustRoute("/api/tours", tourServiceURL),
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

		if rpcClient.tryHandleTourRPC(w, r) {
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

	log.Printf("API gateway listening on :%s", port)
	if err := http.ListenAndServe(":"+port, mux); err != nil {
		log.Fatal(err)
	}
}

func (g rpcGateway) tryHandleTourRPC(w http.ResponseWriter, r *http.Request) bool {
	if r.Method != http.MethodPost {
		return false
	}

	pathParts := strings.Split(strings.Trim(r.URL.Path, "/"), "/")
	if len(pathParts) != 4 || pathParts[0] != "api" || pathParts[1] != "tours" {
		return false
	}

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
	default:
		return false
	}

	applyCORS(w, r)

	if err := g.forwardRPC(w, r, method, tourID); err != nil {
		log.Printf("tour RPC %s failed: %v", method, err)
		w.Header().Set("Content-Type", "application/json")
		w.WriteHeader(http.StatusBadGateway)
		_ = json.NewEncoder(w).Encode(map[string]string{
			"message": "tour service unavailable",
		})
	}

	return true
}

func (g rpcGateway) forwardRPC(w http.ResponseWriter, r *http.Request, method string, tourID int) error {
	requestBody := jsonRPCRequest{
		JsonRPC: "2.0",
		Method:  method,
		Params: map[string]int{
			"tourId": tourID,
		},
		ID: fmt.Sprintf("%s-%d", strings.ToLower(strings.TrimPrefix(method, "TourService.")), time.Now().UnixNano()),
	}

	payload, err := json.Marshal(requestBody)
	if err != nil {
		return err
	}

	upstreamRequest, err := http.NewRequestWithContext(
		r.Context(),
		http.MethodPost,
		g.tourServiceURL+"/internal/rpc",
		bytes.NewReader(payload),
	)
	if err != nil {
		return err
	}

	upstreamRequest.Header.Set("Content-Type", "application/json")
	if authHeader := r.Header.Get("Authorization"); authHeader != "" {
		upstreamRequest.Header.Set("Authorization", authHeader)
	}

	response, err := g.httpClient.Do(upstreamRequest)
	if err != nil {
		return err
	}
	defer response.Body.Close()

	body, err := io.ReadAll(response.Body)
	if err != nil {
		return err
	}

	var rpcResponse jsonRPCResponse
	if err := json.Unmarshal(body, &rpcResponse); err != nil {
		return err
	}

	w.Header().Set("Content-Type", "application/json")

	if rpcResponse.Error != nil {
		w.WriteHeader(normalizeStatusCode(rpcResponse.Error.Code))
		return json.NewEncoder(w).Encode(map[string]any{
			"message": rpcResponse.Error.Message,
			"errors":  rpcResponse.Error.Details,
		})
	}

	if len(rpcResponse.Result) == 0 {
		w.WriteHeader(http.StatusBadGateway)
		return json.NewEncoder(w).Encode(map[string]string{
			"message": "invalid RPC response",
		})
	}

	w.WriteHeader(http.StatusOK)
	_, err = w.Write(rpcResponse.Result)
	return err
}

func normalizeStatusCode(code int) int {
	switch {
	case code >= 400 && code < 600:
		return code
	default:
		return http.StatusBadGateway
	}
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
