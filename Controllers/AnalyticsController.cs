using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WebApp.Data;
using WebApp.Models;
using WebApp.Models.ViewModels;

namespace WebApp.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AnalyticsController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public AnalyticsController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Main entry point for analytics
        public async Task<IActionResult> Index()
        {
            var viewModel = await GenerateHealthAnalyticsViewModel();
            return View(viewModel);
        }

        private async Task<HealthAnalyticsViewModel> GenerateHealthAnalyticsViewModel()
        {
            var viewModel = new HealthAnalyticsViewModel();

            var users = await _context.Users
                .Include(u => u.HealthDetails)
                .Include(u => u.PersonalDetails)
                .ToListAsync();

            var userRoles = new Dictionary<string, List<string>>();
            foreach (var user in users)
            {
                userRoles[user.Id] = (await _userManager.GetRolesAsync(user)).ToList();
            }

            var students = users.Where(u => userRoles.ContainsKey(u.Id) && userRoles[u.Id].Contains("Student")).ToList();
            var medicalStaff = users.Where(u => userRoles.ContainsKey(u.Id) && userRoles[u.Id].Contains("MedicalStaff")).ToList();

            viewModel.TotalStudents = students.Count;
            viewModel.TotalMedicalStaff = medicalStaff.Count;
            viewModel.TotalUsers = users.Count;

            // Calculate all statistics
            CalculateVaccinationStats(viewModel, students);
            CalculateEmergencyContactStats(viewModel, students);
            CalculateBloodTypeStats(viewModel, students);
            CalculateHealthAlertsStats(viewModel, students);
            CalculateMedicalDocumentsStats(viewModel, students);
            CalculateDepartmentStats(viewModel, students);

            viewModel.BloodTypeDistribution = students
                .Where(s => s.HealthDetails != null && !string.IsNullOrEmpty(s.HealthDetails.BloodType))
                .GroupBy(s => s.HealthDetails.BloodType)
                .ToDictionary(g => g.Key, g => g.Count());

            return viewModel;
        }

        private void CalculateVaccinationStats(HealthAnalyticsViewModel viewModel, List<Users> students)
        {
            var stat = new RequirementStatistic
            {
                Total = students.Count,
                Completed = students.Count(s => s.HealthDetails != null && !string.IsNullOrEmpty(s.HealthDetails.ImmunizationHistory))
            };
            viewModel.RequirementStats["Vaccination"] = stat;
        }

        private void CalculateEmergencyContactStats(HealthAnalyticsViewModel viewModel, List<Users> students)
        {
            var stat = new RequirementStatistic
            {
                Total = students.Count,
                Completed = students.Count(s =>
                    s.HealthDetails != null &&
                    !string.IsNullOrEmpty(s.HealthDetails.EmergencyContactName) &&
                    !string.IsNullOrEmpty(s.HealthDetails.EmergencyContactPhone))
            };
            viewModel.RequirementStats["EmergencyContact"] = stat;
        }

        private void CalculateBloodTypeStats(HealthAnalyticsViewModel viewModel, List<Users> students)
        {
            var stat = new RequirementStatistic
            {
                Total = students.Count,
                Completed = students.Count(s => s.HealthDetails != null && !string.IsNullOrEmpty(s.HealthDetails.BloodType))
            };
            viewModel.RequirementStats["BloodType"] = stat;
        }

        private void CalculateHealthAlertsStats(HealthAnalyticsViewModel viewModel, List<Users> students)
        {
            int totalAlerts = 0;
            var alertsDistribution = new Dictionary<string, int>();

            foreach (var student in students.Where(s => s.HealthDetails != null && !string.IsNullOrEmpty(s.HealthDetails.HealthAlerts)))
            {
                try
                {
                    var alerts = JsonSerializer.Deserialize<List<string>>(student.HealthDetails.HealthAlerts);
                    if (alerts != null)
                    {
                        totalAlerts += alerts.Count;
                        foreach (var alert in alerts)
                        {
                            if (!alertsDistribution.ContainsKey(alert))
                                alertsDistribution[alert] = 0;
                            alertsDistribution[alert]++;
                        }
                    }
                }
                catch { /* Ignore deserialization errors */ }
            }

            viewModel.TotalHealthAlerts = totalAlerts;
            viewModel.HealthAlertsDistribution = alertsDistribution;
        }

        private void CalculateMedicalDocumentsStats(HealthAnalyticsViewModel viewModel, List<Users> students)
        {
            var xRayStat = new RequirementStatistic
            {
                Total = students.Count,
                Completed = students.Count(s => s.HealthDetails != null && !string.IsNullOrEmpty(s.HealthDetails.XRayFileUrl))
            };
            viewModel.RequirementStats["XRay"] = xRayStat;

            var medCertStat = new RequirementStatistic
            {
                Total = students.Count,
                Completed = students.Count(s => s.HealthDetails != null && !string.IsNullOrEmpty(s.HealthDetails.MedicalCertificateUrl))
            };
            viewModel.RequirementStats["MedicalCertificate"] = medCertStat;

            var vaccinationRecordStat = new RequirementStatistic
            {
                Total = students.Count,
                Completed = students.Count(s => s.HealthDetails != null && !string.IsNullOrEmpty(s.HealthDetails.VaccinationRecordUrl))
            };
            viewModel.RequirementStats["VaccinationRecord"] = vaccinationRecordStat;
        }

        private void CalculateDepartmentStats(HealthAnalyticsViewModel viewModel, List<Users> students)
        {
            var departmentGroups = students
                .Where(s => s.PersonalDetails != null && !string.IsNullOrEmpty(s.PersonalDetails.Department))
                .GroupBy(s => s.PersonalDetails.Department)
                .ToDictionary(
                    g => g.Key,
                    g =>
                    {
                        var studentsInDept = g.ToList();
                        return new DepartmentStats
                        {
                            TotalStudents = studentsInDept.Count,
                            CompletedHealthRequirements = studentsInDept.Count(s =>
                                s.HealthDetails != null &&
                                !string.IsNullOrEmpty(s.HealthDetails.BloodType) &&
                                !string.IsNullOrEmpty(s.HealthDetails.ImmunizationHistory) &&
                                !string.IsNullOrEmpty(s.HealthDetails.EmergencyContactName) &&
                                !string.IsNullOrEmpty(s.HealthDetails.EmergencyContactPhone))
                        };
                    });

            viewModel.DepartmentStatistics = departmentGroups;
        }
    }
}