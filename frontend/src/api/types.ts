export type UserRole = 'Capturista' | 'Administrador' | 'Lector'

export interface AuthResponse {
  token: string
  expiresAtUtc: string
  refreshToken: string
  refreshExpiresAtUtc: string
  userId: number
  email: string
  nombre: string
  role: UserRole
  mustChangePassword: boolean
}

// Respuesta de GET /auth/me — usada por el cliente web para saber si hay
// sesión activa al cargar la app (no puede leer la cookie httpOnly con JS).
export interface CurrentUserDto {
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

// user-management: username es nullable hasta que M4 (backend) lo vuelva NOT
// NULL — en la práctica todo usuario creado tras PR1 ya trae alias generado.
export interface UserDto {
  id: number
  email: string
  nombre: string
  role: UserRole
  isActive: boolean
  createdAt: string
  username: string | null
}

export interface CreateUserRequest {
  email: string
  nombre: string
  role: UserRole
}

// user-management: "Admin-Editable Alias" — username usa el namespace amplio
// (^[a-z0-9._-]{3,30}$); el backend re-valida formato y unicidad y responde 409
// en conflicto.
export interface UpdateUserRequest {
  role?: UserRole
  isActive?: boolean
  nombre?: string
  username?: string
}

export interface AuditLogDto {
  id: number
  entityType: string
  entityId: number
  campo: string
  valorAnterior: string | null
  valorNuevo: string | null
  autorId: number
  autorNombre: string
  createdAt: string
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
  tiempoEnCursoSegundos: number | null
  ritmoPromedioSegundosPorKm: number | null
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

// Result — Valido cuenta para posiciones y podio; Controversia queda en revisión (dos
// resultados reclamando el mismo dorsal); Anulado es una captura deshecha (nunca borrado
// físico). JsonStringEnumConverter → PascalCase strings, igual que DisputeEstado.
export type ResultEstado = 'Valido' | 'Controversia' | 'Anulado'

// DorsalDuplicado tiene DisputeGroupId (dos lados). CategoriaSinSalida/CategoriaCerrada
// se resuelven corrigiendo el StartUtc de la categoría, no desde la pestaña de disputas.
export type DisputeMotivo = 'DorsalDuplicado' | 'CategoriaSinSalida' | 'CategoriaCerrada'

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
  estado: ResultEstado
  dorsalPropuesto: string | null
  disputeGroupId: number | null
  disputeMotivo: DisputeMotivo | null
  elapsedMillis: number | null
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
  shareKey: string | null
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

// Enlace público permanente de detalle del corredor (GET /public/corredor/{shareKey}).
// Nullable-honesto — igual que el DTO del backend, ningún campo usa un valor centinela.
export interface PublicRunnerShareDto {
  raceName: string
  slogan: string
  fechaCarrera: string
  nombre: string
  apellidos: string | null
  club: string | null
  dorsal: string
  nombreCategoria: string
  distancia: number
  tiempoLlegadaUtc: string | null
  tiempoTranscurridoSegundos: number | null
  posicionCategoria: number | null
  totalCategoria: number | null
  posicionGeneral: number | null
  totalGeneral: number | null
}

// Controversias (TimingDispute) — JsonStringEnumConverter → PascalCase strings.
export type DisputeEstado = 'Revision' | 'Disputa' | 'Oficial'
export type TimingSource = 'Chip' | 'Checkpoint' | 'Camera'

export interface TimingDisputeDto {
  id: number
  raceId: number
  resultId: number | null
  runnerId: number | null
  dorsal: string | null
  corredorNombre: string
  chipSeconds: number | null
  checkpointSeconds: number | null
  cameraSeconds: number | null
  estado: DisputeEstado
  capturistaNombre: string | null
  capturaNote: string | null
  resolvedByUserId: number | null
  resolvedAt: string | null
  updatedAt: string
}

export interface ResolveDisputeRequest {
  estado: DisputeEstado
  selectedSource?: TimingSource | null
  confirmApplyOfficial?: boolean
}

export interface OpenDisputeCountDto {
  count: number
}

// Dorsales en disputa (DorsalDuplicado, Result.Estado) — Admin-only, distinto de
// Controversias (TimingDispute: chip vs. checkpoint vs. cámara) de arriba.
export interface DisputeGroupDto {
  disputeGroupId: number
  raceId: number
  motivo: DisputeMotivo
  dorsalEnDisputa: string | null
  abiertaUtc: string
  resultados: ResultDto[]
}

export interface DisputeAssignment {
  resultId: number
  dorsal: string | null
}

export interface ResolveDisputeGroupRequest {
  asignaciones: DisputeAssignment[]
  anular: number[]
  razon: string
}

// Feed de eventos administrativos (campana del topbar) — distinto de
// NotificationDto (avisos a corredores por email/WhatsApp).
export type AdminNotificationType = 'DorsalDuplicado' | 'UsuarioCreado' | 'ImportacionConErrores'

export interface AdminNotificationDto {
  id: number
  type: AdminNotificationType
  mensaje: string
  raceId: number | null
  createdAt: string
  leida: boolean
}

export interface AdminNotificationsPageDto {
  items: AdminNotificationDto[]
  unreadCount: number
}
