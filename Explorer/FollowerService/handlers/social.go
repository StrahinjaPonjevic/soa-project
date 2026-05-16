package handlers

import (
	"context"
	"follower-service/db"
	"net/http"
	"strconv"

	"github.com/gin-gonic/gin"
	"github.com/neo4j/neo4j-go-driver/v5/neo4j"
)

type RecommendationResult struct {
	ID          int `json:"id"`
	MutualCount int `json:"mutualCount"`
}

// GET /api/followers/following/:userId
func GetFollowing(c *gin.Context) {
	userID, err := strconv.Atoi(c.Param("userId"))
	if err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "invalid userId"})
		return
	}
	ids, err := db.QueryIDs(
		`MATCH (a:User {id: $userId})-[:FOLLOWS]->(b:User) RETURN b.id AS id`,
		map[string]any{"userId": userID},
	)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}
	c.JSON(http.StatusOK, ids)
}

// GET /api/followers/followers/:userId
func GetFollowers(c *gin.Context) {
	userID, err := strconv.Atoi(c.Param("userId"))
	if err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "invalid userId"})
		return
	}
	ids, err := db.QueryIDs(
		`MATCH (a:User)-[:FOLLOWS]->(b:User {id: $userId}) RETURN a.id AS id`,
		map[string]any{"userId": userID},
	)
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}
	c.JSON(http.StatusOK, ids)
}

// GET /api/followers/is-following?followerId=1&followingId=2
func IsFollowing(c *gin.Context) {
	followerID, err1 := strconv.Atoi(c.Query("followerId"))
	followingID, err2 := strconv.Atoi(c.Query("followingId"))
	if err1 != nil || err2 != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "invalid followerId or followingId"})
		return
	}

	ctx := context.Background()
	session := db.Driver.NewSession(ctx, neo4j.SessionConfig{AccessMode: neo4j.AccessModeRead})
	defer session.Close(ctx)

	result, err := session.ExecuteRead(ctx, func(tx neo4j.ManagedTransaction) (any, error) {
		res, err := tx.Run(ctx,
			`MATCH (a:User {id: $followerId})-[:FOLLOWS]->(b:User {id: $followingId})
             RETURN count(*) > 0 AS isFollowing`,
			map[string]any{"followerId": followerID, "followingId": followingID})
		if err != nil {
			return false, err
		}
		if res.Next(ctx) {
			val, _ := res.Record().Get("isFollowing")
			return val.(bool), nil
		}
		return false, nil
	})
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}
	c.JSON(http.StatusOK, gin.H{"isFollowing": result.(bool)})
}

// GET /api/followers/recommendations/:userId
func GetRecommendations(c *gin.Context) {
	userID, err := strconv.Atoi(c.Param("userId"))
	if err != nil {
		c.JSON(http.StatusBadRequest, gin.H{"error": "invalid userId"})
		return
	}

	ctx := context.Background()
	session := db.Driver.NewSession(ctx, neo4j.SessionConfig{AccessMode: neo4j.AccessModeRead})
	defer session.Close(ctx)

	result, err := session.ExecuteRead(ctx, func(tx neo4j.ManagedTransaction) (any, error) {
		res, err := tx.Run(ctx,
			`MATCH (me:User {id: $userId})-[:FOLLOWS]->(friend:User)-[:FOLLOWS]->(rec:User)
             WHERE NOT (me)-[:FOLLOWS]->(rec) AND rec.id <> $userId
             RETURN rec.id AS id, count(friend) AS mutualCount
             ORDER BY mutualCount DESC
             LIMIT 10`,
			map[string]any{"userId": userID})
		if err != nil {
			return nil, err
		}
		var recs []RecommendationResult
		for res.Next(ctx) {
			id, _ := res.Record().Get("id")
			mutual, _ := res.Record().Get("mutualCount")
			recs = append(recs, RecommendationResult{
				ID:          int(id.(int64)),
				MutualCount: int(mutual.(int64)),
			})
		}
		return recs, nil
	})
	if err != nil {
		c.JSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}

	recs, _ := result.([]RecommendationResult)
	if recs == nil {
		recs = []RecommendationResult{}
	}
	c.JSON(http.StatusOK, recs)
}
