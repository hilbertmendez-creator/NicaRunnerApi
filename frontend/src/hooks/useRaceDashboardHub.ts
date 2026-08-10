import { useEffect, useRef, useState } from 'react'
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'

function getHubUrl(): string {
  const apiBase = import.meta.env.VITE_API_URL ?? '/api'
  const origin = apiBase.replace(/\/api\/?$/, '')
  return `${origin}/hubs/race-dashboard`
}

/**
 * Se une al grupo de la carrera en el hub de SignalR y llama onResultsChanged
 * cuando el backend confirma una captura o edición — así el dashboard puede
 * refrescar de inmediato en vez de esperar al próximo tick del polling.
 * Devuelve `connected` para que el caller pueda bajar la frecuencia de su
 * propio polling de respaldo mientras el hub esté sano (sigue existiendo
 * como red de seguridad, pero no hace falta que corra cada 5s si el hub
 * ya está empujando los cambios en tiempo real).
 */
export function useRaceDashboardHub(raceId: number | null, onResultsChanged: () => void): { connected: boolean } {
  const callbackRef = useRef(onResultsChanged)
  const [connected, setConnected] = useState(false)

  useEffect(() => {
    callbackRef.current = onResultsChanged
  })

  useEffect(() => {
    if (!raceId) {
      setConnected(false)
      return
    }

    // El JWT viaja en la cookie httpOnly nr_at, que el browser adjunta solo
    // en el handshake (withCredentials) — ya no hace falta accessTokenFactory.
    const connection = new HubConnectionBuilder()
      .withUrl(getHubUrl(), { withCredentials: true })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on('resultsChanged', () => callbackRef.current())
    connection.onreconnected(() => setConnected(true))
    connection.onreconnecting(() => setConnected(false))
    connection.onclose(() => setConnected(false))

    connection
      .start()
      .then(() => {
        setConnected(true)
        return connection.invoke('JoinRace', raceId)
      })
      .catch(() => {
        // El polling existente sigue funcionando como respaldo si el hub falla.
        setConnected(false)
      })

    return () => {
      setConnected(false)
      if (connection.state === HubConnectionState.Connected) {
        connection.invoke('LeaveRace', raceId).catch(() => {})
      }
      connection.stop()
    }
  }, [raceId])

  return { connected }
}
