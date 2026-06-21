export type UserRole = 'Capturista' | 'Administrador' | 'Lector'

export interface AuthResponse {
  token: string
  expiresAtUtc: string
  userId: number
  email: string
  nombre: string
  role: UserRole
}

export type RaceStatus = 'Planeada' | 'EnCurso' | 'Terminada'

export interface RaceDto {
  id: number
  nombre: string
  descripcion?: string | null
  fechaCarrera: string
  estado: RaceStatus
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
  runnerId: number
  dorsal: string
  tiempoLlegada: string
  posicion: number
  categoryId: number
  capturistaId: number
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
