using System.Collections.Generic;

namespace WebApp.Models.ViewModels
{
    public class DocumentStatisticsViewModel
    {
        // Total document count statistics
        public int TotalSubmitted { get; set; }
        public int TotalApproved { get; set; }
        public int TotalRejected { get; set; }
        public int TotalPending { get; set; }

        // Document type-specific statistics
        public Dictionary<string, DocumentTypeStats> DocumentTypeStatistics { get; set; } = new Dictionary<string, DocumentTypeStats>();
    }

    public class DocumentTypeStats
    {
        public string DocumentType { get; set; }
        public int Submitted { get; set; }
        public int Approved { get; set; }
        public int Rejected { get; set; }
        public int Pending { get; set; }

        // Calculated properties for percentages
        public double ApprovalRate => Submitted > 0 ? (double)Approved / Submitted * 100 : 0;
        public double RejectionRate => Submitted > 0 ? (double)Rejected / Submitted * 100 : 0;
        public double PendingRate => Submitted > 0 ? (double)Pending / Submitted * 100 : 0;
    }
}