# Chunk Application - Distributed File Processing System

Bu proje, dosyaları chunk'lara bölen ve RabbitMQ kullanarak distributed processing yapan bir sistemdir.

## Mimari

Proje iki ana bileşenden oluşur:

1. **ChunkService** (`ChunkApplication/`) - RabbitMQ consumer, dosya chunk işlemlerini yapar
2. **ChunkClient** (`ChunkClient/`) - Kullanıcı arayüzü, RabbitMQ publisher, menü ve dosya yönetimi

## Özellikler

- Dosya chunk'lama ve birleştirme
- RabbitMQ ile distributed processing
- SQL Server veritabanı desteği
- Docker containerization
- Logging ve monitoring

## Kurulum

### Gereksinimler

- .NET 8.0 SDK
- Docker ve Docker Compose
- SQL Server (Docker ile otomatik kurulum)

### Docker ile Çalıştırma

1. Projeyi klonlayın:
```bash
git clone <repository-url>
cd ChunkApplication
```

2. Docker Compose ile servisleri başlatın:
```bash
docker-compose up -d
```

Bu komut şu servisleri başlatacak:
- RabbitMQ (port 15672 - Web UI)
- SQL Server (port 1433)
- ChunkService (consumer)
- ChunkClient (publisher)

3. RabbitMQ Web UI'ya erişin:
   - URL: http://localhost:15672
   - Kullanıcı: admin
   - Şifre: admin123

### Manuel Çalıştırma

1. ChunkService'i çalıştırın:
```bash
cd ChunkApplication
dotnet run
```

2. Yeni bir terminal açın ve ChunkClient'i çalıştırın:
```bash
cd ChunkClient
dotnet run
```

## Kullanım

### ChunkClient Menüsü

ChunkClient çalıştığında şu menüyü göreceksiniz:

1. **Send chunk file request** - Dosya chunk'lama isteği gönder
2. **Send reconstruct file request** - Dosya birleştirme isteği gönder
3. **Send list files request** - Dosya listesi isteği gönder
4. **Send get file info request** - Dosya bilgisi isteği gönder
5. **Send delete file request** - Dosya silme isteği gönder
6. **Exit** - Çıkış

### Örnek Kullanım

1. **Dosya Chunk'lama:**
   - Menüden "1" seçin
   - Chunk'lanacak dosya yolunu girin
   - İstek RabbitMQ'ya gönderilecek ve ChunkService tarafından işlenecek

2. **Dosya Birleştirme:**
   - Menüden "2" seçin
   - File ID'yi girin
   - Çıktı dosya adını girin
   - Dosya Desktop/ChunkApplication_Output klasörüne birleştirilecek

## RabbitMQ Queue'ları

Sistem şu queue'ları kullanır:

- `chunk-file-request` - Dosya chunk'lama istekleri
- `reconstruct-file-request` - Dosya birleştirme istekleri
- `list-files-request` - Dosya listesi istekleri
- `get-file-info-request` - Dosya bilgisi istekleri
- `delete-file-request` - Dosya silme istekleri

## Loglar

- ChunkService logları: `ChunkApplication/logs/`
- ChunkClient logları: Console output
- RabbitMQ logları: Docker container logs

## Docker Komutları

```bash
# Servisleri başlat
docker-compose up -d

# Logları görüntüle
docker-compose logs -f

# Belirli servisin loglarını görüntüle
docker-compose logs -f chunk-service
docker-compose logs -f chunk-client

# Servisleri durdur
docker-compose down

# Servisleri yeniden başlat
docker-compose restart
```

## Geliştirme

### Yeni Consumer Ekleme

1. `ChunkApplication/Consumers/` klasöründe yeni consumer sınıfı oluşturun
2. `Program.cs`'de consumer'ı kaydedin
3. Docker Compose'u yeniden başlatın

### Yeni Message Type Ekleme

1. Message sınıfını oluşturun
2. Consumer'ı oluşturun
3. ChunkClient'te publisher metodunu ekleyin

## Sorun Giderme

### RabbitMQ Bağlantı Hatası
- RabbitMQ container'ının çalıştığından emin olun
- Port 5672'nin açık olduğunu kontrol edin
- Kullanıcı adı/şifre bilgilerini kontrol edin

### Veritabanı Bağlantı Hatası
- SQL Server container'ının çalıştığından emin olun
- Port 1433'ün açık olduğunu kontrol edin
- Connection string'i kontrol edin

### ChunkService Çalışmıyor
- Logları kontrol edin: `docker-compose logs chunk-service`
- Consumer'ların doğru kaydedildiğinden emin olun
- RabbitMQ bağlantısını kontrol edin

## Lisans

Bu proje MIT lisansı altında lisanslanmıştır.
