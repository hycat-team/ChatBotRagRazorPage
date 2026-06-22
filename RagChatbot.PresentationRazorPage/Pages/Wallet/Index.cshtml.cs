using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.PresentationRazorPage.Helpers;
using System.Security.Claims;

namespace RagChatbot.PresentationRazorPage.Pages.Wallet
{
    public class IndexModel : PageModel
    {
        private readonly RagChatbot.Business.Interfaces.IAppUserService _userService;

        public IndexModel(RagChatbot.Business.Interfaces.IAppUserService userService)
        {
            _userService = userService;
        }

        public void OnGet()
        {
        }

        // 1. HÀM TẠO LINK VÀ ĐIỀU HƯỚNG SANG VNPAY
        public IActionResult OnPostPayPremium()
        {
            string vnp_Url = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            string vnp_TmnCode = "QGEPANHQ";
            string vnp_HashSecret = "INOS733PMZQZR8KO4EH5VG8L0TKPRQ66";
            string vnp_ReturnUrl = "https://localhost:7030/Wallet?handler=VnpayReturn";

            var vnpay = new VnPayLibrary();

            // SỬA TẠI ĐÂY: Xử lý ép IP về IPv4 chuẩn để không bị lỗi ký tự ":" trên localhost
            string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            if (ipAddress == "::1")
            {
                ipAddress = "127.0.0.1";
            }

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);
            vnpay.AddRequestData("vnp_Amount", (100000 * 100).ToString());
            vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", ipAddress);
            vnpay.AddRequestData("vnp_Locale", "vn");

            // SỬA TẠI ĐÂY: Viết liền không dấu/không cách để triệt tiêu hoàn toàn rủi ro lệch mã hóa URL
            vnpay.AddRequestData("vnp_OrderInfo", "ThanhToanPremium");

            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl);
            vnpay.AddRequestData("vnp_TxnRef", DateTime.Now.Ticks.ToString());

            string paymentUrl = vnpay.CreateRequestUrl(vnp_Url, vnp_HashSecret);

            return Redirect(paymentUrl);
        }

        // 2. HÀM ĐÓN KẾT QUẢ TRẢ VỀ TỪ VNPAY
        public async Task<IActionResult> OnGetVnpayReturnAsync()
        {
            string vnp_HashSecret = "INOS733PMZQZR8KO4EH5VG8L0TKPRQ66";
            var vnpay = new VnPayLibrary();

            foreach (var key in Request.Query.Keys)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_") && key != "vnp_SecureHash")
                {
                    vnpay.AddResponseData(key, Request.Query[key].ToString());
                }
            }

            string vnp_SecureHash = Request.Query["vnp_SecureHash"];
            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

            if (checkSignature)
            {
                string responseCode = vnpay.GetResponseData("vnp_ResponseCode");
                if (responseCode == "00")
                {
                    var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (int.TryParse(userIdStr, out int uId))
                    {
                        var currentUser = await _userService.GetByIdAsync(uId);
                        if (currentUser != null)
                        {
                            currentUser.Subscription = "Premium";
                            await _userService.UpdateUserAsync(currentUser);
                        }
                    }

                    ViewData["Message"] = "🎉 Thanh toán thành công! Tài khoản của bạn đã được nâng cấp lên Premium. Bạn đã có quyền đọc toàn bộ tài liệu.";
                }
                else
                {
                    ViewData["Message"] = $"❌ Giao dịch thất bại hoặc đã bị hủy bởi người dùng. (Mã lỗi: {responseCode})";
                }
            }
            else
            {
                ViewData["Message"] = "⚠️ Cảnh báo: Chữ ký phản hồi không hợp lệ! Dữ liệu có thể đã bị can thiệp.";
            }

            return Page();
        }
    }
}