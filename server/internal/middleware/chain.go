package middleware

import "net/http"

// Chain 把多个中间件按声明顺序串起来：
// 第一个是最外层，最后一个最内层，最终落到 handler。
func Chain(h http.Handler, mws ...func(http.Handler) http.Handler) http.Handler {
	for i := len(mws) - 1; i >= 0; i-- {
		h = mws[i](h)
	}
	return h
}
