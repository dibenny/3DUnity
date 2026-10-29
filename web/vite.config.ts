import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// 开发期用 Vite 代理把 /api 转发到 Go 服务，避免浏览器跨域。
// 生产构建（npm run build）产物里没有代理，需要设置 VITE_API_BASE。
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    strictPort: false,
    proxy: {
      '/api': {
        target: 'http://127.0.0.1:8080',
        changeOrigin: true,
      },
    },
  },
})
