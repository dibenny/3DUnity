/** 与 Go 后端 model.Issue 一一对应的类型，三端字段名必须保持一致。 */
export type Priority = 'low' | 'medium' | 'high'
export type IssueStatus = 'open' | 'in_progress' | 'resolved'

export interface Position {
  x: number
  y: number
  z: number
}

export interface Issue {
  id: string
  title: string
  description: string
  priority: Priority
  status: IssueStatus
  position: Position
  createdAt: string
  updatedAt: string
}

export const PRIORITY_LABEL: Record<Priority, string> = {
  low: '低',
  medium: '中',
  high: '高',
}

export const STATUS_LABEL: Record<IssueStatus, string> = {
  open: '待处理',
  in_progress: '处理中',
  resolved: '已解决',
}

export const STATUS_OPTIONS: IssueStatus[] = ['open', 'in_progress', 'resolved']
