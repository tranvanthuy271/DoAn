$dashBlock = @'
  - skillName: "L\u01B0\u1EDBt Nhanh"
    skillCode: DASH
    skillType: 2
    activationKey: 0
    cooldown: 1
    projectilePrefab: {fileID: 0}
    projectileSpeed: 0
    spawnOffset: 0
    projectileLifetime: 0
    animationTriggerName: 
    playerSkillEffectObject: {fileID: 0}
    projectileSkillEffectPrefab: {fileID: 0}
    projectileSpriteFacesLeft: 0
    disablePlayerSkillEffectAnimation: 1
    iconId: icon_skill_5
    currentEffectValue: 0
    currentMpCost: 0
    cooldownTimer: 0
    canUse: 0
    isUsing: 0
'@

$dashBlockLines = $dashBlock -split "`r`n|`n" | Where-Object { $_ -ne '' -or $true }
# Keep as-is (includes the leading/trailing empty string from heredoc trim)
$dashInsertLines = $dashBlock.TrimEnd() -split "`r`n|`n"

$prefabs = @(
    "c:\Hub\DoAn\Client\Assets\Prefabs\Player\He\Phong.prefab",
    "c:\Hub\DoAn\Client\Assets\Prefabs\Player\He\Hoa.prefab",
    "c:\Hub\DoAn\Client\Assets\Prefabs\Player\He\Thuy.prefab",
    "c:\Hub\DoAn\Client\Assets\Prefabs\Player\He\Kim.prefab",
    "c:\Hub\DoAn\Client\Assets\Prefabs\Player\He\Tho.prefab",
    "c:\Hub\DoAn\Client\Assets\Prefabs\Player\Fusion\F_Phong.prefab",
    "c:\Hub\DoAn\Client\Assets\Prefabs\Player\Fusion\F_Hoa.prefab",
    "c:\Hub\DoAn\Client\Assets\Prefabs\Player\Fusion\F_Thuy.prefab",
    "c:\Hub\DoAn\Client\Assets\Prefabs\Player\Fusion\F_Kim.prefab",
    "c:\Hub\DoAn\Client\Assets\Prefabs\Player\Fusion\F_Tho.prefab",
    "c:\Hub\DoAn\Client\Assets\Prefabs\Player\Fusion\F_Moc.prefab"
)

$mocPrefab = "c:\Hub\DoAn\Client\Assets\Prefabs\Player\He\Moc.prefab"

foreach ($path in $prefabs) {
    $lines = [System.IO.File]::ReadAllLines($path)
    
    # Check if DASH already in skills list
    if ($lines | Where-Object { $_ -match '^\s+skillCode:\s+DASH\s*$' }) {
        Write-Host "SKIP $([System.IO.Path]::GetFileName($path)): DASH already present."
        continue
    }

    $newLines = [System.Collections.Generic.List[string]]::new()
    $inserted = 0

    foreach ($line in $lines) {
        if ($line -match '^\s{2}defaultSkillEffectObject:') {
            # Insert DASH block before this line
            foreach ($dashLine in $dashInsertLines) {
                $newLines.Add($dashLine)
            }
            $inserted++
        }
        $newLines.Add($line)
    }

    if ($inserted -eq 1) {
        [System.IO.File]::WriteAllLines($path, $newLines, [System.Text.Encoding]::UTF8)
        Write-Host "OK $([System.IO.Path]::GetFileName($path)): DASH inserted."
    } else {
        Write-Host "WARN $([System.IO.Path]::GetFileName($path)): defaultSkillEffectObject found $inserted times - skipped."
    }
}

# Handle He/Moc.prefab separately (skills: [] -> skills: + dash block)
$mocLines = [System.IO.File]::ReadAllLines($mocPrefab)
if ($mocLines | Where-Object { $_ -match '^\s+skillCode:\s+DASH\s*$' }) {
    Write-Host "SKIP Moc.prefab: DASH already present."
} else {
    $mocNew = [System.Collections.Generic.List[string]]::new()
    $mocInserted = 0
    foreach ($line in $mocLines) {
        if ($line -match '^\s{2}skills:\s*\[\]\s*$') {
            $mocNew.Add("  skills:")
            foreach ($dashLine in $dashInsertLines) {
                $mocNew.Add($dashLine)
            }
            $mocInserted++
        } else {
            $mocNew.Add($line)
        }
    }
    if ($mocInserted -eq 1) {
        [System.IO.File]::WriteAllLines($mocPrefab, $mocNew, [System.Text.Encoding]::UTF8)
        Write-Host "OK Moc.prefab: skills: [] replaced with DASH entry."
    } else {
        Write-Host "WARN Moc.prefab: skills: [] pattern found $mocInserted times - skipped."
    }
}

Write-Host "Done."
