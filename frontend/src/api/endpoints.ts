import { apiClient } from './client'
import type {
  AuthResponse,
  CategoryStandingsDto,
  CreatePublicTokenRequest,
  CreateRaceCategoryRequest,
  CreateRaceRequest,
  CreateRunnerRequest,
  ImportRunnersResultDto,
  JoinByCodeRequest,
  NotificationDto,
  NotifyAllSummaryDto,
  PublicRaceResultsDto,
  PublicRunnerDetailDto,
  PublicTokenDto,
  RaceCategoryDto,
  RaceDashboardDto,
  RaceDto,
  ResultAuditDto,
  ResultDto,
  RunnerDto,
  UpdateRaceCategoryRequest,
  UpdateRaceRequest,
  UpdateResultRequest,
  UpdateRunnerRequest,
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

export async function getRace(raceId: number): Promise<RaceDto> {
  const { data } = await apiClient.get<RaceDto>(`/races/${raceId}`)
  return data
}

export async function createRace(request: CreateRaceRequest): Promise<RaceDto> {
  const { data } = await apiClient.post<RaceDto>('/races', request)
  return data
}

export async function updateRace(raceId: number, request: UpdateRaceRequest): Promise<RaceDto> {
  const { data } = await apiClient.put<RaceDto>(`/races/${raceId}`, request)
  return data
}

export async function deleteRace(raceId: number): Promise<void> {
  await apiClient.delete(`/races/${raceId}`)
}

export async function startRace(raceId: number): Promise<RaceDto> {
  const { data } = await apiClient.post<RaceDto>(`/races/${raceId}/start`)
  return data
}

export async function joinRaceByCode(request: JoinByCodeRequest): Promise<RaceDto> {
  const { data } = await apiClient.post<RaceDto>('/races/join', request)
  return data
}

export async function getCategories(raceId: number): Promise<RaceCategoryDto[]> {
  const { data } = await apiClient.get<RaceCategoryDto[]>(`/races/${raceId}/categories`)
  return data
}

export async function createCategory(
  raceId: number,
  request: CreateRaceCategoryRequest,
): Promise<RaceCategoryDto> {
  const { data } = await apiClient.post<RaceCategoryDto>(`/races/${raceId}/categories`, request)
  return data
}

export async function updateCategory(
  raceId: number,
  categoryId: number,
  request: UpdateRaceCategoryRequest,
): Promise<RaceCategoryDto> {
  const { data } = await apiClient.put<RaceCategoryDto>(`/races/${raceId}/categories/${categoryId}`, request)
  return data
}

export async function deleteCategory(raceId: number, categoryId: number): Promise<void> {
  await apiClient.delete(`/races/${raceId}/categories/${categoryId}`)
}

export async function getRunners(raceId: number): Promise<RunnerDto[]> {
  const { data } = await apiClient.get<RunnerDto[]>(`/races/${raceId}/runners`)
  return data
}

export async function createRunner(raceId: number, request: CreateRunnerRequest): Promise<RunnerDto> {
  const { data } = await apiClient.post<RunnerDto>(`/races/${raceId}/runners`, request)
  return data
}

export async function updateRunner(
  raceId: number,
  runnerId: number,
  request: UpdateRunnerRequest,
): Promise<RunnerDto> {
  const { data } = await apiClient.put<RunnerDto>(`/races/${raceId}/runners/${runnerId}`, request)
  return data
}

export async function deleteRunner(raceId: number, runnerId: number): Promise<void> {
  await apiClient.delete(`/races/${raceId}/runners/${runnerId}`)
}

export async function importRunnersExcel(raceId: number, file: File): Promise<ImportRunnersResultDto> {
  const formData = new FormData()
  formData.append('file', file)
  const { data } = await apiClient.post<ImportRunnersResultDto>(
    `/races/${raceId}/import-excel`,
    formData,
    { headers: { 'Content-Type': 'multipart/form-data' } },
  )
  return data
}

export async function notifyAll(raceId: number): Promise<NotifyAllSummaryDto> {
  const { data } = await apiClient.post<NotifyAllSummaryDto>(`/races/${raceId}/notify-all`)
  return data
}

export async function notifyResult(resultId: number): Promise<NotificationDto[]> {
  const { data } = await apiClient.post<NotificationDto[]>(`/results/${resultId}/notify`)
  return data
}

export async function getNotificationStatus(id: number): Promise<NotificationDto> {
  const { data } = await apiClient.get<NotificationDto>(`/notifications/${id}`)
  return data
}

export async function getPublicTokens(raceId: number): Promise<PublicTokenDto[]> {
  const { data } = await apiClient.get<PublicTokenDto[]>(`/races/${raceId}/public-token`)
  return data
}

export async function createPublicToken(
  raceId: number,
  request: CreatePublicTokenRequest,
): Promise<PublicTokenDto> {
  const { data } = await apiClient.post<PublicTokenDto>(`/races/${raceId}/public-token`, request)
  return data
}

export async function getPublicResults(token: string): Promise<PublicRaceResultsDto> {
  const { data } = await apiClient.get<PublicRaceResultsDto>(`/public/results/${token}`)
  return data
}

export async function getPublicRunnerResult(
  token: string,
  runnerId: number,
): Promise<PublicRunnerDetailDto> {
  const { data } = await apiClient.get<PublicRunnerDetailDto>(`/public/runner/${token}/${runnerId}`)
  return data
}
