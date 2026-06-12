package main

import (
	"context"
	"follower-service/db"
	"follower-service/handlers"
	"follower-service/middleware"
	"log"
	"os"

	"github.com/gin-gonic/gin"
	"go.opentelemetry.io/contrib/instrumentation/github.com/gin-gonic/gin/otelgin"
)

func main() {
	neo4jURI := getEnv("NEO4J_URI", "bolt://localhost:7687")
	neo4jUser := getEnv("NEO4J_USER", "neo4j")
	neo4jPassword := getEnv("NEO4J_PASSWORD", "neo4jpassword")
	port := getEnv("PORT", "8080")

	tp, err := initTracer("follower-service")
	if err != nil {
		log.Fatalf("Failed to initialize tracer: %v", err)
	}
	defer func() { _ = tp.Shutdown(context.Background()) }()

	db.Init(neo4jURI, neo4jUser, neo4jPassword)

	r := gin.Default()
	// Kreira server span za svaki HTTP zahtev i cita traceparent heder
	// koji prosledjuje gateway, pa se spanovi nastavljaju na isti trace
	r.Use(otelgin.Middleware("follower-service"))

	api := r.Group("/api/followers")
	{
		// Zahtevaju JWT
		auth := api.Group("", middleware.JWTAuth())
		auth.POST("/follow", handlers.Follow)
		auth.DELETE("/follow", handlers.Unfollow)
		auth.GET("/feed", handlers.GetFeed)

		// Javni endpointi
		api.GET("/following/:userId", handlers.GetFollowing)
		api.GET("/followers/:userId", handlers.GetFollowers)
		api.GET("/is-following", handlers.IsFollowing)
		api.GET("/recommendations/:userId", handlers.GetRecommendations)
	}

	log.Printf("Follower service running on :%s", port)
	if err := r.Run(":" + port); err != nil {
		log.Fatal(err)
	}
}

func getEnv(key, fallback string) string {
	if v := os.Getenv(key); v != "" {
		return v
	}
	return fallback
}
