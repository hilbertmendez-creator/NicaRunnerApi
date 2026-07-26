# LoadingText

## When to use

A lightweight, inline loading indicator for content that takes a moment to arrive — a table's `isLoading` state (DataTable renders this internally), a panel refreshing after a save, a background sync. Not meant as a full-page loading screen.

## Accessibility

Carries `role="status"` and `aria-live="polite"`, so the message is announced to screen readers without interrupting whatever they're currently reading (unlike `aria-live="assertive"`, which would cut in).

## Usage

Default message is "Cargando..." — override `message` when a more specific label helps ("Cargando dashboard...", "Guardando cambios..."). Keep it short; this isn't a progress-percentage indicator, it's a "something is happening" signal.
