# -*- coding: utf-8 -*-
import re
import sys

def main():
    # Force stdout to be utf-8
    sys.stdout.reconfigure(encoding='utf-8')
    
    file_path = "DO_AN_TOT_NGHIEP_FINAL.md"
    try:
        with open(file_path, "r", encoding="utf-8") as f:
            content = f.read()
    except Exception as e:
        print(f"Error reading file: {e}")
        return

    # Split into paragraphs by blank lines
    paragraphs = [p.strip() for p in re.split(r'\n\s*\n', content) if p.strip()]
    
    seen = {}
    duplicates = []
    
    for i, p in enumerate(paragraphs):
        # Ignore markdown headers, code blocks, or very short sentences
        if p.startswith('#') or p.startswith('```') or len(p) < 80:
            continue
        
        # Clean text slightly to avoid minor formatting differences
        cleaned = re.sub(r'\s+', ' ', p).lower()
        if cleaned in seen:
            duplicates.append((seen[cleaned], i, p))
        else:
            seen[cleaned] = i

    if duplicates:
        print(f"Found {len(duplicates)} duplicate paragraphs:")
        for orig_idx, dup_idx, text in duplicates:
            print(f"- Duplicate of paragraph {orig_idx} found at index {dup_idx}:")
            # Replace non-ascii chars that might fail printing
            safe_text = text[:150].encode('ascii', 'replace').decode('ascii')
            print(f"  Snippet: {safe_text}...")
            print("-" * 50)
    else:
        print("No exact duplicate paragraphs (length >= 80) found!")

if __name__ == "__main__":
    main()
