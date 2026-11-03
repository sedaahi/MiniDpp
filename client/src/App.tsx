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
  skipped: number
  errors: RowError[]
}

export default function App() {
  const [products, setProducts] = useState<Product[] | null>(null)
  const [listError, setListError] = useState('')
  const [refreshCount, setRefreshCount] = useState(0)

  const [file, setFile] = useState<File | null>(null)
  const [isUploading, setIsUploading] = useState(false)
  const [uploadError, setUploadError] = useState('')
  const [importResult, setImportResult] = useState<ImportResult | null>(null)

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

  async function uploadCsv() {
    if (!file || isUploading) return

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
            onChange={(event) => setFile(event.currentTarget.files?.[0] ?? null)}
            disabled={isUploading}
            required
          />
          <button type="submit" disabled={!file || isUploading}>
            {isUploading ? 'Yükleniyor…' : 'İçe aktar'}
          </button>
        </form>

        {uploadError && <p role="alert">{uploadError}</p>}

        <div aria-live="polite">
          {importResult && (
            <>
              <p>
                {importResult.imported} yeni kayıt eklendi.{' '}
                {importResult.skipped} mevcut kayıt atlandı.
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
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </main>
  )
}