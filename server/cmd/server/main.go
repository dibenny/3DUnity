package main

import (
	"context"
	"errors"
	"flag"
	"log"
	"net/http"
	"os"
	"os/signal"
	"path/filepath"
	"syscall"
	"time"

	"inspection-demo/server/internal/handler"
	"inspection-demo/server/internal/store"
)

func main() {
	addr := flag.String("addr", ":8080", "HTTP 监听地址")
	dbPath := flag.String("db", defaultDBPath(), "SQLite 数据库文件路径")
	flag.Parse()

	absDB, err := filepath.Abs(*dbPath)
	if err != nil {
		log.Fatalf("解析数据库路径失败: %v", err)
	}

	st, err := store.Open(absDB)
	if err != nil {
		log.Fatalf("初始化存储失败: %v", err)
	}
	defer st.Close()

	h := handler.New(st)
	srv := &http.Server{
		Addr:              *addr,
		Handler:           h.Routes(),
		ReadHeaderTimeout: 10 * time.Second,
	}

	// 收到 Ctrl+C / SIGTERM 时优雅退出，保证 SQLite 正常落盘。
	ctx, stop := signal.NotifyContext(context.Background(), os.Interrupt, syscall.SIGTERM)
	defer stop()

	go func() {
		<-ctx.Done()
		log.Println("正在关闭服务...")
		shutdownCtx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
		defer cancel()
		if err := srv.Shutdown(shutdownCtx); err != nil {
			log.Printf("关闭服务出错: %v", err)
		}
	}()

	log.Printf("巡检问题服务已启动")
	log.Printf("  监听地址: http://%s", normalizeAddr(*addr))
	log.Printf("  数据库文件: %s", absDB)
	log.Printf("  健康检查: http://%s/api/health", normalizeAddr(*addr))

	if err := srv.ListenAndServe(); err != nil && !errors.Is(err, http.ErrServerClosed) {
		log.Fatalf("服务异常退出: %v", err)
	}
	log.Println("服务已退出")
}

// defaultDBPath 默认数据库放在 server/data/inspection.db。
// 服务用 `go run ./cmd/server` 从 server/ 目录启动时正好命中这个相对路径。
func defaultDBPath() string {
	return filepath.Join("data", "inspection.db")
}

// normalizeAddr 把 ":8080" 补成 "127.0.0.1:8080"，方便直接复制到浏览器。
func normalizeAddr(addr string) string {
	if addr == "" {
		return ":8080"
	}
	if addr[0] == ':' {
		return "127.0.0.1" + addr
	}
	return addr
}
