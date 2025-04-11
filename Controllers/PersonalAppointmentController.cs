using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace WebApp.Controllers
{
    [Authorize]
    public class PersonalAppointmentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly EmailService _emailService;
        private readonly NotificationService _notificationService;

        public PersonalAppointmentController(
            AppDbContext context,
            UserManager<Users> userManager,
            EmailService emailService,
            NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _notificationService = notificationService;
        }

        public IActionResult Index()
        {
            var appointments = _context.PersonalAppointments
                .Include(a => a.User)
                .ToList();
            return View(appointments);
        }

        [HttpGet]
        public IActionResult GetUsers()
        {
            var users = _userManager.Users
                .Select(u => new { u.Id, u.UserName })
                .ToList();
            return Json(users);
        }

        [HttpGet]
        public IActionResult GetUsersWithProfile()
        {
            var users = _userManager.Users
                .Select(u => new {
                    u.Id,
                    u.UserName,
                    u.FullName,
                    ProfilePictureUrl = u.ProfilePictureUrl ?? "/default-profile.png"
                })
                .ToList();
            return Json(users);
        }

        [HttpGet]
        public async Task<IActionResult> GetAppointment(int id)
        {
            var appointment = await _context.PersonalAppointments
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null)
            {
                return Json(new { success = false, message = "Appointment not found" });
            }

            return Json(new
            {
                success = true,
                appointment = new
                {
                    id = appointment.Id,
                    userId = appointment.UserId,
                    purpose = appointment.Purpose,
                    visitationDate = appointment.VisitationDate.ToString("yyyy-MM-ddTHH:mm"),
                    approvalStatus = appointment.ApprovalStatus,
                    progressStatus = appointment.ProgressStatus,
                    createdAt = appointment.CreatedAt
                }
            });
        }

        private string CreateEmailBody(string title, string userName, string message, string details, string status = null)
        {
            string statusHtml = string.Empty;
            if (!string.IsNullOrEmpty(status))
            {
                string statusColor = status == "Approved" ? "#28a745" :
                                    status == "Rejected" ? "#dc3545" : "#17a2b8";
                statusHtml = $@"
                <div style='margin: 15px 0;'>
                    <strong>Status:</strong> 
                    <span style='color: {statusColor}; font-weight: bold;'>{status}</span>
                </div>";
            }

            return $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #e0e0e0; border-radius: 8px; padding: 20px;'>
                <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin-bottom: 20px;'>
                    <h2 style='color: #343a40; margin: 0;'>{title}</h2>
                </div>
                
                <p>Dear {userName},</p>
                
                <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 15px 0;'>
                    {message}
                </div>
                
                {statusHtml}
                
                <div style='margin: 20px 0;'>
                    <h3 style='color: #343a40; border-bottom: 1px solid #e0e0e0; padding-bottom: 5px;'>Appointment Details:</h3>
                    {details}
                </div>
                
                <div style='margin-top: 25px; padding-top: 15px; border-top: 1px solid #e0e0e0;'>
                    <p>If you have any questions, please contact our support team at <a href='mailto:neucare.sa@gmail.com'>neucare.sa@gmail.com</a></p>
                    <p>Best regards,<br>The Support Team</p>
                </div>
            </div>";
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AppointmentDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.UserId))
                {
                    return Json(new { success = false, message = "Please select a user" });
                }

                if (string.IsNullOrEmpty(dto.Purpose))
                {
                    return Json(new { success = false, message = "Please enter a purpose" });
                }

                if (string.IsNullOrEmpty(dto.VisitationDate))
                {
                    return Json(new { success = false, message = "Please select a date" });
                }

                var user = await _userManager.FindByIdAsync(dto.UserId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                if (!DateTime.TryParse(dto.VisitationDate, out var visitationDate))
                {
                    return Json(new { success = false, message = "Invalid date format" });
                }

                if (visitationDate < DateTime.Now.AddHours(1))
                {
                    return Json(new { success = false, message = "Appointment date must be at least 1 hour from now" });
                }

                var appointment = new PersonalAppointment
                {
                    UserId = dto.UserId,
                    Purpose = dto.Purpose,
                    VisitationDate = visitationDate,
                    ApprovalStatus = dto.ApprovalStatus ?? "Pending",
                    ProgressStatus = dto.ProgressStatus ?? "Upcoming",
                    CreatedAt = DateTime.Now
                };

                _context.PersonalAppointments.Add(appointment);
                await _context.SaveChangesAsync();

                // Create and send notification
                string notificationMessage = $"New appointment created: {appointment.Purpose} on {appointment.VisitationDate:MMM dd, yyyy}";
                await _notificationService.NotifyUserAsync(dto.UserId, "System", notificationMessage);

                // Create and send email
                string emailSubject = "New Appointment Created";
                string emailBody = CreateEmailBody(
                    emailSubject,
                    user.FullName,
                    "Your appointment has been successfully created with the following details:",
                    $@"<p><strong>Purpose:</strong> {appointment.Purpose}</p>
                       <p><strong>Date & Time:</strong> {appointment.VisitationDate:MMMM dd, yyyy 'at' hh:mm tt}</p>
                       <p><strong>Status:</strong> {appointment.ApprovalStatus}</p>");

                await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody);

                return Json(new
                {
                    success = true,
                    message = "Appointment created successfully",
                    appointment = new
                    {
                        id = appointment.Id,
                        userName = user.UserName,
                        fullName = user.FullName,
                        profilePictureUrl = user.ProfilePictureUrl ?? "/default-profile.png",
                        purpose = appointment.Purpose,
                        visitationDate = appointment.VisitationDate.ToString("g"),
                        approvalStatus = appointment.ApprovalStatus,
                        progressStatus = appointment.ProgressStatus,
                        createdAt = appointment.CreatedAt.ToString("g")
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error creating appointment: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] AppointmentDto dto)
        {
            try
            {
                var appointment = await _context.PersonalAppointments.FindAsync(dto.Id);
                if (appointment == null)
                {
                    return Json(new { success = false, message = "Appointment not found" });
                }

                if (string.IsNullOrEmpty(dto.UserId))
                {
                    return Json(new { success = false, message = "Please select a user" });
                }

                if (string.IsNullOrEmpty(dto.Purpose))
                {
                    return Json(new { success = false, message = "Please enter a purpose" });
                }

                if (string.IsNullOrEmpty(dto.VisitationDate))
                {
                    return Json(new { success = false, message = "Please select a date" });
                }

                var user = await _userManager.FindByIdAsync(dto.UserId);
                if (user == null)
                {
                    return Json(new { success = false, message = "User not found" });
                }

                if (!DateTime.TryParse(dto.VisitationDate, out var visitationDate))
                {
                    return Json(new { success = false, message = "Invalid date format" });
                }

                if (visitationDate < DateTime.Now.AddHours(1))
                {
                    return Json(new { success = false, message = "Appointment date must be at least 1 hour from now" });
                }

                // Update appointment
                appointment.UserId = dto.UserId;
                appointment.Purpose = dto.Purpose;
                appointment.VisitationDate = visitationDate;
                appointment.ApprovalStatus = dto.ApprovalStatus ?? appointment.ApprovalStatus;
                appointment.ProgressStatus = dto.ProgressStatus ?? appointment.ProgressStatus;

                _context.PersonalAppointments.Update(appointment);
                await _context.SaveChangesAsync();

                // Create and send notification
                string notificationMessage = appointment.ApprovalStatus switch
                {
                    "Approved" => $"Your appointment for {appointment.Purpose} has been approved",
                    "Rejected" => $"Your appointment for {appointment.Purpose} has been rejected",
                    _ => $"Your appointment for {appointment.Purpose} has been updated"
                };

                await _notificationService.NotifyUserAsync(dto.UserId, "System", notificationMessage);

                // Create and send email
                string emailSubject = appointment.ApprovalStatus switch
                {
                    "Approved" => "Appointment Approved",
                    "Rejected" => "Appointment Rejected",
                    _ => "Appointment Updated"
                };

                string emailBody = CreateEmailBody(
                    emailSubject,
                    user.FullName,
                    notificationMessage,
                    $@"<p><strong>Purpose:</strong> {appointment.Purpose}</p>
                       <p><strong>Date & Time:</strong> {appointment.VisitationDate:MMMM dd, yyyy 'at' hh:mm tt}</p>
                       <p><strong>Status:</strong> {appointment.ApprovalStatus}</p>",
                    appointment.ApprovalStatus);

                await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody);

                return Json(new
                {
                    success = true,
                    message = "Appointment updated successfully",
                    appointment = new
                    {
                        id = appointment.Id,
                        userName = user.UserName,
                        fullName = user.FullName,
                        profilePictureUrl = user.ProfilePictureUrl ?? "/default-profile.png",
                        purpose = appointment.Purpose,
                        visitationDate = appointment.VisitationDate.ToString("g"),
                        approvalStatus = appointment.ApprovalStatus,
                        progressStatus = appointment.ProgressStatus,
                        createdAt = appointment.CreatedAt.ToString("g")
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error updating appointment: {ex.Message}" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var appointment = await _context.PersonalAppointments
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (appointment == null)
                {
                    return Json(new { success = false, message = "Appointment not found" });
                }

                // Create and send notification before deleting
                string notificationMessage = $"Appointment cancelled: {appointment.Purpose} on {appointment.VisitationDate:MMM dd, yyyy}";
                await _notificationService.NotifyUserAsync(appointment.UserId, "System", notificationMessage);

                // Create and send email
                string emailSubject = "Appointment Cancelled";
                string emailBody = CreateEmailBody(
                    emailSubject,
                    appointment.User.FullName,
                    "Your appointment has been cancelled with the following details:",
                    $@"<p><strong>Purpose:</strong> {appointment.Purpose}</p>
                       <p><strong>Date:</strong> {appointment.VisitationDate:MMMM dd, yyyy 'at' hh:mm tt}</p>");

                await _emailService.SendEmailAsync(appointment.User.Email, emailSubject, emailBody);

                // Delete the appointment
                _context.PersonalAppointments.Remove(appointment);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Appointment deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting appointment: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserAppointments()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            var appointments = await _context.PersonalAppointments
                .Where(a => a.UserId == currentUser.Id)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new
                {
                    id = a.Id,
                    purpose = a.Purpose,
                    visitationDate = a.VisitationDate.ToString("g"),
                    createdAt = a.CreatedAt.ToString("g"),
                    approvalStatus = a.ApprovalStatus,
                    progressStatus = a.ProgressStatus
                })
                .ToListAsync();

            return Json(new { success = true, appointments });
        }

        public class AppointmentDto
        {
            public int Id { get; set; }
            public string UserId { get; set; }
            public string Purpose { get; set; }
            public string VisitationDate { get; set; }
            public string ApprovalStatus { get; set; }
            public string ProgressStatus { get; set; }
            public string RejectionReason { get; set; }
        }
    }
}