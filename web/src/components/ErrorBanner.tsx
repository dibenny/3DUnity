interface ErrorBannerProps {
  message: string
  onRetry: () => void
}

/** 后端不可用 / 请求失败时的错误提示条，带重试按钮。 */
export function ErrorBanner({ message, onRetry }: ErrorBannerProps) {
  return (
    <div className="error-banner" role="alert">
      <span className="error-icon">!</span>
      <div className="error-body">
        <strong>请求失败</strong>
        <span>{message}</span>
      </div>
      <button className="btn" onClick={onRetry}>重试</button>
    </div>
  )
}
