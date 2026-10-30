import { PriorityBadge } from './PriorityBadge'
import { StatusSelect } from './StatusSelect'
import { STATUS_LABEL } from '../types'
import type { Issue, IssueStatus } from '../types'

interface IssueTableProps {
  issues: Issue[]
  updatingId: string | null
  deletingId: string | null
  onStatusChange: (id: string, status: IssueStatus) => void
  onView: (issue: Issue) => void
  onDelete: (issue: Issue) => void
}

function formatTime(iso: string): string {
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return iso
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`
}

function formatPosition(p: Issue['position']): string {
  return `x ${p.x.toFixed(2)} / y ${p.y.toFixed(2)} / z ${p.z.toFixed(2)}`
}

const T = {
  title: '\u6807\u9898',
  priority: '\u4f18\u5148\u7ea7',
  status: '\u72b6\u6001',
  position: '\u573a\u666f\u5750\u6807',
  createdAt: '\u521b\u5efa\u65f6\u95f4',
  actions: '\u64cd\u4f5c',
  view: '\u67e5\u770b',
  delete: '\u5220\u9664',
  saving: '\u4fdd\u5b58\u4e2d',
}

export function IssueTable({ issues, updatingId, deletingId, onStatusChange, onView, onDelete }: IssueTableProps) {
  return (
    <div className="table-wrap">
      <table className="issue-table">
        <thead>
          <tr>
            <th className="col-title">{T.title}</th>
            <th className="col-priority">{T.priority}</th>
            <th className="col-status">{T.status}</th>
            <th className="col-pos">{T.position}</th>
            <th className="col-time">{T.createdAt}</th>
            <th className="col-action">{T.actions}</th>
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
                <div className="action-stack">
                  <StatusSelect
                    value={issue.status}
                    disabled={updatingId === issue.id}
                    onChange={(next) => onStatusChange(issue.id, next)}
                  />
                  {updatingId === issue.id && <span className="saving">{T.saving}...</span>}
                  <div className="row-buttons">
                    <button className="btn btn-small" onClick={() => onView(issue)}>{T.view}</button>
                    <button className="btn btn-small btn-danger" disabled={deletingId === issue.id} onClick={() => onDelete(issue)}>
                      {deletingId === issue.id ? '...' : T.delete}
                    </button>
                  </div>
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
