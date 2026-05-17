package main

import (
	"log"
	"net/http"
	"net/http/httputil"
	"net/url"
	"os"
	"strings"
)

type route struct {
	prefix string
	target *url.URL
	proxy  *httputil.ReverseProxy
}

func main() {
	port := getEnv("PORT", "8080")

	routes := []route{
		mustRoute("/api/auth", getEnv("AUTH_SERVICE_URL", "http://auth_service:8080")),
		mustRoute("/api/profiles", getEnv("STAKEHOLDERS_SERVICE_URL", "http://stakeholders_service:8080")),
		mustRoute("/api/blogs", getEnv("BLOG_SERVICE_URL", "http://blog_service:8080")),
		mustRoute("/api/followers", getEnv("FOLLOWER_SERVICE_URL", "http://follower_service:8080")),
		mustRoute("/api/tours", getEnv("TOUR_SERVICE_URL", "http://tour_service:8080")),
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
