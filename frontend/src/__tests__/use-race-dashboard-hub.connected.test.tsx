import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { useRaceDashboardHub } from '../hooks/useRaceDashboardHub'

const handlers: Record<string, (...args: unknown[]) => void> = {}
const mockConnection = {
  on: vi.fn((event: string, cb: (...args: unknown[]) => void) => {
    handlers[event] = cb
  }),
  onreconnected: vi.fn((cb: (...args: unknown[]) => void) => {
    handlers.reconnected = cb
  }),
  onreconnecting: vi.fn((cb: (...args: unknown[]) => void) => {
    handlers.reconnecting = cb
  }),
  onclose: vi.fn((cb: (...args: unknown[]) => void) => {
    handlers.close = cb
  }),
  start: vi.fn().mockResolvedValue(undefined),
  invoke: vi.fn().mockResolvedValue(undefined),
  stop: vi.fn().mockResolvedValue(undefined),
  state: 'Connected',
}

vi.mock('@microsoft/signalr', () => ({
  HubConnectionBuilder: vi.fn().mockImplementation(() => ({
    withUrl: vi.fn().mockReturnThis(),
    withAutomaticReconnect: vi.fn().mockReturnThis(),
    configureLogging: vi.fn().mockReturnThis(),
    build: vi.fn().mockReturnValue(mockConnection),
  })),
  HubConnectionState: { Connected: 'Connected' },
  LogLevel: { Warning: 3 },
}))

describe('useRaceDashboardHub — connected state', () => {
  beforeEach(() => {
    for (const key of Object.keys(handlers)) delete handlers[key]
    mockConnection.start.mockClear().mockResolvedValue(undefined)
    mockConnection.invoke.mockClear().mockResolvedValue(undefined)
  })

  it('reports connected=true once start() resolves, so callers can back off their own polling', async () => {
    const { result } = renderHook(() => useRaceDashboardHub(1, () => {}))

    expect(result.current.connected).toBe(false)
    await waitFor(() => expect(result.current.connected).toBe(true))
  })

  it('reports connected=false while start() is still failing', async () => {
    mockConnection.start.mockRejectedValueOnce(new Error('network down'))

    const { result } = renderHook(() => useRaceDashboardHub(1, () => {}))

    await waitFor(() => expect(mockConnection.start).toHaveBeenCalled())
    expect(result.current.connected).toBe(false)
  })

  it('flips back to connected=false on onreconnecting/onclose', async () => {
    const { result } = renderHook(() => useRaceDashboardHub(1, () => {}))
    await waitFor(() => expect(result.current.connected).toBe(true))

    act(() => handlers.reconnecting())
    await waitFor(() => expect(result.current.connected).toBe(false))

    act(() => handlers.reconnected())
    await waitFor(() => expect(result.current.connected).toBe(true))

    act(() => handlers.close())
    await waitFor(() => expect(result.current.connected).toBe(false))
  })
})
