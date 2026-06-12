package main

import (
	"context"
	"log"
	"os"

	"go.opentelemetry.io/otel"
	"go.opentelemetry.io/otel/exporters/otlp/otlptrace/otlptracehttp"
	"go.opentelemetry.io/otel/exporters/stdout/stdouttrace"
	"go.opentelemetry.io/otel/propagation"
	"go.opentelemetry.io/otel/sdk/resource"
	"go.opentelemetry.io/otel/sdk/trace"
	semconv "go.opentelemetry.io/otel/semconv/v1.26.0"
)

// initTracer podesava globalni TracerProvider.
// Ako je definisana JAEGER_ENDPOINT env varijabla, trace-ovi se salju Jaeger-u
// preko OTLP protokola; u suprotnom se upisuju u traces.json fajl.
func initTracer(serviceName string) (*trace.TracerProvider, error) {
	exporter, err := newExporter()
	if err != nil {
		return nil, err
	}

	tp := trace.NewTracerProvider(
		trace.WithBatcher(exporter),
		trace.WithSampler(trace.AlwaysSample()),
		trace.WithResource(resource.NewWithAttributes(
			semconv.SchemaURL,
			semconv.ServiceNameKey.String(serviceName),
		)),
	)

	otel.SetTracerProvider(tp)
	// Propagator upisuje trace context u HTTP hedere (traceparent) pri
	// prosledjivanju zahteva, pa downstream servisi nastavljaju isti trace
	otel.SetTextMapPropagator(propagation.NewCompositeTextMapPropagator(
		propagation.TraceContext{},
		propagation.Baggage{},
	))
	return tp, nil
}

func newExporter() (trace.SpanExporter, error) {
	url := os.Getenv("JAEGER_ENDPOINT")
	if url != "" {
		log.Printf("Initializing tracing to Jaeger (OTLP) at %s", url)
		return otlptracehttp.New(context.Background(),
			otlptracehttp.WithEndpointURL(url),
		)
	}

	log.Println("JAEGER_ENDPOINT not set — initializing tracing to traces.json")
	f, err := os.Create("traces.json")
	if err != nil {
		return nil, err
	}
	return stdouttrace.New(
		stdouttrace.WithWriter(f),
		stdouttrace.WithPrettyPrint(),
	)
}
