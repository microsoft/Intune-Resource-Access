#requires -Version 7.0

<#
.SYNOPSIS
Runs a non-production end-to-end Intune PFX import test.

.DESCRIPTION
NOT FOR PRODUCTION USE.

Signs in to Microsoft Graph, creates a temporary RSA or ECC certificate and
machine CNG key, creates or reuses an Entra application in the selected tenant,
authenticates through the IntunePfxImport setup object, imports the certificate
record into Intune, and reads it back. RSA with a 2048-bit key is the default
certificate configuration.

After the record is submitted, the Entra application, machine key, and Intune
record are intentionally retained because Certificate Connector processing is
asynchronous. A newly created key is removed if the run fails before submission.
The temporary PFX file is always deleted.

Run from an elevated PowerShell 7 session. The script can install Microsoft
Graph PowerShell modules for the current user and can open an admin-consent page.

.PARAMETER TargetUpn
Existing Microsoft Entra user in the authenticated tenant that receives the
temporary imported certificate record.

.PARAMETER CertificateSubject
X.500 subject for the temporary certificate. Defaults to CN=<TargetUpn>. The
target UPN remains in the certificate email Subject Alternative Name so Intune
can infer the user identity independently of the subject.

.PARAMETER TenantId
Optional Microsoft Entra tenant GUID. When omitted, the script prompts for
Microsoft Graph sign-in and uses the tenant selected during that sign-in. When
specified, sign-in is restricted to that tenant.

.PARAMETER ModulePath
Path to the IntunePfxImport module manifest.

.PARAMETER ApplicationDisplayName
Display name of the Entra application to create or reuse.

.PARAMETER ApplicationId
Application (client) ID of an existing, configured Intune PFX import
application. When supplied, the script skips Graph SDK onboarding and requires
only the Import PFX sign-in. TenantId remains optional.

.PARAMETER Setup
Setup object returned by a previous onboarding run of this dot-sourced script
or by Initialize-IntunePfxImportApplication. This is the preferred single-sign-
in mode because it already contains the application, tenant, and endpoint
configuration.

.PARAMETER ProviderName
Windows CNG provider used for the temporary machine encryption key.

.PARAMETER KeyName
Name of the temporary machine CNG encryption key.

.PARAMETER Algorithm
Private-key algorithm for the temporary certificate. Defaults to rsa.

.PARAMETER Curve
Named NIST curve for an ECC certificate. Valid only when Algorithm is ecc.

.PARAMETER KeySize
Key size for an RSA certificate. Valid only when Algorithm is rsa. Defaults to
2048 bits.

.PARAMETER IntendedPurpose
Intune certificate purpose assigned to the imported record. Defaults to
smimeEncryption.

.PARAMETER ConnectorServiceAccount
Optional Windows account used by the Certificate Connector service. When the
script creates the machine key, this account is granted read access.

.PARAMETER GrantAdminConsent
Opens the admin-consent page and pauses while the operator grants consent.

.EXAMPLE
.\Test-IntunePfxImportE2E.ps1 -TargetUpn user@contoso.com

Prompts for Microsoft Graph sign-in, uses the selected tenant, and runs the
workflow with the default RSA 2048 certificate.

.EXAMPLE
.\Test-IntunePfxImportE2E.ps1 -TargetUpn user@contoso.com -CertificateSubject 'CN=Intune PFX E2E Test,O=Contoso'

Uses a custom certificate subject while retaining the target UPN in the email
Subject Alternative Name used for Intune identity inference.

.EXAMPLE
.\Test-IntunePfxImportE2E.ps1 -TenantId 00000000-0000-0000-0000-000000000000 -TargetUpn user@contoso.com -Algorithm ecc -Curve nistP384

Restricts sign-in to the specified tenant and runs the workflow with an ECC
P-384 certificate.

.EXAMPLE
.\Test-IntunePfxImportE2E.ps1 -TargetUpn user@contoso.com -ApplicationId 11111111-1111-1111-1111-111111111111

Uses an existing configured application and requires only one sign-in.

.EXAMPLE
. .\Test-IntunePfxImportE2E.ps1 -TargetUpn user@contoso.com -Setup $setup

Reuses the setup object retained by an earlier dot-sourced onboarding run and
requires only one sign-in.

.NOTES
This sample changes a live tenant and the local machine. Review it before use.
Do not use it as production automation.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$TargetUpn,

    [ValidateNotNullOrEmpty()]
    [string]$CertificateSubject = "CN=$TargetUpn",

    [ValidatePattern('^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$')]
    [string]$TenantId,

    [ValidateScript({ Test-Path -LiteralPath $_ -PathType Leaf })]
    [string]$ModulePath = (Join-Path $PSScriptRoot '..\PFXImportPS\IntunePfxImport.psd1'),

    [string]$ApplicationDisplayName = 'Intune PFX Import E2E',

    [ValidatePattern('^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$')]
    [string]$ApplicationId,

    [ValidateNotNull()]
    [psobject]$Setup,

    [string]$ProviderName = 'Microsoft Software Key Storage Provider',

    [string]$KeyName = "IntunePfxImportE2E-$([Guid]::NewGuid().ToString('N'))",

    [ValidateSet('rsa', 'ecc')]
    [string]$Algorithm = 'rsa',

    [ValidateSet('nistP256', 'nistP384', 'nistP521')]
    [string]$Curve = 'nistP384',

    [ValidateSet(2048, 3072, 4096)]
    [int]$KeySize = 2048,

    [ValidateSet('unassigned', 'smimeEncryption', 'smimeSigning', 'vpn', 'wifi')]
    [string]$IntendedPurpose = 'smimeEncryption',

    [ValidateNotNullOrEmpty()]
    [string]$ConnectorServiceAccount,

    [switch]$GrantAdminConsent
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Write-Warning 'NON-PRODUCTION SAMPLE: this script creates local and live-tenant test artifacts.'

if ($Algorithm -eq 'rsa' -and $PSBoundParameters.ContainsKey('Curve')) {
    throw '-Curve is valid only when -Algorithm is ecc.'
}
if ($Algorithm -eq 'ecc' -and $PSBoundParameters.ContainsKey('KeySize')) {
    throw '-KeySize is valid only when -Algorithm is rsa.'
}
if ($ApplicationId -and $GrantAdminConsent) {
    throw '-GrantAdminConsent cannot be used with -ApplicationId. Use onboarding or supply a reusable -Setup object.'
}
if ($Setup -and $ApplicationId) {
    throw 'Specify either -Setup or -ApplicationId, not both.'
}
if (($Setup -or $ApplicationId) -and $PSBoundParameters.ContainsKey('ApplicationDisplayName')) {
    throw '-ApplicationDisplayName is available only during application onboarding.'
}

$principal = New-Object Security.Principal.WindowsPrincipal(
    [Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this script from an elevated PowerShell session. Creating the machine CNG key requires administrator access.'
}

Import-Module $ModulePath -Force

if (
    -not $Setup -and
    -not $ApplicationId -and
    $null -eq (Get-Command Initialize-IntunePfxImportApplication -ErrorAction SilentlyContinue)
) {
    throw "The module at '$ModulePath' does not contain the Version 3 onboarding command."
}
$loadedModule = Get-Module -Name IntunePfxImport
if ($null -eq $loadedModule -or $loadedModule.Version -lt [version]'3.0.0') {
    throw "This E2E script requires IntunePfxImport 3.0.0 or later. Use the manifest and script module from the same release."
}
$addIntuneKspKeyCommand = Get-Command Add-IntuneKspKey -ErrorAction SilentlyContinue
if ($null -eq $addIntuneKspKeyCommand) {
    throw "The module at '$ModulePath' does not contain Add-IntuneKspKey. Use the manifest and script module from the same release."
}
if (
    $PSBoundParameters.ContainsKey('ConnectorServiceAccount') -and
    -not $addIntuneKspKeyCommand.Parameters.ContainsKey('ConnectorServiceAccount')
) {
    throw "The module at '$ModulePath' does not support -ConnectorServiceAccount. Copy the PFXImportPS and Examples folders from the same release."
}

if (-not $Setup -and -not $ApplicationId) {
    if (
        $null -eq (Get-Command Connect-MgGraph -ErrorAction SilentlyContinue) -or
        $null -eq (Get-Command Disconnect-MgGraph -ErrorAction SilentlyContinue) -or
        $null -eq (Get-Command Get-MgApplication -ErrorAction SilentlyContinue)
    ) {
        Write-Host 'Installing Microsoft Graph PowerShell modules for the current user...'
        Install-Module Microsoft.Graph.Authentication, Microsoft.Graph.Applications -Scope CurrentUser -Force
    }
}

$provider = New-Object Security.Cryptography.CngProvider($ProviderName)
$createdKey = $false
$certificate = $null
$certificateKey = $null
$record = $null
$recordSubmissionAttempted = $false
$graphConnected = $false
$effectiveTenantId = if ($Setup) { [string]$Setup.TenantId } else { $null }
$effectiveApplicationId = if ($Setup) { [string]$Setup.ApplicationId } else { $ApplicationId }
if (
    $Setup -and
    $PSBoundParameters.ContainsKey('TenantId') -and
    $TenantId -ne $effectiveTenantId
) {
    throw "TenantId '$TenantId' does not match the tenant '$effectiveTenantId' in Setup."
}
$pfxPath = Join-Path ([IO.Path]::GetTempPath()) "IntunePfxImportE2E-$([Guid]::NewGuid()).pfx"
$passwordText = [Guid]::NewGuid().ToString('N')
$pfxPassword = ConvertTo-SecureString $passwordText -AsPlainText -Force

try {
    if (-not [Security.Cryptography.CngKey]::Exists(
        $KeyName,
        $provider,
        [Security.Cryptography.CngKeyOpenOptions]::MachineKey)) {
        Write-Host "Creating machine CNG key '$ProviderName\$KeyName'..."
        $keyParameters = @{
            ProviderName = $ProviderName
            KeyName = $KeyName
            Confirm = $false
        }
        if ($PSBoundParameters.ContainsKey('ConnectorServiceAccount')) {
            $keyParameters.ConnectorServiceAccount = $ConnectorServiceAccount
        }
        Add-IntuneKspKey @keyParameters
        $createdKey = $true
    }
    else {
        Write-Host "Reusing machine CNG key '$ProviderName\$KeyName'."
    }

    if ($Algorithm -eq 'rsa') {
        Write-Host "Generating an RSA $KeySize-bit certificate with subject '$CertificateSubject' for '$TargetUpn'..."
        $certificateKey = [Security.Cryptography.RSA]::Create()
        $certificateKey.KeySize = $KeySize
        $request = [Security.Cryptography.X509Certificates.CertificateRequest]::new(
            $CertificateSubject,
            $certificateKey,
            [Security.Cryptography.HashAlgorithmName]::SHA256,
            [Security.Cryptography.RSASignaturePadding]::Pkcs1)
    }
    else {
        $curveHashAlgorithm = switch ($Curve) {
            'nistP256' { [Security.Cryptography.HashAlgorithmName]::SHA256 }
            'nistP384' { [Security.Cryptography.HashAlgorithmName]::SHA384 }
            'nistP521' { [Security.Cryptography.HashAlgorithmName]::SHA512 }
        }
        Write-Host "Generating an ECC $Curve certificate with subject '$CertificateSubject' for '$TargetUpn'..."
        $certificateKey = [Security.Cryptography.ECDsa]::Create()
        $certificateKey.GenerateKey(
            [Security.Cryptography.ECCurve]::CreateFromFriendlyName($Curve))
        $request = [Security.Cryptography.X509Certificates.CertificateRequest]::new(
            $CertificateSubject,
            $certificateKey,
            $curveHashAlgorithm)
    }

    $san = [Security.Cryptography.X509Certificates.SubjectAlternativeNameBuilder]::new()
    $san.AddEmailAddress($TargetUpn)
    $request.CertificateExtensions.Add($san.Build())
    $request.CertificateExtensions.Add(
        [Security.Cryptography.X509Certificates.X509BasicConstraintsExtension]::new(
            $false,
            $false,
            0,
            $true))

    $certificate = $request.CreateSelfSigned(
        [DateTimeOffset]::UtcNow.AddMinutes(-5),
        [DateTimeOffset]::UtcNow.AddDays(7))
    $pfxBytes = $certificate.Export(
        [Security.Cryptography.X509Certificates.X509ContentType]::Pfx,
        $passwordText)
    [IO.File]::WriteAllBytes($pfxPath, $pfxBytes)

    if ($Setup) {
        if ([string]::IsNullOrWhiteSpace($effectiveApplicationId)) {
            throw 'Setup does not contain an ApplicationId.'
        }
        if ([string]::IsNullOrWhiteSpace($effectiveTenantId)) {
            throw 'Setup does not contain a TenantId.'
        }
        Write-Host "Reusing setup for Intune PFX application '$effectiveApplicationId' in tenant '$effectiveTenantId'..."
        Write-Host "Application ID: $effectiveApplicationId"
        if ($GrantAdminConsent) {
            if ([string]::IsNullOrWhiteSpace([string]$Setup.AdminConsentUri)) {
                throw 'Setup does not contain an AdminConsentUri.'
            }
            Write-Host 'Opening the admin-consent page...'
            Start-Process $Setup.AdminConsentUri
            Read-Host 'Grant consent in the browser, then press Enter to continue'
        }
        Set-IntuneAuthenticationToken -Setup $Setup -Confirm:$false
    }
    elseif ($ApplicationId) {
        Write-Host "Authenticating once with existing Intune PFX application '$ApplicationId'..."
        Write-Host "Application ID: $effectiveApplicationId"
        $authenticationTenantId = $TenantId
        if (-not $PSBoundParameters.ContainsKey('TenantId')) {
            $targetDomain = ($TargetUpn -split '@', 2)[1]
            if ([string]::IsNullOrWhiteSpace($targetDomain)) {
                throw "TargetUpn '$TargetUpn' does not contain a tenant domain."
            }
            Write-Host "Resolving the Microsoft Entra tenant for '$targetDomain'..."
            $escapedDomain = [Uri]::EscapeDataString($targetDomain)
            $openIdConfiguration = Invoke-RestMethod `
                -Uri "https://login.microsoftonline.com/$escapedDomain/v2.0/.well-known/openid-configuration" `
                -Method Get `
                -ErrorAction Stop
            $tenantPattern = '/([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})(?:/|$)'
            $tenantSource = @(
                $openIdConfiguration.issuer
                $openIdConfiguration.authorization_endpoint
            ) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
            $tenantMatch = @($tenantSource | Where-Object { $_ -match $tenantPattern })[0]
            if ($null -eq $tenantMatch) {
                throw "Microsoft Entra metadata for '$targetDomain' did not identify a tenant GUID. Supply -TenantId explicitly."
            }
            $authenticationTenantId = [regex]::Match($tenantMatch, $tenantPattern).Groups[1].Value
            Write-Host "Resolved Microsoft Entra tenant '$authenticationTenantId'."
        }
        if ($ApplicationId -eq $authenticationTenantId) {
            throw "ApplicationId '$ApplicationId' is the tenant ID, not an application client ID. Use the Application ID printed by a successful onboarding run."
        }
        $effectiveTenantId = $authenticationTenantId
        $authenticationParameters = @{
            ClientId = $ApplicationId
            TenantId = $authenticationTenantId
            Confirm = $false
        }
        Set-IntuneAuthenticationToken @authenticationParameters
    }
    else {
        $existingGraphContext = Get-MgContext -ErrorAction SilentlyContinue
        if ($null -ne $existingGraphContext) {
            Write-Host "Disconnecting existing Microsoft Graph context for tenant '$($existingGraphContext.TenantId)'..."
            Disconnect-MgGraph | Out-Null
        }
        $graphConnectionParameters = @{
            Scopes = 'Application.ReadWrite.All', 'Application.Read.All'
            ContextScope = 'Process'
            NoWelcome = $true
        }
        if ($PSBoundParameters.ContainsKey('TenantId')) {
            $graphConnectionParameters.TenantId = $TenantId
            Write-Host "Signing in to Microsoft Graph tenant '$TenantId' for this process..."
        }
        else {
            Write-Host 'Signing in to Microsoft Graph. Select the tenant to use for this test...'
        }
        Connect-MgGraph @graphConnectionParameters
        $graphConnected = $true

        $graphContext = Get-MgContext
        if ($null -eq $graphContext -or [string]::IsNullOrWhiteSpace($graphContext.TenantId)) {
            throw 'Microsoft Graph sign-in did not return a tenant ID.'
        }
        $effectiveTenantId = $graphContext.TenantId
        if ($PSBoundParameters.ContainsKey('TenantId') -and $effectiveTenantId -ne $TenantId) {
            throw "Microsoft Graph connected to tenant '$effectiveTenantId' instead of requested tenant '$TenantId'."
        }

        Write-Host "Using Microsoft Graph tenant '$effectiveTenantId'."
        Write-Host "Creating or reusing Entra application '$ApplicationDisplayName' in tenant '$effectiveTenantId'..."
        $Setup = Initialize-IntunePfxImportApplication `
            -DisplayName $ApplicationDisplayName `
            -TenantId $effectiveTenantId `
            -AuthenticationMode PublicClient `
            -Confirm:$false
        $effectiveApplicationId = $Setup.ApplicationId
        Write-Host "Application ID: $effectiveApplicationId"

        if ($GrantAdminConsent) {
            Write-Host 'Opening the admin-consent page...'
            Start-Process $Setup.AdminConsentUri
            Read-Host 'Grant consent in the browser, then press Enter to continue'
        }

        Write-Host 'Authenticating to Intune from the setup object...'
        Set-IntuneAuthenticationToken -Setup $Setup -Confirm:$false
        if ($MyInvocation.InvocationName -eq '.') {
            Write-Host 'The setup object remains available as $setup because this script was dot-sourced.'
            Write-Host 'For future single-sign-in runs, use -Setup $setup.'
        }
        else {
            Write-Host 'Dot-source this script during onboarding to retain $setup for future -Setup runs.'
        }
        Write-Host "For future single-sign-in runs, use -ApplicationId '$effectiveApplicationId'."
    }

    Write-Host "Validating that '$TargetUpn' exists in the authenticated tenant..."
    Get-IntuneUserId -UPN $TargetUpn | Out-Null

    Write-Host 'Creating the imported PFX record. UPN is inferred from the certificate SAN...'
    $record = New-IntuneUserPfxCertificate `
        -PathToPfxFile $pfxPath `
        -PfxPassword $pfxPassword `
        -ProviderName $ProviderName `
        -KeyName $KeyName `
        -IntendedPurpose $IntendedPurpose `
        -PaddingScheme OaepSha512

    if ($record.UserPrincipalName -ne $TargetUpn) {
        throw "Certificate identity inference returned '$($record.UserPrincipalName)' instead of '$TargetUpn'."
    }
    if ($record.KeyAlgorithm -ne $Algorithm) {
        throw "Expected the generated record to identify a $Algorithm certificate but got '$($record.KeyAlgorithm)'."
    }
    if ($record.IntendedPurpose -ne $IntendedPurpose) {
        throw "Expected intended purpose '$IntendedPurpose' but got '$($record.IntendedPurpose)'."
    }

    Write-Host 'Importing the PFX record into Intune...'
    $recordSubmissionAttempted = $true
    Import-IntuneUserPfxCertificate -CertificateList $record -Confirm:$false

    Write-Host 'Reading the record back from Intune...'
    $importedRecord = @(
        Get-IntuneUserPfxCertificate -UserThumbprintList ([pscustomobject]@{
            User = $TargetUpn
            Thumbprint = $record.Thumbprint
        })
    )
    if ($importedRecord.Count -ne 1) {
        throw "Expected one imported record but found $($importedRecord.Count)."
    }
    if ($importedRecord[0].UserPrincipalName -ne $TargetUpn) {
        throw "Expected persisted UPN '$TargetUpn' but got '$($importedRecord[0].UserPrincipalName)'."
    }
    if ($importedRecord[0].Thumbprint -ne $record.Thumbprint) {
        throw "Expected persisted thumbprint '$($record.Thumbprint)' but got '$($importedRecord[0].Thumbprint)'."
    }

    Write-Host ''
    Write-Host 'PASS: Intune PFX import E2E workflow completed successfully.' -ForegroundColor Green
    Write-Host "Application ID: $effectiveApplicationId"
    Write-Host "Tenant ID: $effectiveTenantId"
    Write-Host "Certificate thumbprint: $($record.Thumbprint)"
    Write-Host "Retained connector key: $ProviderName\$KeyName"
    Write-Host 'The key and Intune record must remain available until the Certificate Connector finishes processing.'
}
finally {
    if (Test-Path -LiteralPath $pfxPath) {
        Remove-Item -LiteralPath $pfxPath -Force
    }
    if ($null -ne $certificate) { $certificate.Dispose() }
    if ($null -ne $certificateKey) { $certificateKey.Dispose() }
    if ($graphConnected) {
        Disconnect-MgGraph -ErrorAction SilentlyContinue | Out-Null
    }
    if ($createdKey -and -not $recordSubmissionAttempted) {
        try {
            $failedRunKey = [Security.Cryptography.CngKey]::Open(
                $KeyName,
                $provider,
                [Security.Cryptography.CngKeyOpenOptions]::MachineKey)
            try {
                $failedRunKey.Delete()
            }
            finally {
                $failedRunKey.Dispose()
            }
        }
        catch {
            Write-Warning "The failed run could not remove newly created machine key '$ProviderName\$KeyName': $($_.Exception.Message)"
        }
    }
    $passwordText = $null
}
