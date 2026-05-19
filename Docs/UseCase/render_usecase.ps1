param(
    [ValidateSet('png','svg')]
    [string]$Format = 'png',
    [string]$PlantUmlJar = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$files = Get-ChildItem -Path $root -Filter '*.puml' | Sort-Object Name

if ($files.Count -eq 0) {
    Write-Error "Không tìm thấy file .puml trong $root"
}

$plantUmlCmd = Get-Command plantuml -ErrorAction SilentlyContinue
if ($plantUmlCmd) {
    foreach ($file in $files) {
        Write-Host "Render $($file.Name) -> $Format"
        & $plantUmlCmd.Source "-t$Format" $file.FullName
    }
    exit 0
}

if ([string]::IsNullOrWhiteSpace($PlantUmlJar) -or -not (Test-Path $PlantUmlJar)) {
    Write-Error 'Không tìm thấy lệnh plantuml. Hãy cài PlantUML hoặc truyền -PlantUmlJar <duong-dan-toi-plantuml.jar>.'
}

$javaCmd = Get-Command java -ErrorAction SilentlyContinue
if (-not $javaCmd) {
    Write-Error 'Không tìm thấy Java runtime để chạy plantuml.jar.'
}

foreach ($file in $files) {
    Write-Host "Render $($file.Name) -> $Format"
    & $javaCmd.Source -jar $PlantUmlJar "-t$Format" $file.FullName
}
