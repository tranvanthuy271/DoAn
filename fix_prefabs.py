"""
Script tự động xóa ClientAuthHandler và thêm NetworkPlayerDataSync vào player prefabs
Chạy script này từ thư mục gốc của project: python fix_prefabs.py
"""

import os
import re
from pathlib import Path

# Guid của các script
CLIENT_AUTH_HANDLER_GUID = "9f9f5a6625a4b594da770f726e13be60"
NETWORK_PLAYER_DATA_SYNC_GUID = "150b2bfd85773494e93e2c2baac022f7"

# Đường dẫn tới thư mục prefabs
PREFAB_DIRS = [
    "Client/Assets/Prefabs/Player",
    "Client_clone_0/Assets/Prefabs/Player"
]

def find_component_id_for_script(content, script_guid):
    """
    Tìm component ID (fileID) của một script trong prefab
    """
    # Tìm block MonoBehaviour có script guid này
    pattern = rf'--- !u!114 &(-?\d+)\s*\nMonoBehaviour:.*?m_Script: \{{fileID: 11500000, guid: {script_guid}'
    matches = re.finditer(pattern, content, re.DOTALL)
    
    component_ids = []
    for match in matches:
        component_id = match.group(1)
        component_ids.append(component_id)
    
    return component_ids

def remove_component_from_prefab(prefab_path):
    """
    Xóa ClientAuthHandler component khỏi prefab
    """
    print(f"\nProcessing: {prefab_path}")
    
    with open(prefab_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # Tìm component ID của ClientAuthHandler
    component_ids = find_component_id_for_script(content, CLIENT_AUTH_HANDLER_GUID)
    
    if not component_ids:
        print(f"  ✓ No ClientAuthHandler found (already clean)")
        return content, False
    
    print(f"  Found {len(component_ids)} ClientAuthHandler component(s)")
    
    modified = False
    for component_id in component_ids:
        # Xóa component khỏi m_Component list
        component_list_pattern = rf'  - component: \{{fileID: {component_id}\}}\s*\n'
        if re.search(component_list_pattern, content):
            content = re.sub(component_list_pattern, '', content)
            print(f"    ✓ Removed from component list: {component_id}")
            modified = True
        
        # Xóa MonoBehaviour block
        block_pattern = rf'--- !u!114 &{component_id}\s*\nMonoBehaviour:.*?(?=---|\\Z)'
        block_match = re.search(block_pattern, content, re.DOTALL)
        if block_match:
            content = content.replace(block_match.group(0), '')
            print(f"    ✓ Removed MonoBehaviour block: {component_id}")
            modified = True
    
    return content, modified

def check_has_network_player_data_sync(content):
    """
    Kiểm tra xem prefab đã có NetworkPlayerDataSync chưa
    """
    return NETWORK_PLAYER_DATA_SYNC_GUID in content

def add_network_player_data_sync(content, prefab_name):
    """
    Thêm NetworkPlayerDataSync component vào prefab
    CẢNH BÁO: Hàm này phức tạp và có thể gây lỗi
    Khuyến nghị thêm component thủ công trong Unity Editor
    """
    print(f"  ⚠️ NetworkPlayerDataSync cần được thêm thủ công trong Unity Editor")
    print(f"  ⚠️ Auto-add component vào prefab file có thể gây lỗi")
    return content, False

def process_prefab(prefab_path):
    """
    Xử lý một prefab file
    """
    content, modified = remove_component_from_prefab(prefab_path)
    
    # Kiểm tra xem đã có NetworkPlayerDataSync chưa
    if not check_has_network_player_data_sync(content):
        print(f"  ⚠️ Missing NetworkPlayerDataSync - please add manually in Unity Editor")
    else:
        print(f"  ✓ NetworkPlayerDataSync already present")
    
    if modified:
        # Backup original file
        backup_path = str(prefab_path) + ".backup"
        with open(backup_path, 'w', encoding='utf-8') as f:
            # Read original again for backup
            with open(prefab_path, 'r', encoding='utf-8') as orig:
                f.write(orig.read())
        print(f"  ✓ Backup created: {backup_path}")
        
        # Write modified content
        with open(prefab_path, 'w', encoding='utf-8') as f:
            f.write(content)
        print(f"  ✓ Prefab updated successfully")
        return True
    
    return False

def main():
    """
    Main function
    """
    print("=" * 70)
    print("Unity Prefab Auto-Fix Script")
    print("Xóa ClientAuthHandler khỏi player prefabs")
    print("=" * 70)
    
    total_modified = 0
    total_processed = 0
    
    for prefab_dir in PREFAB_DIRS:
        prefab_path = Path(prefab_dir)
        
        if not prefab_path.exists():
            print(f"\n⚠️ Directory not found: {prefab_dir}")
            continue
        
        print(f"\n📁 Scanning directory: {prefab_dir}")
        
        # Tìm tất cả .prefab files
        for prefab_file in prefab_path.glob("*.prefab"):
            total_processed += 1
            if process_prefab(prefab_file):
                total_modified += 1
    
    print("\n" + "=" * 70)
    print("SUMMARY")
    print("=" * 70)
    print(f"Total prefabs processed: {total_processed}")
    print(f"Total prefabs modified: {total_modified}")
    
    if total_modified > 0:
        print("\n✅ Success! Prefabs have been updated.")
        print("⚠️ IMPORTANT:")
        print("   1. Backup files created with .backup extension")
        print("   2. You MUST add NetworkPlayerDataSync component manually in Unity Editor")
        print("   3. Open Unity and check each prefab in Inspector")
        print("   4. Add 'NetworkPlayerDataSync' component to each player prefab")
    else:
        print("\n✓ No changes needed - all prefabs are already clean!")

if __name__ == "__main__":
    main()
