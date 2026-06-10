import os
import re
import sys

def convert_markdown_to_html(md_text):
    # Basic markdown parsing to clean, structured HTML
    html_lines = []
    in_code_block = False
    in_list = False
    in_table = False
    table_headers = []
    table_rows = []
    
    # CSS stylesheet for a premium, academic, and modern document aesthetic
    css_style = """
    <style>
        body {
            font-family: 'Times New Roman', Times, serif;
            line-height: 1.6;
            color: #333333;
            max-width: 900px;
            margin: 0 auto;
            padding: 40px 60px;
            background-color: #ffffff;
        }
        h1, h2, h3, h4 {
            color: #111111;
            font-family: 'Times New Roman', Times, serif;
            font-weight: bold;
            page-break-after: avoid;
        }
        h1 {
            font-size: 24pt;
            text-align: center;
            margin-top: 40px;
            margin-bottom: 20px;
            text-transform: uppercase;
            border-bottom: 2px solid #111111;
            padding-bottom: 10px;
        }
        h2 {
            font-size: 16pt;
            margin-top: 30px;
            margin-bottom: 15px;
            border-bottom: 1px solid #cccccc;
            padding-bottom: 5px;
        }
        h3 {
            font-size: 14pt;
            margin-top: 20px;
            margin-bottom: 10px;
        }
        h4 {
            font-size: 12pt;
            margin-top: 15px;
            margin-bottom: 5px;
            font-style: italic;
        }
        p {
            font-size: 12pt;
            text-indent: 0.5in;
            text-align: justify;
            margin-bottom: 15px;
        }
        .no-indent {
            text-indent: 0;
        }
        blockquote {
            margin: 20px 0;
            padding: 15px 20px;
            background-color: #f9f9f9;
            border-left: 4px solid #333333;
            font-style: italic;
        }
        ul, ol {
            margin-left: 30px;
            margin-bottom: 15px;
            font-size: 12pt;
        }
        li {
            margin-bottom: 5px;
            text-align: justify;
        }
        table {
            width: 100%;
            border-collapse: collapse;
            margin: 25px 0;
            font-size: 11pt;
            page-break-inside: avoid;
        }
        th, td {
            border: 1px solid #666666;
            padding: 8px 12px;
            text-align: left;
        }
        th {
            background-color: #f2f2f2;
            font-weight: bold;
            text-align: center;
        }
        tr:nth-child(even) {
            background-color: #fafafa;
        }
        pre {
            background-color: #2d3748;
            color: #f7fafc;
            padding: 15px;
            border-radius: 6px;
            overflow-x: auto;
            font-family: 'Consolas', 'Courier New', Courier, monospace;
            font-size: 10pt;
            line-height: 1.4;
            margin: 20px 0;
            page-break-inside: avoid;
            border: 1px solid #4a5568;
        }
        code {
            font-family: 'Consolas', 'Courier New', Courier, monospace;
            background-color: #edf2f7;
            color: #2d3748;
            padding: 2px 6px;
            border-radius: 4px;
            font-size: 10.5pt;
        }
        pre code {
            background-color: transparent;
            color: inherit;
            padding: 0;
            border-radius: 0;
            font-size: inherit;
        }
        .page-break {
            page-break-before: always;
        }
        .center {
            text-align: center;
            text-indent: 0;
        }
        .title-page {
            height: 900px;
            display: flex;
            flex-direction: column;
            justify-content: space-between;
            text-align: center;
            border: 3px double #000000;
            padding: 40px;
            margin-bottom: 50px;
            page-break-after: always;
        }
        .divider {
            margin: 20px auto;
            width: 40%;
            border-top: 1px solid #000000;
        }
    </style>
    """
    
    html_lines.append("<!DOCTYPE html>")
    html_lines.append("<html>")
    html_lines.append("<head>")
    html_lines.append("    <meta charset='utf-8'>")
    html_lines.append("    <title>Do An Tot Nghiep - Tran Van Thuy</title>")
    html_lines.append(css_style)
    html_lines.append("</head>")
    html_lines.append("<body>")
    
    lines = md_text.split('\n')
    idx = 0
    while idx < len(lines):
        line = lines[idx]
        stripped = line.strip()
        
        # Code block handler
        if stripped.startswith("```"):
            if in_code_block:
                html_lines.append("</code></pre>")
                in_code_block = False
            else:
                lang = stripped[3:].strip()
                html_lines.append(f"<pre><code class='language-{lang}'>")
                in_code_block = True
            idx += 1
            continue
            
        if in_code_block:
            # Escape HTML tags inside code blocks
            escaped = line.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;')
            html_lines.append(escaped)
            idx += 1
            continue
            
        # Table handler
        if stripped.startswith("|") and not in_code_block:
            if not in_table:
                in_table = True
                table_headers = [c.strip() for c in stripped.split("|")[1:-1]]
                table_rows = []
                # Skip the delimiter line if present
                if idx + 1 < len(lines) and re.match(r'^\s*\|?\s*:?-+:?\s*\|', lines[idx+1].strip()):
                    idx += 2
                    continue
            else:
                row_cols = [c.strip() for c in stripped.split("|")[1:-1]]
                table_rows.append(row_cols)
            idx += 1
            continue
        elif in_table:
            # Render Table
            html_lines.append("<table>")
            html_lines.append("  <thead>")
            html_lines.append("    <tr>")
            for h in table_headers:
                html_lines.append(f"      <th>{h}</th>")
            html_lines.append("    </tr>")
            html_lines.append("  </thead>")
            html_lines.append("  <tbody>")
            for row in table_rows:
                html_lines.append("    <tr>")
                for cell in row:
                    # Parse formatting inside cell
                    cell_html = re.sub(r'\*\*([^*]+)\*\*', r'<strong>\1</strong>', cell)
                    cell_html = re.sub(r'\*([^*]+)\*', r'<em>\1</em>', cell_html)
                    cell_html = re.sub(r'`([^`]+)`', r'<code>\1</code>', cell_html)
                    html_lines.append(f"      <td>{cell_html}</td>")
                html_lines.append("    </tr>")
            html_lines.append("  </tbody>")
            html_lines.append("</table>")
            in_table = False
            
        # List handler
        if (stripped.startswith("- ") or stripped.startswith("* ") or re.match(r'^\d+\.\s', stripped)) and not in_code_block:
            if not in_list:
                in_list = True
                html_lines.append("<ul>")
            
            list_content = stripped[2:] if (stripped.startswith("- ") or stripped.startswith("* ")) else stripped[stripped.find('.')+1:].strip()
            # Formats bold/italic/code inside list item
            list_content = re.sub(r'\*\*([^*]+)\*\*', r'<strong>\1</strong>', list_content)
            list_content = re.sub(r'\*([^*]+)\*', r'<em>\1</em>', list_content)
            list_content = re.sub(r'`([^`]+)`', r'<code>\1</code>', list_content)
            html_lines.append(f"  <li>{list_content}</li>")
            idx += 1
            continue
        elif in_list:
            html_lines.append("</ul>")
            in_list = False
            
        # Divider / Page break handler
        if stripped == "---":
            html_lines.append("<div class='page-break'></div>")
            idx += 1
            continue
            
        # Headings
        if stripped.startswith("# ") and not in_code_block:
            title = stripped[2:].strip()
            html_lines.append(f"<h1>{title}</h1>")
        elif stripped.startswith("## ") and not in_code_block:
            title = stripped[3:].strip()
            html_lines.append(f"<h2>{title}</h2>")
        elif stripped.startswith("### ") and not in_code_block:
            title = stripped[4:].strip()
            html_lines.append(f"<h3>{title}</h3>")
        elif stripped.startswith("#### ") and not in_code_block:
            title = stripped[5:].strip()
            html_lines.append(f"<h4>{title}</h4>")
        elif stripped == "" and not in_code_block:
            pass  # Ignore empty lines
        else:
            # Paragraph
            # Bold
            p_text = re.sub(r'\*\*([^*]+)\*\*', r'<strong>\1</strong>', stripped)
            # Italic
            p_text = re.sub(r'\*([^*]+)\*', r'<em>\1</em>', p_text)
            # Inline Code
            p_text = re.sub(r'`([^`]+)`', r'<code>\1</code>', p_text)
            
            # Formatting blockquotes
            if p_text.startswith("> "):
                html_lines.append(f"<blockquote>{p_text[2:]}</blockquote>")
            else:
                # Custom classes for centers/titles
                if "HỌC VIỆN KỸ THUẬT MẬT MÃ" in p_text or "ĐỒ ÁN TỐT NGHIỆP" in p_text or "Sinh viên thực hiện" in p_text:
                    html_lines.append(f"<p class='no-indent center'>{p_text}</p>")
                else:
                    html_lines.append(f"<p>{p_text}</p>")
                    
        idx += 1
        
    if in_list:
        html_lines.append("</ul>")
    if in_table:
        html_lines.append("</table>")
        
    html_lines.append("</body>")
    html_lines.append("</html>")
    
    return "\n".join(html_lines)

def compile_thesis():
    sys.stdout.reconfigure(encoding='utf-8')
    docs_dir = r"c:\Hub\DoAn\docs"
    
    # Chronological chapter files in the thesis report
    files_to_compile = [
        "FRONT_MATTER.md",
        "LOI_NOI_DAU.md",
        "CHUONG1_BAO_CAO.md",
        "CHUONG2_BAO_CAO.md",
        "CHUONG3_BAO_CAO_VIET_LAI.md",
        "CHUONG4_BAO_CAO.md",
        "KET_LUAN_PHU_LUC.md"
    ]
    
    compiled_content = []
    
    print("Beginning compilation of final report chapters...")
    for filename in files_to_compile:
        file_path = os.path.join(docs_dir, filename)
        if not os.path.exists(file_path):
            print(f"Error: {file_path} not found! Skipping...")
            continue
            
        print(f"Reading and appending {filename}...")
        with open(file_path, "r", encoding="utf-8") as f:
            content = f.read()
            compiled_content.append(content)
            # Add section divider/page break between chapters
            compiled_content.append("\n\n---\n\n")
            
    full_text = "".join(compiled_content)
    
    # Meta replacements: Injecting Tran Van Thuy's actual details into the templates
    replacements = {
        r"\[Họ và tên GVHD\]": "TS. Nguyễn Đức Hiếu",
        r"\[Họ và tên\]": "Trần Văn Thủy",
        r"\[MSV\]": "CT060439",
        r"\[Lớp\]": "CT6",
        r"\[Khóa\]": "Khóa 6",
        r"\[Ký và ghi rõ họ tên\]": "Trần Văn Thủy",
        # Clean double-lines and other artifact characters
        r"\t": " ",
    }
    
    print("Performing logic template replacements (injecting real metadata)...")
    for pattern, repl in replacements.items():
        full_text = re.sub(pattern, repl, full_text)
        
    output_md_path = os.path.join(docs_dir, "DO_AN_TOT_NGHIEP_FINAL.md")
    print(f"Saving compiled markdown to {output_md_path}...")
    with open(output_md_path, "w", encoding="utf-8") as f:
        f.write(full_text)
        
    # Generate the premium HTML report
    print("Generating elegant HTML report representation...")
    html_text = convert_markdown_to_html(full_text)
    
    output_html_path = os.path.join(docs_dir, "DO_AN_TOT_NGHIEP_FINAL.html")
    print(f"Saving elegant HTML to {output_html_path}...")
    with open(output_html_path, "w", encoding="utf-8") as f:
        f.write(html_text)
        
    print("Compilation and formatting process completed successfully!")

if __name__ == "__main__":
    compile_thesis()
