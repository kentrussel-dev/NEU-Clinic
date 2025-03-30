// Models/StudentVisitation.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class StudentVisitation
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public Users User { get; set; }
        public string Purpose { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime VisitationDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}