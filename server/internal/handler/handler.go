package handler

import (
	"encoding/json"
	"log"
	"net/http"
	"strings"

	"inspection-demo/server/internal/middleware"
	"inspection-demo/server/internal/model"
	"inspection-demo/server/internal/store"
)

// Handler 负责把所有 HTTP 请求映射到业务逻辑。
type Handler struct {
	store *store.Store
}

// New 构造 Handler。
func New(s *store.Store) *Handler {
	return &Handler{store: s}
}

// Routes 组装路由表和中间件，返回可直接挂到 http.Server 的 handler。
//
// 路由刻意用「路径注册 + 内部判断 method」的方式，而不是 Go 1.22 的
// "METHOD /path" 模式，因为这样能把 405 / 404 也统一成 JSON 错误体。
func (h *Handler) Routes() http.Handler {
	mux := http.NewServeMux()
	mux.HandleFunc("/api/health", h.handleHealth)
	mux.HandleFunc("/api/issues", h.handleIssues)
	mux.HandleFunc("/api/issues/", h.handleIssueSubPath)
	mux.HandleFunc("/", h.handleFallback)

	return middleware.Chain(mux,
		middleware.Recovery(),
		middleware.Logger(),
		middleware.CORS(),
	)
}

// ---- 公共响应工具 ----

func writeJSON(w http.ResponseWriter, status int, body any) {
	w.Header().Set("Content-Type", "application/json; charset=utf-8")
	w.WriteHeader(status)
	if body == nil {
		return
	}
	if err := json.NewEncoder(w).Encode(body); err != nil {
		log.Printf("写响应失败: %v", err)
	}
}

func writeError(w http.ResponseWriter, status int, code, message string) {
	writeJSON(w, status, model.APIError{
		Error: model.ErrorBody{Code: code, Message: message},
	})
}

// ---- 路由处理 ----

// handleHealth 健康检查：GET /api/health
func (h *Handler) handleHealth(w http.ResponseWriter, r *http.Request) {
	if r.Method != http.MethodGet {
		writeError(w, http.StatusMethodNotAllowed, model.CodeMethodNotAllowed, "健康检查只支持 GET")
		return
	}
	writeJSON(w, http.StatusOK, map[string]any{
		"status": "ok",
		"time":   nowRFC3339(),
	})
}

// handleIssues 处理问题列表与创建：
//   - GET  /api/issues  查询列表
//   - POST /api/issues  创建问题
func (h *Handler) handleIssues(w http.ResponseWriter, r *http.Request) {
	switch r.Method {
	case http.MethodGet:
		h.listIssues(w, r)
	case http.MethodPost:
		h.createIssue(w, r)
	default:
		writeError(w, http.StatusMethodNotAllowed, model.CodeMethodNotAllowed, "该接口只支持 GET 和 POST")
	}
}

// handleIssueSubPath 处理 /api/issues/{id}/status 之类的子路径。
func (h *Handler) handleIssueSubPath(w http.ResponseWriter, r *http.Request) {
	rest := strings.TrimPrefix(r.URL.Path, "/api/issues/")
	parts := strings.Split(strings.Trim(rest, "/"), "/")

	if len(parts) != 2 || parts[0] == "" || parts[1] != "status" {
		writeError(w, http.StatusNotFound, model.CodeNotFound, "接口不存在")
		return
	}
	if r.Method != http.MethodPatch {
		writeError(w, http.StatusMethodNotAllowed, model.CodeMethodNotAllowed, "修改状态只支持 PATCH")
		return
	}
	h.updateStatus(w, r, parts[0])
}

// handleFallback 兜底 404。
func (h *Handler) handleFallback(w http.ResponseWriter, r *http.Request) {
	writeError(w, http.StatusNotFound, model.CodeNotFound, "接口不存在")
}
