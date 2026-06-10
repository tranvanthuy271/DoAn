import sys

try:
    import fitz # PyMuPDF
    print("fitz is installed!")
    doc = fitz.open(r'c:\Hub\DoAn\ĐATN-slide.pdf')
    page = doc[8] # Page 9 (0-indexed 8)
    pix = page.get_pixmap(dpi=150)
    pix.save(r'c:\Hub\DoAn\scratch\slide9_page_render.png')
    print("Saved page 9 render to c:\\Hub\\DoAn\\scratch\\slide9_page_render.png")
except Exception as e:
    print("Failed with fitz:", str(e))
