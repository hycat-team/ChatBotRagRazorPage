using System;
using System.IO;
using System.Text.RegularExpressions;

var directory = @"d:\Education\ChatBotRag_New\ChatBotRag\ChatBotRagRazorPage\RagChatbot.PresentationRazorPage";
var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);

foreach (var file in files)
{
    if (file.EndsWith(".cs") || file.EndsWith(".cshtml"))
    {
        if (Path.GetFileName(file) == "Program.cs")
        {
            continue;
        }

        var content = File.ReadAllText(file);
        var newContent = Regex.Replace(content, @"^(using|@using)\s+RagChatbot\.DataAccess.*(?:\r\n|\n)?", "", RegexOptions.Multiline);

        if (content != newContent)
        {
            File.WriteAllText(file, newContent, new System.Text.UTF8Encoding(false));
            Console.WriteLine($"Cleaned {file}");
        }
    }
}
Console.WriteLine("Done!");
