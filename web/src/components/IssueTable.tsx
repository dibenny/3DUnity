import { PriorityBadge } from './PriorityBadge'
import { StatusSelect } from './StatusSelect'
import { STATUS_LABEL } from '../types'
import type { Issue, IssueStatus } from '../types'

interface IssueTableProps {
  issues: Issue[]
  updatingId: string | null
  onStatusChange: (id: string, status: IssueStatus) => void
}

function formatTime(iso: string): string {
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return iso
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`
}

function formatPosition(p: Issue['position']): string {
  return `x ${p.x.toFixed(2)} · y ${p.y.toFixed(2)} · z ${p.z.toFixed(2)}`
}

export function IssueTable({ issues, updatingId, onStatusChange }: IssueTableProps) {
  return (
    <div className="table-wrap">
      <table className="issue-table">
        <thead>
          <tr>
            <th className="col-title">标题</th>
            <th className="col-priority">优先级</th>
            <th className="col-status">状态</th>
            <th className="col-pos">场景坐标</th>
            <th className="col-time">创建时间</th>
            <th className="col-action">操作</th>
          </tr>
        </thead>
        <tbody>
          {issues.map((issue) => (
            <tr key={issue.id}>
              <td className="col-title">
                <div className="issue-title">{issue.title}</div>
                {issue.description && <div className="issue-desc">{issue.description}</div>}
              </td>
              <td className="col-priority"><PriorityBadge priority={issue.priority} /></td>
              <td className="col-status">
                <span className={`status-text status-text-${issue.status}`}>{STATUS_LABEL[issue.status]}</span>
              </td>
              <td className="col-pos"><code>{formatPosition(issue.position)}</code></td>
              <td className="col-time">{formatTime(issue.createdAt)}</td>
              <td className="col-action">
                <StatusSelect
                  value={issue.status}
                  disabled={updatingId === issue.id}
                  onChange={(next) => onStatusChange(issue.id, next)}
                />
                {updatingId === issue.id && <span className="saving">保存中…</span>}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
