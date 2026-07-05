export type UserRole = 'Capturista' | 'Administrador' | 'Lector'

export interface AuthResponse {
  token: string
  expiresAtUtc: string
  userId: number
  email: string
  nombre: string
  role: UserRole
  mustChangePassword: boolean
}

export interface ChangePasswordRequest {
  currentPassword: string
  newPassword: string
}

export interface ForgotPasswordRequest {
  email: string
}

export interface ResetPasswordRequest {
  token: string
  newPassword: string
}

export interface UserDto {
  id: number
  email: string
  nombre: string
  role: UserRole
  isActive: boolean
  createdAt: string
}

export interface CreateUserRequest {
  email: string
  nombre: string
  role: UserRole
}

export interface UpdateUserRequest {
  role?: UserRole
  isActive?: boolean
}

export type RaceStatus = 'Planeada' | 'EnCurso' | 'Terminada'

export interface RaceDto {
  id: number
  nombre: string
  descripcion?: string | null
  fechaCarrera: string
  estado: RaceStatus
  joinCode: string
  raceStartUtc?: string | null
  adminId: number
  createdAt: string
  updatedAt: string
}

export interface CategoryProgressDto {
  categoryId: number
  nombreCategoria: string
  inscritos: number
  conTiempo: number
  pendientes: number
}

export interface RecentResultDto {
  resultId: number
  dorsal: string
  nombre: string
  tiempoLlegada: string
  posicion: number
  nombreCategoria: string
  capturistaId: number
}

export interface RaceDashboardDto {
  raceId: number
  raceName: string
  estado: RaceStatus
  totalInscritos: number
  totalConTiempo: number
  totalPendientes: number
  categorias: CategoryProgressDto[]
  ultimosResultados: RecentResultDto[]
}

export interface RunnerStandingDto {
  runnerId: number
  nombre: string
  dorsal: string
  posicion: number
  tiempoLlegada: string
}

export interface CategoryStandingsDto {
  categoryId: number
  nombreCategoria: string
  distancia: number
  resultados: RunnerStandingDto[]
}

export interface ResultDto {
  id: number
  raceId: number
  runnerId: number | null
  runnerNombre: string
  dorsal: string | null
  tiempoLlegada: string
  posicion: number
  categoryId: number | null
  categoriaNombre: string
  capturistaId: number
  capturistaNombre: string
  createdAt: string
  updatedAt: string
}

export interface UpdateResultRequest {
  dorsal: string
  tiempoLlegada: string
  razon: string
}

export interface ResultAuditDto {
  id: number
  resultId: number
  adminId: number
  campoModificado: string
  valorAnterior: string
  valorNuevo: string
  razon?: string | null
  createdAt: string
}

export interface CreateRaceRequest {
  nombre: string
  descripcion?: string | null
  fechaCarrera: string
  categoryIds?: number[]
}

export interface UpdateRaceRequest {
  nombre: string
  descripcion?: string | null
  fechaCarrera: string
  estado: RaceStatus
}

// Catálogo global de categorías (backoffice), independiente de cualquier carrera.
export interface CategoryDto {
  id: number
  codigo: string
  nombreCategoria: string
  descripcion?: string | null
  distancia: number
  edadMinima: number
  edadMaxima: number
  orden: number
}

export interface CreateCategoryRequest {
  codigo: string
  nombreCategoria: string
  descripcion?: string | null
  distancia: number
  edadMinima: number
  edadMaxima: number
  orden: number
}

export type UpdateCategoryRequest = CreateCategoryRequest

// Categorías del catálogo seleccionadas para una carrera específica.
export interface RaceCategoryDto {
  categoryId: number
  codigo: string
  nombreCategoria: string
  descripcion?: string | null
  distancia: number
  edadMinima: number
  edadMaxima: number
  orden: number
}

export interface AssignCategoryRequest {
  categoryId: number
}

export type Sexo = 'M' | 'F'

export interface RunnerDto {
  id: number
  raceId: number
  nombre: string
  apellidos?: string | null
  dorsal: string
  telefono?: string | null
  email?: string | null
  sexo?: Sexo | null
  club?: string | null
  fechaNacimiento?: string | null
  edad: number
  categoryId: number
  categoriaNombre: string
  distancia: number
  createdAt: string
}

export interface CreateRunnerRequest {
  nombre: string
  apellidos?: string | null
  dorsal: string
  telefono?: string | null
  email?: string | null
  sexo?: Sexo | null
  club?: string | null
  fechaNacimiento?: string | null
  edad?: number | null
  categoryId: number
}

export type UpdateRunnerRequest = CreateRunnerRequest

export interface ImportRunnerError {
  fila: number
  motivo: string
}

export interface ImportRunnersResultDto {
  totalFilas: number
  importados: number
  errores: ImportRunnerError[]
}

export type NotificationChannel = 'Email' | 'WhatsApp'
export type NotificationStatus = 'Pendiente' | 'Enviada' | 'Fallida'

export interface NotificationDto {
  id: number
  raceId: number
  runnerId: number
  resultId: number
  channel: NotificationChannel
  status: NotificationStatus
  mensaje: string
  error?: string | null
  createdAt: string
  sentAt?: string | null
}

export interface NotifyAllSummaryDto {
  totalResultados: number
  notificacionesCreadas: number
  enviadas: number
  fallidas: number
}

export interface PublicTokenDto {
  id: number
  raceId: number
  token: string
  fechaExpiracion: string
  createdAt: string
}

export interface CreatePublicTokenRequest {
  diasExpiracion: number
}

export interface PublicRunnerResultDto {
  runnerId: number
  nombre: string
  dorsal: string
  posicion: number
  tiempoLlegada: string
}

export interface PublicCategoryResultsDto {
  nombreCategoria: string
  distancia: number
  resultados: PublicRunnerResultDto[]
}

export interface PublicRaceResultsDto {
  raceName: string
  fechaCarrera: string
  categorias: PublicCategoryResultsDto[]
}

export interface PublicRunnerDetailDto {
  raceName: string
  nombreCategoria: string
  distancia: number
  runnerId: number
  nombre: string
  dorsal: string
  posicion: number
  tiempoLlegada: string
}
