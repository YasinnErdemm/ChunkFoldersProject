# ChunkApplication - Distributed File Chunking System

## 📋 Proje Açıklaması

Bu proje, büyük dosyaların otomatik olarak küçük parçalara (chunk) ayrılması, bu parçaların farklı depolama sağlayıcılarına dağıtılması ve gerektiğinde birleştirilerek dosya bütünlüğünün korunmasının sağlandığı bir altyapıyı .NET Console Application olarak tasarlayıp geliştirmektedir.

## 🎯 Temel Özellikler

- **Dinamik Chunk'lama:** Dosya boyutuna göre otomatik optimal chunk boyutu hesaplama
- **Çoklu Dosya Desteği:** Tek mesajda virgülle ayrılmış dosya yolları ile toplu işlem
- **Distributed Storage:** Chunk'ları rastgele FileSystem ve Database provider'lara dağıtma
- **Checksum Doğrulaması:** SHA256 ile dosya ve chunk bütünlüğü kontrolü
- **Asenkron İşlem:** RabbitMQ ile message-based processing
- **Metadata Yönetimi:** Entity Framework Core ile SQL Server veritabanı
- **File Reconstruction:** Chunk'lardan dosya birleştirme ve output klasörüne kaydetme

## 🏗️ Mimari Yapı

### **Design Patterns:**
- **Repository Pattern:** Veri erişim katmanı
- **Strategy Pattern:** Storage provider seçimi
- **Factory Pattern:** Consumer oluşturma
- **Observer Pattern:** RabbitMQ message handling

### **SOLID Prensipleri:**
- **Single Responsibility:** Her sınıf tek sorumluluk
- **Open/Closed:** Interface'ler ile genişletilebilir
- **Liskov Substitution:** Storage provider'lar değiştirilebilir
- **Interface Segregation:** Küçük, odaklanmış interface'ler
- **Dependency Inversion:** DI container kullanımı

## 🚀 Kurulum ve Çalıştırma

### **Gereksinimler:**
- .NET 8.0 SDK
- Docker & Docker Compose
- SQL Server (Docker ile otomatik kurulum)

### **1. Projeyi Klonlayın:**
```bash
git clone <repository-url>
cd ChunkApplication
```

### **2. Docker Servislerini Başlatın:**
```bash
docker-compose up -d
```

### **3. Uygulamayı Çalıştırın:**
```bash
# Application Console (Consumer)
cd ChunkApplication
dotnet run --project ChunkApplication.Console

# Client (yeni terminal)
cd ChunkClient  
dotnet run
```

## 📱 Kullanım

### **Client Menüsü:**
1. **Send chunk file request** - Dosya chunk'lama (tekli/çoklu)
2. **Send reconstruct file request** - Dosya birleştirme
3. **Send list files request** - Dosya listesi
4. **Send get file info request** - Dosya detayları
5. **Send delete file request** - Dosya silme
6. **Show cached file list** - Cache'lenmiş dosyalar
7. **Exit** - Çıkış

### **Çoklu Dosya Örneği:**
```
Enter the path(s) to the file(s) you want to chunk (separate multiple files with comma):
C:\Users\MONSTER\Desktop\dosyalar\test1.txt,C:\Users\MONSTER\Desktop\dosyalar\test2.txt,C:\Users\MONSTER\Desktop\dosyalar\test3.txt

→ Tek mesajda 3 dosya gönderilir
→ Application virgülle ayırıp teker teker chunk'lar
→ Her dosya ayrı FileId ile database'e kaydedilir
```

## 🔧 Teknik Detaylar

### **Chunk'lama Algoritması:**
- **< 64 KB:** 2 chunk (dosya boyutu / 2)
- **64 KB - 256 KB:** 128 KB chunk boyutu
- **256 KB - 1 MB:** 256 KB chunk boyutu
- **1 MB - 5 MB:** 1 MB chunk boyutu
- **5 MB+:** 5 MB chunk boyutu

### **Storage Provider'lar:**
- **FileSystemStorageProvider:** `chunks/` klasörüne `.chunk` dosyaları
- **DatabaseStorageProvider:** `chunk2/` klasörüne `.dbchunk` dosyaları
- **Random Selection:** Her chunk rastgele provider'a atanır

### **Veritabanı Şeması:**
- **Files:** Dosya metadata'ları (Id, FileName, OriginalPath, FileSize, Checksum, etc.)
- **Chunks:** Chunk bilgileri (Id, FileId, ChunkNumber, StorageProvider, StoragePath, etc.)
- **Relations:** Files → Chunks (1:N) cascade delete

## 📊 Performans Özellikleri

- **Concurrent Processing:** Scoped service lifetime ile thread-safe
- **Memory Efficient:** Streaming ile büyük dosya desteği  
- **File Sharing:** FileShare.Read ile paralel dosya erişimi
- **Scalable:** RabbitMQ ile horizontal scaling
- **Fault Tolerant:** Retry mechanism ve comprehensive error handling
- **Consumer Optimization:** BasicQos ile controlled message processing

## 🔒 Güvenlik

- **Checksum Verification:** SHA256 ile dosya bütünlüğü
- **Input Validation:** Dosya yolu ve boyut kontrolü
- **Error Handling:** Comprehensive exception management
- **Logging:** Tüm işlemler loglanıyor

## 🧪 Test

### **Build:**
```bash
dotnet build
```

### **Run:**
```bash
# Terminal 1: Application Console
dotnet run --project ChunkApplication.Console

# Terminal 2: Client
dotnet run --project ChunkClient
```

### **Test Senaryosu:**
1. **ChunkClient → 1. Send chunk file request**
   ```
   C:\Users\MONSTER\Desktop\dosyalar\test1.txt,C:\Users\MONSTER\Desktop\dosyalar\test2.txt
   ```
2. **ChunkClient → 3. Send list files request** (dosyaları listele)
3. **ChunkClient → 6. Show cached file list** (cache'i gör)
4. **ChunkClient → 2. Send reconstruct file request** 
   - Dosya seç: `test1.txt`
   - Output filename: `restored_test1.txt`
   - Sonuç: `output/restored_test1.txt`

## 📝 Loglar

Loglar `logs/` klasöründe günlük olarak saklanır:
- `chunk-application-YYYYMMDD.log`
- Structured logging ile JSON format

## 📁 Klasör Yapısı

```
ChunkApplication/
├── chunks/                 # FileSystemStorageProvider chunk'ları (.chunk)
├── chunk2/                 # DatabaseStorageProvider chunk'ları (.dbchunk)  
├── output/                 # Reconstruct edilen dosyalar
├── logs/                   # Application logları
└── ChunkApplication/       # Ana uygulama
    ├── Application/        # Business logic
    ├── Domain/            # Domain entities
    ├── Infrastructure/    # Data access, messaging
    └── Console/           # Console app
```

## ✅ Mevcut Özellikler

- [x] **FileSystemStorageProvider** - chunks/ klasörü
- [x] **DatabaseStorageProvider** - chunk2/ klasörü  
- [x] **Random Provider Selection** - Her chunk farklı provider'a
- [x] **Multi-file Processing** - Tek mesajda çoklu dosya
- [x] **File Reconstruction** - output/ klasörüne birleştirme
- [x] **Checksum Verification** - SHA256 doğrulama
- [x] **RabbitMQ Integration** - Async message processing
- [x] **EF Core Integration** - SQL Server database

## 🔮 Gelecek Özellikler

- [ ] **Cloud Storage** provider'ları (AWS S3, Azure Blob)
- [ ] **Compression** desteği
- [ ] **Encryption** desteği
- [ ] **Web UI** dashboard
- [ ] **REST API** endpoints
- [ ] **Monitoring** ve metrics

## 🤝 Katkıda Bulunma

1. Fork yapın
2. Feature branch oluşturun (`git checkout -b feature/amazing-feature`)
3. Commit yapın (`git commit -m 'Add amazing feature'`)
4. Push yapın (`git push origin feature/amazing-feature`)
5. Pull Request açın

## 📄 Lisans

Bu proje MIT lisansı altında lisanslanmıştır.

## 👨‍💻 Geliştirici

- **Teknoloji:** .NET 8, C#, Entity Framework Core, RabbitMQ
- **Mimari:** Clean Architecture, Repository Pattern, SOLID Principles
- **Veritabanı:** SQL Server
- **Message Broker:** RabbitMQ
- **Logging:** Microsoft.Extensions.Logging
