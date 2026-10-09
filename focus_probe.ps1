Add-Type -AssemblyName System.Drawing
Add-Type @"
using System;
using System.Runtime.InteropServices;
public class J {
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern bool BringWindowToTop(IntPtr h);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] public static extern bool GetCursorPos(out POINT p);
  [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int vkey);
  [DllImport("user32.dll")] public static extern uint SendInput(uint n, INPUT[] p, int size);
  [DllImport("user32.dll")] public static extern void mouse_event(uint f, int dx, int dy, uint d, IntPtr e);
  [StructLayout(LayoutKind.Sequential)] public struct POINT { public int X, Y; }
  [StructLayout(LayoutKind.Sequential)]
  public struct MOUSEINPUT { public int dx, dy; public uint mouseData, dwFlags, time; public IntPtr dwExtraInfo; }
  [StructLayout(LayoutKind.Explicit)]
  public struct INPUTUNION { [FieldOffset(0)] public MOUSEINPUT mi; }
  [StructLayout(LayoutKind.Sequential)]
  public struct INPUT { public uint type; public INPUTUNION u; }
}
"@ -ReferencedAssemblies System.Drawing

$exe = "C:\Users\Administrator\AppData\Local\Temp\opencode\pixeldemo\Build\PixelDemo.exe"

Add-Type @"
using System;
using System.Runtime.InteropServices;
public class K {
  [DllImport("user32.dll")] public static extern bool MoveWindow(IntPtr h, int x, int y, int w, int ht, bool r);
}
"@ -ReferencedAssemblies System.Drawing

$p = Start-Process -FilePath $exe -PassThru `
      -ArgumentList "-screen-width","1280","-screen-height","720","-screen-fullscreen","0","-popupwindow"
Start-Sleep -Seconds 9
$h = (Get-Process -Id $p.Id).MainWindowHandle
Write-Output "unity hwnd = $h"
[K]::MoveWindow($h, 0, 0, 1280, 720, $true) | Out-Null
[J]::BringWindowToTop($h) | Out-Null
[J]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 1000

$fg = [J]::GetForegroundWindow()
Write-Output "foreground = $fg   match = $($fg -eq $h)"

[J]::SetCursorPos(300, 360) | Out-Null
Start-Sleep -Milliseconds 300
$pt = New-Object J+POINT
[J]::GetCursorPos([ref]$pt) | Out-Null
Write-Output "cursor now at ($($pt.X),$($pt.Y)) target was (300,360)"

$down = New-Object J+INPUT; $down.type = 0; $down.u.mi.dwFlags = 0x0002
$sz = [System.Runtime.InteropServices.Marshal]::SizeOf([type][J+INPUT])

$s = [J]::SendInput(1, @($down), $sz)
Start-Sleep -Milliseconds 120
Write-Output "during hold: LButton GetAsyncKeyState = 0x$('{0:X4}' -f [J]::GetAsyncKeyState(0x01))  sendInput=$s"
Start-Sleep -Milliseconds 500
Write-Output "during hold: LButton GetAsyncKeyState = 0x$('{0:X4}' -f [J]::GetAsyncKeyState(0x01))"

$up = New-Object J+INPUT; $up.type = 0; $up.u.mi.dwFlags = 0x0004
[J]::SendInput(1, @($up), $sz) | Out-Null
Write-Output "released; LButton = 0x$('{0:X4}' -f [J]::GetAsyncKeyState(0x01))"

# keyboard cross-check: hold W, sample during
$kd = New-Object J+INPUT; $kd.type = 1; $kd.u.mi.dwFlags = 0x0001
$ku = New-Object J+INPUT; $ku.type = 1; $ku.u.mi.dwFlags = 0x0003
[void][J]::SendInput(1, @($kd), $sz)
Start-Sleep -Milliseconds 300
Write-Output "during hold: W GetAsyncKeyState = 0x$('{0:X4}' -f [J]::GetAsyncKeyState(0x57))"
[void][J]::SendInput(1, @($ku), $sz)

Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
Write-Output "done"
