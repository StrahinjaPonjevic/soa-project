package handlers

import (
	"encoding/json"
	"follower-service/db"
	"io"
	"net/http"
	"os"

	"github.com/gin-gonic/gin"
	"go.opentelemetry.io/contrib/instrumentation/net/http/otelhttp"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/codes"
)

type BlogResponse struct {
	ID                  int      `json:"id"`
	Title               string   `json:"title"`
	DescriptionMarkdown string   `json:"descriptionMarkdown"`
	AuthorID            int      `json:"authorId"`
	AuthorUsername      string   `json:"authorUsername"`
	CreatedAtUtc        string   `json:"createdAtUtc"`
	ImageUrls           []string `json:"imageUrls"`
	LikesCount          int      `json:"likesCount"`
}

// HTTP klijent sa OTel transportom — poziv ka blog servisu se belezi kao
// client span i propagira trace context kroz hedere
var tracedHTTPClient = http.Client{Transport: otelhttp.NewTransport(http.DefaultTransport)}

// GET /api/followers/feed
func GetFeed(c *gin.Context) {
	userID := c.GetInt("userId")

	// Parent span za celu feed operaciju; c.Request.Context() nosi span
	// koji je kreirao otelgin middleware
	ctx, span := otel.Tracer("follower-service").Start(c.Request.Context(), "get-feed")
	defer span.End()

	span.AddEvent("Querying Neo4j for followed users")
	followingIDs, err := db.QueryIDs(ctx,
		`MATCH (a:User {id: $userId})-[:FOLLOWS]->(b:User) RETURN b.id AS id`,
		map[string]any{"userId": userID},
	)
	if err != nil {
		span.RecordError(err)
		span.SetStatus(codes.Error, err.Error())
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}
	if len(followingIDs) == 0 {
		c.JSON(http.StatusOK, []BlogResponse{})
		return
	}

	blogServiceURL := os.Getenv("BLOG_SERVICE_URL")
	if blogServiceURL == "" {
		blogServiceURL = "http://blog_service:8080"
	}

	span.AddEvent("Fetching blogs from blog service")
	req, err := http.NewRequestWithContext(ctx, http.MethodGet, blogServiceURL+"/api/blogs", nil)
	if err != nil {
		span.RecordError(err)
		span.SetStatus(codes.Error, err.Error())
		c.JSON(http.StatusInternalServerError, gin.H{"error": "cannot build blog request"})
		return
	}
	resp, err := tracedHTTPClient.Do(req)
	if err != nil {
		span.RecordError(err)
		span.SetStatus(codes.Error, err.Error())
		c.JSON(http.StatusInternalServerError, gin.H{"error": "cannot reach blog service"})
		return
	}
	defer resp.Body.Close()

	body, err := io.ReadAll(resp.Body)
	if err != nil {
		span.RecordError(err)
		span.SetStatus(codes.Error, err.Error())
		c.JSON(http.StatusInternalServerError, gin.H{"error": "failed to read blog response"})
		return
	}

	var allBlogs []BlogResponse
	if err := json.Unmarshal(body, &allBlogs); err != nil {
		span.RecordError(err)
		span.SetStatus(codes.Error, err.Error())
		c.JSON(http.StatusInternalServerError, gin.H{"error": "failed to parse blogs"})
		return
	}

	followingSet := make(map[int]bool, len(followingIDs))
	for _, id := range followingIDs {
		followingSet[id] = true
	}

	feed := make([]BlogResponse, 0)
	for _, blog := range allBlogs {
		if followingSet[blog.AuthorID] {
			feed = append(feed, blog)
		}
	}
	c.JSON(http.StatusOK, feed)
}
