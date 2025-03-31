using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Controllers
{
    [Authorize]
    public class PersonalAppointmentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public PersonalAppointmentController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
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
                TempData["ErrorMessage"] = "Appointment not found";
                return Json(new { success = false });
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

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] AppointmentDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.UserId))
                {
                    TempData["ErrorMessage"] = "Please select a user";
                    return Json(new { success = false });
                }

                if (string.IsNullOrEmpty(dto.Purpose))
                {
                    TempData["ErrorMessage"] = "Please enter a purpose";
                    return Json(new { success = false });
                }

                if (string.IsNullOrEmpty(dto.VisitationDate))
                {
                    TempData["ErrorMessage"] = "Please select a date";
                    return Json(new { success = false });
                }

                var user = await _userManager.FindByIdAsync(dto.UserId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found";
                    return Json(new { success = false });
                }

                if (!DateTime.TryParse(dto.VisitationDate, out var visitationDate))
                {
                    TempData["ErrorMessage"] = "Invalid date format";
                    return Json(new { success = false });
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

                TempData["SuccessMessage"] = "Appointment created successfully";
                return Json(new
                {
                    success = true,
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
                TempData["ErrorMessage"] = "Error creating appointment: " + ex.Message;
                return Json(new { success = false });
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
                    TempData["ErrorMessage"] = "Appointment not found";
                    return Json(new { success = false });
                }

                if (string.IsNullOrEmpty(dto.UserId))
                {
                    TempData["ErrorMessage"] = "Please select a user";
                    return Json(new { success = false });
                }

                if (string.IsNullOrEmpty(dto.Purpose))
                {
                    TempData["ErrorMessage"] = "Please enter a purpose";
                    return Json(new { success = false });
                }

                if (string.IsNullOrEmpty(dto.VisitationDate))
                {
                    TempData["ErrorMessage"] = "Please select a date";
                    return Json(new { success = false });
                }

                var user = await _userManager.FindByIdAsync(dto.UserId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found";
                    return Json(new { success = false });
                }

                if (!DateTime.TryParse(dto.VisitationDate, out var visitationDate))
                {
                    TempData["ErrorMessage"] = "Invalid date format";
                    return Json(new { success = false });
                }

                appointment.UserId = dto.UserId;
                appointment.Purpose = dto.Purpose;
                appointment.VisitationDate = visitationDate;
                appointment.ApprovalStatus = dto.ApprovalStatus ?? appointment.ApprovalStatus;
                appointment.ProgressStatus = dto.ProgressStatus ?? appointment.ProgressStatus;

                _context.PersonalAppointments.Update(appointment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Appointment updated successfully";
                return Json(new
                {
                    success = true,
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
                TempData["ErrorMessage"] = "Error updating appointment: " + ex.Message;
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var appointment = await _context.PersonalAppointments.FindAsync(id);
                if (appointment == null)
                {
                    TempData["ErrorMessage"] = "Appointment not found";
                    return Json(new { success = false });
                }

                _context.PersonalAppointments.Remove(appointment);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Appointment deleted successfully";
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting appointment: " + ex.Message;
                return Json(new { success = false });
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
        }
    }
}