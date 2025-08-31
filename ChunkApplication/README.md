# Chunk Application

## Proje Açıklaması

Bu proje, büyük dosyaların otomatik olarak küçük parçalara (chunk) ayrılması, bu parçaların farklı depolama sağlayıcılarına dağıtılması ve gerektiğinde birleştirilerek dosya bütünlüğünün korunmasının sağlandığı bir altyapıyı .NET Console Application olarak tasarlayıp geliştirmektedir.

## Özellikler

### 🎯 Temel Özellikler
- **Dosya Chunk'lama**: Büyük dosyaları yapılandırılabilir boyutlarda parçalara ayırma
- **Çoklu Depolama**: Farklı storage provider'lara chunk'ları dağıtma
- **Veritabanı Metadata**: Tüm chunk bilgilerini ve dosya metadata'sını SQLite'da saklama
- **Dosya Birleştirme**: Chunk'lardan orijinal dosyayı yeniden oluşturma
- **Checksum Doğrulama**: SHA256 ile dosya ve chunk bütünlüğü kontrolü

### 🏗️ Mimari Özellikler
- **SOLID Prensipleri**: Interface segregation, dependency inversion, single responsibility
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection ile IoC container
- **Repository Pattern**: Generic repository ve özel repository implementasyonları
- **Strategy Pattern**: Farklı storage provider'lar için interface tabanlı yaklaşım
- **Async/Await**: Tüm I/O operasyonları için asenkron programlama

### 📊 Depolama Sağlayıcıları
- **FileSystemStorageProvider**: Chunk'ları local dosya sisteminde saklama
- **DatabaseStorageProvider**: Chunk'ları in-memory database'de saklama (genişletilebilir)

### 🔍 Loglama ve Monitoring
- **Serilog**: Structured logging ile detaylı log kayıtları
- **Console ve File Logging**: Hem konsol hem dosya tabanlı loglama
- **Rolling Logs**: Günlük log dosyaları

## Teknik Detaylar

### 🛠️ Teknoloji Stack
- **.NET 8.0**: Modern .NET platformu
- **Entity Framework Core**: SQL Server veritabanı ORM
- **Microsoft.Extensions.DependencyInjection**: IoC container
- **Serilog**: Advanced logging framework
- **SQL Server**: Enterprise-grade relational database

### 📁 Proje Yapısı
```
ChunkApplication/
├── Data/                   # Entity Framework context
├── Interfaces/             # Contract definitions
├── Models/                 # Domain models
├── Repositories/           # Data access layer
├── Services/               # Business logic
└── Program.cs             # Main application entry point
```

### 🔧 Design Patterns
1. **Repository Pattern**: Data access abstraction
2. **Strategy Pattern**: Storage provider selection
3. **Factory Pattern**: Service instantiation
4. **Dependency Injection**: Service composition
5. **Interface Segregation**: Clean contract definitions

## Kurulum ve Çalıştırma

### Gereksinimler
- .NET 8.0 SDK
- Visual Studio 2022 veya VS Code
- Windows/macOS/Linux

### Adım 1: Projeyi Klonlayın
```bash
git clone <repository-url>
cd ChunkApplication
```

### Adım 2: SQL Server Database'i Oluşturun
```sql
-- SQL Server Management Studio'da veya Azure Data Studio'da çalıştırın
-- InitialCreate.sql dosyasını kullanın
```

### Adım 3: Bağımlılıkları Yükleyin
```bash
dotnet restore
```

### Adım 4: Uygulamayı Çalıştırın
```bash
dotnet run
```

## Kullanım

### 🚀 Ana Menü
Uygulama başlatıldığında aşağıdaki menü görüntülenir:

1. **Chunk a file**: Dosyayı parçalara ayırma
2. **Reconstruct a file**: Chunk'lardan dosyayı yeniden oluşturma
3. **List all files**: Sistemdeki tüm dosyaları listeleme
4. **Get file information**: Dosya detaylarını görüntüleme
5. **Delete a file**: Dosyayı ve chunk'larını silme
6. **Exit**: Uygulamadan çıkış

### 📁 Dosya Chunk'lama
1. Menüden "1" seçeneğini seçin
2. Chunk'lanacak dosyanın tam yolunu girin
3. Chunk boyutunu MB cinsinden belirtin (varsayılan: 1MB)
4. Sistem dosyayı otomatik olarak parçalara ayırır ve farklı storage'lara dağıtır

### 🔄 Dosya Birleştirme
1. Menüden "2" seçeneğini seçin
2. Birleştirilecek dosyanın ID'sini girin
3. Çıktı dosyasının yolunu belirtin
4. Sistem chunk'ları toplar, checksum doğrulaması yapar ve dosyayı oluşturur

## Mimari Avantajlar

### 🔒 Güvenlik
- **Checksum Doğrulama**: Her chunk ve dosya için SHA256 hash
- **Veri Bütünlüğü**: Chunk'lar arası sıralama kontrolü
- **Hata Toleransı**: Bozuk chunk'lar için otomatik temizlik

### 📈 Ölçeklenebilirlik
- **Modüler Yapı**: Yeni storage provider'lar kolayca eklenebilir
- **Interface Tabanlı**: Loose coupling ile genişletilebilir mimari
- **Repository Pattern**: Farklı veri kaynakları için esnek yapı

### 🧪 Test Edilebilirlik
- **Interface Segregation**: Mock'lanabilir servisler
- **Dependency Injection**: Unit test'ler için kolay service replacement
- **Clean Architecture**: Business logic'in test edilebilir ayrımı

## Genişletme Önerileri

### 🔮 Gelecek Özellikler
1. **Cloud Storage Providers**: AWS S3, Azure Blob Storage desteği
2. **Compression**: Chunk'larda sıkıştırma algoritmaları
3. **Encryption**: End-to-end encryption desteği
4. **Parallel Processing**: Çoklu thread ile chunk işleme
5. **Web API**: RESTful API endpoint'leri
6. **Monitoring Dashboard**: Real-time sistem durumu

### 🔌 Yeni Storage Provider Ekleme
```csharp
public class CloudStorageProvider : IStorageProvider
{
    public string ProviderName => "CloudStorage";
    
    public async Task StoreChunkAsync(string chunkId, byte[] data)
    {
        // Cloud storage implementation
    }
    
    // Implement other interface methods...
}
```

## Performans Özellikleri

### ⚡ Optimizasyonlar
- **Buffer Management**: Efficient memory usage for large files
- **Async I/O**: Non-blocking file operations
- **Database Indexing**: Optimized queries with EF Core
- **Chunk Distribution**: Load balancing across storage providers

### 📊 Benchmark Bilgileri
- **Chunk Boyutu**: 1MB (yapılandırılabilir)
- **Maksimum Dosya**: Teorik olarak sınırsız
- **Storage Providers**: Şu anda 2 (genişletilebilir)
- **Veritabanı**: SQL Server (Enterprise-grade, production-ready)

## Hata Yönetimi

### 🚨 Exception Handling
- **FileNotFoundException**: Dosya bulunamadığında
- **IOException**: I/O operasyonlarında hata
- **ChecksumMismatchException**: Veri bütünlüğü hatası
- **StorageProviderException**: Storage provider hataları

### 📝 Logging Strategy
- **Information Level**: Normal operasyonlar
- **Warning Level**: Potansiyel problemler
- **Error Level**: Hatalar ve exception'lar
- **Fatal Level**: Kritik sistem hataları

## Lisans

Bu proje eğitim amaçlı geliştirilmiştir. MIT lisansı altında kullanılabilir.

## İletişim

Proje hakkında sorularınız için:
- GitHub Issues: [Repository Issues](https://github.com/username/repo/issues)
- Email: [your-email@domain.com]

---

**Not**: Bu proje, modern .NET development practices, SOLID principles ve enterprise-level architecture patterns kullanılarak geliştirilmiştir. Production ortamında kullanılmadan önce ek güvenlik, monitoring ve backup stratejileri eklenmelidir.
