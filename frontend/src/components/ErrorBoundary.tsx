import { Component, type ErrorInfo, type ReactNode } from 'react'

interface Props {
  children: ReactNode
}

interface State {
  error: Error | null
}

export class ErrorBoundary extends Component<Props, State> {
  state: State = { error: null }

  static getDerivedStateFromError(error: Error): State {
    return { error }
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('[ErrorBoundary]', error, info.componentStack)
  }

  private handleRetry = () => {
    this.setState({ error: null })
  }

  render() {
    if (this.state.error) {
      return (
        <div
          className="flex min-h-screen flex-col items-center justify-center gap-4 p-8 text-center"
          style={{ background: 'var(--bg-app)', color: 'var(--text-hi)' }}
          role="alert"
        >
          <h1 className="text-2xl font-semibold">Algo salió mal</h1>
          <p className="max-w-md text-sm" style={{ color: 'var(--text-lo)' }}>
            Ocurrió un error inesperado. Podés reintentar o recargar la página. Si el problema
            continúa, contactá al administrador.
          </p>
          <div className="flex flex-wrap items-center justify-center gap-2">
            <button
              type="button"
              className="px-4 py-2 text-sm font-medium text-white"
              style={{ background: 'var(--ac)', borderRadius: 'var(--radius-btn)', border: 'none', cursor: 'pointer' }}
              onClick={this.handleRetry}
            >
              Reintentar
            </button>
            <button
              type="button"
              className="px-4 py-2 text-sm font-medium"
              style={{
                background: 'var(--bg-card)',
                color: 'var(--text-hi)',
                border: '1px solid var(--bd)',
                borderRadius: 'var(--radius-btn)',
                cursor: 'pointer',
              }}
              onClick={() => window.location.reload()}
            >
              Recargar página
            </button>
          </div>
          {import.meta.env.DEV && (
            <pre
              className="mt-4 max-w-2xl overflow-auto p-4 text-left text-xs"
              style={{
                background: 'var(--bg-input)',
                color: 'var(--badge-er-text)',
                borderRadius: 'var(--radius-card)',
                border: '1px solid var(--bd)',
                wordBreak: 'break-word',
              }}
            >
              {this.state.error.message}
              {'\n'}
              {this.state.error.stack}
            </pre>
          )}
        </div>
      )
    }

    return this.props.children
  }
}
