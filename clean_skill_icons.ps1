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
foreach ($p in $prefabPaths) {
    $lines = [System.IO.File]::ReadAllLines($p)
    $out = $lines | Where-Object { $_ -notmatch '^\s+iconId:' }
    [System.IO.File]::WriteAllLines($p, $out, ([System.Text.UTF8Encoding]::new($false)))
    $removed = $lines.Count - $out.Count
    Write-Host "Cleaned $([IO.Path]::GetFileName($p)): removed $removed iconId lines"
}
Write-Host "Cleanup done."
