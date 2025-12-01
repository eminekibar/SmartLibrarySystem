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
