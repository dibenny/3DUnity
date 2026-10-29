package middleware

import "net/http"

// CORS 处理跨域。
//
// React 开发服务器默认跑在 5173，Go 服务跑在 8080，属于不同源，
// 浏览器会先发 OPTIONS 预检请求，所以这里必须：
//  1. 对 OPTIONS 直接返回 204，不进入业务处理；
//  2. 回显 Origin 并放行 Content-Type 等请求头。
//
// 说明：服务端没有 Cookie / 鉴权，所以不需要设置 Allow-Credentials。
func CORS() func(http.Handler) http.Handler {
	return func(next http.Handler) http.Handler {
		return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
			origin := r.Header.Get("Origin")
			if origin == "" {
				origin = "*"
			}
			h := w.Header()
			h.Set("Access-Control-Allow-Origin", origin)
			h.Set("Vary", "Origin")
			h.Set("Access-Control-Allow-Methods", "GET, POST, PATCH, PUT, DELETE, OPTIONS")
			h.Set("Access-Control-Allow-Headers", "Content-Type, Authorization, Accept")
			h.Set("Access-Control-Max-Age", "86400")

			if r.Method == http.MethodOptions {
				w.WriteHeader(http.StatusNoContent)
				return
			}
			next.ServeHTTP(w, r)
		})
	}
}
