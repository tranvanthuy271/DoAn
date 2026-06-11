import docx
import os
import sys

def check_images(file_path):
    sys.stdout.reconfigure(encoding='utf-8')
    doc = docx.Document(file_path)
    
    # Check paragraphs 842 to 846 for any shapes or drawings
    for idx in range(842, 846):
        p = doc.paragraphs[idx]
        print(f"Paragraph {idx} | Style: '{p.style.name}' | Text: '{p.text}'")
        # Find images in paragraph runs XML
        p_element = p._p
        drawings = p_element.xpath('.//w:drawing')
        print(f"  Number of drawings (images): {len(drawings)}")
        
        # Check inline shapes
        for r in p.runs:
            if r.element.xpath('.//w:drawing'):
                print(f"    Run text: '{r.text}' | Contains drawing element.")

if __name__ == "__main__":
    check_images(r"c:\Hub\DoAn\DoAn.docx")
