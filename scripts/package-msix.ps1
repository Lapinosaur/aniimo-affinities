param([string]$SdkVersion = '10.0.26100.0')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$makeAppx = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin\$SdkVersion\x64\makeappx.exe"
if (!(Test-Path -LiteralPath $makeAppx)) { throw "Windows SDK MakeAppx missing: $makeAppx" }
Push-Location $taskRoot
try {
    node scripts/cli.mjs build
    if ($LASTEXITCODE -ne 0) { throw 'Runtime build failed' }
    # A fresh staging folder prevents stale files from entering the package.
    $stage = Join-Path $taskRoot ('dist\msix\stage-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path (Join-Path $stage 'Assets') -Force | Out-Null
    Copy-Item -LiteralPath 'dist/Affinisaur.exe' -Destination $stage
    Copy-Item -LiteralPath 'LICENSE','THIRD_PARTY_NOTICES.md' -Destination $stage
    Copy-Item -LiteralPath 'docs/legal.md','docs/privacy.md' -Destination $stage
    [xml]$manifest = Get-Content -LiteralPath 'packaging/msix/AppxManifest.xml'
    $version = (Get-Content package.json | ConvertFrom-Json).version + '.0'
    $manifest.Package.Identity.Version = $version
    $manifest.Save((Join-Path $stage 'AppxManifest.xml'))
    Add-Type -AssemblyName System.Drawing
    # The largest ICO entry is PNG-compressed; decode it directly (GDI Icon.ToBitmap cannot).
    $iconBytes = [System.IO.File]::ReadAllBytes((Join-Path $taskRoot 'assets/branding/app.ico'))
    $entry = 6 + 16 * ([BitConverter]::ToUInt16($iconBytes, 4) - 1)
    $length = [BitConverter]::ToUInt32($iconBytes, $entry + 8)
    $offset = [BitConverter]::ToUInt32($iconBytes, $entry + 12)
    $imageStream = New-Object System.IO.MemoryStream
    $imageStream.Write($iconBytes, $offset, $length)
    $imageStream.Position = 0
    $original = [System.Drawing.Image]::FromStream($imageStream)
    try {
        foreach ($item in @(@('StoreLogo',50),@('Square44x44Logo',44),@('Square150x150Logo',150))) {
            $bitmap = New-Object System.Drawing.Bitmap($item[1], $item[1])
            $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
            try {
                $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
                $graphics.DrawImage($original, 0, 0, $item[1], $item[1])
                $bitmap.Save((Join-Path $stage ('Assets\' + $item[0] + '.png')), [System.Drawing.Imaging.ImageFormat]::Png)
            } finally { $graphics.Dispose(); $bitmap.Dispose() }
        }
    } finally { $original.Dispose(); $imageStream.Dispose() }
    $package = Join-Path $taskRoot "dist\msix\Affinisaur-$version-local-test.msix"
    & $makeAppx pack /d $stage /p $package /o
    if ($LASTEXITCODE -ne 0) { throw 'MakeAppx validation failed' }
    Write-Output "Unsigned LOCAL TEST package: $package"
    Write-Output "Staging: $stage"
} finally { Pop-Location }
