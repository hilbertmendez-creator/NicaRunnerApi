import { apiClient } from './client'
import type {
  AssignCategoryRequest,
  AuditLogDto,
  AuthResponse,
  CategoryDto,
  CategoryStandingsDto,
  ChangePasswordRequest,
  CreateCategoryRequest,
  CreatePublicTokenRequest,
  CreateRaceRequest,
  CreateRunnerRequest,
  CreateUserRequest,
  CurrentUserDto,
  ForgotPasswordRequest,
  ImportRunnersResultDto,
  NotificationDto,
  NotifyAllSummaryDto,
  PublicRaceResultsDto,
  PublicRunnerDetailDto,
  PublicTokenDto,
  RaceCategoryDto,
  RaceDashboardDto,
  RaceDto,
  ResetPasswordRequest,
  ResultAuditDto,
  ResultDto,
  RunnerDto,
  UpdateCategoryRequest,
  UpdateRaceRequest,
  UpdateResultRequest,
  UpdateRunnerRequest,
  UpdateUserRequest,
  UserDto,
} from './types'

// user-auth: "Unified Identifier Login" / "Backward-Compatible Login Payload" —
// el backend acepta `identifier` (email o alias); `email` legado se mantiene
// server-side para clientes ya publicados, pero el backoffice ya envía
// `identifier` (design.md §4.1).
export async function login(identifier: string, password: string): Promise<AuthResponse> {
  const { data } = await apiClient.post<AuthResponse>('/auth/login', { identifier, password })
  return data
}

export async function getCurrentUser(): Promise<CurrentUserDto> {
  const { data } = await apiClient.get<CurrentUserDto>('/auth/me')
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

// Catálogo global de categorías (backoffice)
export async function getCategoryCatalog(): Promise<CategoryDto[]> {
  const { data } = await apiClient.get<CategoryDto[]>('/categories')
  return data
}

export async function createCategoryCatalogEntry(request: CreateCategoryRequest): Promise<CategoryDto> {
  const { data } = await apiClient.post<CategoryDto>('/categories', request)
  return data
}

export async function updateCategoryCatalogEntry(
  categoryId: number,
  request: UpdateCategoryRequest,
): Promise<CategoryDto> {
  const { data } = await apiClient.put<CategoryDto>(`/categories/${categoryId}`, request)
  return data
}

export async function deleteCategoryCatalogEntry(categoryId: number): Promise<void> {
  await apiClient.delete(`/categories/${categoryId}`)
}

// Categorías del catálogo asignadas a una carrera específica
export async function getCategories(raceId: number): Promise<RaceCategoryDto[]> {
  const { data } = await apiClient.get<RaceCategoryDto[]>(`/races/${raceId}/categories`)
  return data
}

export async function assignCategory(
  raceId: number,
  request: AssignCategoryRequest,
): Promise<RaceCategoryDto> {
  const { data } = await apiClient.post<RaceCategoryDto>(`/races/${raceId}/categories`, request)
  return data
}

export async function unassignCategory(raceId: number, categoryId: number): Promise<void> {
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

export async function downloadRunnerImportTemplate(raceId: number): Promise<Blob> {
  const { data } = await apiClient.get(`/races/${raceId}/import-excel/template`, { responseType: 'blob' })
  return data
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

export async function changePassword(request: ChangePasswordRequest): Promise<void> {
  await apiClient.post('/auth/change-password', request)
}

export async function forgotPassword(request: ForgotPasswordRequest): Promise<void> {
  await apiClient.post('/auth/forgot-password', request)
}

export async function resetPassword(request: ResetPasswordRequest): Promise<void> {
  await apiClient.post('/auth/reset-password', request)
}

export async function getUsers(): Promise<UserDto[]> {
  const { data } = await apiClient.get<UserDto[]>('/users')
  return data
}

export async function createUser(request: CreateUserRequest): Promise<UserDto> {
  const { data } = await apiClient.post<UserDto>('/users', request)
  return data
}

export async function updateUser(id: number, request: UpdateUserRequest): Promise<UserDto> {
  const { data } = await apiClient.patch<UserDto>(`/users/${id}`, request)
  return data
}

export async function getUserAudit(id: number): Promise<AuditLogDto[]> {
  const { data } = await apiClient.get<AuditLogDto[]>(`/users/${id}/audit`)
  return data
}

// login-lockout: "Admin Unlock" — POST /users/{id}/unlock, admin-only, sin
// body; limpia FailedLoginCount/LockedUntilUtc y audita (design.md §3.3/§4.4).
export async function unlockUser(id: number): Promise<UserDto> {
  const { data } = await apiClient.post<UserDto>(`/users/${id}/unlock`)
  return data
}

export async function getRaceAudit(raceId: number): Promise<AuditLogDto[]> {
  const { data } = await apiClient.get<AuditLogDto[]>(`/races/${raceId}/audit`)
  return data
}

export async function getCategoryAudit(categoryId: number): Promise<AuditLogDto[]> {
  const { data } = await apiClient.get<AuditLogDto[]>(`/categories/${categoryId}/audit`)
  return data
}
