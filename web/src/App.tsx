import { useCallback, useEffect, useMemo, useState } from 'react'
import { api, ApiError } from './api/client'
import { ErrorBanner } from './components/ErrorBanner'
import { IssueTable } from './components/IssueTable'
import type { Issue, IssueStatus } from './types'

export default function App() {
  const [issues, setIssues] = useState<Issue[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [updatingId, setUpdatingId] = useState<string | null>(null)

  const load = useCallback(async () => {
    setLoading(true)
    setError(null)
    try {
      const list = await api.listIssues()
      setIssues(list)
    } catch (e) {
      // 后端不可用时显示错误，绝不白屏
      setIssues([])
      setError(e instanceof ApiError ? e.message : '加载失败，请稍后重试。')
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    void load()
  }, [load])

  const handleStatusChange = useCallback(async (id: string, status: IssueStatus) => {
    const previous = issues
    // 乐观更新：先改本地列表，失败再回滚
    setIssues((list) => list.map((it) => (it.id === id ? { ...it, status } : it)))
    setUpdatingId(id)
    setError(null)
    try {
      const updated = await api.updateStatus(id, status)
      setIssues((list) => list.map((it) => (it.id === id ? updated : it)))
    } catch (e) {
      setIssues(previous)
      setError(e instanceof ApiError ? e.message : '修改状态失败。')
    } finally {
      setUpdatingId(null)
    }
  }, [issues])

  const stats = useMemo(() => {
    const total = issues.length
    const open = issues.filter((it) => it.status === 'open').length
    const inProgress = issues.filter((it) => it.status === 'in_progress').length
    const resolved = issues.filter((it) => it.status === 'resolved').length
    return { total, open, inProgress, resolved }
  }, [issues])

  return (
    <div className="app">
      <header className="app-header">
        <div>
          <h1>巡检问题管理</h1>
          <p className="subtitle">Unity 场景点击上报 → Go 存储 → 本页查看</p>
        </div>
        <button className="btn btn-primary" onClick={() => void load()} disabled={loading}>
          {loading ? '加载中…' : '刷新'}
        </button>
      </header>

      <section className="stats">
        <div className="stat-card"><span className="stat-num">{stats.total}</span><span className="stat-label">全部</span></div>
        <div className="stat-card"><span className="stat-num">{stats.open}</span><span className="stat-label">待处理</span></div>
        <div className="stat-card"><span className="stat-num">{stats.inProgress}</span><span className="stat-label">处理中</span></div>
        <div className="stat-card"><span className="stat-num">{stats.resolved}</span><span className="stat-label">已解决</span></div>
      </section>

      {error && <ErrorBanner message={error} onRetry={() => void load()} />}

      {loading ? (
        <div className="state-box">正在加载问题列表…</div>
      ) : issues.length === 0 ? (
        <div className="state-box">
          <p>还没有上报的问题。</p>
          <p className="state-hint">在 Unity 场景里左键点击设备即可上报，或点右上角「刷新」。</p>
        </div>
      ) : (
        <IssueTable issues={issues} updatingId={updatingId} onStatusChange={handleStatusChange} />
      )}
    </div>
  )
}
