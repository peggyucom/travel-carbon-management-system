using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace StarterM.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required, MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Role { get; set; } = "Employee"; // Employee / Manager

        public bool IsActive { get; set; } = true;

        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }

        public string? ManagerId { get; set; }
        public ApplicationUser? Manager { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<ExpenseRecord> Expenses { get; set; } = new List<ExpenseRecord>();
        public ICollection<DailyTrip> DailyTrips { get; set; } = new List<DailyTrip>();
        public ICollection<Application> Applications { get; set; } = new List<Application>();
    }
}
