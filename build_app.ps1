$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$wpfDir = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\WPF"
$outExe = "C:\Users\Enry\.gemini\antigravity\scratch\system-optimizer-app\WinOptimizer.exe"
$icon = "C:\Users\Enry\.gemini\antigravity\scratch\system-optimizer-app\app_icon.ico"
$manifest = "C:\Users\Enry\.gemini\antigravity\scratch\system-optimizer-app\app.manifest"
$src = "C:\Users\Enry\.gemini\antigravity\scratch\system-optimizer-app\Program.cs"

$args = @(
    "/target:winexe",
    "/optimize+",
    "/platform:x64",
    "/out:`"$outExe`"",
    "/win32icon:`"$icon`"",
    "/win32manifest:`"$manifest`"",
    "/reference:`"$wpfDir\PresentationFramework.dll`"",
    "/reference:`"$wpfDir\PresentationCore.dll`"",
    "/reference:`"$wpfDir\WindowsBase.dll`"",
    "/reference:System.Xaml.dll",
    "/reference:System.dll",
    "/reference:System.Management.dll",
    "`"$src`""
)

Write-Host "Compilazione WinOptimizer.exe a 64-bit con privilegi di Amministratore e WPF puro..." -ForegroundColor Cyan
& $csc $args

if (Test-Path $outExe) {
    $size = (Get-Item $outExe).Length / 1KB
    Write-Host "[OK] Eseguibile Desktop Nativo compilato con successo ($([math]::Round($size, 1)) KB)" -ForegroundColor Green

    # NOTA: Per la distribuzione ad altri PC, NON firmiamo il file con certificato locale self-signed.
    # Un certificato locale non attendibile causa il blocco immediato/eliminazione da parte di Defender su altri computer.
    # Non firmato, il file può essere semplicemente sbloccato dalle proprietà.

    # Copy to Desktop
    Copy-Item $outExe -Destination "C:\Users\Enry\Desktop\SystemOptimizer.exe" -Force
    Write-Host "[OK] SystemOptimizer.exe copiato sul Desktop: C:\Users\Enry\Desktop\SystemOptimizer.exe" -ForegroundColor Green
} else {
    Write-Host "[ERRORE] Compilazione fallita." -ForegroundColor Red
}
