package handler

import (
	"encoding/json"
	"errors"
	"log"
	"net/http"
	"strings"
	"time"

	"inspection-demo/server/internal/model"
	"inspection-demo/server/internal/store"
)

func nowRFC3339() string {
	return time.Now().UTC().Format(time.RFC3339)
}

// listIssues 查询全部问题。
func (h *Handler) listIssues(w http.ResponseWriter, r *http.Request) {
	issues, err := h.store.List()
	if err != nil {
		log.Printf("查询列表失败: %v", err)
		writeError(w, http.StatusInternalServerError, model.CodeInternal, "查询问题列表失败")
		return
	}
	writeJSON(w, http.StatusOK, issues)
}

// createIssue 创建一条新问题。
//
// 校验规则：
//   - title 不能为空（去空格后）；
//   - priority 可以不填，默认 medium；填了必须是 low/medium/high 之一。
func (h *Handler) createIssue(w http.ResponseWriter, r *http.Request) {
	var req model.CreateIssueRequest
	decoder := json.NewDecoder(http.MaxBytesReader(w, r.Body, 1<<20))
	if err := decoder.Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, model.CodeBadRequest, "请求体不是合法 JSON")
		return
	}

	req.Title = strings.TrimSpace(req.Title)
	if req.Title == "" {
		writeError(w, http.StatusBadRequest, model.CodeValidation, "title is required")
		return
	}

	req.Priority = strings.TrimSpace(req.Priority)
	if req.Priority == "" {
		req.Priority = model.PriorityMedium
	}
	if !model.IsValidPriority(req.Priority) {
		writeError(w, http.StatusBadRequest, model.CodeValidation, "priority must be one of low|medium|high")
		return
	}

	now := nowRFC3339()
	issue := model.Issue{
		ID:          store.NewUUID(),
		Title:       req.Title,
		Description: req.Description,
		Priority:    req.Priority,
		Status:      model.StatusOpen,
		Position:    req.Position,
		CreatedAt:   now,
		UpdatedAt:   now,
	}

	if err := h.store.Create(issue); err != nil {
		log.Printf("创建问题失败: %v", err)
		writeError(w, http.StatusInternalServerError, model.CodeInternal, "创建问题失败")
		return
	}

	writeJSON(w, http.StatusCreated, issue)
}

// updateStatus 修改问题状态：PATCH /api/issues/{id}/status
func (h *Handler) updateStatus(w http.ResponseWriter, r *http.Request, id string) {
	var req model.UpdateStatusRequest
	if err := json.NewDecoder(http.MaxBytesReader(w, r.Body, 1<<20)).Decode(&req); err != nil {
		writeError(w, http.StatusBadRequest, model.CodeBadRequest, "请求体不是合法 JSON")
		return
	}

	req.Status = strings.TrimSpace(req.Status)
	if req.Status == "" {
		writeError(w, http.StatusBadRequest, model.CodeValidation, "status is required")
		return
	}
	if !model.IsValidStatus(req.Status) {
		writeError(w, http.StatusBadRequest, model.CodeValidation, "status must be one of open|in_progress|resolved")
		return
	}

	issue, err := h.store.UpdateStatus(id, req.Status, nowRFC3339())
	if err != nil {
		if errors.Is(err, store.ErrNotFound) {
			writeError(w, http.StatusNotFound, model.CodeNotFound, "issue not found")
			return
		}
		log.Printf("更新状态失败: %v", err)
		writeError(w, http.StatusInternalServerError, model.CodeInternal, "更新状态失败")
		return
	}

	writeJSON(w, http.StatusOK, issue)
}
