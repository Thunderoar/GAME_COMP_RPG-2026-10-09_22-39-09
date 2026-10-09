param(
  [int]$ClickX = 300,
  [int]$ClickY = 360
)

# Decides whether real input can be injected into the running player.
# Click-to-move is active in this build, so a successful LEFTDOWN on open
# ground must make the character walk left. Reflection is NOT used here.

Add-Type -AssemblyName System.Drawing

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class I {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr h, int x, int y, int w, int ht, bool repaint);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint flags);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern uint SendInput(uint n, INPUT[] p, int size);
  [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vkey);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
  [StructLayout(LayoutKind.Sequential)]
  public struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public IntPtr dwExtraInfo; }
  [StructLayout(LayoutKind.Explicit)]
  public struct INPUTUNION { [FieldOffset(0)] public MOUSEINPUT mi; }
  [StructLayout(LayoutKind.Sequential)]
  public struct INPUT { public uint type; public INPUTUNION u; }
}
"@ -ReferencedAssemblies System.Drawing

$exe = "C:\Users\Administrator\AppData\Local\Temp\opencode\pixeldemo\Build\PixelDemo.exe"
$out = "C:\Users\Administrator\AppData\Local\Temp\opencode\pixeldemo\Shots"

$p = Start-Process -FilePath $exe -PassThru `
      -ArgumentList "-screen-width","1280","-screen-height","720","-screen-fullscreen","0","-popupwindow"
Start-Sleep -Seconds 9
if ($p.HasExited) { Write-Output "EXITED early"; exit 1 }

$h = (Get-Process -Id $p.Id).MainWindowHandle
[I]::MoveWindow($h, 0, 0, 1280, 720, $true) | Out-Null
[I]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 900

function Grab($name) {
  $r = New-Object I+RECT
  [I]::GetWindowRect($h, [ref]$r) | Out-Null
  $bmp = New-Object System.Drawing.Bitmap ($r.R-$r.L), ($r.B-$r.T)
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $hdc = $g.GetHdc()
  [I]::PrintWindow($h, $hdc, 2) | Out-Null
  $g.ReleaseHdc($hdc)
  $bmp.Save((Join-Path $out $name), [System.Drawing.Imaging.ImageFormat]::Png)
  $g.Dispose(); $bmp.Dispose()
}

Grab "inject_00_before.png"
Write-Output "captured before; injecting LEFTDOWN at ($ClickX,$ClickY)"

[I]::SetCursorPos($ClickX, $ClickY) | Out-Null
Start-Sleep -Milliseconds 300

$down = New-Object I+INPUT
$down.type = 0
$down.u.mi.dwFlags = 0x0002      # MOUSEEVENTF_LEFTDOWN

$up = New-Object I+INPUT
$up.type = 0
$up.u.mi.dwFlags = 0x0004        # MOUSEEVENTF_LEFTUP

$sent1 = [I]::SendInput(1, @($down), [System.Runtime.InteropServices.Marshal]::SizeOf([type][I+INPUT]))
Write-Output "SendInput(down) sent=$sent1 err=$([System.Runtime.InteropServices.Marshal]::GetLastWin32Error())"

Start-Sleep -Milliseconds 400
$sent2 = [I]::SendInput(1, @($up), [System.Runtime.InteropServices.Marshal]::SizeOf([type][I+INPUT]))
Write-Output "SendInput(up) sent=$sent2"

# also sample a keyboard key to cross-check the hold theory
$kd = New-Object I+INPUT
$kd.type = 1
$kd.u.mi.dwFlags = 0x0001        # KEYEVENTF_KEYDOWN (unused placeholder)
Write-Output "LBUTTON async state during hold = $('0x{0:X4}' -f [I]::GetAsyncKeyState(0x01))"

Start-Sleep -Milliseconds 2600
Grab "inject_01_after.png"
Write-Output "captured after"

Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
