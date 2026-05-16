package handlers

import (
	"encoding/json"
	"follower-service/db"
	"io"
	"net/http"
	"os"

	"github.com/gin-gonic/gin"
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

// GET /api/followers/feed
func GetFeed(c *gin.Context) {
	userID := c.GetInt("userId")

	followingIDs, err := db.QueryIDs(
		`MATCH (a:User {id: $userId})-[:FOLLOWS]->(b:User) RETURN b.id AS id`,
		map[string]any{"userId": userID},
	)
	if err != nil {
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

	resp, err := http.Get(blogServiceURL + "/api/blogs")
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "cannot reach blog service"})
		return
	}
	defer resp.Body.Close()

	body, err := io.ReadAll(resp.Body)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": "failed to read blog response"})
		return
	}

	var allBlogs []BlogResponse
	if err := json.Unmarshal(body, &allBlogs); err != nil {
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
