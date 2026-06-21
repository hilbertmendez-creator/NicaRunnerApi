import { apiClient } from './client'
import type {
  AuthResponse,
  CategoryStandingsDto,
  RaceDashboardDto,
  RaceDto,
  ResultAuditDto,
  ResultDto,
  UpdateResultRequest,
} from './types'

export async function login(email: string, password: string): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>('/auth/login', { email, password })
  return data
}

export async function getRaces(): Promise<RaceDto[]> {
  const { data } = await apiClient.get<RaceDto[]>('/races')
  return data
}

export async function getDashboard(raceId: number): Promise<RaceDashboardDto> {
  const { data } = await apiClient.get<RaceDashboardDto>(`/races/${raceId}/dashboard`)
  return data
}

export async function getStandings(raceId: number): Promise<CategoryStandingsDto[]> {
  const { data } = await apiClient.get<CategoryStandingsDto[]>(`/races/${raceId}/standings`)
  return data
}

export async function getResults(raceId: number): Promise<ResultDto[]> {
  const { data } = await apiClient.get<ResultDto[]>(`/races/${raceId}/results`)
  return data
}

export async function updateResult(
  raceId: number,
  resultId: number,
  request: UpdateResultRequest,
): Promise<ResultDto> {
  const { data } = await apiClient.put<ResultDto>(`/races/${raceId}/results/${resultId}`, request)
  return data
}

export async function getResultAudit(raceId: number, resultId: number): Promise<ResultAuditDto[]> {
  const { data } = await apiClient.get<ResultAuditDto[]>(`/races/${raceId}/results/${resultId}/audit`)
  return data
}
