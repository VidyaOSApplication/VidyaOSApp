using System;
using System.Collections.Generic;

namespace VidyaOSDAL.Models;

public partial class User
{
    public int UserId { get; set; }

    public int? SchoolId { get; set; }

    public string? Username { get; set; }

    public string? PasswordHash { get; set; }

    public string? Role { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public bool? IsFirstLogin { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }
}
