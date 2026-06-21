import { useRef, useState } from 'react'
import { importRunnersExcel } from '../../api/endpoints'
import type { ImportRunnersResultDto } from '../../api/types'

interface ImportExcelModalProps {
  raceId: number
  onClose: () => void
  onImported: () => void
}

export function ImportExcelModal({ raceId, onClose, onImported }: ImportExcelModalProps) {
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [result, setResult] = useState<ImportRunnersResultDto | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  async function handleUpload() {
    const file = fileInputRef.current?.files?.[0]
    if (!file) {
      setError('Selecciona un archivo .xlsx primero.')
      return
    }
    setError(null)
    setSubmitting(true)
    try {
      const data = await importRunnersExcel(raceId, file)
      setResult(data)
      if (data.importados > 0) onImported()
    } catch {
      setError('No se pudo procesar el archivo. Verifica el formato.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="fixed inset-0 flex items-center justify-center bg-black/30">
      <div className="w-full max-w-lg rounded-lg bg-white p-6 shadow-lg">
        <h2 className="mb-4 text-base font-semibold text-gray-900">Importar corredores desde Excel</h2>

        <p className="mb-3 text-sm text-gray-500">
          Columnas esperadas: Nombre, Dorsal, Teléfono, Email, Edad, Categoría, Distancia.
        </p>

        <input
          ref={fileInputRef}
          type="file"
          accept=".xlsx"
          className="mb-3 w-full text-sm"
        />

        {error && <p className="mb-3 text-sm text-red-600">{error}</p>}

        {result && (
          <div className="mb-3 rounded border border-gray-200 p-3 text-sm">
            <p>
              Total filas: <strong>{result.totalFilas}</strong> — Importados:{' '}
              <strong className="text-teal-700">{result.importados}</strong> — Errores:{' '}
              <strong className="text-red-700">{result.errores.length}</strong>
            </p>
            {result.errores.length > 0 && (
              <ul className="mt-2 max-h-40 list-disc overflow-y-auto pl-5 text-gray-600">
                {result.errores.map((err) => (
                  <li key={err.fila}>
                    Fila {err.fila}: {err.motivo}
                  </li>
                ))}
              </ul>
            )}
          </div>
        )}

        <div className="flex justify-end gap-2">
          <button
            type="button"
            onClick={onClose}
            className="rounded border border-gray-300 px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
          >
            Cerrar
          </button>
          <button
            type="button"
            onClick={handleUpload}
            disabled={submitting}
            className="rounded bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-60"
          >
            {submitting ? 'Importando...' : 'Importar'}
          </button>
        </div>
      </div>
    </div>
  )
}
