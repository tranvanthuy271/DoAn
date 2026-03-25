$iconMap = @{
    'WIND_STRIKE'='icon_wind_1'
    'WIND_BLADE'='icon_wind_2'
    'WIND_STEP'='icon_wind_3'
    'FIRE_BOLT'='icon_fire_bolt'
    'FIRE_BURST'='icon_fire_burst'
    'FIRE_RAIN'='icon_fire_rain'
    'WATER_BOLT'='icon_water_bolt'
    'WATER_PILLAR'='icon_water_pillar'
    'WATER_ARMOR'='icon_water_armor'
    'METAL_STRIKE'='icon_metal_strike'
    'METAL_BLADE'='icon_metal_blade'
    'METAL_SHIELD'='icon_metal_shield'
    'EARTH_AURA'='icon_earth_aura'
    'EARTH_BOOMERANG'='icon_earth_boomerang'
    'EARTH_BLINK'='icon_earth_blink'
    'HYBRID_METAL_WIND_BARRAGE'='icon_hybrid_113'
    'HYBRID_FIRE_EARTH_LAVA_AURA'='icon_hybrid_101'
    'HYBRID_WATER_WOOD_VENOM'='icon_hybrid_110'
}

$prefabPaths = @(
    'c:\Hub\DoAn\Client\Assets\Prefabs\Player\He\Phong.prefab'
    'c:\Hub\DoAn\Client\Assets\Prefabs\Player\He\Hoa.prefab'
    'c:\Hub\DoAn\Client\Assets\Prefabs\Player\He\Thuy.prefab'
    'c:\Hub\DoAn\Client\Assets\Prefabs\Player\He\Kim.prefab'
    'c:\Hub\DoAn\Client\Assets\Prefabs\Player\He\Tho.prefab'
    'c:\Hub\DoAn\Client\Assets\Prefabs\Player\Fusion\F_Phong.prefab'
    'c:\Hub\DoAn\Client\Assets\Prefabs\Player\Fusion\F_Hoa.prefab'
    'c:\Hub\DoAn\Client\Assets\Prefabs\Player\Fusion\F_Thuy.prefab'
    'c:\Hub\DoAn\Client\Assets\Prefabs\Player\Fusion\F_Kim.prefab'
    'c:\Hub\DoAn\Client\Assets\Prefabs\Player\Fusion\F_Tho.prefab'
    'c:\Hub\DoAn\Client\Assets\Prefabs\Player\Fusion\F_Moc.prefab'
)

$totalInserted = 0
foreach ($p in $prefabPaths) {
    $lines = [System.IO.File]::ReadAllLines($p)
    $out = [System.Collections.Generic.List[string]]::new()
    $lastCode = ''
    $fileInserted = 0
    foreach ($line in $lines) {
        if ($line -match '^\s+skillCode:\s+(\S+)\s*$') {
            $lastCode = $Matches[1].Trim()
        }
        $out.Add($line)
        if ($line -match '^(\s+)disablePlayerSkillEffectAnimation:\s' -and $lastCode -ne '') {
            $icon = $iconMap[$lastCode]
            if ($icon) {
                $indent = $Matches[1]
                $out.Add("${indent}iconId: $icon")
                $fileInserted++
                $totalInserted++
            }
        }
    }
    [System.IO.File]::WriteAllLines($p, $out, ([System.Text.UTF8Encoding]::new($false)))
    Write-Host "OK $([IO.Path]::GetFileName($p)): $fileInserted iconId(s)"
}
Write-Host "Total: $totalInserted iconId(s) inserted across $($prefabPaths.Count) prefabs."
