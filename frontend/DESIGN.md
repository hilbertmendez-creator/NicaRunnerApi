---
name: NicaRunner Back-Office
description: Consola operativa de día de carrera para organizadores, jueces y capturistas de carreras de running en Nicaragua.
colors:
  signal-blue: "#2563EB"
  signal-blue-bg: "#EFF6FF"
  signal-blue-dim: "#BFDBFE"
  neutral-app-bg: "#F4F6FA"
  neutral-surface: "#FFFFFF"
  neutral-border: "#E2E8F0"
  text-high: "#0F172A"
  text-medium: "#475569"
  text-low: "#5B6B7C"
  success: "#15803D"
  success-bg: "#F0FDF4"
  success-border: "#BBF7D0"
  warning: "#92400E"
  warning-bg: "#FFFBEB"
  warning-border: "#FDE68A"
  critical: "#DC2626"
  critical-bg: "#FEF2F2"
  critical-border: "#FECACA"
  info: "#1D4ED8"
  info-bg: "#EFF6FF"
  info-border: "#BFDBFE"
  night-console-bg: "#1C2333"
  night-console-fg: "#E2E8F0"
  night-console-muted: "#94A3B8"
  medal-gold: "#F59E0B"
  medal-silver: "#94A3B8"
  medal-bronze: "#B45309"
typography:
  body:
    fontFamily: "Inter, system-ui, 'Segoe UI', sans-serif"
    fontSize: "14px"
    fontWeight: 400
    lineHeight: 1.5
  label:
    fontFamily: "Inter, system-ui, 'Segoe UI', sans-serif"
    fontSize: "12px"
    fontWeight: 600
    letterSpacing: "0.02em"
  data:
    fontFamily: "'IBM Plex Mono', ui-monospace, monospace"
    fontSize: "14px"
    fontWeight: 500
    lineHeight: 1.4
rounded:
  card: "7px"
  btn: "6px"
  badge: "20px"
spacing:
  xs: "4px"
  sm: "8px"
  md: "12px"
  lg: "16px"
  xl: "24px"
components:
  button-primary:
    backgroundColor: "{colors.signal-blue}"
    textColor: "#FFFFFF"
    rounded: "{rounded.btn}"
    padding: "0 12px"
    height: "32px"
  button-secondary:
    backgroundColor: "{colors.neutral-surface}"
    textColor: "{colors.text-high}"
    rounded: "{rounded.btn}"
    padding: "0 12px"
    height: "32px"
  button-destructive:
    backgroundColor: "{colors.critical-bg}"
    textColor: "{colors.critical}"
    rounded: "{rounded.btn}"
    padding: "0 12px"
    height: "32px"
  badge-status:
    backgroundColor: "{colors.success-bg}"
    textColor: "{colors.success}"
    rounded: "{rounded.badge}"
    padding: "2px 8px"
  card:
    backgroundColor: "{colors.neutral-surface}"
    rounded: "{rounded.card}"
    padding: "16px"
---

# Design System: NicaRunner Back-Office

## Overview

**Creative North Star: "Race-Day Control Room"**

Este es el nombre que el propio código ya se da a sí mismo (`index.css`, sección `ANIMATION SYSTEM — Race-Day Control Room`): una consola operativa que un organizador, juez de tiempos o capturista abre en plena carrera para tomar decisiones rápidas con datos en vivo — no una app de consumo, no un panel de marketing. El tono es funcional y directo: los datos y el estado de la carrera son el protagonista, sin florituras que compitan por atención. Los componentes son densos y utilitarios — compactos, con relleno ajustado, priorizando caber más información en pantalla sobre el respiro visual generoso de una app de consumo. Las superficies son planas por defecto; la sombra aparece solo como respuesta a un cambio de estado (hover, menú desplegable, modal), nunca como decoración de reposo.

La decisión más distintiva del sistema es el **Night Console**: el sidebar de navegación se queda oscuro (`#1C2333`) sin importar el tema elegido para el resto de la app. En tema claro, es la única superficie oscura de la pantalla — un ancla visual constante, coherente con la idea de "consola" que no cambia aunque el resto de la interfaz sí.

**Key Characteristics:**
- Denso y funcional — prioriza datos por sobre espacio en blanco.
- Plano por defecto — la sombra es una señal de estado, no un adorno.
- Bicolor consistente por rol: cada estado semántico (éxito, alerta, crítico, info) tiene su propio par texto+fondo, nunca solo color.
- El rail de navegación (Night Console) es una isla de identidad fija, independiente del tema claro/oscuro del resto de la app.
- Tipografía monoespaciada reservada exclusivamente para datos que se comparan en columna (tiempos, dorsales, conteos) — nunca para texto de lectura.

## Colors

La paleta es funcional antes que decorativa: cada color comunica un estado del sistema (éxito, alerta, crítico, informativo) o un rol estructural (superficie, texto, borde). El acento aparece con moderación — solo en controles interactivos primarios y el indicador de "activo" del sidebar.

### Primary
- **Signal Blue** (`#2563EB` claro / `#3B82F6` oscuro): el color de "esto es interactivo o está activo ahora" — botones primarios, enlaces, foco de teclado, y el ítem activo del menú del sidebar. Es el único acento cromático fuera de los colores semánticos de estado.

### Neutral
- **App Background** (`#F4F6FA` claro / `#080E1A` oscuro): fondo de página, un peldaño por debajo de las superficies de contenido.
- **Surface** (`#FFFFFF` claro / `#0D1522` oscuro): fondo de cards, tablas, modales — donde vive el contenido.
- **Border** (`#E2E8F0` claro / `rgba(255,255,255,.07)` oscuro): separación entre superficies, siempre sutil.
- **Text High** (`#0F172A` claro / `#E2E8F0` oscuro): títulos, valores primarios, texto de mayor jerarquía.
- **Text Medium** (`#475569` claro / `#94A3B8` oscuro): texto de cuerpo, etiquetas de columna.
- **Text Low** (`#5B6B7C` claro / `#6B8499` oscuro): metadatos, texto auxiliar, timestamps.

### Semantic (par texto + fondo, nunca solo uno)
- **Success / Official** (`#15803D` sobre `#F0FDF4`): resultado oficial, categoría en curso, "en línea".
- **Warning / Pending** (`#92400E` sobre `#FFFBEB`): pendiente de resolución, carrera terminada (en el sentido de "requiere revisión"), reconectando.
- **Critical / Dispute** (`#DC2626` sobre `#FEF2F2`): disputa abierta, error de validación, sin conexión.
- **Info** (`#1D4ED8` sobre `#EFF6FF`): estado neutro-informativo, distinto del acento interactivo aunque comparta familia de azul.

### Night Console (rail del sidebar — constante, no varía con el tema)
- **Console Background** (`#1C2333`): siempre oscuro, en ambos temas. No es un alias de `--bg-sb` del tema — es una decisión de identidad fija, documentada así en el propio CSS ("Night Console — always dark rail; do NOT alias to content --tx-*").
- **Console Foreground** (`#E2E8F0`) / **Console Muted** (`#94A3B8`, ≥4.5:1 sobre el fondo del rail).

### Marca no-semántica
- **Medallas de podio**: oro `#F59E0B`, plata `#94A3B8`, bronce `#B45309` — paleta propia, no reutiliza los tokens semánticos (una medalla de oro no significa "éxito del sistema").

### Named Rules
**The Night Console Rule.** El fondo del sidebar nunca seas alias del tema activo. Es oscuro en tema claro y en tema oscuro — es la identidad fija de la consola, no una superficie más.

**The Paired Semantic Rule.** Ningún estado (éxito/alerta/crítico/info) se comunica solo con el color de texto o solo con el de fondo — siempre viaja el par completo, y en las últimas correcciones de accesibilidad se agregó texto explícito además del color donde el estado era binario (ej. "Sin leer", "(vencido)").

## Typography

**Body Font:** Inter (con `system-ui, 'Segoe UI', sans-serif` de respaldo)
**Data/Label Font:** IBM Plex Mono (con `ui-monospace, monospace` de respaldo)

**Character:** Inter para todo el texto de lectura — neutral, muy legible a tamaños chicos, sin personalidad que compita con los datos. IBM Plex Mono se reserva estrictamente para datos que se alinean en columna: dorsales, tiempos, conteos de página, ritmo por km — nunca para prosa. La combinación no busca distinguirse por sí misma; busca que las cifras se puedan comparar de un vistazo.

### Hierarchy
- **Título de página** (600, 18px/`text-lg`): un `<h1>` por pantalla, siempre con el token `pageTitle` compartido.
- **Título de card/sección** (600, 14px/`text-sm`): encabezados de tabla, secciones de dashboard.
- **Body** (400, 14px/`text-sm`): texto de formularios, celdas de tabla, contenido general.
- **Label/eyebrow** (600, 11–12px, uppercase, +0.02em tracking): encabezados de columna de tabla, etiquetas de KPI.
- **Data/mono** (500, 14px, tabular-nums): dorsales, tiempos, ritmos, conteos — siempre con `font-variant-numeric: tabular-nums` para que las cifras alineen en columna.

### Named Rules
**The Mono-For-Data-Only Rule.** IBM Plex Mono aparece únicamente donde hay cifras que se comparan verticalmente. Si el texto se lee en prosa, es Inter — sin excepción.

## Layout

El shell tiene tres regiones fijas: sidebar (rail de navegación, Night Console), topbar (selector de carrera activa + acciones globales), y el área de contenido con scroll propio. El sidebar colapsa a 52px (solo íconos) o se expande a 210px (íconos + etiqueta), con la etiqueta alineada a la izquierda dentro del botón — nunca centrada. Arranca expandido por defecto en pantallas ≥1280px y colapsado en el resto, salvo que el usuario ya haya fijado una preferencia (persistida en `localStorage`). En mobile (≤640px) el sidebar se convierte en un drawer con scrim, no en un rail angosto.

El contenido de cada página vive en `flex flex-col gap-3/4/5` — el spacing entre bloques lo da el `gap` del contenedor, nunca márgenes acumulados en cada hijo. Las tablas de datos usan un layout dual: tarjetas apiladas en mobile (`<sm`), tabla real con scroll horizontal contenido en desktop (`≥sm`) — nunca overflow de página completa.

Densidad: los controles interactivos (botones, inputs, ítems de tabla) son compactos por defecto (`h-8`/`h-6`), y se agrandan a 44×44px bajo `@media (pointer: coarse)` para mantener el mismo layout visual en desktop mientras se cumple el mínimo táctil en tablet/mobile — la densidad no se sacrifica en pantallas grandes solo por dar soporte táctil.

## Elevation & Depth

Las superficies son planas en reposo — bordes de 1px sutiles (`--bd`) delimitan cards, tablas y separadores; la sombra nunca decora un elemento estático. `--shadow-sm` y `--shadow-md` existen y son deliberadamente casi imperceptibles (`0 1px 2px rgba(0,0,0,.05)` en claro), reservados para elementos que se despegan del flujo normal: dropdowns, modales, el KPI bar. La profundidad estructural entre regiones (app → card → hover) se transmite principalmente con capas de color (`--bg-app` → `--bg-card` → `--bg-hover`), no con sombra — la sombra es la excepción para overlays, el color de fondo es la regla para todo lo demás.

### Shadow Vocabulary
- **Ambient** (`--shadow-sm`): cards con estado hover, KPI bar. Casi imperceptible, un leve despegue.
- **Overlay** (`--shadow-md`): modales, dropdowns (menú de cuenta, notificaciones). La única sombra realmente visible del sistema.

### Named Rules
**The Flat-At-Rest Rule.** Ninguna superficie estática lleva sombra. Si algo tiene `--shadow-*` en reposo (no en hover/overlay), es una desviación del sistema, no un precedente a seguir.

## Shapes

Radios consistentes por rol, no por tamaño de componente: `--r-btn` (6px) para todo control interactivo (botones, inputs, selects), `--r-card` (7px) para superficies de contenido (cards, modales, tablas), `--r-badge` (20px, pill completo) exclusivamente para badges de estado. Los bordes son siempre de 1px y del color `--bd`/`--bd-inner` del tema activo — nunca un borde de color semántico salvo en badges (donde el borde SÍ lleva el color del estado, ej. `--ok-bd`, `--er-bd`) y en el borde lateral de severidad de las filas de disputa (`border-left` de 3px con el color del estado — un uso deliberado y verificado como legítimo, no un antipatrón genérico de "card con barra lateral").

## Components

### Buttons
- **Shape:** `--r-btn` (6px), altura fija por tamaño (`sm`: 24px base / 44px táctil, `md`: 32px base / 44px táctil).
- **Primary:** fondo Signal Blue, texto blanco — la única acción "hazlo ahora" de cada pantalla.
- **Secondary** (default): fondo de superficie, borde neutro, texto alto-contraste — la mayoría de los botones del sistema.
- **Destructive:** fondo/texto en la pareja crítica (`--er-bg`/`--er-tx`) — nunca un botón sólido rojo, mantiene el mismo patrón "par semántico suave" que el resto del sistema.
- **Info:** fondo/texto en la pareja de éxito — usado para acciones confirmatorias no destructivas.
- **Focus:** anillo de foco de 2px en Signal Blue, offset 1px, visible en todos los tamaños y variantes.

### Badges (StatusBadge)
- **Shape:** pill completo (`--r-badge`, 20px).
- **Estado:** cada `RaceStatus`/`ResultEstado` mapea a un par semántico completo (fondo+borde+texto) — nunca solo texto de color sobre fondo neutro.
- **Vivo:** el estado "En curso" agrega un punto pulsante (`pulse-live`, 2s) además del color — doble codificación para el estado más importante de la app.

### Cards / Containers
- **Corner Style:** `--r-card` (7px).
- **Background:** `--bg-card`, borde 1px `--bd`.
- **Shadow Strategy:** ninguna en reposo — ver Elevation & Depth.
- **Padding interno:** 14–16px.

### Inputs / Fields
- **Style:** borde 1px `--bd`, fondo `--bg-input`, radio `--r-btn`.
- **Focus:** anillo de 1px en el color del acento (o crítico si el campo es inválido).
- **Requerido:** asterisco visual junto al label (`aria-hidden`, el `required` nativo del input es lo que realmente lo anuncia a lectores de pantalla) — todo campo obligatorio lleva los dos en paralelo, nunca uno solo.
- **Error:** mensaje en `--er-tx` con `role="alert"` — nunca solo color, siempre anunciado.

### Navigation (Night Console)
- **Estilo:** rail vertical, fondo siempre oscuro (`--sb-fg`/`--sb-muted` sobre `#1C2333`), independiente del tema del resto de la app.
- **Estados:** ítem activo con fondo `--sb-active-bg` + texto en Signal Blue; hover con `--sb-hover`; etiqueta de texto alineada a la izquierda cuando el rail está expandido, oculta cuando está colapsado (solo ícono + tooltip).
- **Mobile:** el rail se convierte en drawer de ancho fijo con scrim, siempre con etiquetas visibles (sin depender de hover).
- **Accesibilidad:** primer elemento tabulable de la página es un skip-link oculto hasta foco ("Saltar al contenido"), antes que cualquier ítem del rail.

### DataTable
- **Layout dual:** tarjetas apiladas `<sm`, tabla real `≥sm` con scroll horizontal contenido en su propio wrapper.
- **Encabezado:** `<th scope="col">`, texto uppercase pequeño en `--tx-th`.
- **Paginación:** controles tokenizados con las mismas variables que el resto del componente (nunca colores Tailwind fijos tipo `bg-white`/`text-gray-*` — fue justamente el bug más visible detectado en la auditoría de theming).

## Do's and Don'ts

### Do:
- **Do** usar los tokens CSS (`var(--tx-hi)`, `var(--bg-card)`, etc.) o los estilos compartidos de `theme/styles.ts` (`pageTitle`, `cardTitle`, `card`) para cualquier color/superficie — nunca un hex literal ni una clase de paleta fija de Tailwind (`text-gray-700`, `bg-blue-600`) para algo que deba reaccionar al tema.
- **Do** duplicar el par completo fondo+texto+borde de un token semántico (`--ok-*`, `--er-*`, etc.) — nunca solo uno de los tres.
- **Do** anunciar cada mensaje de error con `role="alert"`, cada campo requerido con `required` nativo + asterisco visual en el `Label`, y cada estado binario (leído/no-leído, vencido/vigente) con texto además de color.
- **Do** reservar IBM Plex Mono para datos tabulares/comparables; todo lo demás es Inter.
- **Do** mantener el sidebar (`--bg-sb`/`--sb-*`) oscuro sin importar el tema — es una decisión de identidad, no un descuido.
- **Do** revisar `@media (pointer: coarse)` al agregar un control interactivo nuevo — el mínimo táctil de 44px aplica a todo botón/ícono clicable.

### Don't:
- **Don't** usar clases de paleta fija de Tailwind (`bg-white`, `text-gray-700`, `bg-blue-600`) en ningún componente que se muestre en ambos temas — es el bug de theming más recurrente encontrado en la auditoría (rompía el pie de paginación de `DataTable` en modo oscuro).
- **Don't** agregar sombra a una superficie en reposo — la sombra es señal de overlay/hover, no decoración.
- **Don't** dejar un control clicable como `<div>`/`<span>`/`<tr onClick>` sin `role`, `tabIndex` y manejo de teclado — si es clicable con mouse, tiene que ser operable con teclado.
- **Don't** dejar CSS sin usar "por las dudas" — el sistema tuvo un sistema de toasts custom completo (`~70 líneas`) que quedó huérfano tras migrar a `react-hot-toast` y nadie lo notó hasta la auditoría; verificar con grep antes de asumir que una clase está en uso.
