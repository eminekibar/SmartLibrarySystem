using System;
using System.Collections.Generic;
using SmartLibrarySystem.DAL;
using SmartLibrarySystem.Models;

namespace SmartLibrarySystem.BLL
{
    public class BookService
    {
        private readonly BookRepository bookRepository = new BookRepository();

        public IEnumerable<Book> GetAll() => bookRepository.GetAll();

        public IEnumerable<Book> Search(string category, string author, int? year, string keyword)
            => bookRepository.Search(category, author, year, keyword);

        public Book GetById(int bookId) => bookRepository.GetById(bookId);

        public ValidationResult AddBook(Book book)
        {
            var validation = ValidateBook(book);
            if (validation.IsValid)
            {
                bookRepository.Add(book);
            }
            return validation;
        }

        public ValidationResult UpdateBook(Book book)
        {
            var validation = ValidateBook(book);
            if (validation.IsValid)
            {
                bookRepository.Update(book);
            }
            return validation;
        }

        public void DeleteBook(int bookId) => bookRepository.Delete(bookId);

        public void UpdateStock(int bookId, int newStock) => bookRepository.UpdateStock(bookId, newStock);

        public void AdjustStock(int bookId, int delta) => bookRepository.AdjustStock(bookId, delta);

        private static ValidationResult ValidateBook(Book book)
        {
            var result = new ValidationResult();
            if (string.IsNullOrWhiteSpace(book.Title))
            {
                result.AddError("Kitap başlığı boş bırakılamaz.");
            }
            if (string.IsNullOrWhiteSpace(book.Author))
            {
                result.AddError("Yazar bilgisi boş bırakılamaz.");
            }
            if (string.IsNullOrWhiteSpace(book.Category))
            {
                result.AddError("Kategori boş bırakılamaz.");
            }
            if (book.PublishYear <= 0)
            {
                result.AddError("Yayın yılı geçersiz.");
            }
            if (book.Stock < 0)
            {
                result.AddError("Stok 0'dan küçük olamaz.");
            }
            if (string.IsNullOrWhiteSpace(book.Shelf))
            {
                result.AddError("Raf bilgisi boş bırakılamaz.");
            }

            return result;
        }
    }
}
