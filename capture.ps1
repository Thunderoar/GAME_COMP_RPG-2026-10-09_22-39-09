param(
  [int]$Seconds = 8,
  [string]$OutFile = "C:\Users\Administrator\AppData\Local\Temp\opencode\pixeldemo\Shots\live.png",
  [string]$Press = ""
)

Add-Type -AssemblyName System.Windows.Forms, System.Drawing

Add-Type @"
using System;
using System.Drawing;
using System.Runtime.InteropServices;
public class W {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr h, int x, int y, int w, int ht, bool repaint);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
}
"@ -ReferencedAssemblies System.Drawing, System.Windows.Forms

$exe = "C:\Users\Administrator\AppData\Local\Temp\opencode\pixeldemo\Build\PixelDemo.exe"

$p = Start-Process -FilePath $exe -PassThru `
      -ArgumentList "-screen-width","1280","-screen-height","720","-screen-fullscreen","0","-popupwindow"

Write-Output "launched pid=$($p.Id)"
Start-Sleep -Seconds $Seconds

if ($p.HasExited) { Write-Output "PROCESS_EXITED code=$($p.ExitCode)"; exit 1 }

$proc = Get-Process -Id $p.Id
$h = $proc.MainWindowHandle
Write-Output "handle=$h title='$($proc.MainWindowTitle)'"

# Park the window at the top-left so it fully fits on screen
[W]::MoveWindow($h, 0, 0, 1280, 720, $true) | Out-Null
[W]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 800

if ($Press -ne "") {
  Write-Output "pressing: $Press"
  [System.Windows.Forms.SendKeys]::SendWait($Press)
  Start-Sleep -Seconds 2
}

$r = New-Object W+RECT
[W]::GetWindowRect($h, [ref]$r) | Out-Null
$w = $r.R - $r.L; $ht = $r.B - $r.T

# PrintWindow grabs only this window, ignoring whatever is behind it
$bmp = New-Object System.Drawing.Bitmap $w, $ht
$g = [System.Drawing.Graphics]::FromImage($bmp)
$hdc = $g.GetHdc()
[W]::PrintWindow($h, $hdc, 2) | Out-Null
$g.ReleaseHdc($hdc)

$bmp.Save($OutFile, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bmp.Dispose()

Write-Output "SAVED $OutFile ($w x $ht)"
Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
