package handlers

import (
	"follower-service/db"
	"net/http"

	"github.com/gin-gonic/gin"
	"github.com/neo4j/neo4j-go-driver/v5/neo4j"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/codes"
)

type FollowRequest struct {
	FollowingID int `json:"followingId" binding:"required"`
}

// POST /api/followers/follow
func Follow(c *gin.Context) {
	followerID := c.GetInt("userId")

	var req FollowRequest
	if err := c.ShouldBindJSON(&req); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}
	if followerID == req.FollowingID {
		c.JSON(http.StatusBadRequest, gin.H{"error": "cannot follow yourself"})
		return
	}

	ctx, span := otel.Tracer("follower-service").Start(c.Request.Context(), "neo4j.create-follow")
	defer span.End()

	session := db.Driver.NewSession(ctx, neo4j.SessionConfig{AccessMode: neo4j.AccessModeWrite})
	defer session.Close(ctx)

	_, err := session.ExecuteWrite(ctx, func(tx neo4j.ManagedTransaction) (any, error) {
		_, err := tx.Run(ctx,
			`MERGE (a:User {id: $followerId})
             MERGE (b:User {id: $followingId})
             MERGE (a)-[:FOLLOWS]->(b)`,
			map[string]any{"followerId": followerID, "followingId": req.FollowingID})
		return nil, err
	})
	if err != nil {
		span.RecordError(err)
		span.SetStatus(codes.Error, err.Error())
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}
	c.JSON(http.StatusOK, gin.H{"message": "followed successfully"})
}

// DELETE /api/followers/follow
func Unfollow(c *gin.Context) {
	followerID := c.GetInt("userId")

	var req FollowRequest
	if err := c.ShouldBindJSON(&req); err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	ctx, span := otel.Tracer("follower-service").Start(c.Request.Context(), "neo4j.delete-follow")
	defer span.End()

	session := db.Driver.NewSession(ctx, neo4j.SessionConfig{AccessMode: neo4j.AccessModeWrite})
	defer session.Close(ctx)

	_, err := session.ExecuteWrite(ctx, func(tx neo4j.ManagedTransaction) (any, error) {
		_, err := tx.Run(ctx,
			`MATCH (a:User {id: $followerId})-[r:FOLLOWS]->(b:User {id: $followingId}) DELETE r`,
			map[string]any{"followerId": followerID, "followingId": req.FollowingID})
		return nil, err
	})
	if err != nil {
		span.RecordError(err)
		span.SetStatus(codes.Error, err.Error())
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}
	c.JSON(http.StatusOK, gin.H{"message": "unfollowed successfully"})
}
