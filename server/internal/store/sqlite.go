package store

import (
	"database/sql"
	"errors"
	"fmt"
	"os"
	"path/filepath"

	"inspection-demo/server/internal/model"

	_ "modernc.org/sqlite"
)

// ErrNotFound 表示目标问题不存在。
var ErrNotFound = errors.New("issue not found")

// Store 是基于 SQLite 的问题存储。
type Store struct {
	db *sql.DB
}

// Open 打开（或创建）SQLite 数据库，并确保表结构存在。
func Open(path string) (*Store, error) {
	if dir := filepath.Dir(path); dir != "" && dir != "." {
		if err := os.MkdirAll(dir, 0o755); err != nil {
			return nil, fmt.Errorf("创建数据目录 %s 失败: %w", dir, err)
		}
	}

	// busy_timeout 避免并发写入直接报 database is locked；
	// WAL 让读不阻塞写，联调时 React 轮询不会卡住 Unity 的写入。
	dsn := fmt.Sprintf("file:%s?_pragma=busy_timeout(5000)&_pragma=journal_mode(WAL)&_pragma=foreign_keys(1)", path)
	db, err := sql.Open("sqlite", dsn)
	if err != nil {
		return nil, fmt.Errorf("打开数据库失败: %w", err)
	}

	// SQLite 是单写入者模型，限制连接数能显著减少 lock 冲突。
	db.SetMaxOpenConns(1)

	if err := db.Ping(); err != nil {
		db.Close()
		return nil, fmt.Errorf("连接数据库失败: %w", err)
	}

	s := &Store{db: db}
	if err := s.migrate(); err != nil {
		db.Close()
		return nil, err
	}
	return s, nil
}

// Close 关闭底层连接。
func (s *Store) Close() error { return s.db.Close() }

func (s *Store) migrate() error {
	const ddl = `
CREATE TABLE IF NOT EXISTS issues (
    id          TEXT PRIMARY KEY,
    title       TEXT NOT NULL,
    description TEXT NOT NULL DEFAULT '',
    priority    TEXT NOT NULL,
    status      TEXT NOT NULL DEFAULT 'open',
    pos_x       REAL NOT NULL,
    pos_y       REAL NOT NULL,
    pos_z       REAL NOT NULL,
    created_at  TEXT NOT NULL,
    updated_at  TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_issues_created_at ON issues(created_at DESC);
`
	if _, err := s.db.Exec(ddl); err != nil {
		return fmt.Errorf("初始化表结构失败: %w", err)
	}
	return nil
}

// Create 写入一条新问题。
func (s *Store) Create(issue model.Issue) error {
	const q = `
INSERT INTO issues (id, title, description, priority, status, pos_x, pos_y, pos_z, created_at, updated_at)
VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`
	_, err := s.db.Exec(q,
		issue.ID, issue.Title, issue.Description, issue.Priority, issue.Status,
		issue.Position.X, issue.Position.Y, issue.Position.Z,
		issue.CreatedAt, issue.UpdatedAt,
	)
	if err != nil {
		return fmt.Errorf("写入问题失败: %w", err)
	}
	return nil
}

// List 按创建时间倒序返回全部问题。
func (s *Store) List() ([]model.Issue, error) {
	const q = `
SELECT id, title, description, priority, status, pos_x, pos_y, pos_z, created_at, updated_at
FROM issues ORDER BY created_at DESC, rowid DESC`

	rows, err := s.db.Query(q)
	if err != nil {
		return nil, fmt.Errorf("查询问题列表失败: %w", err)
	}
	defer rows.Close()

	issues := make([]model.Issue, 0)
	for rows.Next() {
		var it model.Issue
		if err := rows.Scan(
			&it.ID, &it.Title, &it.Description, &it.Priority, &it.Status,
			&it.Position.X, &it.Position.Y, &it.Position.Z,
			&it.CreatedAt, &it.UpdatedAt,
		); err != nil {
			return nil, fmt.Errorf("读取问题记录失败: %w", err)
		}
		issues = append(issues, it)
	}
	if err := rows.Err(); err != nil {
		return nil, fmt.Errorf("遍历问题列表失败: %w", err)
	}
	return issues, nil
}

// UpdateStatus 修改问题状态，返回更新后的完整记录。
func (s *Store) UpdateStatus(id, status, updatedAt string) (model.Issue, error) {
	const q = `UPDATE issues SET status = ?, updated_at = ? WHERE id = ?`
	res, err := s.db.Exec(q, status, updatedAt, id)
	if err != nil {
		return model.Issue{}, fmt.Errorf("更新状态失败: %w", err)
	}
	affected, err := res.RowsAffected()
	if err != nil {
		return model.Issue{}, fmt.Errorf("读取更新结果失败: %w", err)
	}
	if affected == 0 {
		return model.Issue{}, ErrNotFound
	}
	return s.get(id)
}

func (s *Store) get(id string) (model.Issue, error) {
	const q = `
SELECT id, title, description, priority, status, pos_x, pos_y, pos_z, created_at, updated_at
FROM issues WHERE id = ?`

	var it model.Issue
	err := s.db.QueryRow(q, id).Scan(
		&it.ID, &it.Title, &it.Description, &it.Priority, &it.Status,
		&it.Position.X, &it.Position.Y, &it.Position.Z,
		&it.CreatedAt, &it.UpdatedAt,
	)
	if err == sql.ErrNoRows {
		return model.Issue{}, ErrNotFound
	}
	if err != nil {
		return model.Issue{}, fmt.Errorf("查询问题失败: %w", err)
	}
	return it, nil
}

// Delete removes an issue by id.
func (s *Store) Delete(id string) error {
	res, err := s.db.Exec("DELETE FROM issues WHERE id = ?", id)
	if err != nil {
		return fmt.Errorf("delete issue failed: %w", err)
	}
	affected, err := res.RowsAffected()
	if err != nil {
		return fmt.Errorf("read delete result failed: %w", err)
	}
	if affected == 0 {
		return ErrNotFound
	}
	return nil
}
