# Mini Dijital Ürün Pasaportu

CSV dosyasından ürün bilgilerini içe aktaran, doğrulayan ve dijital ürün
pasaportu oluşturan full-stack uygulama.

Ürün bilgileri HTML ve JSON-LD olarak sunulur. Her ürün için pasaport
adresini içeren PNG biçiminde QR kod oluşturulur. Değişen kayıtların
önceki sürümleri korunur.

## Teknolojiler

- ASP.NET Core / .NET 10
- Entity Framework Core 10 ve SQLite
- React 19 ve TypeScript
- Vite
- QRCoder
- Razor Views

React, CSV yükleme ve ürün yönetim ekranını sağlar. Razor, doğrudan
bağlantıyla ve QR üzerinden açılan ürün pasaportunu oluşturur.

## Gereksinimler

- .NET 10 SDK
- .NET 10 ile uyumlu Node.js ortamından bağımsız olarak Node.js 24 LTS ve npm
- Entity Framework Core CLI: dotnet-ef 10.0.14

SQLite için ayrıca veritabanı sunucusu kurulması gerekmez.

## Kurulum ve çalıştırma

Depoyu indirdikten sonra depo kökünde bir terminal açın.

### Backend

```bash
cd MiniDpp
dotnet restore
```

EF Core CLI kurulu değilse:

```bash
dotnet tool install --global dotnet-ef --version 10.0.14
```

Global araç zaten kuruluysa ve sürümünü eşitlemek isterseniz:

```bash
dotnet tool update --global dotnet-ef --version 10.0.14
```

Veritabanını oluşturup backend'i başlatın:

```bash
dotnet ef database update
dotnet run --no-launch-profile --urls http://localhost:5016
```

Migration dosyaları depoya dahildir. Mevcut projeyi çalıştırmak için
yeni migration oluşturmak gerekmez.

Bağlantı ayarı `MiniDpp/appsettings.json` dosyasındadır:

```json
{
  "ConnectionStrings": {
    "MiniDpp": "Data Source=minidpp.db"
  }
}
```

Bu bölüm mevcut yapılandırma dosyasındaki diğer ayarlarla birlikte kullanılır.

### Frontend

Depo kökünden ikinci bir terminal açın:

```bash
cd client
npm ci
npm run dev
```

Uygulamayı açın:

http://localhost:5173

Geliştirme ortamında Vite, `/api` ve `/dpp` isteklerini
`http://localhost:5016` adresindeki backend'e yönlendirir.
Her iki uygulama da çalışıyor olmalıdır.

## Kullanım

1. Ana sayfada CSV dosyasını seçin.
2. “İçe aktar” düğmesine basın.
3. Eklenen, güncellenen ve değişmediği için atlanan kayıt sayılarını inceleyin.
4. Geçersiz satırların numaralarını ve hata açıklamalarını kontrol edin.
5. Ürün listesinden “Pasaportu aç” bağlantısına tıklayın.
6. “Geçmiş” düğmesiyle ürünün güncel ve önceki sürümlerini görüntüleyin.

## CSV biçimi

Dosya UTF-8 kodlamalı ve virgülle ayrılmış olmalıdır.
Ondalık sayılar nokta ile yazılır.

```csv
gtin,model_adi,batch_no,elyaf_pamuk_yuzde,elyaf_elastan_yuzde,uretim_yeri,karbon_kg_co2e
08690000000050,Mom Jean 505,B2027-05,97,3,Bursa,12.2
```

GTIN metin olarak değerlendirilir; başındaki sıfırlar korunmalıdır.
Numbers veya Excel kullanılıyorsa dışa aktarma sırasında ayırıcıya ve
GTIN hücrelerinin metin olarak korunmasına dikkat edilmelidir.
Dosya uzantısını değiştirmek CSV'ye dönüştürmez.

### Doğrulama kuralları

- GTIN, 14 rakamdan oluşmalı ve kontrol basamağı geçerli olmalıdır.
- Model adı, parti numarası ve üretim yeri boş olamaz.
- Pamuk ve elastan oranları sayısal ve 0–100 aralığında olmalıdır.
- Pamuk ve elastan oranlarının toplamı 100 olmalıdır.
- Karbon değeri sayısal ve sıfır veya pozitif olmalıdır.
- Gerekli sütun başlıkları bulunmalıdır.

Başlık satırı 1 olarak sayılır. Geçersiz veri satırları kaydedilmez;
geçerli satırlar işlenir. Başlık hatasında dosya reddedilir.

Aynı dosyada aynı GTIN için birden fazla geçerli satır varsa ilk geçerli
satır işlenir, sonraki tekrarlar satır hatası olarak raporlanır.

## Örnek dosyalar ve güncelleme testi

- `samples/products.csv`: Üç geçerli ve iki geçersiz satır içerir.
- `samples/products-update.csv`: Mom Jean 505 ürününün üretim yerini
  Bursa'dan İzmir'e değiştirir.

Boş veritabanında ilk dosya yüklendiğinde:

- 3 kayıt eklenir.
- 4. satırdaki boş GTIN raporlanır.
- 5. satırdaki malzeme oranı toplamı hatası raporlanır.

Ardından güncelleme dosyası yüklendiğinde:

- 1 kayıt güncellenir.
- 2 değişmeyen kayıt atlanır.
- Aynı iki geçersiz satır raporlanır.
- Mom Jean 505 için Bursa bilgisi eski sürümde korunur.

Güncelleme dosyası tekrar yüklendiğinde yeni sürüm oluşturulmaz;
üç geçerli kayıt da değişmediği için atlanır.

Bu sayılar yukarıdaki dosya sırasına ve başlangıçta boş veritabanına göre
beklenen sonuçlardır.

## Ürün pasaportu ve çıktılar

Aşağıdaki bağlantılar örnek GTIN içe aktarıldıktan sonra kullanılabilir.


| Çıktı               | Adres                                                       |
| ---------------------- | ----------------------------------------------------------- |
| HTML pasaport          | http://localhost:5016/dpp/08690000000050                    |
| Yetkili görünümü   | http://localhost:5016/dpp/08690000000050?rol=yetkili        |
| JSON-LD                | http://localhost:5016/dpp/08690000000050.jsonld             |
| Yetkili JSON-LD        | http://localhost:5016/dpp/08690000000050.jsonld?rol=yetkili |
| QR PNG                 | http://localhost:5016/dpp/08690000000050/qr.png             |
| Sürüm geçmişi JSON | http://localhost:5016/dpp/08690000000050/history            |

JSON-LD, `https://schema.org` bağlamında `Product` olarak oluşturulur
ve `application/ld+json` içerik türüyle döner.

Kayıt bulunamayan GTIN için 404 yanıtı döner.

## Karbon bilgisi ve rol gösterimi

Karbon değeri CSV'den alınır; uygulama karbon ayak izi hesaplamaz.

Normal HTML, JSON-LD ve sürüm geçmişi çıktılarında karbon alanı
gösterilmez. `?rol=yetkili` parametresi ile bu alan dahil edilir.

Bu parametre teknik ödev kapsamındaki rol gösterimidir.
Gerçek kimlik doğrulama veya erişim yetkilendirmesi sağlamaz.

## Sürüm geçmişi

- Yeni ürün sürüm 1 olarak oluşturulur.
- Aynı GTIN'in bilgileri değiştiğinde önceki durum arşivlenir.
- Güncel kaydın sürüm numarası artırılır ve UTC güncelleme tarihi tutulur.
- Değişmeyen bilgiler yeniden yüklendiğinde sürüm artırılmaz.
- Model adı da geçmiş kayda kopyalanır.
- Arşivleme ve güncelleme aynı SaveChanges işlemiyle birlikte kaydedilir.
- Sürüm alanı, eş zamanlı güncelleme çakışmalarını algılamak için
  concurrency token olarak kullanılır.

Arayüz tarihleri Türkiye saatine göre gösterir. Sürüm takibi eklenmeden
önce oluşturulmuş kayıtların bilinmeyen tarihleri uydurulmaz;
“Tarih kaydedilmemiş” olarak gösterilir.

## Veri modeli

- `ProductModels`: Ürün modelleri.
- `ProductBatches`: GTIN ile tanımlanan güncel ürün/parti bilgileri.
- `ProductBatchVersions`: Güncelleme öncesindeki sürümlerin kopyaları.

Bu uygulamada GTIN benzersizdir. Aynı GTIN altında birden fazla ayrı
partiyi eş zamanlı yönetmek kapsam dışındadır; aynı GTIN ile gelen
değişiklik mevcut kaydın yeni sürümü olarak değerlendirilir.

## Telefonda QR testi

Telefon ve bilgisayar aynı yerel ağda olmalıdır.

Backend'i yerel ağdan erişilebilir başlatın:

```bash
cd MiniDpp
dotnet run --no-launch-profile --urls http://0.0.0.0:5016
```

Bilgisayarın yerel IP adresini kullanarak pasaportu bilgisayarda açın:

```text
http://<bilgisayar-ip-adresi>:5016/dpp/08690000000050
```

Bu sayfadaki QR'ı telefonla okutun. QR, pasaport adresini içerir;
okutulduğunda ürünün HTML pasaportu açılır.

QR adresi istek yapılan sunucu adresinden üretilir. `localhost` üzerinden
oluşturulan QR başka cihazda çalışmaz. IP üzerinden oluşturulan QR için
bilgisayarın, backend'in ve yerel ağ erişiminin açık olması gerekir.

## Derleme ve kod kontrolü

Depo kökünden backend kontrolü:

```bash
dotnet build MiniDpp/MiniDpp.csproj
```

Frontend kontrolleri:

```bash
cd client
npm run build
npm run lint
```

`npm run build`, frontend'in üretim dosyalarını `client/dist` altında
oluşturur. Vite proxy ayarları geliştirme sunucusuna aittir;
üretim yayını için ayrıca backend yönlendirmesi gerekir.

## Kapsam ve varsayımlar

- Yerel geliştirme ve teknik değerlendirme için hazırlanmıştır.
- SQLite veritabanı dosyası depoya eklenmez; migration ile oluşturulur.
- Örnek veriler CSV yükleme üzerinden eklenir.
- Gerçek kullanıcı yönetimi ve canlı yayın yapılandırması kapsam dışındadır.
- Otomatik test paketi bulunmaz; derleme, lint ve manuel akış kontrolleri kullanılır.
