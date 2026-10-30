import { useCallback, useEffect, useMemo, useState } from 'react'
import { api, ApiError } from './api/client'
import { ErrorBanner } from './components/ErrorBanner'
import { IssueDetailModal } from './components/IssueDetailModal'
import { IssueTable } from './components/IssueTable'
import type { Issue, IssueStatus, Priority } from './types'

type StatusFilter = 'all' | IssueStatus
type PriorityFilter = 'all' | Priority

const T = {
  title: '\u5de1\u68c0\u95ee\u9898\u7ba1\u7406',
  subtitle: 'Unity \u573a\u666f\u4e0a\u62a5 \u2192 Go \u5b58\u50a8 \u2192 \u672c\u9875\u67e5\u770b',
  refresh: '\u5237\u65b0',
  loading: '\u52a0\u8f7d\u4e2d...',
  all: '\u5168\u90e8',
  open: '\u5f85\u5904\u7406',
  inProgress: '\u5904\u7406\u4e2d',
  resolved: '\u5df2\u89e3\u51b3',
  search: '\u641c\u7d22\u6807\u9898\u6216\u63cf\u8ff0',
  statusFilter: '\u7b5b\u9009\u72b6\u6001',
  priorityFilter: '\u7b5b\u9009\u4f18\u5148\u7ea7',
  clear: '\u6e05\u9664\u7b5b\u9009',
  noFiltered: '\u6ca1\u6709\u7b26\u5408\u7b5b\u9009\u6761\u4ef6\u7684\u95ee\u9898',
  empty: '\u8fd8\u6ca1\u6709\u4e0a\u62a5\u7684\u95ee\u9898\u3002',
  emptyHint: '\u5728 Unity \u573a\u666f\u91cc\u5de6\u952e\u70b9\u51fb\u8bbe\u5907\u5373\u53ef\u4e0a\u62a5\uff0c\u6216\u70b9\u53f3\u4e0a\u89d2\u300c\u5237\u65b0\u300d\u3002',
  loadingList: '\u6b63\u5728\u52a0\u8f7d\u95ee\u9898\u5217\u8868...',
  loadFailed: '\u52a0\u8f7d\u5931\u8d25\uff0c\u8bf7\u7a0d\u540e\u91cd\u8bd5\u3002',
  updateFailed: '\u4fee\u6539\u72b6\u6001\u5931\u8d25\u3002',
  deleteFailed: '\u5220\u9664\u5931\u8d25\u3002',
  confirmDelete: '\u786e\u5b9a\u8981\u5220\u9664\u8fd9\u6761\u95ee\u9898\u5417\uff1f',
}

export default function App() {
  const [issues, setIssues] = useState<Issue[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [updatingId, setUpdatingId] = useState<string | null>(null)
  const [deletingId, setDeletingId] = useState<string | null>(null)
  const [selectedIssue, setSelectedIssue] = useState<Issue | null>(null)
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all')
  const [priorityFilter, setPriorityFilter] = useState<PriorityFilter>('all')
  const [query, setQuery] = useState('')

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const list = await api.listIssues()
      setIssues(list)
    } catch (e) {
      setIssues([])
      setError(e instanceof ApiError ? e.message : T.loadFailed)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  const handleStatusChange = useCallback(async (id: string, status: IssueStatus) => {
    const previous = issues
    setIssues((list) => list.map((it) => (it.id === id ? { ...it, status } : it)))
    setUpdatingId(id)
    setError(null)
    try {
      const updated = await api.updateStatus(id, status)
      setIssues((list) => list.map((it) => (it.id === id ? updated : it)))
      setSelectedIssue((current) => (current?.id === id ? updated : current))
    } catch (e) {
      setIssues(previous)
      setError(e instanceof ApiError ? e.message : T.updateFailed)
    } finally {
      setUpdatingId(null)
    }
  }, [issues])

  const handleDelete = useCallback(async (issue: Issue) => {
    if (window.confirm(T.confirmDelete) === false) return
    setDeletingId(issue.id)
    setError(null)
    try {
      await api.deleteIssue(issue.id)
      setIssues((list) => list.filter((it) => it.id === issue.id ? false : true))
      setSelectedIssue((current) => (current?.id === issue.id ? null : current))
    } catch (e) {
      setError(e instanceof ApiError ? e.message : T.deleteFailed)
    } finally {
      setDeletingId(null)
    }
  }, [])

  const stats = useMemo(() => {
    const total = issues.length
    const open = issues.filter((it) => it.status === 'open').length
    const inProgress = issues.filter((it) => it.status === 'in_progress').length
    const resolved = issues.filter((it) => it.status === 'resolved').length
    return { total, open, inProgress, resolved }
  }, [issues])

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase()
    return issues.filter((issue) => {
      const statusOk = statusFilter === 'all' || issue.status === statusFilter
      const priorityOk = priorityFilter === 'all' || issue.priority === priorityFilter
      const textOk = q.length === 0 || issue.title.toLowerCase().includes(q) || issue.description.toLowerCase().includes(q)
      return statusOk && priorityOk && textOk
    })
  }, [issues, statusFilter, priorityFilter, query])

  const clearFilters = () => {
    setStatusFilter('all')
    setPriorityFilter('all')
    setQuery('')
  }

  return (
    <div className="app">
      <header className="app-header">
        <div>
          <h1>{T.title}</h1>
          <p className="subtitle">{T.subtitle}</p>
        </div>
        <button className="btn btn-primary" onClick={() => void load()} disabled={loading}>
          {loading ? T.loading : T.refresh}
        </button>
      </header>

      <section className="stats">
        <div className="stat-card"><span className="stat-num">{stats.total}</span><span className="stat-label">{T.all}</span></div>
        <div className="stat-card"><span className="stat-num">{stats.open}</span><span className="stat-label">{T.open}</span></div>
        <div className="stat-card"><span className="stat-num">{stats.inProgress}</span><span className="stat-label">{T.inProgress}</span></div>
        <div className="stat-card"><span className="stat-num">{stats.resolved}</span><span className="stat-label">{T.resolved}</span></div>
      </section>

      {error ? <ErrorBanner message={error} onRetry={() => void load()} /> : null}

      <section className="filters">
        <input
          className="search-input"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder={T.search}
          aria-label={T.search}
        />
        <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value as StatusFilter)} aria-label={T.statusFilter}>
          <option value="all">{T.statusFilter}: {T.all}</option>
          <option value="open">{T.open}</option>
          <option value="in_progress">{T.inProgress}</option>
          <option value="resolved">{T.resolved}</option>
        </select>
        <select value={priorityFilter} onChange={(e) => setPriorityFilter(e.target.value as PriorityFilter)} aria-label={T.priorityFilter}>
          <option value="all">{T.priorityFilter}: {T.all}</option>
          <option value="low">{'\u4f4e'}</option>
          <option value="medium">{'\u4e2d'}</option>
          <option value="high">{'\u9ad8'}</option>
        </select>
        <button className="btn" onClick={clearFilters}>{T.clear}</button>
        <span className="filter-count">{filtered.length} / {issues.length}</span>
      </section>

      {loading ? (
        <div className="state-box">{T.loadingList}</div>
      ) : issues.length === 0 ? (
        <div className="state-box">
          <p>{T.empty}</p>
          <p className="state-hint">{T.emptyHint}</p>
        </div>
      ) : filtered.length === 0 ? (
        <div className="state-box">{T.noFiltered}</div>
      ) : (
        <IssueTable
          issues={filtered}
          updatingId={updatingId}
          deletingId={deletingId}
          onStatusChange={handleStatusChange}
          onView={setSelectedIssue}
          onDelete={(issue) => void handleDelete(issue)}
        />
      )}

      {selectedIssue ? (
        <IssueDetailModal
          issue={selectedIssue}
          deleting={deletingId === selectedIssue.id}
          onClose={() => setSelectedIssue(null)}
          onDelete={(issue) => void handleDelete(issue)}
        />
      ) : null}
    </div>
  )
}
