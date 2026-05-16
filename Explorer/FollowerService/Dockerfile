FROM golang:1.22-alpine AS builder

WORKDIR /app

COPY go.mod ./
COPY . .

RUN go mod tidy
RUN CGO_ENABLED=0 GOOS=linux go build -o follower-service .

# ─── Minimal runtime image ────────────────────────────────────────────────────
FROM alpine:latest

RUN apk add --no-cache ca-certificates

WORKDIR /root/

COPY --from=builder /app/follower-service .

EXPOSE 8080

CMD ["./follower-service"]
