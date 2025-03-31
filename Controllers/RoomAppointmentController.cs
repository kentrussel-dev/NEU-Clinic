﻿using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace WebApp.Controllers
{
    public class RoomAppointmentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;
        private readonly EmailService _emailService;
        private readonly NotificationService _notificationService;

        public RoomAppointmentController(
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
                    CreatedBy = user.FullName ?? user.UserName,
                    CreatedOn = DateTime.UtcNow
                };

                appointment.QRCodePath = "/temp/qrcode.png";

                _context.RoomAppointments.Add(appointment);
                await _context.SaveChangesAsync();

                appointment.QRCodePath = qrCodeService.GenerateAppointmentQRCode(appointment.Id);

                _context.RoomAppointments.Update(appointment);
                await _context.SaveChangesAsync();

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
                    userId = rau.UserId,
                    fullName = rau.User.FullName,
                    email = rau.User.Email,
                    studentId = rau.User.PersonalDetails?.StudentId,
                    profileUrl = $"/profile/{rau.User.Id}",
                    profilePictureUrl = rau.User.ProfilePictureUrl ?? "/default-profile.png"
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

            if (appointment.RoomAppointmentUsers.Count >= appointment.UserLimit)
            {
                TempData["ErrorMessage"] = "User limit has been reached.";
                return Ok();
            }

            if (!appointment.RoomAppointmentUsers.Any(rau => rau.UserId == userId))
            {
                appointment.RoomAppointmentUsers.Add(new RoomAppointmentUser
                {
                    UserId = userId,
                    RoomAppointmentId = appointmentId
                });

                await _context.SaveChangesAsync();

                var message = $"You have been added to the appointment '{appointment.RoomName}' scheduled from {appointment.StartTime} to {appointment.EndTime}.";
                await _notificationService.NotifyUserAsync(userId, "System", message);

                var subject = "Appointment Enrollment";
                var emailMessage = $"Dear {user.FullName},<br><br>{message}<br><br>Thank you.";
                await _emailService.SendEmailAsync(user.Email, subject, emailMessage);

                TempData["SuccessMessage"] = "User added to appointment successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "User is already enrolled in the appointment.";
            }

            return Ok();
        }

        [HttpGet("RoomAppointment/GetStudents/{appointmentId}")]
        public async Task<IActionResult> GetStudents(int appointmentId)
        {
            var students = await _context.Users
                .Where(u => _context.UserRoles
                    .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                    .Any(ur => ur.UserId == u.Id && ur.Name == "Student"))
                .Select(u => new Users
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    ProfilePictureUrl = u.ProfilePictureUrl,
                    PersonalDetails = new PersonalDetails
                    {
                        StudentId = u.PersonalDetails.StudentId
                    }
                })
                .ToListAsync();

            var enrolledUserIds = await _context.RoomAppointmentUsers
                .Where(rau => rau.RoomAppointmentId == appointmentId)
                .Select(rau => rau.UserId)
                .ToListAsync();

            ViewBag.EnrolledUserIds = enrolledUserIds;

            return PartialView("_AddUserModal", students);
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

            var userToRemove = appointment.RoomAppointmentUsers.FirstOrDefault(rau => rau.UserId == userId);
            if (userToRemove != null)
            {
                appointment.RoomAppointmentUsers.Remove(userToRemove);
                await _context.SaveChangesAsync();

                var message = $"You have been removed from the appointment '{appointment.RoomName}' scheduled from {appointment.StartTime} to {appointment.EndTime}.";
                await _notificationService.NotifyUserAsync(userId, "System", message);

                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    var subject = "Appointment Removal";
                    var emailMessage = $"Dear {user.FullName},<br><br>{message}<br><br>Thank you.";
                    await _emailService.SendEmailAsync(user.Email, subject, emailMessage);
                }

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

            if (userLimit < appointment.RoomAppointmentUsers.Count)
            {
                TempData["ErrorMessage"] = "User limit cannot be less than the number of enrolled users.";
                return Ok();
            }

            appointment.UserLimit = userLimit;
            _context.RoomAppointments.Update(appointment);
            await _context.SaveChangesAsync();

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
    }
}