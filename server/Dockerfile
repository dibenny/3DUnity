FROM golang:1.22-alpine AS build
WORKDIR /src
COPY go.mod go.sum ./
RUN go mod download
COPY . .
RUN CGO_ENABLED=0 GOOS=linux go build -o /out/inspection-server ./cmd/server

FROM alpine:3.20
RUN addgroup -S app && adduser -S app -G app
WORKDIR /app
RUN mkdir -p /app/data && chown -R app:app /app
COPY --from=build /out/inspection-server /app/inspection-server
USER app
EXPOSE 8080
VOLUME ["/app/data"]
ENTRYPOINT ["/app/inspection-server", "-addr", ":8080", "-db", "/app/data/inspection.db"]
