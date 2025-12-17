-- SmartLibrarySystem database setup
IF DB_ID('SmartLibraryDB') IS NULL
BEGIN
    CREATE DATABASE SmartLibraryDB;
END
GO

USE SmartLibraryDB;
GO

IF OBJECT_ID('dbo.BorrowRequests', 'U') IS NOT NULL DROP TABLE dbo.BorrowRequests;
IF OBJECT_ID('dbo.Books', 'U') IS NOT NULL DROP TABLE dbo.Books;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
GO

CREATE TABLE Users (
    UserId INT IDENTITY PRIMARY KEY,
    FullName NVARCHAR(80) NOT NULL,
    Email NVARCHAR(80) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(200) NOT NULL,
    SchoolNumber NVARCHAR(20) NOT NULL,
    Phone NVARCHAR(20),
    Role NVARCHAR(20) NOT NULL, -- Student | Staff | Admin
    CreatedAt DATETIME DEFAULT GETDATE()
);
GO

CREATE TABLE Books (
    BookId INT IDENTITY PRIMARY KEY,
    Title NVARCHAR(100) NOT NULL,
    Author NVARCHAR(100) NOT NULL,
    Category NVARCHAR(50) NOT NULL,
    PublishYear INT NOT NULL,
    Stock INT NOT NULL,
    Shelf NVARCHAR(20) NOT NULL
);
GO

CREATE TABLE BorrowRequests (
    RequestId INT IDENTITY PRIMARY KEY,
    UserId INT FOREIGN KEY REFERENCES Users(UserId),
    BookId INT FOREIGN KEY REFERENCES Books(BookId),
    Status NVARCHAR(20) NOT NULL,   -- Pending | Approved | Delivered | Returned
    RequestDate DATETIME DEFAULT GETDATE(),
    DeliveryDate DATETIME NULL,
    ReturnDate DATETIME NULL
);
GO

-- Sample seed data
INSERT INTO Books (Title, Author, Category, PublishYear, Stock, Shelf) VALUES
('Clean Code', 'Robert C. Martin', 'Software', 2008, 5, 'A1'),
('Introduction to Algorithms', 'Cormen', 'Computer Science', 2009, 3, 'A2'),
('Design Patterns', 'GoF', 'Software', 1994, 4, 'B1');

USE SmartLibraryDB;
GO

/* 1) Kitaplar */
DECLARE @Books TABLE (Title NVARCHAR(100), BookId INT);
INSERT INTO Books (Title, Author, Category, PublishYear, Stock, Shelf)
OUTPUT inserted.Title, inserted.BookId INTO @Books
VALUES
 (N'Clean Code', N'Robert C. Martin', N'Software', 2008, 5, N'A1')
,(N'Introduction to Algorithms', N'Cormen', N'Computer Science', 2009, 3, N'A2')
,(N'Design Patterns', N'GoF', N'Software', 1994, 4, N'B1')
,(N'Refactorings', N'Martin Fowler', N'Software', 1999, 4, N'A3')
,(N'The Pragmatic Programmer', N'Andrew Hunt', N'Software', 1999, 5, N'B2')
,(N'Clean Architecture', N'Robert C. Martin', N'Software', 2017, 4, N'A4')
,(N'Database System Concepts', N'Silberschatz', N'Database', 2010, 3, N'C1')
,(N'Operating System Concepts', N'Silberschatz', N'OS', 2012, 3, N'C2')
,(N'Artificial Intelligence', N'Russell & Norvig', N'AI', 2020, 2, N'D1')
,(N'Learning Python', N'Mark Lutz', N'Programming', 2013, 5, N'D2');

/* 2) Kullanıcılar (admin eklenmiyor) */
DECLARE @Users TABLE (Email NVARCHAR(80), UserId INT);
DECLARE @DefaultPasswordHash NVARCHAR(64) = N'a109e36947ad56de1dca1cc49f0ef8ac9ad9a7b1aa0df41fb3c4cb73c1ff01ea'; -- Password123!
INSERT INTO Users (FullName, Email, PasswordHash, SchoolNumber, Phone, Role)
OUTPUT inserted.Email, inserted.UserId INTO @Users
VALUES
 (N'Ayşe Yılmaz',     N'ayse@example.com',     @DefaultPasswordHash,  N'1001', N'555-1001', N'Student'),
 (N'Emre Demir',      N'emre@example.com',     @DefaultPasswordHash,  N'1002', N'555-1002', N'Student'),
 (N'Merve Koç',       N'merve@example.com',    @DefaultPasswordHash,  N'1003', N'555-1003', N'Student'),
 (N'Kerem Arslan',    N'kerem@example.com',    @DefaultPasswordHash,  N'1004', N'555-1004', N'Student'),
 (N'Zeynep Çelik',    N'zeynep@example.com',   @DefaultPasswordHash,  N'1005', N'555-1005', N'Student'),
 (N'Canan Ak',        N'canan@example.com',    @DefaultPasswordHash,  N'1006', N'555-1006', N'Staff'),
 (N'Mustafa Kara',    N'mustafa@example.com',  @DefaultPasswordHash,  N'1007', N'555-1007', N'Staff'),
 (N'Ece Şahin',       N'ece@example.com',      @DefaultPasswordHash,  N'1008', N'555-1008', N'Staff'),
 (N'Burak Yıldız',    N'burak@example.com',    @DefaultPasswordHash,  N'1009', N'555-1009', N'Staff'),
 (N'Deniz Er',        N'deniz@example.com',    @DefaultPasswordHash,  N'1010', N'555-1010', N'Staff');  -- 10. kullanıcı

/* 3) Kimlikleri değişkene çek */
DECLARE @u1 INT = (SELECT UserId FROM @Users WHERE Email = N'ayse@example.com');
DECLARE @u2 INT = (SELECT UserId FROM @Users WHERE Email = N'emre@example.com');
DECLARE @u3 INT = (SELECT UserId FROM @Users WHERE Email = N'merve@example.com');
DECLARE @u4 INT = (SELECT UserId FROM @Users WHERE Email = N'kerem@example.com');
DECLARE @u5 INT = (SELECT UserId FROM @Users WHERE Email = N'zeynep@example.com');
DECLARE @u6 INT = (SELECT UserId FROM @Users WHERE Email = N'canan@example.com');
DECLARE @u7 INT = (SELECT UserId FROM @Users WHERE Email = N'mustafa@example.com');
DECLARE @u8 INT = (SELECT UserId FROM @Users WHERE Email = N'ece@example.com');
DECLARE @u9 INT = (SELECT UserId FROM @Users WHERE Email = N'burak@example.com');
DECLARE @u10 INT = (SELECT UserId FROM @Users WHERE Email = N'deniz@example.com');

DECLARE @bCleanCode INT   = (SELECT BookId FROM @Books WHERE Title = N'Clean Code');
DECLARE @bAlgo INT        = (SELECT BookId FROM @Books WHERE Title = N'Introduction to Algorithms');
DECLARE @bDesign INT      = (SELECT BookId FROM @Books WHERE Title = N'Design Patterns');
DECLARE @bRefactor INT    = (SELECT BookId FROM @Books WHERE Title = N'Refactorings');
DECLARE @bPragmatic INT   = (SELECT BookId FROM @Books WHERE Title = N'The Pragmatic Programmer');
DECLARE @bCleanArch INT   = (SELECT BookId FROM @Books WHERE Title = N'Clean Architecture');
DECLARE @bDB INT          = (SELECT BookId FROM @Books WHERE Title = N'Database System Concepts');
DECLARE @bOS INT          = (SELECT BookId FROM @Books WHERE Title = N'Operating System Concepts');
DECLARE @bAI INT          = (SELECT BookId FROM @Books WHERE Title = N'Artificial Intelligence');
DECLARE @bPy INT          = (SELECT BookId FROM @Books WHERE Title = N'Learning Python');

/* 4) Ödünç kayıtları (günlük/haftalık/aylık dağıtılmış) */
DECLARE @today DATE = CAST(GETDATE() AS DATE);

INSERT INTO BorrowRequests (UserId, BookId, Status, RequestDate, DeliveryDate, ReturnDate) VALUES
-- Bugün (günlük)
(@u1,  @bCleanCode,   'Delivered', DATEADD(HOUR, 9,  CAST(@today AS DATETIME)), DATEADD(HOUR, 9,  CAST(@today AS DATETIME)), NULL),
(@u2,  @bAlgo,        'Delivered', DATEADD(HOUR, 11, CAST(@today AS DATETIME)), DATEADD(HOUR, 11, CAST(@today AS DATETIME)), NULL),
-- Son 7 gün (haftalık)
(@u3,  @bDesign,      'Delivered', DATEADD(DAY, -2, DATEADD(HOUR, 10, CAST(@today AS DATETIME))), DATEADD(DAY, -2, DATEADD(HOUR, 10, CAST(@today AS DATETIME))), NULL),
(@u4,  @bCleanArch,   'Delivered', DATEADD(DAY, -3, DATEADD(HOUR, 15, CAST(@today AS DATETIME))), DATEADD(DAY, -3, DATEADD(HOUR, 15, CAST(@today AS DATETIME))), NULL),
(@u5,  @bPragmatic,   'Delivered', DATEADD(DAY, -5, DATEADD(HOUR, 14, CAST(@today AS DATETIME))), DATEADD(DAY, -5, DATEADD(HOUR, 14, CAST(@today AS DATETIME))), NULL),
(@u6,  @bDB,          'Delivered', DATEADD(DAY, -6, DATEADD(HOUR, 16, CAST(@today AS DATETIME))), DATEADD(DAY, -6, DATEADD(HOUR, 16, CAST(@today AS DATETIME))), NULL),
-- Son 30 gün (aylık)
(@u7,  @bOS,          'Delivered', DATEADD(DAY, -10, DATEADD(HOUR, 9,  CAST(@today AS DATETIME))), DATEADD(DAY, -10, DATEADD(HOUR, 9,  CAST(@today AS DATETIME))), NULL),
(@u8,  @bAI,          'Delivered', DATEADD(DAY, -15, DATEADD(HOUR, 13, CAST(@today AS DATETIME))), DATEADD(DAY, -15, DATEADD(HOUR, 13, CAST(@today AS DATETIME))), NULL),
(@u9,  @bPy,          'Delivered', DATEADD(DAY, -20, DATEADD(HOUR, 10, CAST(@today AS DATETIME))), DATEADD(DAY, -20, DATEADD(HOUR, 10, CAST(@today AS DATETIME))), NULL),
(@u10, @bRefactor,    'Delivered', DATEADD(DAY, -25, DATEADD(HOUR, 17, CAST(@today AS DATETIME))), DATEADD(DAY, -25, DATEADD(HOUR, 17, CAST(@today AS DATETIME))), NULL);

USE SmartLibraryDB;
GO

-- 1) Her kitap için tutulacak (kanonik) BookId’yi belirle
WITH d AS (
    SELECT BookId, Title, Author,
           MIN(BookId) OVER (PARTITION BY Title, Author) AS KeepId
    FROM Books
)
-- 2) Ödünç kayıtlarını kanonik BookId’ye taşı
UPDATE br
SET br.BookId = d.KeepId
FROM BorrowRequests br
JOIN d ON br.BookId = d.BookId
WHERE d.BookId <> d.KeepId;

-- 3) Artık kullanılmayan mükerrer kitap kayıtlarını sil
;WITH d AS (
    SELECT BookId, Title, Author,
           MIN(BookId) OVER (PARTITION BY Title, Author) AS KeepId
    FROM Books
)
DELETE FROM Books
WHERE BookId IN (SELECT BookId FROM d WHERE BookId <> KeepId);

USE SmartLibraryDB;
GO

DECLARE @today DATE = CAST(GETDATE() AS DATE);

-- Var olan kitaplardan kimlikleri al
DECLARE @bCleanCode INT = (SELECT TOP 1 BookId FROM Books WHERE Title = N'Clean Code');
DECLARE @bAlgo INT = (SELECT TOP 1 BookId FROM Books WHERE Title = N'Introduction to Algorithms');
DECLARE @bDesign INT = (SELECT TOP 1 BookId FROM Books WHERE Title = N'Design Patterns');
DECLARE @bPragmatic INT = (SELECT TOP 1 BookId FROM Books WHERE Title = N'The Pragmatic Programmer');
DECLARE @bCleanArch INT = (SELECT TOP 1 BookId FROM Books WHERE Title = N'Clean Architecture');
DECLARE @bDB INT = (SELECT TOP 1 BookId FROM Books WHERE Title = N'Database System Concepts');
DECLARE @bOS INT = (SELECT TOP 1 BookId FROM Books WHERE Title = N'Operating System Concepts');
DECLARE @bAI INT = (SELECT TOP 1 BookId FROM Books WHERE Title = N'Artificial Intelligence');
DECLARE @bPy INT = (SELECT TOP 1 BookId FROM Books WHERE Title = N'Learning Python');
DECLARE @bRefactor INT = (SELECT TOP 1 BookId FROM Books WHERE Title = N'Refactorings');

-- Örnek kullanıcılar (mevcut UserId’leri kullan)
DECLARE @u1 INT = (SELECT TOP 1 UserId FROM Users WHERE Email = N'ayse@example.com');
DECLARE @u2 INT = (SELECT TOP 1 UserId FROM Users WHERE Email = N'emre@example.com');
DECLARE @u3 INT = (SELECT TOP 1 UserId FROM Users WHERE Email = N'merve@example.com');
DECLARE @u4 INT = (SELECT TOP 1 UserId FROM Users WHERE Email = N'kerem@example.com');
DECLARE @u5 INT = (SELECT TOP 1 UserId FROM Users WHERE Email = N'zeynep@example.com');
DECLARE @u6 INT = (SELECT TOP 1 UserId FROM Users WHERE Email = N'canan@example.com');
DECLARE @u7 INT = (SELECT TOP 1 UserId FROM Users WHERE Email = N'mustafa@example.com');
DECLARE @u8 INT = (SELECT TOP 1 UserId FROM Users WHERE Email = N'ece@example.com');
DECLARE @u9 INT = (SELECT TOP 1 UserId FROM Users WHERE Email = N'burak@example.com');
DECLARE @u10 INT = (SELECT TOP 1 UserId FROM Users WHERE Email = N'deniz@example.com');

-- Yeni ödünç kayıtları
INSERT INTO BorrowRequests (UserId, BookId, Status, RequestDate, DeliveryDate, ReturnDate) VALUES
-- Bugün (günlük) 3 kayıt
(@u1, @bCleanCode,  'Delivered', DATEADD(HOUR, 9,  CAST(@today AS DATETIME)), DATEADD(HOUR, 9,  CAST(@today AS DATETIME)), NULL),
(@u2, @bAlgo,       'Delivered', DATEADD(HOUR, 11, CAST(@today AS DATETIME)), DATEADD(HOUR, 11, CAST(@today AS DATETIME)), NULL),
(@u3, @bDesign,     'Delivered', DATEADD(HOUR, 15, CAST(@today AS DATETIME)), DATEADD(HOUR, 15, CAST(@today AS DATETIME)), NULL),

-- Son 7 gün (haftalık) 5 kayıt
(@u4, @bCleanArch,  'Delivered', DATEADD(DAY, -2, DATEADD(HOUR, 10, CAST(@today AS DATETIME))), DATEADD(DAY, -2, DATEADD(HOUR, 10, CAST(@today AS DATETIME))), NULL),
(@u5, @bPragmatic,  'Delivered', DATEADD(DAY, -3, DATEADD(HOUR, 13, CAST(@today AS DATETIME))), DATEADD(DAY, -3, DATEADD(HOUR, 13, CAST(@today AS DATETIME))), NULL),
(@u6, @bDB,         'Delivered', DATEADD(DAY, -4, DATEADD(HOUR, 16, CAST(@today AS DATETIME))), DATEADD(DAY, -4, DATEADD(HOUR, 16, CAST(@today AS DATETIME))), NULL),
(@u7, @bOS,         'Delivered', DATEADD(DAY, -6, DATEADD(HOUR, 9,  CAST(@today AS DATETIME))), DATEADD(DAY, -6, DATEADD(HOUR, 9,  CAST(@today AS DATETIME))), NULL),
(@u8, @bAI,         'Delivered', DATEADD(DAY, -6, DATEADD(HOUR, 18, CAST(@today AS DATETIME))), DATEADD(DAY, -6, DATEADD(HOUR, 18, CAST(@today AS DATETIME))), NULL),

-- Son 30 gün (aylık) 6 kayıt
(@u9,  @bPy,        'Delivered', DATEADD(DAY, -10, DATEADD(HOUR, 11, CAST(@today AS DATETIME))), DATEADD(DAY, -10, DATEADD(HOUR, 11, CAST(@today AS DATETIME))), NULL),
(@u10, @bRefactor,  'Delivered', DATEADD(DAY, -12, DATEADD(HOUR, 14, CAST(@today AS DATETIME))), DATEADD(DAY, -12, DATEADD(HOUR, 14, CAST(@today AS DATETIME))), NULL),
(@u2,  @bCleanCode, 'Delivered', DATEADD(DAY, -14, DATEADD(HOUR, 9,  CAST(@today AS DATETIME))), DATEADD(DAY, -14, DATEADD(HOUR, 9,  CAST(@today AS DATETIME))), NULL),
(@u3,  @bAlgo,      'Delivered', DATEADD(DAY, -18, DATEADD(HOUR, 15, CAST(@today AS DATETIME))), DATEADD(DAY, -18, DATEADD(HOUR, 15, CAST(@today AS DATETIME))), NULL),
(@u4,  @bDesign,    'Delivered', DATEADD(DAY, -22, DATEADD(HOUR, 10, CAST(@today AS DATETIME))), DATEADD(DAY, -22, DATEADD(HOUR, 10, CAST(@today AS DATETIME))), NULL),
(@u5,  @bPragmatic, 'Delivered', DATEADD(DAY, -25, DATEADD(HOUR, 16, CAST(@today AS DATETIME))), DATEADD(DAY, -25, DATEADD(HOUR, 16, CAST(@today AS DATETIME))), NULL);


select*from Users
USE SmartLibraryDB;
GO

DECLARE @today DATETIME = GETDATE();

-- Mevcut kitaplar
DECLARE @bCleanCode INT = (SELECT TOP 1 BookId FROM Books WHERE Title = N'Clean Code');
DECLARE @bAlgo INT = (SELECT TOP 1 BookId FROM Books WHERE Title = N'Introduction to Algorithms');
DECLARE @bDesign INT = (SELECT TOP 1 BookId FROM Books WHERE Title = N'Design Patterns');
DECLARE @bPragmatic INT = (SELECT TOP 1 BookId FROM Books WHERE Title = N'The Pragmatic Programmer');
DECLARE @bOS INT = (SELECT TOP 1 BookId FROM Books WHERE Title = N'Operating System Concepts');

-- Mevcut kullanıcılar (kendi e-postalarınıza göre güncelleyebilirsiniz)
DECLARE @uAyse INT = (SELECT TOP 1 UserId FROM Users WHERE Email = N'ayse@example.com');
DECLARE @uEmre INT = (SELECT TOP 1 UserId FROM Users WHERE Email = N'emre@example.com');
DECLARE @uMerve INT = (SELECT TOP 1 UserId FROM Users WHERE Email = N'merve@example.com');
DECLARE @uKerem INT = (SELECT TOP 1 UserId FROM Users WHERE Email = N'kerem@example.com');
DECLARE @uZeynep INT = (SELECT TOP 1 UserId FROM Users WHERE Email = N'zeynep@example.com');

-- Beklemede ve Onaylandı kayıtları
INSERT INTO BorrowRequests (UserId, BookId, Status, RequestDate, DeliveryDate, ReturnDate) VALUES
(@uAyse,  @bCleanCode,   N'Pending',  DATEADD(HOUR,-2,@today),  NULL, NULL),
(@uEmre,  @bAlgo,        N'Pending',  DATEADD(DAY,-1,@today),   NULL, NULL),
(@uMerve, @bDesign,      N'Approved', DATEADD(DAY,-2,@today),   NULL, NULL),
(@uKerem, @bPragmatic,   N'Approved', DATEADD(DAY,-3,@today),   NULL, NULL),
(@uZeynep,@bOS,          N'Pending',  DATEADD(HOUR,-5,@today),  NULL, NULL);

USE SmartLibraryDB;
GO

DECLARE @today DATETIME = GETDATE();

-- Kitap/öğrenci kimliklerini kendi tablolarına göre ayarla:
DECLARE @u1 INT = (SELECT TOP 1 UserId FROM Users WHERE Email = N'ayse@example.com');
DECLARE @u2 INT = (SELECT TOP 1 UserId FROM Users WHERE Email = N'emre@example.com');
DECLARE @u3 INT = (SELECT TOP 1 UserId FROM Users WHERE Email = N'merve@example.com');
DECLARE @u4 INT = (SELECT TOP 1 UserId FROM Users WHERE Email = N'kerem@example.com');

DECLARE @b1 INT = (SELECT TOP 1 BookId FROM Books WHERE Title = N'Clean Code');
DECLARE @b2 INT = (SELECT TOP 1 BookId FROM Books WHERE Title = N'Introduction to Algorithms');
DECLARE @b3 INT = (SELECT TOP 1 BookId FROM Books WHERE Title = N'Design Patterns');
DECLARE @b4 INT = (SELECT TOP 1 BookId FROM Books WHERE Title = N'The Pragmatic Programmer');

INSERT INTO BorrowRequests (UserId, BookId, Status, RequestDate, DeliveryDate, ReturnDate) VALUES
-- Beklemede
(@u1, @b1, N'Pending',  DATEADD(HOUR,-1,@today), NULL, NULL),
(@u2, @b2, N'Pending',  DATEADD(HOUR,-3,@today), NULL, NULL),
-- Onaylandı
(@u3, @b3, N'Approved', DATEADD(HOUR,-2,@today), NULL, NULL),
(@u4, @b4, N'Approved', DATEADD(HOUR,-4,@today), NULL, NULL);

USE SmartLibraryDB;
GO

INSERT INTO Books (Title, Author, Category, PublishYear, Stock, Shelf) VALUES
(N'Unavailable Book', N'No Stock Author', N'Fiction', 2022, 0, N'Z1'),
(N'Low Stock Book',   N'Limited Author',  N'Nonfiction', 2021, 1, N'Z2');

-- Yönetici ve ek personel örnekleri (SHA-256 ile hashlenmiş şifreler)
DECLARE @AdminHash NVARCHAR(64) = N'3eb3fe66b31e3b4d10fa70b5cad49c7112294af6ae4e476a1c405155d45aa121'; -- Admin123!
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'admin@example.com')
BEGIN
    INSERT INTO Users (FullName, Email, PasswordHash, SchoolNumber, Phone, Role)
    VALUES (N'Sistem Yöneticisi', N'admin@example.com', @AdminHash, N'9000', N'555-9000', N'Admin');
END

DECLARE @StaffHash NVARCHAR(64) = N'05dd4a1376a72d9a5e0fad32000f7e61651a5cef5c9c9a0c3816c7443dafbf6f'; -- Staff123!
IF NOT EXISTS (SELECT 1 FROM Users WHERE Email = N'staff@example.com')
BEGIN
    INSERT INTO Users (FullName, Email, PasswordHash, SchoolNumber, Phone, Role)
    VALUES (N'Destek Personeli', N'staff@example.com', @StaffHash, N'9001', N'555-9001', N'Staff');
END
