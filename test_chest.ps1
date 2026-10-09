param(
  [int]$Seconds = 8,
  [string]$OutDir = "C:\Users\Administrator\AppData\Local\Temp\opencode\pixeldemo\Shots"
)

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class W {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr h, int x, int y, int w, int ht, bool repaint);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern void keybd_event(byte vk, byte scan, uint flags, IntPtr extra);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
}
"@ -ReferencedAssemblies System.Drawing

$exe = "C:\Users\Administrator\AppData\Local\Temp\opencode\pixeldemo\Build\PixelDemo.exe"

$VK = @{ W = 0x57; A = 0x41; S = 0x53; D = 0x44; E = 0x45 }

function Hold-Key([char]$c, [int]$taps) {
  # SendKeys holds each char for a few frames of Unity's input polling, unlike keybd_event
  [System.Windows.Forms.SendKeys]::SendWait(([string]$c) * $taps)
}

function Tap-Key([char]$c) { [System.Windows.Forms.SendKeys]::SendWait([string]$c) }

function Grab($h, $name) {
  $r = New-Object W+RECT
  [W]::GetWindowRect($h, [ref]$r) | Out-Null
  $w = $r.R - $r.L; $ht = $r.B - $r.T
  $bmp = New-Object System.Drawing.Bitmap $w, $ht
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $hdc = $g.GetHdc()
  [W]::PrintWindow($h, $hdc, 2) | Out-Null
  $g.ReleaseHdc($hdc)
  $path = Join-Path $OutDir $name
  $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
  $g.Dispose(); $bmp.Dispose()
  Write-Output "SAVED $name"
}

$p = Start-Process -FilePath $exe -PassThru `
      -ArgumentList "-screen-width","1280","-screen-height","720","-screen-fullscreen","0","-popupwindow"
Write-Output "launched pid=$($p.Id)"
Start-Sleep -Seconds $Seconds
if ($p.HasExited) { Write-Output "PROCESS_EXITED code=$($p.ExitCode)"; exit 1 }

$h = (Get-Process -Id $p.Id).MainWindowHandle
[W]::MoveWindow($h, 0, 0, 1280, 720, $true) | Out-Null
[W]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 900

Grab $h "chest_01_before.png"

# Walk left and slightly down toward the chest at (2.99, 0.29); player starts at (5.82, 2.31)
Write-Output "walking..."
Hold-Key 'A' 3
Start-Sleep -Milliseconds 300
Hold-Key 'S' 2
Start-Sleep -Milliseconds 500
Grab $h "chest_02_at_chest.png"

Write-Output "interacting (E)"
Tap-Key 'E'
Start-Sleep -Milliseconds 900
Grab $h "chest_03_opened.png"

Start-Sleep -Milliseconds 1100
Grab $h "chest_04_popup.png"

Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
Write-Output "done"
