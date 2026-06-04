using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;
using RagChatbot.Presentation.Helpers;
using System.Security.Claims;

namespace RagChatbot.Presentation.Controllers
{
    [Route("api/[controller]")]
    public class WalletsController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IAppUserRepository _userRepository;

        public WalletsController(IConfiguration configuration, IAppUserRepository userRepository)
        {
            _configuration = configuration;
            _userRepository = userRepository;
        }

        // 1. Endpoint kích hoạt luồng thanh toán đi sang VNPAY
        [Authorize(Roles = "Student")]
        [HttpPost("pay-premium")]
        public IActionResult CreatePayment()
        {
            // Lấy ID học sinh đang đăng nhập
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
            {
                return BadRequest("Không xác định được danh tính người dùng.");
            }

            // Đọc cấu hình từ appsettings.json
            var vnpayConfig = _configuration.GetSection("VnPay");
            string baseUrl = vnpayConfig["BaseUrl"];
            string tmnCode = vnpayConfig["TmnCode"];
            string hashSecret = vnpayConfig["HashSecret"];
            string returnUrl = vnpayConfig["ReturnUrl"];

            var vnpay = new VnPayLibrary();

            // Gắn các tham số chuẩn VNPAY yêu cầu
            vnpay.AddRequestData("vnp_Version", vnpayConfig["Version"]);
            vnpay.AddRequestData("vnp_Command", vnpayConfig["Command"]);
            vnpay.AddRequestData("vnp_TmnCode", tmnCode);

            // Số tiền thanh toán gói Premium (Ví dụ: 100.000 VND -> VNPAY cần nhân thêm 100 thành 10000000)
            long amount = 100000 * 100;
            vnpay.AddRequestData("vnp_Amount", amount.ToString());

            vnpay.AddRequestData("vnp_CurrCode", vnpayConfig["CurrCode"]);

            // Tạo mã giao dịch độc nhất chứa userId để lát bóc tách
            string txnRef = $"{userId}_{DateTime.UtcNow.Ticks}";
            vnpay.AddRequestData("vnp_TxnRef", txnRef);

            vnpay.AddRequestData("vnp_OrderInfo", $"Nang cap tai khoan Premium cho user id {userId}");
            vnpay.AddRequestData("vnp_OrderType", "other"); // Loại hàng hóa
            vnpay.AddRequestData("vnp_Locale", vnpayConfig["Locale"]);
            vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);

            // Lấy IP Client thực tế hoặc gán mặc định local
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            vnpay.AddRequestData("vnp_IpAddr", ipAddress);
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));

            // Tạo URL chuyển hướng qua cổng thanh toán sandbox
            string paymentUrl = vnpay.CreateRequestUrl(baseUrl, hashSecret);

            return Redirect(paymentUrl);
        }

        // 2. Endpoint hứng dữ liệu VNPAY trả về (Khớp 100% với ReturnUrl cấu hình)
        [HttpGet("vnpay-return")]
        public async Task<IActionResult> VnPayReturn()
        {
            var vnpayConfig = _configuration.GetSection("VnPay");
            string hashSecret = vnpayConfig["HashSecret"];

            var vnpay = new VnPayLibrary();

            // Đọc toàn bộ tham số do VNPAY bắn về qua URL Query
            foreach (var key in Request.Query.Keys)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, Request.Query[key]);
                }
            }

            string vnp_SecureHash = Request.Query["vnp_SecureHash"];
            string vnp_ResponseCode = Request.Query["vnp_ResponseCode"]; // Mã phản hồi kết quả giao dịch
            string vnp_TxnRef = Request.Query["vnp_TxnRef"]; // Mã giao dịch gốc lúc gửi đi

            // Kiểm tra tính toàn vẹn của chữ ký mã hóa bảo mật
            bool isValidSignature = vnpay.ValidateSignature(vnp_SecureHash, hashSecret);

            if (isValidSignature)
            {
                // Nếu vnp_ResponseCode == "00" tức là người dùng đã thanh toán thành công
                if (vnp_ResponseCode == "00")
                {
                    // Bóc tách lấy userId từ cụm mã giao dịch USERID_TICKS
                    var parts = vnp_TxnRef.Split('_');
                    if (parts.Length > 0 && int.TryParse(parts[0], out int userId))
                    {
                        var user = await _userRepository.GetByIdAsync(userId);
                        if (user != null)
                        {
                            // THÀNH CÔNG: Chuyển đổi trạng thái sang gói Premium thực tế!
                            user.Subscription = AppUser.SubscriptionType.Premium;
                            _userRepository.Update(user);
                            await _userRepository.SaveChangesAsync();

                            TempData["Success"] = "Giao dịch qua VNPAY thành công! Tài khoản của bạn đã được nâng cấp lên Premium. 👑✨";
                            return RedirectToAction("Browse", "Document");
                        }
                    }
                    return BadRequest("Xử lý dữ liệu tài khoản sau thanh toán thất bại.");
                }
                else
                {
                    // Thanh toán thất bại hoặc người dùng bấm hủy giao dịch
                    TempData["Error"] = $"Giao dịch không thành công. Mã lỗi từ VNPAY: {vnp_ResponseCode}";
                    return RedirectToAction("Browse", "Document");
                }
            }

            return BadRequest("Chữ ký xác thực VNPAY không hợp lệ (Sai HashSecret).");
        }
    }
}
