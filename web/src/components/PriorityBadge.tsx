import { PRIORITY_LABEL } from '../types'
import type { Priority } from '../types'

export function PriorityBadge({ priority }: { priority: Priority }) {
  return <span className={`badge badge-${priority}`}>{PRIORITY_LABEL[priority] ?? priority}</span>
}
