using System;

namespace SmartLibrarySystem.Models
{
    public class BorrowRequest
    {
        public int RequestId { get; set; }
        public int UserId { get; set; }
        public int BookId { get; set; }
        public string Status { get; set; }
        public DateTime RequestDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public DateTime? ReturnDate { get; set; }

        // Convenience fields for UI projections
        public string UserName { get; set; }
        public string BookTitle { get; set; }
        public string BookAuthor { get; set; }
    }
}
