import { useEffect, useRef } from 'react'
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'
import { getStoredToken } from '../api/client'

function getHubUrl(): string {
  const apiBase = import.meta.env.VITE_API_URL ?? '/api'
  const origin = apiBase.replace(/\/api\/?$/, '')
  return `${origin}/hubs/race-dashboard`
}

/**
 * Se une al grupo de la carrera en el hub de SignalR y llama onResultsChanged
 * cuando el backend confirma una captura o edición — así el dashboard puede
 * refrescar de inmediato en vez de esperar al próximo tick del polling.
 */
export function useRaceDashboardHub(raceId: number | null, onResultsChanged: () => void) {
  const callbackRef = useRef(onResultsChanged)

  useEffect(() => {
    callbackRef.current = onResultsChanged
  })

  useEffect(() => {
    if (!raceId) return

    const connection = new HubConnectionBuilder()
      .withUrl(getHubUrl(), { accessTokenFactory: () => getStoredToken() ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    connection.on('resultsChanged', () => callbackRef.current())

    connection
      .start()
      .then(() => connection.invoke('JoinRace', raceId))
      .catch(() => {
        // El polling existente sigue funcionando como respaldo si el hub falla.
      })

    return () => {
      if (connection.state === HubConnectionState.Connected) {
        connection.invoke('LeaveRace', raceId).catch(() => {})
      }
      connection.stop()
    }
  }, [raceId])
}
