# =============================================================================
# upload_unity_server.ps1
# Chạy trên Windows sau khi build Unity Linux Server build xong.
# Upload toàn bộ build lên VPS và restart container unity.
#
# Cách dùng:
#   .\upload_unity_server.ps1 -VpsUser ubuntu -VpsIp 1.2.3.4 -KeyFile ~/.ssh/id_rsa
# =============================================================================
param(
    [Parameter(Mandatory)][string] $VpsIp,
    [string] $VpsUser     = "ubuntu",
    [string] $KeyFile     = "$HOME\.ssh\id_rsa",
    # Thư mục build Unity Linux Server của bạn (output từ Unity Build Settings)
    [string] $UnityBuildDir = ".\Client\build\LinuxServer",
    # Thư mục đích trên VPS (phải khớp volume trong docker-compose)
    [string] $VpsDestDir  = "~/DoAn/unity-server/GameServer"
)

$ErrorActionPreference = "Stop"

function Write-Step { param([string]$Msg) Write-Host "`n>> $Msg" -ForegroundColor Cyan }

# ── Kiểm tra thư mục build ───────────────────────────────────────────────────
Write-Step "Kiểm tra thư mục build Unity..."
if (-not (Test-Path $UnityBuildDir)) {
    Write-Error "Không tìm thấy Unity build tại: $UnityBuildDir"
    Write-Host "  Build trong Unity: File > Build Settings > Linux > Dedicated Server > Build"
    exit 1
}

# Tìm file executable (.x86_64)
$Exe = Get-ChildItem $UnityBuildDir -Filter "*.x86_64" | Select-Object -First 1
if (-not $Exe) {
    Write-Error "Không tìm thấy file .x86_64 trong $UnityBuildDir"
    exit 1
}
Write-Host "  Tìm thấy: $($Exe.Name)" -ForegroundColor Green

# ── Upload lên VPS bằng rsync (nếu có) hoặc scp ─────────────────────────────
Write-Step "Upload lên VPS $VpsUser@$VpsIp..."

$sshArgs = "-i `"$KeyFile`" -o StrictHostKeyChecking=no"

# Tạo thư mục đích trên VPS
$createDir = "mkdir -p $VpsDestDir"
Invoke-Expression "ssh $sshArgs $VpsUser@$VpsIp `"$createDir`""

# Check rsync
$hasRsync = Get-Command rsync -ErrorAction SilentlyContinue
if ($hasRsync) {
    Write-Host "  Dùng rsync (chỉ upload file thay đổi)..."
    rsync -avz --progress -e "ssh $sshArgs" "$UnityBuildDir/" "${VpsUser}@${VpsIp}:${VpsDestDir}/"
} else {
    Write-Host "  Dùng scp (upload toàn bộ)..."
    Invoke-Expression "scp -r $sshArgs `"$UnityBuildDir/*`" `"${VpsUser}@${VpsIp}:${VpsDestDir}/`""
}

# ── Cấp quyền thực thi cho file executable ───────────────────────────────────
Write-Step "Cấp quyền thực thi..."
$chmodCmd = "chmod +x $VpsDestDir/$($Exe.Name)"
Invoke-Expression "ssh $sshArgs $VpsUser@$VpsIp `"$chmodCmd`""

# ── Restart container unity trên VPS ─────────────────────────────────────────
Write-Step "Restart container unity..."
$restartCmd = "cd ~/DoAn && docker compose restart unity"
Invoke-Expression "ssh $sshArgs $VpsUser@$VpsIp `"$restartCmd`""

Write-Host "`n✓ Upload và restart Unity Server hoàn tất!" -ForegroundColor Green
Write-Host "  Logs: ssh $VpsUser@$VpsIp 'cd ~/DoAn && docker compose logs -f unity'"
