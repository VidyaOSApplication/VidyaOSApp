using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class BookIssue
{
    public int IssueId { get; set; }

    public int? BookId { get; set; }

    public int? StudentId { get; set; }

    public DateOnly? IssueDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateOnly? ReturnDate { get; set; }

    public decimal? FineAmount { get; set; }
}
