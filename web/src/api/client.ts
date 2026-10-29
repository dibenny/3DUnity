import type { Issue, IssueStatus } from '../types'

/**
 * API 根地址。
 * - 开发期留空，走 Vite 代理（/api -> http://127.0.0.1:8080），不会遇到跨域。
 * - 部署到别的地址时，用 VITE_API_BASE 环境变量指定，例如：
 *   VITE_API_BASE=http://192.168.1.102:8080 npm run build
 */
const API_BASE = (import.meta.env.VITE_API_BASE ?? '').replace(/\/+$/, '')

export class ApiError extends Error {
  status: number
  code: string

  constructor(message: string, status = 0, code = 'NETWORK_ERROR') {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
  }
}

interface ErrorBody {
  error?: {
    code?: string
    message?: string
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let res: Response
  try {
    res = await fetch(`${API_BASE}${path}`, {
      headers: { 'Content-Type': 'application/json' },
      ...init,
    })
  } catch {
    // fetch 抛异常 = 网络层面就断了（服务没起 / 地址错 / 端口被占）
    throw new ApiError('无法连接后端服务，请确认 Go 服务是否启动。', 0, 'NETWORK_ERROR')
  }

  const text = await res.text()
  let parsed: unknown = null
  let parsedAsJson = false
  if (text) {
    try {
      parsed = JSON.parse(text)
      parsedAsJson = true
    } catch {
      parsedAsJson = false
    }
  }

  if (!res.ok) {
    const body = parsed as ErrorBody | null

    // 后端返回的标准错误体，优先用它自己的 message
    if (parsedAsJson && body?.error?.message) {
      throw new ApiError(body.error.message, res.status, body.error.code || 'HTTP_ERROR')
    }

    // 响应根本不是 JSON —— 典型情况是 Vite 代理连不上 Go 时返回的错误页
    if (!parsedAsJson) {
      throw new ApiError('无法连接后端服务，请确认 Go 服务是否启动。', res.status, 'BACKEND_UNAVAILABLE')
    }

    throw new ApiError(`请求失败（HTTP ${res.status}）`, res.status, 'HTTP_ERROR')
  }

  return parsed as T
}

export const api = {
  health: () => request<{ status: string; time: string }>('/api/health'),

  listIssues: () => request<Issue[]>('/api/issues'),

  updateStatus: (id: string, status: IssueStatus) =>
    request<Issue>(`/api/issues/${encodeURIComponent(id)}/status`, {
      method: 'PATCH',
      body: JSON.stringify({ status }),
    }),
}
