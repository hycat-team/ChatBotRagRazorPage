$path = "d:\Education\ChatBotRag_New\ChatBotRag\ChatBotRagRazorPage\RagChatbot.PresentationRazorPage\Hubs\ChatHub.cs"
$lines = [System.IO.File]::ReadAllLines($path)

$lines[296] = '                    var systemPrompt = $@"Bạn là trợ lý học tập thông minh. Bạn có thể trò chuyện, chào hỏi thân thiện.'
$lines[297] = 'Tuy nhiên, đối với các câu hỏi tìm kiếm thông tin, bạn phải tuân thủ nghiêm ngặt GROUNDING_RULE: Chỉ sử dụng thông tin từ [NGỮ CẢNH TÀI LIỆU] dưới đây.'
$lines[298] = 'Tuyệt đối không sử dụng kiến thức bên ngoài. '
$lines[299] = "LƯU Ý QUAN TRỌNG: Nếu câu hỏi của người dùng ngắn gọn, thiếu chủ ngữ (ví dụ: 'bao nhiêu tuổi?'), hãy chủ động suy luận từ các thông tin, nhân vật, hoặc sự kiện có trong ngữ cảnh để đưa ra câu trả lời hợp lý nhất."
$lines[300] = "Nếu hoàn toàn không có thông tin nào liên quan trong ngữ cảnh, hãy trả lời: 'Hệ thống không tìm thấy thông tin trong các tài liệu đã chọn'."
$lines[301] = ''
$lines[302] = '[NGỮ CẢNH TÀI LIỆU]:'

$lines[326] = '                        fullResponse.Append("\n\n*(Đã dừng tạo)*");'
$lines[327] = '                        await Clients.Caller.SendAsync("ReceiveToken", "\n\n*(Đã dừng tạo)*", false);'

$lines[332] = '                    if (finalResponseStr.Contains("Hệ thống không tìm thấy thông tin trong các tài liệu đã chọn"))'

[System.IO.File]::WriteAllLines($path, $lines, [System.Text.Encoding]::UTF8)
