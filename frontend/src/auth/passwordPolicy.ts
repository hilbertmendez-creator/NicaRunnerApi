// Debe reflejar StrongPasswordAttribute (backend): mínimo 8 caracteres con
// mayúscula, minúscula, número y símbolo.
const PASSWORD_POLICY_REGEX = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$/

export const PASSWORD_POLICY_HINT =
  'Mínimo 8 caracteres, con mayúscula, minúscula, número y símbolo.'

export function isStrongPassword(password: string): boolean {
  return PASSWORD_POLICY_REGEX.test(password)
}
