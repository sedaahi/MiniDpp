# Mini Dijital Ürün Pasaportu

CSV’den ürün bilgilerini içe aktaran, doğrulayan ve dijital ürün pasaportu
oluşturan full-stack uygulama. HTML pasaport, JSON-LD, QR kod ve ürün
sürüm geçmişi sunar.

**Teknolojiler:** .NET 10, EF Core, SQLite, React 19, TypeScript, Vite, Razor ve QRCoder.

## Kurulum ve çalıştırma

**Gereksinimler:** .NET 10 SDK, Node.js 24 LTS ve npm.
SQLite için ayrıca sunucu kurulması gerekmez.

### 1. Depoyu indirin

```bash
git clone https://github.com/sedaahi/MiniDpp.git
cd MiniDpp
```

Bu klasör depo köküdür; `MiniDpp`, `client`, `samples` ve `README.md` içerir.

### 2. Backend’i hazırlayın

```bash
cd MiniDpp
dotnet restore
```

EF Core CLI kurulu değilse:

```bash
dotnet tool install --global dotnet-ef --version 10.0.14
```

**İlk çalıştırmadan önce veritabanını ve tabloları oluşturun:**

```bash
dotnet ef database update
```

Mevcut migration’lar depoya dahildir; yeni migration oluşturmayın.

### 3. Backend’i başlatın

```bash
dotnet run --no-launch-profile --urls http://localhost:5016
```

Bu terminali açık bırakın.

### 4. Frontend’i başlatın

**Depo kökünde ikinci bir terminal açın:**

```bash
cd client
npm ci
npm run dev
```

Tarayıcıda **http://localhost:5173** adresini açın.
İki uygulama da çalışıyor olmalıdır. Vite, `/api` ve `/dpp` isteklerini
backend’e yönlendirir.

### 5. Örnek verileri yükleyin

1. İlk kurulumda ürün listesinin boş olması normaldir.
2. “Dosya Seç” ile `samples/products.csv` dosyasını seçip “İçe aktar”a basın.
3. Üç geçerli ürün eklenir; iki hatalı satır açıklamalarıyla gösterilir.
4. “Pasaportu aç” bağlantısıyla ürün bilgilerini ve QR kodunu görüntüleyin.
5. `samples/products-update.csv` dosyasını yükleyin. Mom Jean 505’in üretim
   yeri Bursa’dan İzmir’e güncellenir.
6. “Geçmiş” düğmesiyle eski ve güncel sürümleri inceleyin.
   Aynı dosyayı tekrar yüklemek yeni sürüm oluşturmaz.

Sonraki açılışlarda yalnızca backend ve frontend başlatma komutlarını
çalıştırmanız yeterlidir. Kaydedilen veriler SQLite dosyasında korunur.

## CSV ve doğrulama

Dosya **UTF-8, virgülle ayrılmış** olmalı; ondalık değerler noktayla yazılmalıdır.
GTIN’lerin başındaki sıfırlar korunmalıdır.

```csv
gtin,model_adi,batch_no,elyaf_pamuk_yuzde,elyaf_elastan_yuzde,uretim_yeri,karbon_kg_co2e
08690000000050,Mom Jean 505,B2027-05,97,3,Bursa,12.2
```

- GTIN: 14 rakam ve geçerli kontrol basamağı.
- Model adı, parti numarası ve üretim yeri: zorunlu.
- Pamuk ve elastan: 0–100 aralığında, toplamları 100.
- Karbon: sayısal ve negatif olmayan değer.

Geçerli satırlar kaydedilir; hatalı satırlar numarasıyla raporlanır.
Başlık satırı 1’dir. Eksik başlık varsa dosya reddedilir.
Aynı dosyada tekrarlanan geçerli GTIN’lerin yalnızca ilki işlenir.

## Pasaport ve çıktılar

Örnek veriler yüklendikten sonra:

| Çıktı | Adres |
| --- | --- |
| HTML pasaport | http://localhost:5016/dpp/08690000000050 |
| JSON-LD | http://localhost:5016/dpp/08690000000050.jsonld |
| QR PNG | http://localhost:5016/dpp/08690000000050/qr.png |
| Sürüm geçmişi | http://localhost:5016/dpp/08690000000050/history |

JSON-LD, Schema.org `Product` biçiminde ve `application/ld+json` içerik
türüyle döner. Bulunamayan ürün için 404 yanıtı verilir.

**Karbon bilgisi:** Normal çıktılarda gizlidir. HTML, JSON-LD veya geçmiş
adresine `?rol=yetkili` eklenerek gösterilir. Bu, ödev kapsamındaki rol
gösterimidir; gerçek kimlik doğrulama sağlamaz. Karbon değeri CSV’den
alınır, uygulama tarafından hesaplanmaz.

## Veri ve sürüm geçmişi

Modeller, güncel ürün/parti kayıtları ve eski sürümler ayrı tablolarda tutulur.
GTIN benzersizdir; aynı GTIN ile değişen bilgiler geldiğinde eski durum
saklanır, sürüm artırılır ve UTC güncelleme tarihi kaydedilir.
Değişmeyen kayıtlar atlanır.

Arşivleme ve güncelleme birlikte kaydedilir. Sürüm alanı eş zamanlı
güncelleme çakışmalarının algılanmasında kullanılır.
Arayüz tarihleri Türkiye saatinde gösterir.

SQLite dosyası Git’e eklenmez. Bağlantı ayarı
`MiniDpp/appsettings.json` içindeki `ConnectionStrings:MiniDpp` alanıdır.

## Telefonda QR testi

Telefon ve bilgisayar aynı Wi-Fi ağına bağlıyken backend’i başlatın.
Önceden çalışan backend varsa önce durdurun:

```bash
# Depo kökünden:
cd MiniDpp
dotnet run --no-launch-profile --urls http://0.0.0.0:5016
```

Bilgisayarda aşağıdaki adresi kendi yerel IP’nizle açıp sayfadaki QR’ı okutun:

```text
http://<bilgisayar-ip-adresi>:5016/dpp/08690000000050
```

QR, ürünün HTML pasaportuna yönlendirir. `localhost` ile oluşturulan QR
telefonda çalışmaz; bilgisayar ve backend açık kalmalıdır.

## Kontroller ve sorun giderme

Depo kökünden:

```bash
dotnet build MiniDpp/MiniDpp.csproj
cd client
npm run build
npm run lint
```

**`no such table: ProductBatches` hatası:** Backend’i durdurun,
backend klasöründe `dotnet ef database update` çalıştırıp yeniden başlatın.

**Ürün listesi boş:** `samples/products.csv` dosyasını yükleyin.

**CSV başlık hatası:** Dosyanın gerçekten virgülle ayrılmış CSV olduğunu
kontrol edin. Numbers/Excel dışa aktarırken ayırıcıyı ve GTIN’lerin başındaki
sıfırları değiştirebilir.

Proje yerel değerlendirme içindir. Otomatik test paketi bulunmaz;
derleme, lint ve manuel akış kontrolleri kullanılır. Üretim yayını için
ayrıca barındırma ve yönlendirme yapılandırması gerekir.
