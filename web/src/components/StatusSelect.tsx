import { STATUS_LABEL, STATUS_OPTIONS } from '../types'
import type { IssueStatus } from '../types'

interface StatusSelectProps {
  value: IssueStatus
  disabled?: boolean
  onChange: (next: IssueStatus) => void
}

/** 状态下拉框：切换后立即 PATCH 给后端，失败时由上层回滚。 */
export function StatusSelect({ value, disabled, onChange }: StatusSelectProps) {
  return (
    <select
      className={`status-select status-${value}`}
      value={value}
      disabled={disabled}
      onChange={(e) => onChange(e.target.value as IssueStatus)}
    >
      {STATUS_OPTIONS.map((s) => (
        <option key={s} value={s}>
          {STATUS_LABEL[s]}
        </option>
      ))}
    </select>
  )
}
