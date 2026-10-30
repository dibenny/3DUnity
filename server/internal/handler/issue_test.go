package handler

import (
	"bytes"
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"path/filepath"
	"testing"

	"inspection-demo/server/internal/model"
	"inspection-demo/server/internal/store"
)

func newTestServer(t *testing.T) (*httptest.Server, func()) {
	t.Helper()
	st, err := store.Open(filepath.Join(t.TempDir(), "test.db"))
	if err != nil {
		t.Fatalf("open store: %v", err)
	}
	srv := httptest.NewServer(New(st).Routes())
	return srv, func() {
		srv.Close()
		_ = st.Close()
	}
}

func requestJSON(t *testing.T, method, url string, body []byte) *http.Response {
	t.Helper()
	req, err := http.NewRequest(method, url, bytes.NewReader(body))
	if err != nil {
		t.Fatalf("new request: %v", err)
	}
	req.Header.Set("Content-Type", "application/json")
	resp, err := http.DefaultClient.Do(req)
	if err != nil {
		t.Fatalf("request: %v", err)
	}
	return resp
}

func TestIssueLifecycle(t *testing.T) {
	srv, cleanup := newTestServer(t)
	defer cleanup()

	resp := requestJSON(t, http.MethodPost, srv.URL+"/api/issues", []byte(`{"title":"cabinet hot","description":"hot","priority":"high","position":{"x":1,"y":2,"z":3}}`))
	if resp.StatusCode != http.StatusCreated {
		t.Fatalf("create status = %d", resp.StatusCode)
	}
	var created model.Issue
	if err := json.NewDecoder(resp.Body).Decode(&created); err != nil {
		t.Fatalf("decode created: %v", err)
	}
	resp.Body.Close()
	if created.ID == "" || created.Status != model.StatusOpen {
		t.Fatalf("unexpected created issue: %+v", created)
	}

	resp = requestJSON(t, http.MethodGet, srv.URL+"/api/issues", nil)
	if resp.StatusCode != http.StatusOK {
		t.Fatalf("list status = %d", resp.StatusCode)
	}
	var issues []model.Issue
	if err := json.NewDecoder(resp.Body).Decode(&issues); err != nil {
		t.Fatalf("decode list: %v", err)
	}
	resp.Body.Close()
	if len(issues) != 1 || issues[0].ID != created.ID {
		t.Fatalf("unexpected list: %+v", issues)
	}

	resp = requestJSON(t, http.MethodPatch, srv.URL+"/api/issues/"+created.ID+"/status", []byte(`{"status":"resolved"}`))
	if resp.StatusCode != http.StatusOK {
		t.Fatalf("patch status = %d", resp.StatusCode)
	}
	resp.Body.Close()

	resp = requestJSON(t, http.MethodDelete, srv.URL+"/api/issues/"+created.ID, nil)
	if resp.StatusCode != http.StatusNoContent {
		t.Fatalf("delete status = %d", resp.StatusCode)
	}
	resp.Body.Close()

	resp = requestJSON(t, http.MethodGet, srv.URL+"/api/issues", nil)
	if resp.StatusCode != http.StatusOK {
		t.Fatalf("list after delete status = %d", resp.StatusCode)
	}
	var after []model.Issue
	if err := json.NewDecoder(resp.Body).Decode(&after); err != nil {
		t.Fatalf("decode after: %v", err)
	}
	resp.Body.Close()
	if len(after) != 0 {
		t.Fatalf("expected empty list, got %+v", after)
	}
}

func TestCreateIssueValidation(t *testing.T) {
	srv, cleanup := newTestServer(t)
	defer cleanup()

	resp := requestJSON(t, http.MethodPost, srv.URL+"/api/issues", []byte(`{"title":"   "}`))
	defer resp.Body.Close()
	if resp.StatusCode != http.StatusBadRequest {
		t.Fatalf("empty title status = %d", resp.StatusCode)
	}
	var apiErr model.APIError
	if err := json.NewDecoder(resp.Body).Decode(&apiErr); err != nil {
		t.Fatalf("decode error: %v", err)
	}
	if apiErr.Error.Code != model.CodeValidation {
		t.Fatalf("error code = %q", apiErr.Error.Code)
	}
}

func TestDeleteMissingIssue(t *testing.T) {
	srv, cleanup := newTestServer(t)
	defer cleanup()

	resp := requestJSON(t, http.MethodDelete, srv.URL+"/api/issues/missing", nil)
	defer resp.Body.Close()
	if resp.StatusCode != http.StatusNotFound {
		t.Fatalf("missing delete status = %d", resp.StatusCode)
	}
}
