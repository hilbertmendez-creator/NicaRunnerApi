# ErrorAlert

## When to use

Surface a failed operation (save failed, network error, validation rejected by the server) near where it happened — inline above the form or table it relates to, not as a floating toast, unless the product elsewhere establishes a toast pattern.

## Accessibility

Carries `role="alert"`, so it's announced automatically to screen readers the moment it mounts — you don't need `aria-live` on top of it, and you don't need to manually focus it.

## Copy guidance

Always give the user something they can act on, not just a description of what broke:

- Good: "No se pudo conectar al servidor. Verifica tu conexión."
- Good: "El dorsal ingresado no existe en esta carrera."
- Avoid: "Error." / "Something went wrong." — names nothing, suggests no next step.

There is no retry button built in — if the failed action is retryable, add one alongside the message rather than relying on the user to re-trigger it manually.
