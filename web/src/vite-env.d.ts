/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** 后端 API 根地址。留空时使用 Vite 开发代理（/api -> 127.0.0.1:8080）。 */
  readonly VITE_API_BASE?: string
}
