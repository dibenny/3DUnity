package model

// Position 是问题在 Unity 场景中的世界坐标。
type Position struct {
	X float64 `json:"x"`
	Y float64 `json:"y"`
	Z float64 `json:"z"`
}

// Issue 是巡检问题的完整结构。
// 这套 JSON 字段是 Go / Unity / React 三端的数据契约，改动必须三端同步。
type Issue struct {
	ID          string   `json:"id"`
	Title       string   `json:"title"`
	Description string   `json:"description"`
	Priority    string   `json:"priority"`
	Status      string   `json:"status"`
	Position    Position `json:"position"`
	CreatedAt   string   `json:"createdAt"`
	UpdatedAt   string   `json:"updatedAt"`
}

// 优先级取值。
const (
	PriorityLow    = "low"
	PriorityMedium = "medium"
	PriorityHigh   = "high"
)

// 问题状态取值。
const (
	StatusOpen       = "open"
	StatusInProgress = "in_progress"
	StatusResolved   = "resolved"
)

// CreateIssueRequest 是 POST /api/issues 的请求体。
type CreateIssueRequest struct {
	Title       string   `json:"title"`
	Description string   `json:"description"`
	Priority    string   `json:"priority"`
	Position    Position `json:"position"`
}

// UpdateStatusRequest 是 PATCH /api/issues/{id}/status 的请求体。
type UpdateStatusRequest struct {
	Status string `json:"status"`
}

// ErrorBody 是错误响应里的具体信息。
type ErrorBody struct {
	Code    string `json:"code"`
	Message string `json:"message"`
}

// APIError 是统一的错误响应包装：{"error":{"code":"...","message":"..."}}。
type APIError struct {
	Error ErrorBody `json:"error"`
}

// 错误码常量，前端可据此做分支处理。
const (
	CodeValidation       = "VALIDATION_ERROR"
	CodeBadRequest       = "BAD_REQUEST"
	CodeNotFound         = "NOT_FOUND"
	CodeMethodNotAllowed = "METHOD_NOT_ALLOWED"
	CodeInternal         = "INTERNAL_ERROR"
)

// IsValidPriority 判断优先级是否合法。
func IsValidPriority(p string) bool {
	switch p {
	case PriorityLow, PriorityMedium, PriorityHigh:
		return true
	}
	return false
}

// IsValidStatus 判断状态是否合法。
func IsValidStatus(s string) bool {
	switch s {
	case StatusOpen, StatusInProgress, StatusResolved:
		return true
	}
	return false
}
