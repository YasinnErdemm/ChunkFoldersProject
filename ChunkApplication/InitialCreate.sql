-- ChunkApplication Database Creation Script
-- SQL Server Version

-- Create Database
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'ChunkApplication')
BEGIN
    CREATE DATABASE ChunkApplication;
END
GO

USE ChunkApplication;
GO

-- Create Files Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Files]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Files](
        [Id] [nvarchar](50) NOT NULL,
        [FileName] [nvarchar](255) NOT NULL,
        [OriginalPath] [nvarchar](500) NOT NULL,
        [FileSize] [bigint] NOT NULL,
        [Checksum] [nvarchar](64) NOT NULL,
        [CreatedAt] [datetime2](7) NOT NULL,
        [LastAccessed] [datetime2](7) NULL,
        [TotalChunks] [int] NOT NULL,
        [ChunkSize] [int] NOT NULL,
        CONSTRAINT [PK_Files] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- Create Chunks Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Chunks]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Chunks](
        [Id] [nvarchar](50) NOT NULL,
        [FileId] [nvarchar](50) NOT NULL,
        [ChunkNumber] [int] NOT NULL,
        [ChunkSize] [int] NOT NULL,
        [StorageProvider] [nvarchar](100) NOT NULL,
        [Checksum] [nvarchar](64) NOT NULL,
        [CreatedAt] [datetime2](7) NOT NULL,
        CONSTRAINT [PK_Chunks] PRIMARY KEY CLUSTERED ([Id] ASC)
    );
END
GO

-- Create Foreign Key
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Chunks_Files_FileId]') AND parent_object_id = OBJECT_ID(N'[dbo].[Chunks]'))
BEGIN
    ALTER TABLE [dbo].[Chunks]  WITH CHECK ADD  CONSTRAINT [FK_Chunks_Files_FileId] FOREIGN KEY([FileId])
    REFERENCES [dbo].[Files] ([Id])
    ON DELETE CASCADE;
END
GO

-- Create Indexes for Performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Chunks]') AND name = N'IX_Chunks_FileId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Chunks_FileId] ON [dbo].[Chunks] ([FileId] ASC);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE object_id = OBJECT_ID(N'[dbo].[Files]') AND name = N'IX_Files_CreatedAt')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Files_CreatedAt] ON [dbo].[Files] ([CreatedAt] ASC);
END
GO

-- Insert Sample Data (Optional)
-- INSERT INTO [dbo].[Files] ([Id], [FileName], [OriginalPath], [FileSize], [Checksum], [CreatedAt], [TotalChunks], [ChunkSize])
-- VALUES ('sample-id', 'sample.txt', 'C:\sample.txt', 1024, 'sample-checksum', GETUTCDATE(), 1, 1024);

PRINT 'ChunkApplication database and tables created successfully!';
GO
