param(
    [string]$Path = 'dist/Affinisaur.exe',
    [switch]$RequireSigned
)
$ErrorActionPreference = 'Stop'
$signature = Get-AuthenticodeSignature -LiteralPath $Path
if ($signature.Status -eq 'NotSigned' -and !$RequireSigned) {
    Write-Output 'Unsigned release: no verified publisher identity.'
    exit 0
}
if ($signature.Status -ne 'Valid') { throw "Invalid or missing signature: $($signature.Status)" }
if ($null -eq $signature.TimeStamperCertificate) { throw 'A trusted timestamp is required for signed releases.' }
Write-Output "Verified publisher: $($signature.SignerCertificate.Subject)"
