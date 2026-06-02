using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagChatbot.Business.Interfaces;
using RagChatbot.DataAccess.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace RagChatbot.Presentation.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IAppUserRepository _userRepository;
        private readonly IAuthService _authService;

        public AdminController(IAppUserRepository userRepository, IAuthService authService)
        {
            _userRepository = userRepository;
            _authService = authService;
        }

        public async Task<IActionResult> Index()
        {
            var users = _userRepository.Query()
                .Where(u => u.Role == "Lecturer")
                .ToList();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> CreateLecturer(string username, string password, string firstName, string lastName)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.";
                return RedirectToAction("Index");
            }

            var success = await _authService.RegisterAsync(username, password, "Lecturer", firstName, lastName);
            if (!success)
            {
                TempData["Error"] = "Tên đăng nhập đã tồn tại.";
            }
            else
            {
                TempData["Success"] = "Tạo tài khoản Giảng viên thành công.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteLecturer(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user != null && user.Role == "Lecturer")
            {
                _userRepository.Remove(user);
                await _userRepository.SaveChangesAsync();
                TempData["Success"] = "Xóa tài khoản thành công.";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy tài khoản hoặc không được phép xóa.";
            }

            return RedirectToAction("Index");
        }
    }
}
