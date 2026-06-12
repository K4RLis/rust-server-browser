# Rebuild RustServerBrowser.exe from index.html + RustBrowser.cs
# Uses the C# compiler built into Windows (csc.exe). Nothing to install.
# Builds in an ASCII temp folder (so spaces/Cyrillic in the project path
# don't break csc arg parsing), then copies the finished exe back here.
# NOTE: ASCII-only on purpose - PowerShell 5.1 reads .ps1 without BOM as
# Windows-1251 and would mangle Cyrillic, breaking the script.

$proj = $PSScriptRoot
$b = Join-Path $env:TEMP "rsb_build"
Remove-Item $b -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $b | Out-Null
Copy-Item (Join-Path $proj "index.html")     (Join-Path $b "index.html")     -Force
Copy-Item (Join-Path $proj "RustBrowser.cs")  (Join-Path $b "RustBrowser.cs") -Force

$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path $csc)) {
  $csc = (Get-ChildItem "C:\Windows\Microsoft.NET\Framework64\v4*\csc.exe" | Select-Object -Last 1).FullName
}

& $csc /nologo /codepage:65001 /target:winexe "/out:$b\RustServerBrowser.exe" `
   /reference:System.Windows.Forms.dll "/resource:$b\index.html,index.html" "$b\RustBrowser.cs"

if ($LASTEXITCODE -ne 0) {
  Write-Host "`n[ERROR] Compilation failed (code $LASTEXITCODE)" -ForegroundColor Red
  exit 1
}
Copy-Item (Join-Path $b "RustServerBrowser.exe") (Join-Path $proj "RustServerBrowser.exe") -Force
$kb = [math]::Round((Get-Item (Join-Path $proj "RustServerBrowser.exe")).Length/1KB,1)
Write-Host "`n[OK] RustServerBrowser.exe rebuilt ($kb KB)." -ForegroundColor Green
