<#
    Signs the sideload MSIX and drops the matching public certificate beside it.

    Why this exists: v1.1.0.0 shipped to GitHub with an unsigned MSIX and no .cer,
    so it could not be installed at all (issue #4). Signing was switched off in the
    csproj while a certificate had yet to be created, and nothing in the build
    noticed. This script makes an unsigned sideload package fail the build instead
    of reaching a release.

    The Store package is unaffected — the Store re-signs on submission. This is only
    for the zip published on GitHub.

    The certificate subject MUST equal the Publisher in Package.appxmanifest.
    Windows refuses to install an MSIX whose signer differs from its declared
    publisher, so a generic code-signing certificate will not do.

    Creating the certificate once (maintainer machine):

      New-SelfSignedCertificate -Type Custom -Subject "CN=A55D3041-E7F5-46CB-8707-AD9C81D7CB43" `
        -KeyUsage DigitalSignature -FriendlyName "LumiFinder Sideload Signing" `
        -CertStoreLocation "Cert:\CurrentUser\My" -NotAfter (Get-Date).AddYears(3) `
        -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.3", "2.5.29.19={text}Subject Type:End Entity")

    The private key stays in the maintainer's certificate store. It is never in the
    repository — anyone holding it could sign packages that machines trusting this
    certificate would install without a prompt.
#>
[CmdletBinding()]
param(
    # Folder holding the sideload payload (the *_Test folder produced by MSBuild).
    [Parameter(Mandatory = $true)][string]$PackageDir,

    # Must match Package.appxmanifest's Publisher exactly.
    [string]$Subject = "CN=A55D3041-E7F5-46CB-8707-AD9C81D7CB43",

    # The certificate that signed the public v1.1.0.0 release, pinned by thumbprint.
    #
    # Pinned rather than searched because this machine holds three certificates with
    # that same subject ("LumiFiles Dev", "SpanFinder Dev", and this one), and any
    # heuristic - newest, longest-lived - silently picks one of the others. Signing a
    # later release with a different key would strand everyone who already trusted
    # this one: same publisher, different certificate, install refused again.
    #
    # Only change this if the key is being rotated on purpose, and say so in the
    # release notes, because every existing user has to trust the new one.
    [string]$Thumbprint = "457A9E4CCE66DFF4769FC98F3ABCE2621C9B6D0A",

    # Keeps the signature valid after the certificate expires.
    [string]$TimestampUrl = "http://timestamp.digicert.com"
)

$ErrorActionPreference = "Stop"

function Fail($message) {
    Write-Host ""
    Write-Host "ERROR: $message"
    exit 1
}

if (-not (Test-Path -LiteralPath $PackageDir -PathType Container)) {
    Fail "package folder not found: $PackageDir"
}

$packages = @(Get-ChildItem -LiteralPath $PackageDir -Filter *.msix -File)
if ($packages.Count -ne 1) {
    Fail "expected exactly one .msix in $PackageDir, found $($packages.Count)"
}
$msix = $packages[0].FullName

# Newest SDK wins; the signtool that ships with older kits predates MSIX support.
$signtool = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\*\x64\signtool.exe" -ErrorAction SilentlyContinue |
            Sort-Object FullName | Select-Object -Last 1
if (-not $signtool) { Fail "signtool.exe not found - install the Windows 10/11 SDK" }

# HasPrivateKey matters: a public-only copy is present too once the .cer has been
# imported anywhere on this machine, and signing with that one fails obscurely.
$cert = @(Get-ChildItem Cert:\CurrentUser\My |
          Where-Object { $_.Thumbprint -eq $Thumbprint -and $_.HasPrivateKey })[0]

if (-not $cert) {
    Fail @"
signing certificate $Thumbprint not found (or has no private key) in Cert:\CurrentUser\My

This is the key that signed the published v1.1.0.0 release. Without it the sideload
package cannot be signed in a way existing users already trust. Restore it from backup
rather than creating a new certificate - a new key means every user has to trust it
again. See the header of this script if a deliberate rotation really is intended.
"@
}

# The signer must equal the manifest's Publisher or Windows refuses the install, so
# check rather than assume the pinned thumbprint still belongs to the right subject.
if ($cert.Subject -ne $Subject) {
    Fail "certificate $Thumbprint has subject '$($cert.Subject)' but the package declares Publisher '$Subject'"
}

if ($cert.NotAfter -lt (Get-Date)) {
    Fail "signing certificate expired on $($cert.NotAfter) - create a new one"
}

Write-Host "[sign] package : $([System.IO.Path]::GetFileName($msix))"
Write-Host "[sign] cert    : $($cert.Subject)  (expires $($cert.NotAfter.ToString('yyyy-MM-dd')))"

& $signtool.FullName sign /fd SHA256 /td SHA256 /tr $TimestampUrl /sha1 $cert.Thumbprint $msix
if ($LASTEXITCODE -ne 0) { Fail "signtool failed with exit code $LASTEXITCODE" }

# Trust the artifact, not the tool: confirm the signature blob is actually in the
# package. This is the exact check that would have caught the v1.1.0.0 mistake.
Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($msix)
try {
    $signed = @($zip.Entries | Where-Object { $_.FullName -eq "AppxSignature.p7x" }).Count -eq 1
} finally {
    $zip.Dispose()
}
if (-not $signed) { Fail "AppxSignature.p7x missing after signing - package is still unsigned" }

# Add-AppDevPackage.ps1 takes whatever single .cer sits next to the package, so the
# name is free but there must be exactly one.
Get-ChildItem -LiteralPath $PackageDir -Filter *.cer -File | Remove-Item -Force
$cerPath = Join-Path $PackageDir ("{0}.cer" -f [System.IO.Path]::GetFileNameWithoutExtension($msix))
Export-Certificate -Cert $cert -FilePath $cerPath -Type CERT | Out-Null

Write-Host "[sign] signed + timestamped, certificate written to $([System.IO.Path]::GetFileName($cerPath))"
exit 0
