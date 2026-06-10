import os
import win32com.client

ppt_path = r'c:\Hub\DoAn\ĐATN-slide.pptx'
output_dir = r'c:\Hub\DoAn\scratch'
output_img = os.path.join(output_dir, 'slide32_direction.png')

try:
    # Initialize PowerPoint
    powerpoint = win32com.client.Dispatch("PowerPoint.Application")
    powerpoint.Visible = True # PowerPoint needs to be visible or run in a mode that allows exporting
    
    # Open presentation
    presentation = powerpoint.Presentations.Open(ppt_path, WithWindow=False)
    
    # Slide 32 is index 31 (0-indexed)
    # Let's count slides first
    slide_count = presentation.Slides.Count
    print(f"Total slides in presentation: {slide_count}")
    
    # We want Slide 32 (index 32 since PowerPoint slides are 1-indexed!)
    slide = presentation.Slides(32)
    slide.Export(output_img, "PNG")
    presentation.Close()
    powerpoint.Quit()
    print(f"Successfully exported Slide 32 to {output_img}!")
except Exception as e:
    print("Error exporting slide via PowerPoint COM:", str(e))
    # Let's clean up
    try:
        powerpoint.Quit()
    except:
        pass
