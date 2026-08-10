import { renderHook, waitFor } from '@testing-library/react'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { usePolling } from '../hooks/usePolling'

function setHidden(hidden: boolean) {
  Object.defineProperty(document, 'hidden', { configurable: true, get: () => hidden })
}

describe('usePolling — pausa en pestaña oculta', () => {
  afterEach(() => {
    setHidden(false)
  })

  it('no dispara el fetch inicial mientras la pestaña está oculta', () => {
    const fetcher = vi.fn().mockResolvedValue('ok')
    setHidden(true)

    renderHook(() => usePolling(fetcher, 5000, []))

    expect(fetcher).not.toHaveBeenCalled()
  })

  it('refresca al instante al volver a la pestaña, sin esperar el próximo intervalo', async () => {
    const fetcher = vi.fn().mockResolvedValue('ok')
    setHidden(true)
    renderHook(() => usePolling(fetcher, 5000, []))
    expect(fetcher).not.toHaveBeenCalled()

    setHidden(false)
    document.dispatchEvent(new Event('visibilitychange'))

    await waitFor(() => expect(fetcher).toHaveBeenCalledTimes(1))
  })

  it('sigue funcionando normalmente cuando la pestaña está visible', async () => {
    const fetcher = vi.fn().mockResolvedValue('ok')

    const { result } = renderHook(() => usePolling(fetcher, 5000, []))

    await waitFor(() => expect(fetcher).toHaveBeenCalledTimes(1))
    await waitFor(() => expect(result.current.data).toBe('ok'))
  })
})
