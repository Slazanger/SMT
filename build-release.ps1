param(
    [string]$OutputDir = "Release"
)

$ErrorActionPreference = "Stop"
$PublishDir = "publish-single"

Write-Host "=== 1. Clean ===" -ForegroundColor Cyan
dotnet clean SMT.sln -c Release -p:Platform=x64 2>&1 | Out-Null
if (Test-Path $PublishDir) { Remove-Item -Recurse -Force $PublishDir }

Write-Host "=== 2. Publish single-file ===" -ForegroundColor Cyan
dotnet publish SMT/SMT.csproj -c Release -p:Platform=x64 `
    -p:PublishSingleFile=true `
    -p:SelfContained=false `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $PublishDir
if ($LASTEXITCODE -ne 0) { throw "Publish failed" }

Write-Host "=== 3. Prepare output dir ===" -ForegroundColor Cyan
if (Test-Path $OutputDir) { Remove-Item -Recurse -Force $OutputDir }
New-Item -ItemType Directory -Path "$OutputDir/data"   -Force | Out-Null
New-Item -ItemType Directory -Path "$OutputDir/sounds" -Force | Out-Null

Write-Host "=== 4. Copy files ===" -ForegroundColor Cyan
Copy-Item "$PublishDir/*.exe"              "$OutputDir/"
Copy-Item "$PublishDir/*.pdb"              "$OutputDir/"
Copy-Item "EVEData/data/*"                 "$OutputDir/data/" -Exclude "SourceMaps"
Copy-Item "SMT/Sounds/*"                   "$OutputDir/sounds/"
Copy-Item "SMT/DefaultWindowLayout.dat"    "$OutputDir/"

# Cleanup publish temp dir
Remove-Item -Recurse -Force $PublishDir

Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Green
$outFull = (Get-Item $OutputDir).FullName
Write-Host "Output: $outFull"
Write-Host ""

Get-ChildItem $OutputDir -File | ForEach-Object {
    $kb = "{0,8:N0} KB" -f ($_.Length / 1KB)
    Write-Host "  $($_.Name)  $kb"
}
Get-ChildItem $OutputDir -Directory | ForEach-Object {
    $cnt = (Get-ChildItem $_.FullName -Recurse -File).Count
    Write-Host "  $($_.Name)/  ($cnt files)"
}
