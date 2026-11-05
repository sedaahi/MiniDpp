import { useEffect, useState } from 'react'

type Product = {
  gtin: string
  modelName: string
  batchNumber: string
  productionLocation: string
}

type RowError = {
  lineNumber: number
  errors: string[]
}

type ImportResult = {
  imported: number
  updated: number
  skipped: number
  errors: RowError[]
}

type ProductVersion = {
  version: number
  updatedAtUtc: string | null
  isCurrent: boolean
  modelName: string
  batchNumber: string
  productionLocation: string
  cottonPercentage: number
  elastanePercentage: number
}

type ProductHistory = {
  gtin: string
  versions: ProductVersion[]
}

function formatDate(value: string | null) {
  if (!value) return 'Tarih kaydedilmemiş'

  return new Intl.DateTimeFormat('tr-TR', {
    dateStyle: 'short',
    timeStyle: 'medium',
    timeZone: 'Europe/Istanbul',
  }).format(new Date(value))
}

export default function App() {
  const [products, setProducts] = useState<Product[] | null>(null)
  const [listError, setListError] = useState('')
  const [refreshCount, setRefreshCount] = useState(0)

  const [file, setFile] = useState<File | null>(null)
  const [isUploading, setIsUploading] = useState(false)
  const [uploadError, setUploadError] = useState('')
  const [importResult, setImportResult] = useState<ImportResult | null>(null)

  const [history, setHistory] = useState<ProductHistory | null>(null)
  const [historyError, setHistoryError] = useState('')
  const [isHistoryLoading, setIsHistoryLoading] = useState(false)

  useEffect(() => {
    const controller = new AbortController()

    async function loadProducts() {
      try {
        const response = await fetch('/api/products', {
          signal: controller.signal,
        })

        if (!response.ok) {
          throw new Error(`HTTP ${response.status}`)
        }

        const data: Product[] = await response.json()

        if (!controller.signal.aborted) {
          setProducts(data)
          setListError('')
        }
      } catch {
        if (!controller.signal.aborted) {
          setListError('Ürünler yüklenemedi. .NET uygulamasını kontrol edin.')
        }
      }
    }

    void loadProducts()
    return () => controller.abort()
  }, [refreshCount])

  async function loadHistory(gtin: string) {
    if (isHistoryLoading || isUploading) return

    setIsHistoryLoading(true)
    setHistory(null)
    setHistoryError('')

    try {
      const response = await fetch(
        `/dpp/${encodeURIComponent(gtin)}/history`,
      )

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`)
      }

      const data: ProductHistory = await response.json()
      setHistory(data)
    } catch {
      setHistoryError('Sürüm geçmişi yüklenemedi. Tekrar deneyin.')
    } finally {
      setIsHistoryLoading(false)
    }
  }

  async function uploadCsv() {
    if (!file || isUploading || isHistoryLoading) return

    setIsUploading(true)
    setUploadError('')
    setImportResult(null)

    const formData = new FormData()
    formData.append('file', file)

    try {
      const response = await fetch('/api/imports/csv', {
        method: 'POST',
        body: formData,
      })

      if (!response.ok) {
        const problem: { message?: string; errors?: RowError[] } | null =
          await response.json().catch(() => null)

        const rowErrors = Array.isArray(problem?.errors)
          ? problem.errors
              .map((row) => `Satır ${row.lineNumber}: ${row.errors.join(' ')}`)
              .join(' ')
          : ''

        setUploadError(
          rowErrors ||
            problem?.message ||
            `CSV yüklenemedi (HTTP ${response.status}).`,
        )
        return
      }

      const result: ImportResult = await response.json()
      setImportResult(result)
      setHistory(null)
      setHistoryError('')
      setRefreshCount((count) => count + 1)
    } catch {
      setUploadError('CSV yüklenemedi. .NET uygulamasını kontrol edin.')
    } finally {
      setIsUploading(false)
    }
  }

  return (
    <main>
      <h1>Mini Dijital Ürün Pasaportu</h1>

      <section aria-labelledby="import-title">
        <h2 id="import-title">CSV içe aktar</h2>

        <form
          onSubmit={(event) => {
            event.preventDefault()
            void uploadCsv()
          }}
        >
          <label htmlFor="csv-file">CSV dosyası</label>
          <input
            id="csv-file"
            type="file"
            accept=".csv,text/csv"
            onChange={(event) => {
              setFile(event.currentTarget.files?.[0] ?? null)
              setUploadError('')
              setImportResult(null)
            }}
            disabled={isUploading || isHistoryLoading}
            required
          />
          <button
            type="submit"
            disabled={!file || isUploading || isHistoryLoading}
          >
            {isUploading ? 'Yükleniyor…' : 'İçe aktar'}
          </button>
        </form>

        {uploadError && <p role="alert">{uploadError}</p>}

        <div aria-live="polite">
          {importResult && (
            <>
              <p>
                {importResult.imported} yeni kayıt eklendi.{' '}
                {importResult.updated} kayıt güncellendi.{' '}
                {importResult.skipped} değişmeyen kayıt atlandı.
              </p>

              {importResult.errors.length > 0 && (
                <ul>
                  {importResult.errors.map((row) => (
                    <li key={row.lineNumber}>
                      Satır {row.lineNumber}: {row.errors.join(' ')}
                    </li>
                  ))}
                </ul>
              )}
            </>
          )}
        </div>
      </section>

      <section aria-labelledby="products-title">
        <h2 id="products-title">Kayıtlı ürünler</h2>

        {listError && <p role="alert">{listError}</p>}
        {!listError && products === null && (
          <p role="status">Ürünler yükleniyor…</p>
        )}
        {products?.length === 0 && <p>Henüz kayıtlı ürün yok.</p>}

        {products && products.length > 0 && (
          <table>
            <thead>
              <tr>
                <th scope="col">Model</th>
                <th scope="col">GTIN</th>
                <th scope="col">Parti numarası</th>
                <th scope="col">Üretim yeri</th>
                <th scope="col">Ürün pasaportu</th>
                <th scope="col">Sürümler</th>
              </tr>
            </thead>
            <tbody>
              {products.map((product) => (
                <tr key={product.gtin}>
                  <td>{product.modelName}</td>
                  <td>{product.gtin}</td>
                  <td>{product.batchNumber}</td>
                  <td>{product.productionLocation}</td>
                  <td>
                    <a href={`/dpp/${product.gtin}`}>Pasaportu aç</a>
                  </td>
                  <td>
                    <button
                      type="button"
                      disabled={isHistoryLoading || isUploading}
                      onClick={() => void loadHistory(product.gtin)}
                      aria-label={`${product.modelName} sürüm geçmişini göster`}
                    >
                      Geçmiş
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      {(isHistoryLoading || historyError || history) && (
        <section aria-labelledby="history-title" aria-busy={isHistoryLoading}>
          <h2 id="history-title">Sürüm geçmişi</h2>

          {isHistoryLoading && (
            <p role="status">Sürüm geçmişi yükleniyor…</p>
          )}

          {historyError && <p role="alert">{historyError}</p>}

          {history && (
            <>
              <p>
                GTIN: <strong>{history.gtin}</strong>
              </p>
              <p>Tarihler Türkiye saatine göre gösterilir.</p>

              <table>
                <thead>
                  <tr>
                    <th scope="col">Sürüm</th>
                    <th scope="col">Güncelleme tarihi</th>
                    <th scope="col">Model</th>
                    <th scope="col">Parti</th>
                    <th scope="col">Üretim yeri</th>
                    <th scope="col">Malzeme bileşimi</th>
                  </tr>
                </thead>
                <tbody>
                  {history.versions.map((version) => (
                    <tr key={version.version}>
                      <td>
                        {version.version}
                        {version.isCurrent ? ' (Güncel)' : ''}
                      </td>
                      <td>{formatDate(version.updatedAtUtc)}</td>
                      <td>{version.modelName}</td>
                      <td>{version.batchNumber}</td>
                      <td>{version.productionLocation}</td>
                      <td>
                        %{version.cottonPercentage} pamuk,{' '}
                        %{version.elastanePercentage} elastan
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>

              <button
                type="button"
                onClick={() => setHistory(null)}
                style={{ marginTop: '16px' }}
              >
                Geçmişi kapat
              </button>
            </>
          )}
        </section>
      )}
    </main>
  )
}