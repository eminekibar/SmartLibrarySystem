using System;
using System.Collections.Generic;
using System.Linq;
using SmartLibrarySystem.DAL;
using SmartLibrarySystem.Models;

namespace SmartLibrarySystem.BLL
{
    public class RequestService
    {
        private readonly BorrowRequestRepository requestRepository = new BorrowRequestRepository();
        private readonly BookRepository bookRepository = new BookRepository();

        public ValidationResult CreateRequest(int userId, int bookId)
        {
            var result = new ValidationResult();
            var book = bookRepository.GetById(bookId);
            if (book == null)
            {
                result.AddError("Kitap bulunamadı.");
                return result;
            }

            if (book.Stock <= 0)
            {
                result.AddError("Bu kitap şu anda ödünç verilemez. Stokta bulunmamaktadır.");
                return result;
            }

            var request = new BorrowRequest
            {
                UserId = userId,
                BookId = bookId,
                Status = RequestStatus.Pending,
                RequestDate = DateTime.Now
            };

            requestRepository.Add(request);
            return result;
        }

        public IEnumerable<BorrowRequest> GetUserRequests(int userId) => requestRepository.GetByUser(userId);

        public IEnumerable<BorrowRequest> GetRequests(string status = null) => requestRepository.GetAll(status);

        public BorrowRequest GetById(int requestId) => requestRepository.GetById(requestId);

        public ValidationResult UpdateStatus(int requestId, string newStatus)
        {
            var validation = new ValidationResult();
            var existing = requestRepository.GetById(requestId);
            if (existing == null)
            {
                validation.AddError("Talep bulunamadı.");
                return validation;
            }

            if (!IsValidTransition(existing.Status, newStatus))
            {
                validation.AddError("Geçersiz durum geçişi.");
                return validation;
            }

            DateTime? deliveryDate = existing.DeliveryDate;
            DateTime? returnDate = existing.ReturnDate;
            var book = bookRepository.GetById(existing.BookId);

            if (newStatus == RequestStatus.Delivered)
            {
                if (book.Stock <= 0)
                {
                    validation.AddError("Bu kitap şu anda ödünç verilemez. Stokta bulunmamaktadır.");
                    return validation;
                }
                deliveryDate = DateTime.Now;
                bookRepository.AdjustStock(existing.BookId, -1);
            }
            else if (newStatus == RequestStatus.Returned)
            {
                returnDate = DateTime.Now;
                bookRepository.AdjustStock(existing.BookId, 1);
            }

            requestRepository.UpdateStatus(requestId, newStatus, deliveryDate, returnDate);
            return validation;
        }

        public int GetDailyBorrowCount(DateTime date) => requestRepository.GetDailyBorrowCount(date);

        public int GetDailyReturnCount(DateTime date) => requestRepository.GetReturnCount(date);

        public IEnumerable<BorrowRequest> GetOverdue(int allowedDays = 14)
        {
            return requestRepository.GetOverdue(DateTime.Now, allowedDays);
        }

        public IEnumerable<KeyValuePair<string, int>> GetTopBooks(int top) => requestRepository.GetTopBorrowedBooks(top);

        public IDictionary<string, int> GetBorrowStats(DateTime from, DateTime to) => requestRepository.GetBorrowStats(from, to);

        private static bool IsValidTransition(string current, string next)
        {
            var order = RequestStatus.OrderedFlow.ToList();
            var currentIndex = order.IndexOf(current);
            var nextIndex = order.IndexOf(next);
            return currentIndex >= 0 && nextIndex >= 0 && nextIndex - currentIndex == 1;
        }
    }
}
