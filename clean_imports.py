import os
import re

directory = r'd:\Education\ChatBotRag_New\ChatBotRag\ChatBotRagRazorPage\RagChatbot.PresentationRazorPage'
for root, dirs, files in os.walk(directory):
    for file in files:
        if file.endswith('.cs') or file.endswith('.cshtml'):
            filepath = os.path.join(root, file)
            # Do not touch Program.cs
            if file == 'Program.cs':
                continue
            with open(filepath, 'r', encoding='utf-8') as f:
                content = f.read()
            
            new_content = re.sub(r'^(using|@using)\s+RagChatbot\.DataAccess.*\n?', '', content, flags=re.MULTILINE)
            
            if new_content != content:
                with open(filepath, 'w', encoding='utf-8') as f:
                    f.write(new_content)
