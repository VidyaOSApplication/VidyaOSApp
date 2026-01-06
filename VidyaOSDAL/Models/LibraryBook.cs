using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class LibraryBook
{
    public int BookId { get; set; }

    public int? SchoolId { get; set; }

    public string? BookTitle { get; set; }

    public string? Author { get; set; }

    public string? Isbn { get; set; }

    public int? TotalCopies { get; set; }

    public int? AvailableCopies { get; set; }
}
