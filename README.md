# ChunkApplication - Distributed File Chunking System

## 📋 Proje Açıklaması

Bu proje, büyük dosyaların otomatik olarak küçük parçalara (chunk) ayrılması, bu parçaların farklı depolama sağlayıcılarına dağıtılması ve gerektiğinde birleştirilerek dosya bütünlüğünün korunmasının sağlandığı bir **mikroservis tabanlı dağıtık sistem**dir. Proje, **Clean Architecture** prensiplerine uygun olarak **Domain-Driven Design (DDD)** yaklaşımı ile geliştirilmiştir.

## 🎯 Temel Özellikler

- **Dinamik Chunk'lama:** Dosya boyutuna göre otomatik optimal chunk boyutu hesaplama
- **Çoklu Dosya Desteği:** Tek mesajda virgülle ayrılmış dosya yolları ile toplu işlem
- **Distributed Storage:** Chunk'ları rastgele FileSystem ve Database provider'lara dağıtma
- **Checksum Doğrulaması:** SHA256 ile dosya ve chunk bütünlüğü kontrolü
- **Asenkron İşlem:** RabbitMQ ile message-based processing
- **Metadata Yönetimi:** Entity Framework Core ile SQL Server veritabanı
- **File Reconstruction:** Chunk'lardan dosya birleştirme ve output klasörüne kaydetme
- **Mikroservis Mimarisi:** ChunkApplication (Consumer) ve ChunkClient (Publisher) ayrı servisler
- **Docker Containerization:** Tam Docker Compose desteği

## 🏗️ Mimari Yapı

### **Clean Architecture Katmanları:**
- **Domain Layer:** Entities, Interfaces, Business Rules
- **Application Layer:** Services, DTOs, Business Logic
- **Infrastructure Layer:** Data Access, External Services, Message Handlers
- **Console Layer:** Presentation, User Interface

### **Design Patterns:**
- **Repository Pattern:** Veri erişim katmanı abstraction
- **Strategy Pattern:** Storage provider seçimi
- **Factory Pattern:** Consumer ve service instantiation
- **Observer Pattern:** RabbitMQ message handling
- **Domain Model Pattern:** Rich domain entities

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
- RabbitMQ (Docker ile otomatik kurulum)

### **1. Projeyi Klonlayın:**
```bash
git clone <https://github.com/YasinnErdemm/ChunkFoldersProject.git>
cd ChunkFoldersProject
```

### **2. Docker Servislerini Başlatın:**
```bash
# Tüm servisleri başlat (RabbitMQ + SQL Server + Applications)
docker-compose up -d

# Sadece altyapı servislerini başlat
docker-compose up -d rabbitmq mssql
```

### **3. Uygulamayı Çalıştırın:**

#### **Docker ile (Önerilen):**
```bash
# Tüm servisleri başlat
docker-compose up

# Detached modda çalıştır
docker-compose up -d
```

#### **Local Development:**
-Visual Studio yardımı ile multiple proje seçilip client ve application tarafı aynı anda ayağa kaldırılmalıdır.
-Bu sırada dockerdaki mssql ve rabbitmq da ayakta olmalıdır.

### **4. Web UI'lara Erişim:**
- **RabbitMQ Management:** http://localhost:15672 (admin/admin123)
- **SQL Server:** localhost:1433 (sa/YourPassword123!)

## 📱 Kullanım

### **ChunkClient Menüsü:**
ChunkClient uygulaması başlatıldığında aşağıdaki interaktif menü görüntülenir:

```
=== Chunk Client Menu ===
1. Send chunk file request      - Dosya chunk'lama (tekli/çoklu)
2. Send reconstruct file request - Dosya birleştirme
3. Send list files request      - Dosya listesi
4. Send get file info request   - Dosya detayları
5. Send delete file request     - Dosya silme
6. Show cached file list        - Cache'lenmiş dosyalar
7. Exit                         - Çıkış
```

### **Çoklu Dosya İşleme Örneği:**
```bash
# 1. Çoklu dosya chunk'lama
Enter the path(s) to the file(s) you want to chunk (separate multiple files with comma):
C:\Users\MONSTER\Desktop\dosyalar\test1.txt,C:\Users\MONSTER\Desktop\dosyalar\test2.txt,C:\Users\MONSTER\Desktop\dosyalar\test3.txt

→ Tek RabbitMQ mesajında 3 dosya gönderilir
→ ChunkApplication virgülle ayırıp teker teker chunk'lar
→ Her dosya ayrı FileId ile SQL Server database'e kaydedilir
→ Chunk'lar rastgele FileSystem ve Database provider'lara dağıtılır

# 2. Dosya listesi alma
3. Send list files request
→ Tüm dosyalar cache'e yüklenir
→ 6. Show cached file list ile görüntülenebilir

# 3. Dosya birleştirme
2. Send reconstruct file request
→ Cache'den dosya seçilir
→ output/ klasörüne birleştirilen dosya kaydedilir
```

### **Message Flow:**
```
ChunkClient (Publisher) → RabbitMQ → ChunkApplication (Consumer)
                                 ↓
                           SQL Server Database
                                 ↓
                        FileSystem/Database Providers
```

## 🔧 Teknik Detaylar

### **Dinamik Chunk'lama Algoritması:**
- **< 64 KB:** 2 chunk (dosya boyutu / 2, minimum 1KB)
- **64 KB - 256 KB:** 128 KB chunk boyutu
- **256 KB - 1 MB:** 256 KB chunk boyutu  
- **1 MB - 5 MB:** 512 KB chunk boyutu
- **5 MB - 10 MB:** 1 MB chunk boyutu
- **10 MB - 100 MB:** 2 MB chunk boyutu
- **100 MB+:** 5 MB chunk boyutu

**Algoritma Özellikleri:**
- Minimum 2 chunk garantisi
- Son chunk kalan byte'ları içerir
- Dosya boyutuna göre otomatik optimizasyon

### **Storage Provider'lar:**
- **FileSystemStorageProvider:** `chunks/` klasörüne `.chunk` dosyaları
- **DatabaseStorageProvider:** `chunk2/` klasörüne `.dbchunk` dosyaları
- **Random Selection:** Her chunk rastgele provider'a atanır
- ** Simüle etmek istenilen durum google,aws gibi providerlar yokluğnda chunks ve chunks2 dosyalarının
farklı provider hostları olmalarıdır.

### **Veritabanı İlişkileri:**
- **Files ↔ Chunks:** One-to-Many relationship
- **Cascade Delete:** Dosya silindiğinde tüm chunk'ları otomatik silinir
- **Indexing:** FileId ve CreatedAt alanlarında performans indexleri
- **Constraints:** Foreign key constraints ile referential integrity

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

## 📁 Proje Yapısı

```
ChunkFoldersProject/
├── docker-compose.yml           # Tüm servislerin Docker konfigürasyonu
├── README.md                    # Ana dokümantasyon
│
├── ChunkApplication/            # Ana Consumer Servisi
│   ├── docker-compose.yml      # Servis-specific Docker config
│   ├── Dockerfile              # ChunkApplication container
│   ├── appsettings.json        # Konfigürasyon
│   ├── InitialCreate.sql       # Database schema
│   ├── chunks/                 # FileSystemStorageProvider (.chunk)
│   ├── logs/                   # Application logları
│   ├── README.md              # Servis dokümantasyonu
│   │
│   ├── ChunkApplication.Console/      # Console UI
│   │   ├── Program.cs                 # Ana entry point
│   │   └── ChunkApplication.Console.csproj
│   │
│   ├── ChunkApplication.Application/  # Business Logic Layer
│   │   ├── Services/
│   │   │   └── ChunkService.cs        # Ana business logic
│   │   ├── DTOs/
│   │   │   ├── ChunkFileRequest.cs
│   │   │   └── ChunkFileResponse.cs
│   │   └── ChunkApplication.Application.csproj
│   │
│   ├── ChunkApplication.Domain/       # Domain Layer
│   │   ├── Entities/
│   │   │   ├── FileEntity.cs          # Files aggregate root
│   │   │   └── Chunk.cs               # Chunks entity
│   │   ├── Interfaces/
│   │   │   ├── IChunkService.cs
│   │   │   ├── IRepository.cs
│   │   │   └── IStorageProvider.cs
│   │   └── ChunkApplication.Domain.csproj
│   │
│   ├── ChunkApplication.Infrastructure/ # Infrastructure Layer
│   │   ├── Data/
│   │   │   └── ChunkDbContext.cs      # EF Core context
│   │   ├── Repositories/
│   │   │   ├── Repository.cs          # Generic repository
│   │   │   ├── ChunkRepository.cs
│   │   │   └── FileRepository.cs
│   │   ├── StorageProviders/
│   │   │   ├── FileSystemStorageProvider.cs
│   │   │   └── DatabaseStorageProvider.cs
│   │   ├── MessageHandlers/           # RabbitMQ Consumers
│   │   │   ├── ChunkFileRequestConsumer.cs
│   │   │   ├── ReconstructFileRequestConsumer.cs
│   │   │   ├── ListFilesRequestConsumer.cs
│   │   │   ├── GetFileInfoRequestConsumer.cs
│   │   │   ├── DeleteFileRequestConsumer.cs
│   │   │   └── Models/                # Message DTOs
│   │   └── ChunkApplication.Infrastructure.csproj
│   │
│   ├── Messages/                      # Shared Message Contracts
│   │   ├── Requests/
│   │   │   ├── ChunkFileMessage.cs
│   │   │   ├── ReconstructFileMessage.cs
│   │   │   ├── ListFilesMessage.cs
│   │   │   ├── GetFileInfoMessage.cs
│   │   │   └── DeleteFileMessage.cs
│   │   └── Responses/
│   │
│   └── ChunkApplication.csproj        # Main project file
│
├── ChunkClient/                 # Publisher Client Servisi
│   ├── Dockerfile              # ChunkClient container
│   ├── appsettings.json        # Client konfigürasyon
│   ├── Program.cs              # Client ana entry point
│   ├── FileListMessageConsumer.cs     # Response consumer
│   ├── FileProcessingResponseConsumer.cs
│   ├── Messages/               # Message contracts (copy)
│   │   ├── Requests/
│   │   └── Responses/
│   └── ChunkClient.csproj
│
└── ChunkApplication.sln        # Solution file
```

### **Klasör ve Dosya Açıklamaları:**

#### **Runtime Klasörleri:**
- `chunks/` - FileSystemStorageProvider tarafından oluşturulan `.chunk` dosyaları
- `output/` - Reconstruct edilmiş dosyaların kaydedildiği klasör  
- `logs/` - Günlük log dosyaları (`chunk-application-YYYYMMDD.log`)
- `chunk-application.db*` - SQLite database dosyaları (development)

#### **Clean Architecture Katmanları:**
- **Domain:** Core business entities ve interfaces
- **Application:** Business logic ve use cases
- **Infrastructure:** External concerns (DB, RabbitMQ, Storage)
- **Console:** Presentation layer

## 🔄 RabbitMQ Message Queues

Sistem aşağıdaki RabbitMQ kuyruklarını kullanır:

| Queue Name | Producer | Consumer | Açıklama |
|------------|----------|----------|-----------|
| `ChunkFileRequest` | ChunkClient | ChunkFileRequestConsumer | Dosya chunk'lama istekleri |
| `ReconstructFileRequest` | ChunkClient | ReconstructFileRequestConsumer | Dosya birleştirme istekleri |
| `ListFilesRequest` | ChunkClient | ListFilesRequestConsumer | Dosya listesi istekleri |
| `GetFileInfoRequest` | ChunkClient | GetFileInfoRequestConsumer | Dosya detay istekleri |
| `DeleteFileRequest` | ChunkClient | DeleteFileRequestConsumer | Dosya silme istekleri |
| `FileProcessingResponse` | MessageHandlers | FileProcessingResponseConsumer | İşlem sonucu bildirimleri |
| `ListFilesResponse` | ListFilesRequestConsumer | FileListMessageConsumer | Dosya listesi yanıtları |

## 🗄️ Veritabanı Şeması

### **Files Tablosu:**
```sql
Files (
    Id NVARCHAR(32) PRIMARY KEY,
    FileName NVARCHAR(255) NOT NULL,
    OriginalPath NVARCHAR(500) NOT NULL,
    FileSize BIGINT NOT NULL,
    Checksum NVARCHAR(64) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    LastAccessed DATETIME2 NULL,
    ChunkSize INT NOT NULL,
    TotalChunks INT NOT NULL
)
```

### **Chunks Tablosu:**
```sql
Chunks (
    Id NVARCHAR(32) PRIMARY KEY,
    FileId NVARCHAR(32) NOT NULL,
    ChunkNumber INT NOT NULL,
    ChunkSize INT NOT NULL,
    StorageProvider NVARCHAR(50) NOT NULL,
    Checksum NVARCHAR(64) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    FOREIGN KEY (FileId) REFERENCES Files(Id) ON DELETE CASCADE
)
```

## ✅ Mevcut Özellikler

- [x] **Clean Architecture** - Domain, Application, Infrastructure, Console layers
- [x] **Mikroservis Mimarisi** - ChunkApplication + ChunkClient ayrı servisler
- [x] **Docker Containerization** - Tam Docker Compose desteği
- [x] **RabbitMQ Integration** - 7 farklı message queue ile async processing
- [x] **FileSystemStorageProvider** - chunks/ klasöründe .chunk dosyaları
- [x] **DatabaseStorageProvider** - chunk2/ klasöründe .dbchunk dosyaları  
- [x] **Random Provider Selection** - Her chunk rastgele provider'a dağıtım
- [x] **Multi-file Processing** - Tek mesajda çoklu dosya işleme
- [x] **File Reconstruction** - output/ klasörüne birleştirme
- [x] **Checksum Verification** - SHA256 ile dosya ve chunk bütünlüğü
- [x] **EF Core Integration** - SQL Server database with migrations
- [x] **Rich Domain Models** - Domain entities with business logic
- [x] **Repository Pattern** - Generic ve specialized repository'ler
- [x] **Dependency Injection** - Microsoft.Extensions.DependencyInjection
- [x] **Structured Logging** - Daily rotating log files
- [x] **Interactive Client UI** - Console-based menu system
- [x] **Real-time Processing** - Async message-based workflow


## 🛠️ Teknoloji Stack

### **Backend Technologies:**
- **.NET 8.0** - Modern cross-platform framework
- **C# 12** - Latest language features
- **Entity Framework Core** - ORM ve database migrations
- **RabbitMQ** - Message broker ve async processing
- **SQL Server** - Enterprise-grade relational database

### **Architecture & Patterns:**
- **Clean Architecture** - Layered architecture pattern
- **Domain-Driven Design (DDD)** - Rich domain models
- **Repository Pattern** - Data access abstraction
- **Strategy Pattern** - Storage provider selection
- **CQRS Pattern** - Command Query Responsibility Segregation
- **SOLID Principles** - Clean code practices

### **Infrastructure:**
- **Docker & Docker Compose** - Containerization
- **Microsoft.Extensions.DependencyInjection** - IoC container
- **Microsoft.Extensions.Logging** - Structured logging
- **System.Text.Json** - JSON serialization

### **Development Tools:**
- **Visual Studio 2022 / VS Code** - IDE support
- **Git** - Version control
- **NuGet** - Package management

## 📊 Performans Metrikleri

### **Sistem Kapasitesi:**
- **Maksimum Dosya Boyutu:** Teorik olarak sınırsız
- **Concurrent Processing:** Multiple file processing
- **Chunk Boyutu:** Dinamik (64KB - 5MB arası)
- **Storage Providers:** 2 (genişletilebilir)
- **Message Throughput:** RabbitMQ ile yüksek performans

### **Optimizasyonlar:**
- **Memory Efficient:** Streaming ile büyük dosya desteği
- **Async Processing:** Non-blocking I/O operations
- **Database Indexing:** EF Core ile optimize edilmiş queries
- **Message Batching:** Çoklu dosya tek mesajda işleme
- **Provider Load Balancing:** Rastgele chunk dağıtımı


### **Teknik Özellikler:**
- **Teknoloji:** .NET 8, C# 12, Entity Framework Core, RabbitMQ
- **Mimari:** Clean Architecture, DDD, Repository Pattern, SOLID
- **Veritabanı:** SQL Server 2019+ with EF Core migrations
- **Message Broker:** RabbitMQ 3.x with management UI
- **Logging:** Microsoft.Extensions.Logging with file rotation
- **Containerization:** Docker & Docker Compose
