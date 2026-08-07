// shareKey codec: opaque, URL-safe base64url of `${token}|${runnerId}`.
// Design D5 — reuses the existing runner-by-token lookup; the key is self-contained.

function toBase64Url(plain: string): string {
  return btoa(plain).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
}

function fromBase64Url(key: string): string {
  const base64 = key
    .replace(/-/g, '+')
    .replace(/_/g, '/')
    .concat('='.repeat(((4 - (key.length % 4)) % 4)))
  return atob(base64)
}

export function encodeShareKey(token: string, runnerId: number): string {
  return toBase64Url(`${token}|${runnerId}`)
}

export function decodeShareKey(key: string): { token: string; runnerId: number } | null {
  let plain: string
  try {
    plain = fromBase64Url(key)
  } catch {
    return null
  }
  const sep = plain.lastIndexOf('|')
  if (sep <= 0 || sep === plain.length - 1) return null
  const token = plain.slice(0, sep)
  const runnerIdText = plain.slice(sep + 1)
  if (!token || !/^\d+$/.test(runnerIdText)) return null
  const runnerId = Number(runnerIdText)
  if (!Number.isSafeInteger(runnerId) || runnerId <= 0) return null
  return { token, runnerId }
}