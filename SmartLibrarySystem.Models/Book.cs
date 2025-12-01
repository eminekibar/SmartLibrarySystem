using System;

namespace SmartLibrarySystem.Models
{
    public class Book
    {
        public int BookId { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Category { get; set; }
        public int PublishYear { get; set; }
        public int Stock { get; set; }
        public string Shelf { get; set; }

        public override string ToString()
        {
            return $"{Title} - {Author}";
        }
    }
}
