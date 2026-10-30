import { PRIORITY_LABEL, STATUS_LABEL } from '../types'
import type { Issue } from '../types'

interface IssueDetailModalProps {
  issue: Issue
  deleting: boolean
  onClose: () => void
  onDelete: (issue: Issue) => void
}

function formatDateTime(iso: string): string {
  const d = new Date(iso)
  if (Number.isNaN(d.getTime())) return iso
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`
}

const T = {
  title: '\u95ee\u9898\u8be6\u60c5',
  issueTitle: '\u6807\u9898',
  description: '\u63cf\u8ff0',
  priority: '\u4f18\u5148\u7ea7',
  status: '\u72b6\u6001',
  position: '\u4f4d\u7f6e',
  createdAt: '\u521b\u5efa\u65f6\u95f4',
  updatedAt: '\u66f4\u65b0\u65f6\u95f4',
  close: '\u5173\u95ed',
  delete: '\u5220\u9664\u8be5\u95ee\u9898',
  noDescription: '\u65e0\u63cf\u8ff0',
}

export function IssueDetailModal({ issue, deleting, onClose, onDelete }: IssueDetailModalProps) {
  return (
    <div className="modal-backdrop" role="presentation" onMouseDown={onClose}>
      <section className="modal-card" role="dialog" aria-modal="true" onMouseDown={(e) => e.stopPropagation()}>
        <header className="modal-header">
          <div>
            <h2>{T.title}</h2>
            <p className="modal-subtitle">{issue.id}</p>
          </div>
          <button className="icon-button" onClick={onClose} aria-label="close">X</button>
        </header>

        <dl className="detail-grid">
          <dt>{T.issueTitle}</dt><dd>{issue.title}</dd>
          <dt>{T.description}</dt><dd>{issue.description || T.noDescription}</dd>
          <dt>{T.priority}</dt><dd><span className={`badge badge-${issue.priority}`}>{PRIORITY_LABEL[issue.priority]}</span></dd>
          <dt>{T.status}</dt><dd><span className={`status-text status-text-${issue.status}`}>{STATUS_LABEL[issue.status]}</span></dd>
          <dt>{T.position}</dt><dd><code>x {issue.position.x.toFixed(2)} / y {issue.position.y.toFixed(2)} / z {issue.position.z.toFixed(2)}</code></dd>
          <dt>{T.createdAt}</dt><dd>{formatDateTime(issue.createdAt)}</dd>
          <dt>{T.updatedAt}</dt><dd>{formatDateTime(issue.updatedAt)}</dd>
        </dl>

        <footer className="modal-footer">
          <button className="btn" onClick={onClose}>{T.close}</button>
          <button className="btn btn-danger" disabled={deleting} onClick={() => onDelete(issue)}>
            {deleting ? '...' : T.delete}
          </button>
        </footer>
      </section>
    </div>
  )
}
