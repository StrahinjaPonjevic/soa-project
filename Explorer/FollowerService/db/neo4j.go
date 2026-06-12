package db

import (
	"context"
	"log"

	"github.com/neo4j/neo4j-go-driver/v5/neo4j"
	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/attribute"
	"go.opentelemetry.io/otel/codes"
)

var Driver neo4j.DriverWithContext

func Init(uri, user, password string) {
	var err error
	Driver, err = neo4j.NewDriverWithContext(uri, neo4j.BasicAuth(user, password, ""))
	if err != nil {
		log.Fatalf("Failed to create Neo4j driver: %v", err)
	}
	if err = Driver.VerifyConnectivity(context.Background()); err != nil {
		log.Fatalf("Cannot connect to Neo4j: %v", err)
	}
	log.Println("Connected to Neo4j")
}

func QueryIDs(ctx context.Context, cypher string, params map[string]any) ([]int, error) {
	// Subspan za upit ka Neo4j bazi — vidi se kao zaseban segment u trace-u
	ctx, span := otel.Tracer("follower-service").Start(ctx, "neo4j.query")
	defer span.End()
	span.SetAttributes(
		attribute.String("db.system", "neo4j"),
		attribute.String("db.statement", cypher),
	)

	session := Driver.NewSession(ctx, neo4j.SessionConfig{AccessMode: neo4j.AccessModeRead})
	defer session.Close(ctx)

	result, err := session.ExecuteRead(ctx, func(tx neo4j.ManagedTransaction) (any, error) {
		res, err := tx.Run(ctx, cypher, params)
		if err != nil {
			return nil, err
		}
		var ids []int
		for res.Next(ctx) {
			id, _ := res.Record().Get("id")
			ids = append(ids, int(id.(int64)))
		}
		return ids, nil
	})
	if err != nil {
		span.RecordError(err)
		span.SetStatus(codes.Error, err.Error())
		return nil, err
	}
	ids, _ := result.([]int)
	if ids == nil {
		ids = []int{}
	}
	return ids, nil
}
