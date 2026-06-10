import os
import zipfile
import shutil

docx_path = r"c:\Hub\DoAn\DoAn.docx"
output_dir = r"c:\Hub\DoAn\docs\extracted_images"

def extract_images():
    if not os.path.exists(docx_path):
        print(f"Error: {docx_path} does not exist.")
        return

    if os.path.exists(output_dir):
        shutil.rmtree(output_dir)
    os.makedirs(output_dir)

    print(f"Opening {docx_path} as a ZIP file...")
    with zipfile.ZipFile(docx_path, 'r') as archive:
        image_files = [f for f in archive.namelist() if f.startswith('word/media/')]
        print(f"Found {len(image_files)} image files in word/media/")

        if not image_files:
            print("No images found in word/media/")
            return

        extracted_count = 0
        for file_in_zip in sorted(image_files):
            # Extract file extension
            filename = os.path.basename(file_in_zip)
            dest_path = os.path.join(output_dir, filename)
            
            with archive.open(file_in_zip) as source, open(dest_path, 'wb') as target:
                shutil.copyfileobj(source, target)
            
            extracted_count += 1
            # print(f"Extracted: {filename}")

        print(f"\nExtraction complete! Extracted {extracted_count} images to {output_dir}")

if __name__ == "__main__":
    extract_images()
