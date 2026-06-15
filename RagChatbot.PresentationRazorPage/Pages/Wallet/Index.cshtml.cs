using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using RagChatbot.DataAccess.EntityModels;
using RagChatbot.DataAccess.Interfaces;
using RagChatbot.PresentationRazorPage.Helpers;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace RagChatbot.PresentationRazorPage.Pages.Wallet
{
    public class IndexModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly IAppUserRepository _userRepository;

        public IndexModel(IConfiguration configuration, IAppUserRepository userRepository)
        {
            _configuration = configuration;
            _userRepository = userRepository;
        }

        public IActionResult OnPostPayPremium()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
            {
                return BadRequest("Không xác định được danh tính người dùng.");
            }

            var vnpayConfig = _configuration.GetSection("VnPay");
            string baseUrl = vnpayConfig["BaseUrl"];
            string tmnCode = vnpayConfig["TmnCode"];
            string hashSecret = vnpayConfig["HashSecret"];
            string returnUrl = vnpayConfig["ReturnUrl"];

            var vnpay = new VnPayLibrary();

            vnpay.AddRequestData("vnp_Version", vnpayConfig["Version"]);
            vnpay.AddRequestData("vnp_Command", vnpayConfig["Command"]);
            vnpay.AddRequestData("vnp_TmnCode", tmnCode);

            long amount = 100000 * 100;
            vnpay.AddRequestData("vnp_Amount", amount.ToString());

            vnpay.AddRequestData("vnp_CurrCode", vnpayConfig["CurrCode"]);

            string txnRef = $"{userId}_{DateTime.UtcNow.Ticks}";
            vnpay.AddRequestData("vnp_TxnRef", txnRef);

            vnpay.AddRequestData("vnp_OrderInfo", $"Nang cap tai khoan Premium cho user id {userId}");
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_Locale", vnpayConfig["Locale"]);
            vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);

            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            vnpay.AddRequestData("vnp_IpAddr", ipAddress);
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));

            string paymentUrl = vnpay.CreateRequestUrl(baseUrl, hashSecret);

            return Redirect(paymentUrl);
        }

        public async Task<IActionResult> OnGetVnPayReturnAsync()
        {
            var vnpayConfig = _configuration.GetSection("VnPay");
            string hashSecret = vnpayConfig["HashSecret"];

            var vnpay = new VnPayLibrary();

            foreach (var key in Request.Query.Keys)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, Request.Query[key]);
                }
            }

            string vnp_SecureHash = Request.Query["vnp_SecureHash"];
            string vnp_ResponseCode = Request.Query["vnp_ResponseCode"];
            string vnp_TxnRef = Request.Query["vnp_TxnRef"];

            bool isValidSignature = vnpay.ValidateSignature(vnp_SecureHash, hashSecret);

            if (isValidSignature)
            {
                if (vnp_ResponseCode == "00")
                {
                    var parts = vnp_TxnRef.Split('_');
                    if (parts.Length > 0 && int.TryParse(parts[0], out int userId))
                    {
                        var user = await _userRepository.GetByIdAsync(userId);
                        if (user != null)
                        {
                            user.Subscription = AppUser.SubscriptionType.Premium;
                            _userRepository.Update(user);
                            await _userRepository.SaveChangesAsync();

                            TempData["Success"] = "Giao dịch qua VNPAY thành công! Tài khoản của bạn đã được nâng cấp lên Premium. 👑✨";
                            return RedirectToPage("/Document/Browse");
                        }
                    }
                    return BadRequest("Xử lý dữ liệu tài khoản sau thanh toán thất bại.");
                }
                else
                {
                    TempData["Error"] = $"Giao dịch không thành công. Mã lỗi từ VNPAY: {vnp_ResponseCode}";
                    return RedirectToPage("/Document/Browse");
                }
            }

            return BadRequest("Chữ ký xác thực VNPAY không hợp lệ (Sai HashSecret).");
        }
    }
}

