using System;
using System.Collections.Generic;

namespace WebApp.Models.ViewModels
{
    public class HealthAnalyticsViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalMedicalStaff { get; set; }
        public int TotalUsers { get; set; }
        public int TotalHealthAlerts { get; set; }

        // Requirements statistics
        public Dictionary<string, RequirementStatistic> RequirementStats { get; set; } = new Dictionary<string, RequirementStatistic>();

        // Department statistics
        public Dictionary<string, DepartmentStats> DepartmentStatistics { get; set; } = new Dictionary<string, DepartmentStats>();

        // Blood type distribution
        public Dictionary<string, int> BloodTypeDistribution { get; set; } = new Dictionary<string, int>();

        // Health alerts distribution
        public Dictionary<string, int> HealthAlertsDistribution { get; set; } = new Dictionary<string, int>();

        // Document expiration tracking
        public int SoonToExpireXRays { get; set; }
        public int SoonToExpireMedicalCertificates { get; set; }
        public int SoonToExpireVaccinationRecords { get; set; }

        // Data for charts
        public Dictionary<string, int> ExpiringDocumentsByMonth { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> SubmissionsByMonth { get; set; } = new Dictionary<string, int>();

        // Document statistics
        public DocumentStatisticsViewModel DocumentStatistics { get; set; } = new DocumentStatisticsViewModel();

        public Dictionary<string, int> XRaySubmissionsByMonth { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> MedicalCertificateSubmissionsByMonth { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> VaccinationRecordSubmissionsByMonth { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> OtherDocumentsSubmissionsByMonth { get; set; } = new Dictionary<string, int>();
    }

    public class RequirementStatistic
    {
        public int Total { get; set; }
        public int Completed { get; set; }
        public double CompletionRate => Total > 0 ? (double)Completed / Total * 100 : 0;
        public double PendingRate => Total > 0 ? (double)(Total - Completed) / Total * 100 : 0;
    }

    public class DepartmentStats
    {
        public int TotalStudents { get; set; }
        public int CompletedHealthRequirements { get; set; }
        public double CompletionRate => TotalStudents > 0 ? (double)CompletedHealthRequirements / TotalStudents * 100 : 0;
    }
}