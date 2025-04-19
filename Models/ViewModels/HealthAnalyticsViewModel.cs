namespace WebApp.Models.ViewModels
{
    public class HealthAnalyticsViewModel
    {
        public int TotalStudents { get; set; }
        public int TotalMedicalStaff { get; set; }
        public int TotalUsers { get; set; }

        // Health requirements statistics
        public Dictionary<string, RequirementStatistic> RequirementStats { get; set; } = new Dictionary<string, RequirementStatistic>();

        // For grouping by departments
        public Dictionary<string, DepartmentStats> DepartmentStatistics { get; set; } = new Dictionary<string, DepartmentStats>();

        // Blood type distribution
        public Dictionary<string, int> BloodTypeDistribution { get; set; } = new Dictionary<string, int>();

        // Health alerts statistics
        public int TotalHealthAlerts { get; set; }
        public Dictionary<string, int> HealthAlertsDistribution { get; set; } = new Dictionary<string, int>();

        public int SoonToExpireMedicalCertificates { get; set; }
        public int SoonToExpireVaccinationRecords { get; set; }
        public int SoonToExpireXRays { get; set; }
        public Dictionary<string, int> ExpiringDocumentsByMonth { get; set; } = new Dictionary<string, int>();

        // Document submission timeline
        public Dictionary<string, int> SubmissionsByMonth { get; set; } = new Dictionary<string, int>();
    }

    public class RequirementStatistic
    {
        public int Completed { get; set; }
        public int Total { get; set; }
        public double CompletionRate => Total == 0 ? 0 : (double)Completed / Total * 100;
        public double PendingRate => 100 - CompletionRate;
    }

    public class DepartmentStats
    {
        public int TotalStudents { get; set; }
        public int CompletedHealthRequirements { get; set; }
        public double CompletionRate => TotalStudents == 0 ? 0 : (double)CompletedHealthRequirements / TotalStudents * 100;
    }


}
