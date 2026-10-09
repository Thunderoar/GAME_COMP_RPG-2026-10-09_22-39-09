param([string]$Method, [string]$Log = "run.log")

$u = "C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe"
$p = "C:\Users\Administrator\AppData\Local\Temp\opencode\pixeldemo"
$logPath = Join-Path $p $Log

if (Test-Path $logPath) { Remove-Item $logPath -Force }

& $u -batchmode -silent-crashes -projectPath $p -executeMethod $Method -logFile $logPath -quit 2>&1 | Out-Null
$code = $LASTEXITCODE

Write-Output "exit=$code"
if (Test-Path $logPath) {
  Select-String -Path $logPath -Pattern "error CS|INSTALL|Exception:" |
    Select-Object -First 15 | ForEach-Object { $_.Line.Trim() }
} else {
  Write-Output "(no log written)"
}
