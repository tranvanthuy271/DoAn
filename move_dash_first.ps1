$prefabs = @(
    "c:\Hub\DoAn\Client\Assets\Prefabs\Player\He\Phong.prefab",
    "c:\Hub\DoAn\Client\Assets\Prefabs\Player\He\Hoa.prefab",
    "c:\Hub\DoAn\Client\Assets\Prefabs\Player\He\Thuy.prefab",
    "c:\Hub\DoAn\Client\Assets\Prefabs\Player\He\Kim.prefab",
    "c:\Hub\DoAn\Client\Assets\Prefabs\Player\He\Tho.prefab",
    "c:\Hub\DoAn\Client\Assets\Prefabs\Player\He\Moc.prefab",
    "c:\Hub\DoAn\Client\Assets\Prefabs\Player\Fusion\F_Phong.prefab",
    "c:\Hub\DoAn\Client\Assets\Prefabs\Player\Fusion\F_Hoa.prefab",
    "c:\Hub\DoAn\Client\Assets\Prefabs\Player\Fusion\F_Thuy.prefab",
    "c:\Hub\DoAn\Client\Assets\Prefabs\Player\Fusion\F_Kim.prefab",
    "c:\Hub\DoAn\Client\Assets\Prefabs\Player\Fusion\F_Tho.prefab",
    "c:\Hub\DoAn\Client\Assets\Prefabs\Player\Fusion\F_Moc.prefab"
)
foreach ($path in $prefabs) {
    $fname = [System.IO.Path]::GetFileName($path)
    $lines = [System.IO.File]::ReadAllLines($path)
    $skillsLineIdx = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match "^\s{2}skills:\s*$") { $skillsLineIdx = $i; break }
    }
    if ($skillsLineIdx -lt 0) { Write-Host ("SKIP " + $fname + ": no skills line"); continue }
    $dashStart = -1; $dashEnd = -1; $blockStart = -1
    for ($i = $skillsLineIdx + 1; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match "^\s{2}-\s") { $blockStart = $i }
        if ($blockStart -ge 0 -and $lines[$i] -match "^\s{4}skillCode:\s*DASH\s*$") { $dashStart = $blockStart }
        if ($dashStart -ge 0 -and $dashEnd -lt 0 -and $lines[$i] -match "^\s{4}isUsing:") { $dashEnd = $i }
        if ($lines[$i] -match "^\s{2}defaultSkillEffectObject:") { break }
    }
    if ($dashStart -lt 0) { Write-Host ("SKIP " + $fname + ": DASH not found"); continue }
    if ($dashStart -eq $skillsLineIdx + 1) { Write-Host ("OK " + $fname + ": DASH already first"); continue }
    $dashBlock = $lines[$dashStart..$dashEnd]
    $newLines = [System.Collections.Generic.List[string]]::new()
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($i -ge $dashStart -and $i -le $dashEnd) { continue }
        $newLines.Add($lines[$i])
        if ($i -eq $skillsLineIdx) { foreach ($dl in $dashBlock) { $newLines.Add($dl) } }
    }
    [System.IO.File]::WriteAllLines($path, $newLines, [System.Text.Encoding]::UTF8)
    Write-Host ("OK " + $fname + ": DASH moved to slot 1")
}
Write-Host "Done."
