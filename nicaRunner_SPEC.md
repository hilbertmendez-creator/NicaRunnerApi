# nicaRunner — Especificación Técnica Completa

**Versión:** 1.0  
**Fecha:** 2026-06-14  
**Autor:** Hilbert  
**Proyecto:** nicaRunner — Sistema de Captura de Tiempos para Competencias de Atletismo

---

## 1. Resumen Ejecutivo

**nicaRunner** es una solución completa para gestionar competencias de atletismo en Nicaragua. El sistema captura tiempos de llegada en tiempo real, gestiona categorías con distancias variables, y notifica automáticamente a los corredores con sus resultados.

**Componentes:**
- App móvil (captura de tiempos en vivo)
- Back office web (administración y monitoreo)
- Sitio público (resultados con enlace expirable)
- API REST centralizada (.NET Core)

---

## 2. Arquitectura General

```
┌─────────────────────────────────────────────────────────────┐
│                    nicaRunner System                         │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐  │
│  │  App Móvil   │    │  Back Office  │    │ Sitio Público│  │
│  │  (Capturista)│    │  (Admin)      │    │ (Resultados) │  │
│  └──────┬───────┘    └──────┬────────┘    └──────┬───────┘  │
│         │                   │                    │          │
│         └───────────────────┼────────────────────┘          │
│                             │                               │
│                    ┌────────▼────────┐                      │
│                    │  ASP.NET Core   │                      │
│                    │   REST API      │                      │
│                    └────────┬────────┘                      │
│                             │                               │
│                    ┌────────▼────────┐                      │
│                    │  Base de Datos  │                      │
│                    │  (SQL Server)   │                      │
│                    └─────────────────┘                      │
│                                                              │
│                    ┌─────────────────┐                      │
│                    │ Servicios BG    │                      │
│                    │ (Notificaciones)│                      │
│                    └─────────────────┘                      │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 3. Roles y Permisos

| Rol | Acceso | Funcionalidades |
|-----|--------|-----------------|
| **Capturista** | App móvil | Capturar tiempos, actualizar dorsales por evento/categoría, ver su nombre personalizado |
| **Administrador** | Back office web | Crear carreras, subir Excel, editar tiempos/dorsales manualmente, ver dashboard en tiempo real, auditoría de cambios, disparar notificaciones |
| **Lector** | Back office web | Solo lectura de resultados, sin poder modificar nada |
| **Público** | Sitio web | Ver resultados de la carrera mediante enlace expirable, sin autenticación |

---

## 4. Funcionalidades Principales

### 4.1 Precarga de Corredores (Pre-carrera)

**Flujo:**
1. Administrador crea una nueva carrera
2. Sube archivo Excel con plantilla de corredores
3. Sistema valida y almacena datos

**Campos en Excel:**
- Nombre
- Número de dorsal
- Teléfono
- Email
- Edad
- Categoría (Master, Juvenil, Infantil, etc.)
- Distancia de la carrera

**Validaciones:**
- Dorsal único por carrera
- Email válido
- Edad numérica
- Categoría existe en la carrera

### 4.2 Captura de Tiempos (Durante la carrera)

**App móvil:**
- Capturista inicia sesión con email y contraseña
- Sistema muestra nombre personalizado del capturista
- Captura tiempo de llegada (timestamp automático)
- Ingresa número de dorsal
- Sistema busca corredor por dorsal e ingresa posición automáticamente
- Permite actualizar dorsal si hay error

**Datos capturados:**
- Dorsal
- Tiempo de llegada (automático)
- Posición (auto-incremento)
- Categoría (del corredor)
- Distancia (del corredor)
- Capturista (quién lo registró)
- Timestamp de captura

### 4.3 Dashboard en Tiempo Real (Back office)

**Administrador ve:**
- Lista de corredores por categoría/evento
- Resultados parciales mientras corre la carrera
- Posiciones actualizadas en vivo
- Tiempos de llegada

**Acciones en vivo:**
- Editar tiempo manualmente
- Editar dorsal manualmente
- Ver auditoría de cambios (quién, cuándo, antes/después)

### 4.4 Edición Manual de Tiempos

**Caso de uso:** Error humano durante la captura

**Proceso:**
1. Admin ve resultado incorrecto en dashboard
2. Hace clic en "Editar"
3. Cambia tiempo y/o dorsal
4. Sistema registra:
   - Quién hizo el cambio
   - Cuándo
   - Valor anterior
   - Valor nuevo
5. Actualiza posiciones automáticamente
6. Dispara notificación nuevamente al corredor con datos corregidos

### 4.5 Notificaciones a Corredores (Post-carrera)

**Canales (sin costo):**
- Email HTML personalizado
- WhatsApp (API gratuita o webhook)
- Push (Firebase Messaging)

**Contenido del mensaje:**
- Nombre del corredor
- Posición final
- Tiempo realizado
- Nombre de la carrera
- Link a sitio de resultados (opcional)

**Timing:**
- Automáticamente después de terminar la carrera
- Manual desde back office si hay cambios

### 4.6 Sitio Público de Resultados

**Acceso:**
- URL pública con enlace único por carrera
- Formato: `nicarunner.app/resultados/{unique_token}`
- Sin autenticación requerida

**Contenido visible:**
- Nombre del corredor (solo si lo busca o tiene enlace personal)
- Listado completo de resultados por categoría
- Posición
- Tiempo
- Distancia

**Seguridad:**
- Token único por carrera
- Fecha de expiración configurable (ej: 7, 14, 30 días)
- Job background limpia tokens expirados automáticamente
- Después de expirar, enlace retorna error 404

---

## 5. Modelo de Datos

### Tablas principales

```sql
-- Usuarios (Capturistas, Administradores, Lectores)
Users
├── Id (PK)
├── Email (UK)
├── PasswordHash
├── Nombre
├── Role (Capturista, Admin, Lector)
├── CreatedAt
└── IsActive

-- Carreras/Eventos
Races
├── Id (PK)
├── Nombre
├── Descripción
├── FechaCarrera
├── Estado (Planeada, EnCurso, Terminada)
├── AdminId (FK → Users)
├── CreatedAt
└── UpdatedAt

-- Categorías dentro de una carrera
RaceCategories
├── Id (PK)
├── RaceId (FK → Races)
├── NombreCategoria (Master, Juvenil, Infantil)
├── Distancia (km)
├── EdadMinima
├── EdadMaxima
└── Orden

-- Corredores inscritos
Runners
├── Id (PK)
├── RaceId (FK → Races)
├── Nombre
├── Dorsal (UK por carrera)
├── Teléfono
├── Email
├── Edad
├── CategoryId (FK → RaceCategories)
├── Distancia (heredada de categoría)
└── CreatedAt

-- Resultados / Tiempos capturados
Results
├── Id (PK)
├── RaceId (FK → Races)
├── RunnerId (FK → Runners)
├── Dorsal (copiar en momento de captura)
├── TiempoLlegada (timestamp)
├── Posicion
├── CategoryId (FK → RaceCategories)
├── CapturistId (FK → Users)
├── CreatedAt
└── UpdatedAt

-- Auditoría de cambios
ResultsAudit
├── Id (PK)
├── ResultId (FK → Results)
├── AdminId (FK → Users)
├── CampoModificado (TiempoLlegada, Dorsal, Posicion)
├── ValorAnterior
├── ValorNuevo
├── Razon
├── CreatedAt

-- Tokens de enlace público (resultados)
PublicResultTokens
├── Id (PK)
├── RaceId (FK → Races)
├── Token (UK, único)
├── FechaExpiracion
├── IsExpired
├── CreatedAt
└── CreatedBy (FK → Users)

-- Notificaciones enviadas
Notifications
├── Id (PK)
├── ResultId (FK → Results)
├── RunnerId (FK → Runners)
├── Canal (Email, WhatsApp, Push)
├── Destinatario
├── Estado (Pendiente, Enviado, Fallido)
├── IntentosEnvio
├── CreatedAt
└── SentAt
```

---

## 6. API REST Endpoints

### Autenticación
```
POST   /api/auth/register          - Registrar nuevo capturista
POST   /api/auth/login             - Login (genera JWT)
POST   /api/auth/refresh           - Refresh token
POST   /api/auth/logout            - Logout
```

### Gestión de Carreras (Admin)
```
POST   /api/races                  - Crear nueva carrera
GET    /api/races                  - Listar carreras
GET    /api/races/{raceId}         - Detalles carrera
PUT    /api/races/{raceId}         - Editar carrera
DELETE /api/races/{raceId}         - Eliminar carrera
```

### Carga de Corredores (Admin)
```
POST   /api/races/{raceId}/import-excel    - Importar Excel de corredores
GET    /api/races/{raceId}/runners         - Listar corredores de carrera
GET    /api/races/{raceId}/runners/{id}   - Detalles corredor
PUT    /api/races/{raceId}/runners/{id}   - Editar corredor
```

### Categorías (Admin)
```
POST   /api/races/{raceId}/categories      - Crear categoría
GET    /api/races/{raceId}/categories      - Listar categorías
PUT    /api/races/{raceId}/categories/{id} - Editar categoría
DELETE /api/races/{raceId}/categories/{id} - Eliminar categoría
```

### Captura de Tiempos (Capturista)
```
POST   /api/races/{raceId}/results         - Registrar tiempo de llegada
GET    /api/races/{raceId}/results         - Listar resultados de carrera
GET    /api/races/{raceId}/results/live    - WebSocket para actualizaciones en vivo
```

### Edición de Resultados (Admin)
```
PUT    /api/races/{raceId}/results/{id}    - Editar tiempo/dorsal (con auditoría)
GET    /api/races/{raceId}/results/{id}/audit - Ver historial de cambios
```

### Dashboard (Admin & Lector)
```
GET    /api/races/{raceId}/dashboard       - Resumen en vivo de carrera
GET    /api/races/{raceId}/standings       - Posiciones actuales por categoría
```

### Notificaciones (Admin)
```
POST   /api/races/{raceId}/notify-all      - Enviar notificaciones a todos
POST   /api/results/{id}/notify            - Enviar notificación a corredor
GET    /api/notifications/{id}             - Estado de notificación
```

### Enlace Público (Sin autenticación)
```
GET    /api/public/results/{token}         - Ver resultados con token público
GET    /api/public/runner/{token}/{runnerId} - Ver resultado individual (opcional)
```

---

## 7. Flujos de Negocio

### Flujo Pre-carrera
```
1. Admin crea carrera
2. Admin define categorías con distancias
3. Admin sube Excel de corredores
4. Sistema valida e importa datos
5. Sistema está listo para captura
```

### Flujo Durante la Carrera
```
1. Capturista inicia sesión en app
2. App muestra nombre del capturista
3. Carrera comienza
4. Capturista captura tiempos conforme llegan
   - Ingresa dorsal → sistema busca corredor
   - Sistema captura tiempo automático
   - Sistema calcula posición
5. Admin ve dashboard en vivo actualizándose
6. Admin puede editar tiempos si hay error
   - Sistema registra auditoría
```

### Flujo Post-carrera
```
1. Admin marca carrera como "Terminada"
2. Sistema genera resultados finales por categoría
3. Admin genera token público para resultados
4. Admin envía notificaciones a corredores:
   - Email HTML personalizado
   - WhatsApp (optional)
   - Push (optional)
5. Admin comparte enlace público con corredores
6. Corredores acceden a resultados sin login
7. Después de fecha de expiración, enlace se borra automáticamente
```

---

## 8. Stack Técnico

### Backend
- **Framework:** ASP.NET Core 8.0+
- **ORM:** Entity Framework Core
- **Base de datos:** SQL Server
- **Autenticación:** JWT + Refresh tokens
- **Validación:** FluentValidation
- **Logging:** Serilog
- **Background Jobs:** Hangfire (para notificaciones y limpieza de tokens)

### Frontend Web (Back office)
- **Framework:** Blazor (WebAssembly o Server) o React/Angular
- **UI:** Material Design o Tailwind CSS
- **Gráficos:** Chart.js o Recharts
- **Estado:** Blazor StateHasChanged o Redux/Context

### App Móvil
- **Plataforma:** Android/iOS (considerar .NET MAUI o Flutter)
- **Comunicación:** HTTP Client contra API REST
- **Storage local:** SQLite para offline

### Notificaciones
- **Email:** SendGrid (tier gratuito)
- **WhatsApp:** WhatsApp Business API (webhook)
- **Push:** Firebase Cloud Messaging

---

## 9. Consideraciones de Seguridad

### Autenticación & Autorización
- JWT con expiración configurable
- Refresh tokens rotados
- Roles basados en acceso (RBAC)
- Capturista solo ve su app, no back office
- Lector no puede modificar

### Validación
- Validar entrada de Excel (tipos, duplicados, limites)
- Validar que tiempos sean lógicos (no negativos, en orden)
- Rate limiting en endpoints públicos

### Auditoría
- Registrar todos los cambios de tiempos
- Log de quién hizo qué y cuándo
- Historial completo de ediciones

### Privacidad
- Datos de corredores protegidos
- Email/teléfono ocultos en sitio público
- Tokens públicos expiran automáticamente
- No persistir información sensible en app móvil

---

## 10. Implementación — Plan por Fases

### Fase 1: Core API (2-3 semanas)
- [ ] Estructura de proyecto ASP.NET Core
- [ ] Modelos de datos y migraciones
- [ ] Autenticación JWT
- [ ] Endpoints CRUD base (carreras, corredores, resultados)
- [ ] Import de Excel

### Fase 2: Captura de Tiempos (1-2 semanas)
- [ ] Endpoint de captura de tiempos
- [ ] WebSocket para actualizaciones en vivo
- [ ] App móvil básica conectada a API

### Fase 3: Back Office (2-3 semanas)
- [ ] Dashboard en tiempo real
- [ ] Edición manual de tiempos con auditoría
- [ ] Gestión de roles y permisos

### Fase 4: Notificaciones (1-2 semanas)
- [ ] Servicio de notificaciones (Email, WhatsApp, Push)
- [ ] Background job para envío
- [ ] Reenvío en caso de fallos

### Fase 5: Sitio Público (1 semana)
- [ ] Generación de tokens públicos
- [ ] Sitio público con resultados
- [ ] Limpieza automática de tokens expirados

### Fase 6: Refinamiento & Testing (1-2 semanas)
- [ ] Tests unitarios
- [ ] Tests de integración
- [ ] Ajustes de performance
- [ ] Documentación

### Fase 7: Diseño Profesional (A futuro)
- [ ] Cloud design system
- [ ] Rediseño de UI/UX
- [ ] Aplicar a app móvil y back office

---

## 11. Próximos Pasos

1. Validar esta especificación
2. Crear estructura de solución en Visual Studio
3. Configurar base de datos (scripts SQL)
4. Comenzar Fase 1 con Claude Code
5. Iterar sobre feedback

---

**Notas:**
- Este documento es vivo y se actualiza conforme evolucionan los requisitos
- Mantener en sincronización con ADRs específicas por decisión arquitectónica
- Considerar load testing si se espera alto volumen de captura simultánea
