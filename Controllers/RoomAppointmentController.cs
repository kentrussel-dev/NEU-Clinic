using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System.Threading.Tasks;
using System.Linq;

namespace WebApp.Controllers
{
    public class RoomAppointmentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly EmailService _emailService;

        public RoomAppointmentController(AppDbContext context, UserManager<Users> userManager, EmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        public IActionResult Index()
        {
            var appointments = _context.RoomAppointments
                .Include(ra => ra.RoomAppointmentUsers)
                .ThenInclude(rau => rau.User)
                .ToList();

            return View(appointments);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAppointment(int appointmentId)
        {
            var appointment = await _context.RoomAppointments
                .Include(ra => ra.RoomAppointmentUsers)
                .FirstOrDefaultAsync(ra => ra.Id == appointmentId);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return Ok();
            }

            _context.RoomAppointments.Remove(appointment);
            await _context.SaveChangesAsync();

            // Send notification to the creator of the appointment
            await SendSystemNotification(appointment.CreatedBy, "Your room appointment has been deleted.");

            TempData["SuccessMessage"] = "Appointment deleted successfully.";
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAppointment(int appointmentId, string roomName, DateTime startTime, DateTime endTime, string description, int userLimit)
        {
            var appointment = await _context.RoomAppointments
                .Include(ra => ra.RoomAppointmentUsers)
                .FirstOrDefaultAsync(ra => ra.Id == appointmentId);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return Ok();
            }

            if (userLimit < appointment.RoomAppointmentUsers.Count)
            {
                TempData["ErrorMessage"] = "User limit cannot be less than the number of enrolled users.";
                return Ok();
            }

            appointment.RoomName = roomName;
            appointment.StartTime = startTime;
            appointment.EndTime = endTime;
            appointment.Description = description;
            appointment.UserLimit = userLimit;

            _context.RoomAppointments.Update(appointment);
            await _context.SaveChangesAsync();

            // Send notification to the creator of the appointment
            await SendSystemNotification(appointment.CreatedBy, "Your room appointment has been updated.");

            TempData["SuccessMessage"] = "Appointment updated successfully.";
            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> CreateAppointment(string roomName, DateTime startTime, DateTime endTime, string description, int userLimit, [FromServices] QRCodeService qrCodeService)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return Ok();
                }

                var appointment = new RoomAppointment
                {
                    RoomName = roomName,
                    StartTime = startTime,
                    EndTime = endTime,
                    Description = description,
                    UserLimit = userLimit,
                    CreatedBy = user.Id, // Store the user ID instead of name
                    CreatedOn = DateTime.UtcNow
                };

                appointment.QRCodePath = "/temp/qrcode.png"; // Temporary path

                _context.RoomAppointments.Add(appointment);
                await _context.SaveChangesAsync();

                appointment.QRCodePath = qrCodeService.GenerateAppointmentQRCode(appointment.Id);

                _context.RoomAppointments.Update(appointment);
                await _context.SaveChangesAsync();

                // Send notification to the creator of the appointment
                await SendSystemNotification(user.Id, "Your room appointment has been created successfully.");

                TempData["SuccessMessage"] = "Appointment created successfully.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Failed to create appointment: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEnrolledUsers(int appointmentId)
        {
            var appointment = await _context.RoomAppointments
                .Include(ra => ra.RoomAppointmentUsers)
                .ThenInclude(rau => rau.User)
                .FirstOrDefaultAsync(ra => ra.Id == appointmentId);

            if (appointment == null)
            {
                return NotFound();
            }

            var enrolledUsers = appointment.RoomAppointmentUsers
                .Select(rau => new
                {
                    userId = rau.UserId, // Include userId for removal
                    fullName = rau.User.FullName,
                    email = rau.User.Email,
                    studentId = rau.User.PersonalDetails?.StudentId, // Assuming StudentId is in PersonalDetails
                    profileUrl = $"/profile/{rau.User.Id}", // Example profile URL
                    profilePictureUrl = rau.User.ProfilePictureUrl ?? "/default-profile.png" // Profile picture URL
                })
                .ToList();

            return Json(enrolledUsers);
        }

        [HttpPost]
        public async Task<IActionResult> AddUserToAppointment(int appointmentId, string userId)
        {
            var appointment = await _context.RoomAppointments
                .Include(ra => ra.RoomAppointmentUsers)
                .FirstOrDefaultAsync(ra => ra.Id == appointmentId);

            var user = await _context.Users.FindAsync(userId);

            if (appointment == null || user == null)
            {
                TempData["ErrorMessage"] = "Appointment or user not found.";
                return Ok();
            }

            // Check if the user limit has been reached
            if (appointment.RoomAppointmentUsers.Count >= appointment.UserLimit)
            {
                TempData["ErrorMessage"] = "User limit has been reached.";
                return Ok();
            }

            // Check if the user is already enrolled
            if (!appointment.RoomAppointmentUsers.Any(rau => rau.UserId == userId))
            {
                appointment.RoomAppointmentUsers.Add(new RoomAppointmentUser
                {
                    UserId = userId,
                    RoomAppointmentId = appointmentId
                });

                await _context.SaveChangesAsync();

                // Send notification to the user who was added
                await SendSystemNotification(userId, "You have been added to a room appointment.");

                TempData["SuccessMessage"] = "User added to appointment successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "User is already enrolled in the appointment.";
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> RemoveUserFromAppointment(int appointmentId, string userId)
        {
            var appointment = await _context.RoomAppointments
                .Include(ra => ra.RoomAppointmentUsers)
                .FirstOrDefaultAsync(ra => ra.Id == appointmentId);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return Ok();
            }

            // Find the user to remove
            var userToRemove = appointment.RoomAppointmentUsers.FirstOrDefault(rau => rau.UserId == userId);
            if (userToRemove != null)
            {
                appointment.RoomAppointmentUsers.Remove(userToRemove);
                await _context.SaveChangesAsync();

                // Send notification to the user who was removed
                await SendSystemNotification(userId, "You have been removed from a room appointment.");

                TempData["SuccessMessage"] = "User removed from appointment successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "User not found in the appointment.";
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateUserLimit(int appointmentId, int userLimit)
        {
            var appointment = await _context.RoomAppointments.FindAsync(appointmentId);
            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return Ok();
            }

            // Ensure the new limit is not less than the current number of enrolled users
            if (userLimit < appointment.RoomAppointmentUsers.Count)
            {
                TempData["ErrorMessage"] = "User limit cannot be less than the number of enrolled users.";
                return Ok();
            }

            appointment.UserLimit = userLimit;
            _context.RoomAppointments.Update(appointment);
            await _context.SaveChangesAsync();

            // Send notification to the creator of the appointment
            await SendSystemNotification(appointment.CreatedBy, "The user limit for your room appointment has been updated.");

            TempData["SuccessMessage"] = "User limit updated successfully.";
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetAppointmentDetails(int appointmentId)
        {
            var appointment = await _context.RoomAppointments
                .Include(ra => ra.RoomAppointmentUsers)
                .FirstOrDefaultAsync(ra => ra.Id == appointmentId);

            if (appointment == null)
            {
                return NotFound();
            }

            var result = new
            {
                roomAppointmentUsers = appointment.RoomAppointmentUsers.Count,
                userLimit = appointment.UserLimit
            };

            return Json(result);
        }

        private async Task SendSystemNotification(string userId, string message)
        {
            var notification = new Notification
            {
                UserId = userId,
                SenderEmail = "System", // Set the sender as "System"
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send email notification
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                var subject = "System Notification";
                var emailMessage = $"You have received a new system notification:<br><br>{message}";
                await _emailService.SendEmailAsync(user.Email, subject, emailMessage);
            }
        }
    }
}