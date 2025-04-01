

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApp.Data;
using WebApp.Models;

namespace WebApp.Controllers
{
    public class AppointmentController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public AppointmentController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: appointment/{id}
        [HttpGet("appointment/{id}")]
        public async Task<IActionResult> ViewAppointment(int id)
        {
            var appointment = await _context.RoomAppointments
                .Include(ra => ra.RoomAppointmentUsers)
                .ThenInclude(rau => rau.User)
                .FirstOrDefaultAsync(ra => ra.Id == id);

            if (appointment == null)
            {
                TempData["ErrorMessage"] = "Appointment not found.";
                return NotFound("Appointment not found.");
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var currentUserId = currentUser?.Id; // Get the current user's ID

            var isEnrolled = currentUser != null && appointment.RoomAppointmentUsers.Any(rau => rau.UserId == currentUserId);

            var viewModel = new AppointmentViewModel
            {
                Appointment = appointment,
                IsEnrolled = isEnrolled,
                CurrentUserId = currentUserId, // Pass the current user's ID to the view
                EnrolledUsers = appointment.RoomAppointmentUsers
                    .Select(rau => new EnrolledUserViewModel
                    {
                        UserId = rau.User.Id,
                        FullName = rau.User.FullName,
                        Email = rau.User.Email,
                        ProfilePictureUrl = rau.User.ProfilePictureUrl ?? "/default-profile.png",
                        ProfileUrl = $"/profile/{rau.User.Id}"
                    })
                    .ToList()
            };

            return View(viewModel);
        }
    }
    // ViewModel for the appointment details
    public class AppointmentViewModel
    {
        public RoomAppointment Appointment { get; set; }
        public bool IsEnrolled { get; set; }
        public string CurrentUserId { get; set; } // Add this property
        public List<EnrolledUserViewModel> EnrolledUsers { get; set; }
    }

    // ViewModel for enrolled users
    public class EnrolledUserViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string ProfilePictureUrl { get; set; }
        public string ProfileUrl { get; set; }
    }

}

