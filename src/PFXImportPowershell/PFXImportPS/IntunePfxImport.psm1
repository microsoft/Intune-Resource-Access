Set-StrictMode -Version Latest

$legacyCertificateTypeName = 'Microsoft.Management.Services.Api.UserPFXCertificate'
if ($null -eq ($legacyCertificateTypeName -as [type])) {
    Add-Type -TypeDefinition @'
namespace Microsoft.Management.Services.Api
{
    using System;

    public enum UserPfxIntendedPurpose
    {
        Unassigned = 0,
        SmimeEncryption = 1,
        SmimeSigning = 2,
        VPN = 4,
        Wifi = 8
    }

    public enum UserPfxPaddingScheme
    {
        None = 0,

        [Obsolete("Pkcs1 no longer supported")]
        Pkcs1 = 1,

        [Obsolete("OaepSha1 no longer supported")]
        OaepSha1 = 2,

        OaepSha256 = 3,
        OaepSha384 = 4,
        OaepSha512 = 5
    }

    public sealed class UserPFXCertificate
    {
        public string Id { get; set; }
        public string Thumbprint { get; set; }
        public UserPfxIntendedPurpose IntendedPurpose { get; set; }
        public string UserPrincipalName { get; set; }
        public DateTimeOffset StartDateTime { get; set; }
        public DateTimeOffset ExpirationDateTime { get; set; }
        public string ProviderName { get; set; }
        public string KeyName { get; set; }
        public UserPfxPaddingScheme PaddingScheme { get; set; }
        public byte[] EncryptedPfxBlob { get; set; }
        public string EncryptedPfxPassword { get; set; }
        public DateTimeOffset CreatedDateTime { get; set; }
        public DateTimeOffset LastModifiedDateTime { get; set; }
    }
}
'@ -ErrorAction Stop
}

if ($null -eq ('Microsoft.Management.Powershell.PFXImport.Cmdlets.UserThumbprint' -as [type])) {
    Add-Type -TypeDefinition @'
namespace Microsoft.Management.Powershell.PFXImport.Cmdlets
{
    public struct UserThumbprint
    {
        public string User;
        public string Thumbprint;
    }
}
'@ -ErrorAction Stop
}

$script:AuthContext = $null
$script:CommercialAuthUri = 'login.microsoftonline.com'
$script:CommercialGraphUri = 'https://graph.microsoft.com'
$script:DefaultRedirectUri = 'https://login.microsoftonline.com/common/oauth2/nativeclient'
$script:DefaultProviderName = $null
$script:DefaultKeyName = $null

function Get-IntuneLegacyModuleConfiguration {
    [CmdletBinding()]
    param()

    $privateData = $ExecutionContext.SessionState.Module.PrivateData
    if ($privateData -is [Collections.IDictionary]) {
        return $privateData
    }
    return @{}
}

function Get-IntuneLegacyConfigurationValue {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][Collections.IDictionary]$Configuration,
        [Parameter(Mandatory)][string]$Name
    )

    foreach ($key in $Configuration.Keys) {
        if ([string]$key -ieq $Name) {
            return $Configuration[$key]
        }
    }
    return $null
}

function ConvertTo-PlainText {
    [CmdletBinding()]
    param([Parameter(Mandatory)][Security.SecureString]$SecureString)

    $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureString)
    try {
        return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
    }
    finally {
        [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
}

function ConvertTo-PasswordBytes {
    [CmdletBinding()]
    param([Parameter(Mandatory)][Security.SecureString]$SecureString)

    $bstr = [IntPtr]::Zero
    [byte[]]$passwordBytes = New-Object byte[] $SecureString.Length
    try {
        $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($SecureString)
        for ($index = 0; $index -lt $SecureString.Length; $index++) {
            $character = [int]([Runtime.InteropServices.Marshal]::ReadInt16($bstr, $index * 2)) -band 0xffff
            if ($character -gt 0x7f) {
                throw [ArgumentException]::new('PFX passwords must contain only ASCII characters because the Intune Certificate Connector decodes the decrypted password as ASCII.')
            }
            $passwordBytes[$index] = [byte]$character
        }
        Write-Output -NoEnumerate $passwordBytes
    }
    catch {
        [Array]::Clear($passwordBytes, 0, $passwordBytes.Length)
        throw
    }
    finally {
        if ($bstr -ne [IntPtr]::Zero) {
            [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
        }
    }
}

function Get-IntuneExceptionResponse {
    [CmdletBinding()]
    param([Parameter(Mandatory)]$Exception)

    $responseProperty = $Exception.PSObject.Properties['Response']
    if ($null -eq $responseProperty) {
        return $null
    }
    return $responseProperty.Value
}

function ConvertTo-IntuneIntendedPurposeName {
    [CmdletBinding()]
    param([Parameter(Mandatory)][object]$Value)

    $text = [string]$Value
    switch ($text.ToLowerInvariant()) {
        '0' { return 'unassigned' }
        'unassigned' { return 'unassigned' }
        '1' { return 'smimeEncryption' }
        'smimeencryption' { return 'smimeEncryption' }
        '2' { return 'smimeSigning' }
        'smimesigning' { return 'smimeSigning' }
        '4' { return 'vpn' }
        'vpn' { return 'vpn' }
        '8' { return 'wifi' }
        'wifi' { return 'wifi' }
        default {
            throw [ArgumentException]::new("IntendedPurpose '$text' is invalid. Use unassigned, smimeEncryption, smimeSigning, vpn, wifi, or the Version 2 numeric values 0, 1, 2, 4, or 8.")
        }
    }
}

function ConvertTo-IntunePaddingSchemeName {
    [CmdletBinding()]
    param([Parameter(Mandatory)][object]$Value)

    $text = [string]$Value
    switch ($text.ToLowerInvariant()) {
        '0' { return 'none' }
        'none' { return 'none' }
        '1' { return 'pkcs1' }
        'pkcs1' { return 'pkcs1' }
        '2' { return 'oaepSha1' }
        'oaepsha1' { return 'oaepSha1' }
        '3' { return 'oaepSha256' }
        'oaepsha256' { return 'oaepSha256' }
        '4' { return 'oaepSha384' }
        'oaepsha384' { return 'oaepSha384' }
        '5' { return 'oaepSha512' }
        'oaepsha512' { return 'oaepSha512' }
        default {
            throw [ArgumentException]::new("PaddingScheme '$text' is invalid. Use None, Pkcs1, OaepSha1, OaepSha256, OaepSha384, or OaepSha512.")
        }
    }
}

function Get-IntuneAuthorityUri {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$AuthUri,
        [AllowEmptyString()][string]$TenantId
    )

    $hostName = $AuthUri.Trim().TrimEnd('/')
    if ($hostName -match '^https?://') {
        $hostName = ([uri]$hostName).Authority
    }
    if ([string]::IsNullOrWhiteSpace($hostName)) {
        throw [ArgumentException]::new('AuthUri cannot be empty.')
    }
    $tenant = if ([string]::IsNullOrWhiteSpace($TenantId)) { 'organizations' } else { $TenantId }
    return "https://$hostName/$tenant"
}

function Get-IntuneAccessToken {
    [CmdletBinding()]
    param()

    if ($null -eq $script:AuthContext) {
        throw [InvalidOperationException]::new('No token cached. First call Set-IntuneAuthenticationToken.')
    }

    if ($script:AuthContext.AccessToken -and $script:AuthContext.ExpiresOn -gt [DateTimeOffset]::UtcNow.AddMinutes(2)) {
        return $script:AuthContext.AccessToken
    }

    if ($script:AuthContext.AuthenticationType -ne 'ClientSecret') {
        throw [InvalidOperationException]::new('The cached user token has expired. Call Set-IntuneAuthenticationToken again.')
    }

    $secret = ConvertTo-PlainText -SecureString $script:AuthContext.ClientSecret
    try {
        $response = Invoke-RestMethod -Method Post -Uri "$($script:AuthContext.Authority)/oauth2/v2.0/token" -Body @{
            client_id = $script:AuthContext.ClientId
            client_secret = $secret
            scope = "$($script:AuthContext.GraphUri)/.default"
            grant_type = 'client_credentials'
        } -ContentType 'application/x-www-form-urlencoded' -ErrorAction Stop
    }
    finally {
        $secret = $null
    }

    if ([string]::IsNullOrWhiteSpace($response.access_token)) {
        throw [Security.Authentication.AuthenticationException]::new('The token endpoint did not return an access token.')
    }

    $script:AuthContext.AccessToken = $response.access_token
    $script:AuthContext.ExpiresOn = [DateTimeOffset]::UtcNow.AddSeconds([int]$response.expires_in)
    return $script:AuthContext.AccessToken
}

function Get-IntuneGraphUri {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    if ($null -eq $script:AuthContext) {
        throw [InvalidOperationException]::new('No authentication context exists. Call Set-IntuneAuthenticationToken first.')
    }

    $absoluteUri = $null
    if ([uri]::TryCreate($Path, [UriKind]::Absolute, [ref]$absoluteUri)) {
        $graphUri = [uri]$script:AuthContext.GraphUri
        if ($absoluteUri.Scheme -ne 'https' -or $absoluteUri.Authority -ne $graphUri.Authority) {
            throw [ArgumentException]::new("Graph continuation URI '$Path' does not match the configured Graph endpoint.")
        }
        return $absoluteUri.AbsoluteUri
    }

    return "$($script:AuthContext.GraphUri.TrimEnd('/'))/$($script:AuthContext.SchemaVersion)/$($Path.TrimStart('/'))"
}

function Get-IntuneRetryDelay {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]$Exception,
        [ValidateSet('Get', 'Post', 'Patch', 'Delete')][string]$Method = 'Get'
    )

    $response = Get-IntuneExceptionResponse -Exception $Exception
    if ($null -eq $response) {
        return $null
    }

    $statusCode = [int]$response.StatusCode
    if ($statusCode -ne 429 -and $statusCode -lt 500) {
        return $null
    }
    if ($Method -eq 'Post' -and $statusCode -ne 429) {
        return $null
    }

    $milliseconds = $null
    $seconds = $null
    if ($response.Headers -is [Net.WebHeaderCollection]) {
        $milliseconds = $response.Headers['x-ms-retry-after-ms']
        $seconds = $response.Headers['Retry-After']
    }
    else {
        [Collections.Generic.IEnumerable[string]]$headerValues = $null
        if ($response.Headers.TryGetValues('x-ms-retry-after-ms', [ref]$headerValues)) {
            $milliseconds = @($headerValues)[0]
        }
        $headerValues = $null
        if ($response.Headers.TryGetValues('Retry-After', [ref]$headerValues)) {
            $seconds = @($headerValues)[0]
        }
    }

    if ($milliseconds) {
        return [Math]::Max(1, [int][Math]::Ceiling(([double]$milliseconds) / 1000))
    }

    $parsedSeconds = 0
    if ($seconds -and [int]::TryParse($seconds, [ref]$parsedSeconds)) {
        return [Math]::Max(1, $parsedSeconds)
    }
    return 5
}

function Invoke-IntuneGraphRequest {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][ValidateSet('Get', 'Post', 'Patch', 'Delete')][string]$Method,
        [Parameter(Mandatory)][string]$Path,
        [AllowNull()]$Body
    )

    $uri = Get-IntuneGraphUri -Path $Path
    $accessToken = Get-IntuneAccessToken
    $headers = @{ Authorization = "Bearer $accessToken" }
    $parameters = @{
        Method = $Method
        Uri = $uri
        Headers = $headers
        ErrorAction = 'Stop'
        TimeoutSec = 30
    }
    if ($PSBoundParameters.ContainsKey('Body')) {
        $parameters.Body = $Body | ConvertTo-Json -Depth 8
        $parameters.ContentType = 'application/json'
    }

    for ($attempt = 1; $attempt -le 3; $attempt++) {
        Write-Verbose "Sending Graph request (attempt $attempt of 3): $Method $uri"
        try {
            $response = Invoke-RestMethod @parameters
            Write-Verbose "Graph request succeeded: $Method $uri"
            return $response
        }
        catch {
            $exceptionResponse = Get-IntuneExceptionResponse -Exception $_.Exception
            $failure = if ($null -ne $exceptionResponse) {
                "HTTP $([int]$exceptionResponse.StatusCode)"
            }
            else {
                $_.Exception.GetType().Name
            }
            Write-Verbose "Graph request failed on attempt $attempt of 3 ($failure): $Method $uri"
            $delay = Get-IntuneRetryDelay -Exception $_.Exception -Method $Method
            if ($null -eq $delay -or $attempt -eq 3) {
                throw
            }
            Write-Warning "Graph request was throttled or unavailable. Retrying in $delay seconds."
            Start-Sleep -Seconds $delay
        }
    }
}

function ConvertTo-IntuneUserPfxBody {
    [CmdletBinding()]
    param([Parameter(Mandatory)]$Certificate)

    $body = [ordered]@{}
    foreach ($property in @(
        'id', 'thumbprint', 'intendedPurpose', 'userPrincipalName',
        'startDateTime', 'expirationDateTime', 'providerName', 'keyName',
        'paddingScheme', 'encryptedPfxBlob', 'encryptedPfxPassword',
        'createdDateTime', 'lastModifiedDateTime'
    )) {
        $source = $Certificate.PSObject.Properties[$property]
        if ($null -eq $source) {
            $source = $Certificate.PSObject.Properties[($property.Substring(0, 1).ToUpperInvariant() + $property.Substring(1))]
        }
        if ($null -ne $source -and $null -ne $source.Value) {
            $value = $source.Value
            if ($property -eq 'encryptedPfxBlob' -and $value -is [byte[]]) {
                $value = [Convert]::ToBase64String($value)
            }
            elseif ($property -eq 'intendedPurpose') {
                $value = ConvertTo-IntuneIntendedPurposeName -Value $value
            }
            elseif ($property -eq 'paddingScheme') {
                $value = ConvertTo-IntunePaddingSchemeName -Value $value
            }
            elseif ($property -in @('startDateTime', 'expirationDateTime', 'createdDateTime', 'lastModifiedDateTime')) {
                if ($value -is [DateTimeOffset]) {
                    $value = $value.ToString('o', [Globalization.CultureInfo]::InvariantCulture)
                }
                elseif ($value -is [DateTime]) {
                    $value = $value.ToUniversalTime().ToString('o', [Globalization.CultureInfo]::InvariantCulture)
                }
            }
            $body[$property] = $value
        }
    }
    return [pscustomobject]$body
}

function ConvertFrom-IntuneUserPfxResponse {
    [CmdletBinding()]
    param([Parameter(Mandatory)]$Certificate)

    $properties = [ordered]@{}
    foreach ($property in @(
        'Id', 'Thumbprint', 'IntendedPurpose', 'UserPrincipalName',
        'StartDateTime', 'ExpirationDateTime', 'ProviderName', 'KeyName',
        'PaddingScheme', 'EncryptedPfxBlob', 'EncryptedPfxPassword',
        'CreatedDateTime', 'LastModifiedDateTime'
    )) {
        $sourceName = $property.Substring(0, 1).ToLowerInvariant() + $property.Substring(1)
        $source = $Certificate.PSObject.Properties[$sourceName]
        if ($null -eq $source) {
            $source = $Certificate.PSObject.Properties[$property]
        }
        $value = if ($null -ne $source) { $source.Value } else { $null }
        if ($property -eq 'EncryptedPfxBlob' -and $value -is [string]) {
            $value = [Convert]::FromBase64String($value)
        }
        elseif (
            $property -in @('StartDateTime', 'ExpirationDateTime', 'CreatedDateTime', 'LastModifiedDateTime') -and
            $null -ne $value -and
            $value -isnot [DateTimeOffset]
        ) {
            $value = [DateTimeOffset]::Parse(
                [string]$value,
                [Globalization.CultureInfo]::InvariantCulture,
                [Globalization.DateTimeStyles]::RoundtripKind)
        }
        $properties[$property] = $value
    }

    $result = New-Object -TypeName $legacyCertificateTypeName
    foreach ($property in $properties.Keys) {
        $value = $properties[$property]
        if ($property -eq 'IntendedPurpose' -and $null -ne $value) {
            $value = [Enum]::Parse(
                ('Microsoft.Management.Services.Api.UserPfxIntendedPurpose' -as [type]),
                [string]$value,
                $true)
        }
        elseif ($property -eq 'PaddingScheme' -and $null -ne $value) {
            $value = [Enum]::Parse(
                ('Microsoft.Management.Services.Api.UserPfxPaddingScheme' -as [type]),
                [string]$value,
                $true)
        }
        if ($null -ne $value) {
            $result.$property = $value
        }
    }
    return $result
}

function Resolve-IntuneEncryptionKeyParameters {
    [CmdletBinding()]
    param(
        [AllowEmptyString()][string]$ProviderName,
        [AllowEmptyString()][string]$KeyName,
        [Parameter(Mandatory)][bool]$ProviderNameWasBound,
        [Parameter(Mandatory)][bool]$KeyNameWasBound
    )

    if ($ProviderNameWasBound -and -not [string]::IsNullOrWhiteSpace($ProviderName)) {
        $script:DefaultProviderName = $ProviderName
        Set-Variable -Name EncryptPFXFilesProviderName -Value $ProviderName -Scope Script
    }
    elseif ([string]::IsNullOrWhiteSpace($ProviderName)) {
        $ProviderName = $script:DefaultProviderName
    }

    if ($KeyNameWasBound -and -not [string]::IsNullOrWhiteSpace($KeyName)) {
        $script:DefaultKeyName = $KeyName
        Set-Variable -Name EncryptPFXFilesKeyName -Value $KeyName -Scope Script
    }
    elseif ([string]::IsNullOrWhiteSpace($KeyName)) {
        $KeyName = $script:DefaultKeyName
    }

    return [pscustomobject]@{
        ProviderName = $ProviderName
        KeyName = $KeyName
    }
}

function Escape-IntuneODataLiteral {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Value)

    return $Value.Replace("'", "''")
}

function New-IntuneODataFilterPath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [Parameter(Mandatory)][string]$Filter
    )

    return "${Path}?`$filter=$([uri]::EscapeDataString($Filter))"
}

function Resolve-IntuneFileSystemPath {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Path,
        [switch]$MustExist
    )

    if ($MustExist) {
        $item = Get-Item -LiteralPath $Path -ErrorAction Stop
        if ($item.PSProvider.Name -ne 'FileSystem' -or $item.PSIsContainer) {
            throw [IO.FileNotFoundException]::new("File '$Path' was not found.", $Path)
        }
        return $item.FullName
    }

    $provider = $null
    $drive = $null
    $resolvedPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath(
        $Path,
        [ref]$provider,
        [ref]$drive)
    if ($provider.Name -ne 'FileSystem') {
        throw [ArgumentException]::new("Path '$Path' must use the FileSystem provider.")
    }
    return $resolvedPath
}

function Get-IntuneRsaPadding {
    [CmdletBinding()]
    param([Parameter(Mandatory)][ValidateSet('OaepSha256', 'OaepSha384', 'OaepSha512')][string]$PaddingScheme)

    switch ($PaddingScheme) {
        'OaepSha256' { return [Security.Cryptography.RSAEncryptionPadding]::OaepSHA256 }
        'OaepSha384' { return [Security.Cryptography.RSAEncryptionPadding]::OaepSHA384 }
        default { return [Security.Cryptography.RSAEncryptionPadding]::OaepSHA512 }
    }
}

function Get-DerElement {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][byte[]]$Data,
        [Parameter(Mandatory)][ref]$Offset
    )

    if ($Offset.Value -ge $Data.Length) {
        throw [IO.InvalidDataException]::new('Unexpected end of DER data.')
    }
    $tag = $Data[$Offset.Value++]
    $lengthByte = $Data[$Offset.Value++]
    $length = 0
    if (($lengthByte -band 0x80) -eq 0) {
        $length = $lengthByte
    }
    else {
        $lengthLength = $lengthByte -band 0x7f
        if ($lengthLength -eq 0 -or $lengthLength -gt 4 -or ($Offset.Value + $lengthLength) -gt $Data.Length) {
            throw [IO.InvalidDataException]::new('Invalid DER length.')
        }
        for ($index = 0; $index -lt $lengthLength; $index++) {
            $length = ($length * 256) + $Data[$Offset.Value++]
        }
    }
    if (($Offset.Value + $length) -gt $Data.Length) {
        throw [IO.InvalidDataException]::new('DER length exceeds input.')
    }
    $value = New-Object byte[] $length
    [Array]::Copy($Data, $Offset.Value, $value, 0, $length)
    $Offset.Value += $length
    return [pscustomobject]@{ Tag = $tag; Value = $value }
}

function ConvertTo-DerLength {
    [CmdletBinding()]
    param([Parameter(Mandatory)][int]$Length)

    if ($Length -lt 128) { return [byte[]]@($Length) }
    $bytes = New-Object Collections.Generic.List[byte]
    $remaining = $Length
    while ($remaining -gt 0) {
        $bytes.Insert(0, [byte]($remaining -band 0xff))
        $remaining = $remaining -shr 8
    }
    $bytes.Insert(0, [byte](0x80 -bor $bytes.Count))
    return $bytes.ToArray()
}

function ConvertTo-DerElement {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][byte]$Tag,
        [Parameter(Mandatory)][byte[]]$Value
    )

    $result = New-Object Collections.Generic.List[byte]
    $result.Add($Tag)
    $result.AddRange([byte[]](ConvertTo-DerLength -Length $Value.Length))
    $result.AddRange($Value)
    return $result.ToArray()
}

function ConvertTo-DerUnsignedInteger {
    [CmdletBinding()]
    param([Parameter(Mandatory)][byte[]]$Value)

    $first = 0
    while ($first -lt ($Value.Length - 1) -and $Value[$first] -eq 0) { $first++ }
    $unsigned = $Value[$first..($Value.Length - 1)]
    if (($unsigned[0] -band 0x80) -ne 0) { $unsigned = [byte[]]@(0) + $unsigned }
    return ConvertTo-DerElement -Tag 0x02 -Value $unsigned
}

function ConvertTo-RsaPublicKeyPem {
    [CmdletBinding()]
    param([Parameter(Mandatory)][Security.Cryptography.RSA]$Rsa)

    $parameters = $Rsa.ExportParameters($false)
    $rsaKey = [byte[]](ConvertTo-DerUnsignedInteger -Value $parameters.Modulus) + [byte[]](ConvertTo-DerUnsignedInteger -Value $parameters.Exponent)
    $rsaKey = ConvertTo-DerElement -Tag 0x30 -Value $rsaKey
    $algorithmIdentifier = [byte[]]@(0x06, 0x09, 0x2a, 0x86, 0x48, 0x86, 0xf7, 0x0d, 0x01, 0x01, 0x01, 0x05, 0x00)
    $algorithmIdentifier = ConvertTo-DerElement -Tag 0x30 -Value $algorithmIdentifier
    $bitString = [byte[]]@(0) + $rsaKey
    $subjectPublicKeyInfo = [byte[]]$algorithmIdentifier + [byte[]](ConvertTo-DerElement -Tag 0x03 -Value $bitString)
    $subjectPublicKeyInfo = ConvertTo-DerElement -Tag 0x30 -Value $subjectPublicKeyInfo
    $base64 = [Convert]::ToBase64String($subjectPublicKeyInfo)
    $writer = New-Object IO.StringWriter
    $writer.WriteLine('-----BEGIN PUBLIC KEY-----')
    for ($index = 0; $index -lt $base64.Length; $index += 64) {
        $writer.WriteLine($base64.Substring($index, [Math]::Min(64, $base64.Length - $index)))
    }
    $writer.WriteLine('-----END PUBLIC KEY-----')
    return $writer.ToString()
}

function Get-RsaFromPemFile {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    $resolvedPath = Resolve-IntuneFileSystemPath -Path $Path -MustExist
    $pem = [IO.File]::ReadAllText($resolvedPath)
    $base64 = ($pem -replace '-----BEGIN PUBLIC KEY-----', '' -replace '-----END PUBLIC KEY-----', '' -replace '\s', '')
    $der = [Convert]::FromBase64String($base64)
    $offset = 0
    $outer = Get-DerElement -Data $der -Offset ([ref]$offset)
    if ($outer.Tag -ne 0x30) { throw [IO.InvalidDataException]::new('The PEM file does not contain a public-key sequence.') }
    $offset = 0
    $algorithm = Get-DerElement -Data $outer.Value -Offset ([ref]$offset)
    $bitString = Get-DerElement -Data $outer.Value -Offset ([ref]$offset)
    if ($algorithm.Tag -ne 0x30 -or $bitString.Tag -ne 0x03 -or $bitString.Value.Length -lt 2) {
        throw [IO.InvalidDataException]::new('The PEM file is not an RSA SubjectPublicKeyInfo value.')
    }
    $keyBytes = New-Object byte[] ($bitString.Value.Length - 1)
    [Array]::Copy($bitString.Value, 1, $keyBytes, 0, $keyBytes.Length)
    $offset = 0
    $keySequence = Get-DerElement -Data $keyBytes -Offset ([ref]$offset)
    $offset = 0
    $modulus = (Get-DerElement -Data $keySequence.Value -Offset ([ref]$offset)).Value
    $exponent = (Get-DerElement -Data $keySequence.Value -Offset ([ref]$offset)).Value
    if ($modulus[0] -eq 0) { $modulus = $modulus[1..($modulus.Length - 1)] }
    if ($exponent[0] -eq 0) { $exponent = $exponent[1..($exponent.Length - 1)] }
    $parameters = New-Object Security.Cryptography.RSAParameters
    $parameters.Modulus = [byte[]]$modulus
    $parameters.Exponent = [byte[]]$exponent
    $rsa = New-Object Security.Cryptography.RSACng
    $rsa.ImportParameters($parameters)
    return $rsa
}

function Invoke-IntunePasswordEncryption {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][byte[]]$PasswordBytes,
        [AllowEmptyString()][string]$ProviderName,
        [AllowEmptyString()][string]$KeyName,
        [AllowEmptyString()][string]$KeyFilePath,
        [Parameter(Mandatory)][string]$PaddingScheme
    )

    $padding = Get-IntuneRsaPadding -PaddingScheme $PaddingScheme
    if (-not [string]::IsNullOrWhiteSpace($KeyFilePath)) {
        $resolvedKeyFilePath = Resolve-IntuneFileSystemPath -Path $KeyFilePath -MustExist
        $key = $null
        $rsa = $null
        try {
            $keyBytes = [IO.File]::ReadAllBytes($resolvedKeyFilePath)
            $keyText = [Text.Encoding]::ASCII.GetString($keyBytes)
            if ($keyText -match '-----BEGIN PUBLIC KEY-----') {
                $rsa = Get-RsaFromPemFile -Path $resolvedKeyFilePath
            }
            else {
                $key = [Security.Cryptography.CngKey]::Import(
                    $keyBytes,
                    [Security.Cryptography.CngKeyBlobFormat]::new('RSAPUBLICBLOB'))
                $rsa = New-Object Security.Cryptography.RSACng($key)
            }
            return $rsa.Encrypt($PasswordBytes, $padding)
        }
        finally {
            if ($null -ne $rsa) { $rsa.Dispose() }
            if ($null -ne $key) { $key.Dispose() }
        }
    }

    if ([string]::IsNullOrWhiteSpace($ProviderName) -or [string]::IsNullOrWhiteSpace($KeyName)) {
        throw [ArgumentException]::new('KeyFilePath or both ProviderName and KeyName are required.')
    }
    $provider = New-Object Security.Cryptography.CngProvider($ProviderName)
    $key = $null
    $rsa = $null
    try {
        if (-not [Security.Cryptography.CngKey]::Exists($KeyName, $provider, [Security.Cryptography.CngKeyOpenOptions]::MachineKey)) {
            throw [IO.FileNotFoundException]::new("Machine CNG key '$KeyName' was not found in provider '$ProviderName'. Create it with Add-IntuneKspKey or use -KeyFilePath.", $KeyName)
        }
        $key = [Security.Cryptography.CngKey]::Open($KeyName, $provider, [Security.Cryptography.CngKeyOpenOptions]::MachineKey)
        $rsa = New-Object Security.Cryptography.RSACng($key)
        return $rsa.Encrypt($PasswordBytes, $padding)
    }
    catch [IO.FileNotFoundException] {
        throw
    }
    catch {
        throw [Security.Cryptography.CryptographicException]::new(
            "Could not open or use machine CNG key '$KeyName' in provider '$ProviderName'. Verify the key exists and the current account has access.",
            $_.Exception)
    }
    finally {
        if ($null -ne $rsa) { $rsa.Dispose() }
        if ($null -ne $key) { $key.Dispose() }
    }
}

function Add-IntuneConnectorKeyAccess {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][Security.Cryptography.CngKeyCreationParameters]$Parameters,
        [Parameter(Mandatory)][string]$ProviderName,
        [string]$ConnectorServiceAccount
    )

    if ($ProviderName -ne 'Microsoft Software Key Storage Provider') {
        return
    }

    $accessRules = @(
        '(A;;FA;;;BA)',
        '(A;;GR;;;SO)',
        '(A;;GR;;;SY)'
    )
    if (-not [string]::IsNullOrWhiteSpace($ConnectorServiceAccount)) {
        try {
            $accountSid = ([Security.Principal.NTAccount]::new(
                $ConnectorServiceAccount)).Translate(
                    [Security.Principal.SecurityIdentifier])
        }
        catch [Security.Principal.IdentityNotMappedException] {
            throw [ArgumentException]::new(
                "Connector service account '$ConnectorServiceAccount' could not be resolved to a Windows security identifier.",
                $_.Exception)
        }
        $accessRules += "(A;;GR;;;$($accountSid.Value))"
    }

    $security = [Security.AccessControl.RawSecurityDescriptor]::new(
        "D:$($accessRules -join '')")
    [byte[]]$securityDescriptor = New-Object byte[] $security.BinaryLength
    $security.GetBinaryForm($securityDescriptor, 0)
    $daclSecurityInformation = [Security.Cryptography.CngPropertyOptions]4
    $Parameters.Parameters.Add((New-Object Security.Cryptography.CngProperty(
        'Security Descr',
        $securityDescriptor,
        ([Security.Cryptography.CngPropertyOptions]::Persist -bor $daclSecurityInformation))))
}

function Add-IntuneKspKey {
    <#
    .SYNOPSIS
    Creates an RSA key in a local CNG key storage provider.
    .DESCRIPTION
    Creates a machine CNG key used to encrypt imported PFX passwords.
    .PARAMETER ProviderName
    CNG provider name.
    .PARAMETER KeyName
    Name for the CNG key.
    .PARAMETER KeyLength
    RSA key length in bits. The default is 2048.
    .PARAMETER MakeExportable
    Allows the private key to be exported for connector migration.
    .PARAMETER ConnectorServiceAccount
    Windows account used by the Certificate Connector service. The account is
    granted read access to the private key.
    .EXAMPLE
    Add-IntuneKspKey -ProviderName 'Microsoft Software Key Storage Provider' -KeyName 'PfxImportKey' -ConnectorServiceAccount 'CONTOSO\IntuneConnector'
    #>
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
    param(
        [Parameter(Mandatory, Position = 1)][ValidateNotNullOrEmpty()][string]$ProviderName,
        [Parameter(Mandatory, Position = 2)][ValidateNotNullOrEmpty()][string]$KeyName,
        [Parameter(Position = 3)][ValidateRange(2048, 16384)][int]$KeyLength = 2048,
        [switch]$MakeExportable,
        [string]$ConnectorServiceAccount
    )

    $provider = New-Object Security.Cryptography.CngProvider($ProviderName)
    if ([Security.Cryptography.CngKey]::Exists($KeyName, $provider, [Security.Cryptography.CngKeyOpenOptions]::MachineKey)) {
        $exception = [InvalidOperationException]::new("CNG key '$KeyName' already exists in '$ProviderName'.")
        $PSCmdlet.WriteError([Management.Automation.ErrorRecord]::new(
            $exception,
            'IntunePfxImportKeyAlreadyExists',
            [Management.Automation.ErrorCategory]::ResourceExists,
            $KeyName))
        return
    }
    if ($PSCmdlet.ShouldProcess("$ProviderName\$KeyName", 'Create machine RSA key')) {
        $parameters = New-Object Security.Cryptography.CngKeyCreationParameters
        $parameters.Provider = $provider
        $parameters.KeyCreationOptions = [Security.Cryptography.CngKeyCreationOptions]::MachineKey
        $parameters.ExportPolicy = if ($MakeExportable) {
            [Security.Cryptography.CngExportPolicies]::AllowExport -bor [Security.Cryptography.CngExportPolicies]::AllowPlaintextExport
        } else {
            [Security.Cryptography.CngExportPolicies]::None
        }
        $parameters.Parameters.Add((New-Object Security.Cryptography.CngProperty('Length', [BitConverter]::GetBytes($KeyLength), [Security.Cryptography.CngPropertyOptions]::None)))
        Add-IntuneConnectorKeyAccess `
            -Parameters $parameters `
            -ProviderName $ProviderName `
            -ConnectorServiceAccount $ConnectorServiceAccount
        $key = [Security.Cryptography.CngKey]::Create([Security.Cryptography.CngAlgorithm]::Rsa, $KeyName, $parameters)
        $key.Dispose()
    }
}

function ConvertTo-IntuneBase64EncodedPfxCertificate {
    <#
    .SYNOPSIS
    Converts a PFX file to a Base64 string.
    .DESCRIPTION
    Reads a PFX file without importing it into a certificate store.
    .PARAMETER CertificatePath
    Path to the PFX file.
    .EXAMPLE
    ConvertTo-IntuneBase64EncodedPfxCertificate -CertificatePath .\user.pfx
    #>
    [CmdletBinding()]
    param([Parameter(Mandatory, Position = 1)][string]$CertificatePath)
    $resolvedCertificatePath = Resolve-IntuneFileSystemPath -Path $CertificatePath -MustExist
    [Convert]::ToBase64String([IO.File]::ReadAllBytes($resolvedCertificatePath))
}

function Export-IntunePublicKey {
    <#
    .SYNOPSIS
    Exports a local CNG public key.
    .DESCRIPTION
    Exports a machine CNG RSA key as an RSAPUBLICBLOB or PEM public key.
    .PARAMETER ProviderName
    Name of the CNG provider that stores the machine key.
    .PARAMETER KeyName
    Name of the machine CNG key to export.
    .PARAMETER FilePath
    New destination file for the exported public key.
    .PARAMETER FileFormat
    Selects CngBlob (0) or Pem (1) output.
    .EXAMPLE
    Export-IntunePublicKey -ProviderName 'Microsoft Software Key Storage Provider' -KeyName PfxImportKey -FilePath .\key.pem -FileFormat Pem
    #>
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
    param(
        [Parameter(Mandatory, Position = 1)][ValidateNotNullOrEmpty()][string]$ProviderName,
        [Parameter(Mandatory, Position = 2)][ValidateNotNullOrEmpty()][string]$KeyName,
        [Parameter(Mandatory, Position = 3)][ValidateNotNullOrEmpty()][string]$FilePath,
        [Parameter(Position = 4)][ValidateSet('CngBlob', 'Pem', '0', '1')][string]$FileFormat = 'CngBlob'
    )

    if ($FileFormat -eq '0') { $FileFormat = 'CngBlob' }
    elseif ($FileFormat -eq '1') { $FileFormat = 'Pem' }
    $resolvedFilePath = Resolve-IntuneFileSystemPath -Path $FilePath
    if (Test-Path -LiteralPath $resolvedFilePath) { throw [IO.IOException]::new("File '$FilePath' already exists.") }
    if ($PSCmdlet.ShouldProcess($resolvedFilePath, "Export $FileFormat public key")) {
        $key = [Security.Cryptography.CngKey]::Open($KeyName, (New-Object Security.Cryptography.CngProvider($ProviderName)), [Security.Cryptography.CngKeyOpenOptions]::MachineKey)
        $rsa = New-Object Security.Cryptography.RSACng($key)
        try {
            if ($FileFormat -eq 'CngBlob') {
                [IO.File]::WriteAllBytes($resolvedFilePath, $key.Export([Security.Cryptography.CngKeyBlobFormat]::new('RSAPUBLICBLOB')))
            }
            else {
                [IO.File]::WriteAllText($resolvedFilePath, (ConvertTo-RsaPublicKeyPem -Rsa $rsa))
            }
        }
        finally {
            $rsa.Dispose()
            $key.Dispose()
        }
    }
}

function Export-IntunePrivateKey {
    <#
    .SYNOPSIS
    Exports an RSA private CNG key.
    .DESCRIPTION
    Exports an exportable machine CNG key as an RSAFULLPRIVATEBLOB.
    .PARAMETER ProviderName
    Name of the CNG provider that stores the machine key.
    .PARAMETER KeyName
    Name of the machine CNG key to export.
    .PARAMETER FilePath
    Destination file that must not already exist.
    .EXAMPLE
    Export-IntunePrivateKey -ProviderName 'Microsoft Software Key Storage Provider' -KeyName PfxImportKey -FilePath .\key.bin
    #>
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
    param(
        [Parameter(Mandatory, Position = 1)][ValidateNotNullOrEmpty()][string]$ProviderName,
        [Parameter(Mandatory, Position = 2)][ValidateNotNullOrEmpty()][string]$KeyName,
        [Parameter(Mandatory, Position = 3)][ValidateNotNullOrEmpty()][string]$FilePath
    )

    $resolvedFilePath = Resolve-IntuneFileSystemPath -Path $FilePath
    if (Test-Path -LiteralPath $resolvedFilePath) { throw [IO.IOException]::new("File '$FilePath' already exists.") }
    if ($PSCmdlet.ShouldProcess($resolvedFilePath, 'Export private key')) {
        $key = [Security.Cryptography.CngKey]::Open($KeyName, (New-Object Security.Cryptography.CngProvider($ProviderName)), [Security.Cryptography.CngKeyOpenOptions]::MachineKey)
        try { [IO.File]::WriteAllBytes($resolvedFilePath, $key.Export([Security.Cryptography.CngKeyBlobFormat]::new('RSAFULLPRIVATEBLOB'))) }
        finally { $key.Dispose() }
    }
}

function Import-IntunePrivateKey {
    <#
    .SYNOPSIS
    Imports an RSA private key into a CNG provider.
    .DESCRIPTION
    Imports an RSAFULLPRIVATEBLOB as a machine CNG key.
    .PARAMETER ProviderName
    Name of the destination CNG provider.
    .PARAMETER KeyName
    Name for the imported machine CNG key.
    .PARAMETER FilePath
    Path to an RSAFULLPRIVATEBLOB exported by Export-IntunePrivateKey.
    .PARAMETER MakeExportable
    Allows the imported key to be exported later.
    .EXAMPLE
    Import-IntunePrivateKey -ProviderName 'Microsoft Software Key Storage Provider' -KeyName PfxImportKey -FilePath .\key.bin
    #>
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
    param(
        [Parameter(Mandatory, Position = 1)][ValidateNotNullOrEmpty()][string]$ProviderName,
        [Parameter(Mandatory, Position = 2)][ValidateNotNullOrEmpty()][string]$KeyName,
        [Parameter(Mandatory, Position = 3)][string]$FilePath,
        [switch]$MakeExportable
    )

    $resolvedFilePath = Resolve-IntuneFileSystemPath -Path $FilePath -MustExist
    $provider = New-Object Security.Cryptography.CngProvider($ProviderName)
    if ([Security.Cryptography.CngKey]::Exists($KeyName, $provider, [Security.Cryptography.CngKeyOpenOptions]::MachineKey)) {
        $exception = [InvalidOperationException]::new("CNG key '$KeyName' already exists in '$ProviderName'.")
        $PSCmdlet.WriteError([Management.Automation.ErrorRecord]::new(
            $exception,
            'IntunePfxImportKeyAlreadyExists',
            [Management.Automation.ErrorCategory]::ResourceExists,
            $KeyName))
        return
    }
    if ($PSCmdlet.ShouldProcess("$ProviderName\$KeyName", 'Import machine RSA private key')) {
        $parameters = New-Object Security.Cryptography.CngKeyCreationParameters
        $parameters.Provider = $provider
        $parameters.KeyCreationOptions = [Security.Cryptography.CngKeyCreationOptions]::MachineKey
        $parameters.ExportPolicy = if ($MakeExportable) {
            [Security.Cryptography.CngExportPolicies]::AllowExport -bor [Security.Cryptography.CngExportPolicies]::AllowPlaintextExport
        } else { [Security.Cryptography.CngExportPolicies]::None }
        $parameters.Parameters.Add((New-Object Security.Cryptography.CngProperty('RSAFULLPRIVATEBLOB', [IO.File]::ReadAllBytes($resolvedFilePath), [Security.Cryptography.CngPropertyOptions]::None)))
        Add-IntuneConnectorKeyAccess -Parameters $parameters -ProviderName $ProviderName
        $key = [Security.Cryptography.CngKey]::Create([Security.Cryptography.CngAlgorithm]::Rsa, $KeyName, $parameters)
        $key.Dispose()
    }
}

function Set-IntuneAuthenticationToken {
    <#
    .SYNOPSIS
    Authenticates the current PowerShell session to Microsoft Graph.
    .DESCRIPTION
    Stores a session-only auth context. Prefer command-line parameters or a Setup object.
    For Version 2 compatibility, omitted values can be read from the module manifest PrivateData block.
    Manifest-stored client secrets remain supported only for migration and are not recommended.
    .PARAMETER ClientId
    Application (client) ID of the Entra application registration.
    .PARAMETER Setup
    Configuration object returned by Initialize-IntunePfxImportApplication. Uses its client secret when present; otherwise uses device-code authentication.
    .PARAMETER ClientSecret
    Secure client secret for application authentication.
    .PARAMETER TenantId
    Tenant GUID. Required for client-secret authentication and optional for delegated authentication.
    .PARAMETER AdminUserName
    User principal name used as a device-code login hint or with AdminPassword for legacy ROPC.
    .PARAMETER AdminPassword
    Secure password for the legacy ROPC authentication flow.
    .PARAMETER AuthUri
    Entra authority host. Defaults to the commercial cloud host.
    .PARAMETER GraphUri
    Microsoft Graph resource URI. Defaults to the commercial cloud endpoint.
    .PARAMETER SchemaVersion
    Microsoft Graph API version used for Intune requests. Defaults to beta.
    .PARAMETER RedirectUri
    Registered native-client redirect URI used for delegated authentication.
    .EXAMPLE
    Set-IntuneAuthenticationToken -ClientId $clientId -TenantId $tenantId -ClientSecret $secret
    .EXAMPLE
    Set-IntuneAuthenticationToken -Setup $setup
    .EXAMPLE
    Set-IntuneAuthenticationToken -AdminUserName admin@contoso.com
    Uses the Version 2 manifest ClientId when present and starts delegated device-code authentication.
    .EXAMPLE
    Set-IntuneAuthenticationToken
    Uses Version 2 manifest ClientId, TenantId, and ClientSecret values when configured.
    #>
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium', DefaultParameterSetName = 'DeviceCode')]
    param(
        [Parameter(ParameterSetName = 'ClientSecret')]
        [Parameter(ParameterSetName = 'Password')]
        [Parameter(ParameterSetName = 'DeviceCode')]
        [ValidatePattern('^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$')][string]$ClientId,
        [Parameter(Mandatory, Position = 0, ParameterSetName = 'Setup')]
        [ValidateNotNull()][psobject]$Setup,
        [Parameter(Mandatory, ParameterSetName = 'ClientSecret')]
        [Parameter(ParameterSetName = 'Password')]
        [Parameter(ParameterSetName = 'DeviceCode')]
        [ValidatePattern('^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$')][string]$TenantId,
        [Parameter(ParameterSetName = 'ClientSecret', Mandatory)][Security.SecureString]$ClientSecret,
        [Parameter(ParameterSetName = 'Password', Mandatory)]
        [Parameter(ParameterSetName = 'DeviceCode')]
        [ValidateNotNullOrEmpty()][string]$AdminUserName,
        [Parameter(ParameterSetName = 'Password', Mandatory)][Security.SecureString]$AdminPassword,
        [ValidateNotNullOrEmpty()][string]$AuthUri = $script:CommercialAuthUri,
        [ValidatePattern('^https://')][string]$GraphUri = $script:CommercialGraphUri,
        [ValidateNotNullOrEmpty()][string]$SchemaVersion = 'beta',
        [ValidatePattern('^https://')][string]$RedirectUri = $script:DefaultRedirectUri
    )

    $authenticationType = $PSCmdlet.ParameterSetName
    $legacyConfiguration = Get-IntuneLegacyModuleConfiguration
    if ($authenticationType -eq 'Setup') {
        $setupParametersProperty = $Setup.PSObject.Properties['SetIntuneAuthenticationTokenParameters']
        if ($null -eq $setupParametersProperty -or $setupParametersProperty.Value -isnot [Collections.IDictionary]) {
            throw [ArgumentException]::new('Setup must be an object returned by Initialize-IntunePfxImportApplication.')
        }
        $setupParameters = $setupParametersProperty.Value
        foreach ($requiredParameter in @('ClientId', 'TenantId', 'AuthUri', 'GraphUri', 'SchemaVersion', 'RedirectUri')) {
            if (-not $setupParameters.Contains($requiredParameter) -or [string]::IsNullOrWhiteSpace([string]$setupParameters[$requiredParameter])) {
                throw [ArgumentException]::new("Setup is missing authentication parameter '$requiredParameter'.")
            }
        }

        $ClientId = [string]$setupParameters['ClientId']
        $TenantId = [string]$setupParameters['TenantId']
        $AuthUri = [string]$setupParameters['AuthUri']
        $GraphUri = [string]$setupParameters['GraphUri']
        $SchemaVersion = [string]$setupParameters['SchemaVersion']
        $RedirectUri = [string]$setupParameters['RedirectUri']
        $secretProperty = $Setup.PSObject.Properties['ClientSecret']
        $ClientSecret = if ($null -ne $secretProperty) { $secretProperty.Value } else { $null }
        if ($null -ne $ClientSecret -and $ClientSecret -isnot [Security.SecureString]) {
            throw [ArgumentException]::new('Setup.ClientSecret must be a SecureString.')
        }
        $modeProperty = $Setup.PSObject.Properties['AuthenticationMode']
        $authenticationMode = if ($null -ne $modeProperty) { [string]$modeProperty.Value } else { 'Both' }
        if ($authenticationMode -eq 'ClientSecret' -and $null -eq $ClientSecret) {
            throw [ArgumentException]::new('Setup requires a client secret. Run Initialize-IntunePfxImportApplication with -CreateClientSecret, or provide explicit authentication parameters.')
        }
        $authenticationType = if ($null -ne $ClientSecret) { 'ClientSecret' } else { 'DeviceCode' }
    }
    else {
        if ([string]::IsNullOrWhiteSpace($ClientId)) {
            $ClientId = [string](Get-IntuneLegacyConfigurationValue -Configuration $legacyConfiguration -Name 'ClientId')
        }
        if (-not $PSBoundParameters.ContainsKey('TenantId')) {
            $legacyValue = [string](Get-IntuneLegacyConfigurationValue -Configuration $legacyConfiguration -Name 'TenantId')
            if (-not [string]::IsNullOrWhiteSpace($legacyValue)) { $TenantId = $legacyValue }
        }
        if (-not $PSBoundParameters.ContainsKey('AuthUri')) {
            $legacyValue = [string](Get-IntuneLegacyConfigurationValue -Configuration $legacyConfiguration -Name 'AuthURI')
            if (-not [string]::IsNullOrWhiteSpace($legacyValue)) { $AuthUri = $legacyValue }
        }
        if (-not $PSBoundParameters.ContainsKey('GraphUri')) {
            $legacyValue = [string](Get-IntuneLegacyConfigurationValue -Configuration $legacyConfiguration -Name 'GraphURI')
            if (-not [string]::IsNullOrWhiteSpace($legacyValue)) { $GraphUri = $legacyValue }
        }
        if (-not $PSBoundParameters.ContainsKey('SchemaVersion')) {
            $legacyValue = [string](Get-IntuneLegacyConfigurationValue -Configuration $legacyConfiguration -Name 'SchemaVersion')
            if (-not [string]::IsNullOrWhiteSpace($legacyValue)) { $SchemaVersion = $legacyValue }
        }
        if (-not $PSBoundParameters.ContainsKey('RedirectUri')) {
            $legacyValue = [string](Get-IntuneLegacyConfigurationValue -Configuration $legacyConfiguration -Name 'RedirectURI')
            if (-not [string]::IsNullOrWhiteSpace($legacyValue)) { $RedirectUri = $legacyValue }
        }

        if ($authenticationType -eq 'DeviceCode' -and [string]::IsNullOrWhiteSpace($AdminUserName)) {
            $legacySecret = Get-IntuneLegacyConfigurationValue -Configuration $legacyConfiguration -Name 'ClientSecret'
            if ($legacySecret -is [Security.SecureString]) {
                $ClientSecret = $legacySecret
                $authenticationType = 'ClientSecret'
            }
            elseif (-not [string]::IsNullOrWhiteSpace([string]$legacySecret)) {
                $ClientSecret = ConvertTo-SecureString ([string]$legacySecret) -AsPlainText -Force
                $authenticationType = 'ClientSecret'
            }
        }
    }

    if ([string]::IsNullOrWhiteSpace($ClientId)) {
        throw [ArgumentException]::new('ClientId is required. Pass -ClientId, use -Setup, or configure ClientId in the Version 2 compatibility PrivateData block.')
    }
    if ($authenticationType -eq 'ClientSecret' -and [string]::IsNullOrWhiteSpace($TenantId)) {
        throw [ArgumentException]::new('TenantId is required for client-secret authentication.')
    }

    $authority = Get-IntuneAuthorityUri -AuthUri $AuthUri -TenantId $TenantId
    if (
        $null -ne $script:AuthContext -and
        $script:AuthContext.AccessToken -and
        $script:AuthContext.ExpiresOn -gt [DateTimeOffset]::UtcNow.AddMinutes(2) -and
        $script:AuthContext.AuthenticationType -eq $authenticationType -and
        $script:AuthContext.ClientId -eq $ClientId -and
        $script:AuthContext.TenantId -eq $TenantId -and
        $script:AuthContext.AdminUserName -eq $AdminUserName -and
        $script:AuthContext.Authority -eq $authority -and
        $script:AuthContext.GraphUri -eq $GraphUri.TrimEnd('/') -and
        $script:AuthContext.SchemaVersion -eq $SchemaVersion
    ) {
        Write-Verbose 'Reusing the cached Intune authentication token for this PowerShell session.'
        return
    }

    if (-not $PSCmdlet.ShouldProcess($authority, 'Acquire and cache access token for this session')) { return }
    $scope = "$($GraphUri.TrimEnd('/'))/.default"
    $tokenUri = "$authority/oauth2/v2.0/token"
    $context = [pscustomobject]@{
        ClientId = $ClientId; TenantId = $TenantId; AuthUri = $AuthUri; Authority = $authority
        GraphUri = $GraphUri.TrimEnd('/'); SchemaVersion = $SchemaVersion; RedirectUri = $RedirectUri
        AuthenticationType = $authenticationType; AdminUserName = $AdminUserName
        ClientSecret = $null; AccessToken = $null; ExpiresOn = [DateTimeOffset]::MinValue
    }

    if ($authenticationType -eq 'ClientSecret') {
        $secret = ConvertTo-PlainText -SecureString $ClientSecret
        try {
            $response = Invoke-RestMethod -Method Post -Uri $tokenUri -Body @{
                client_id = $ClientId; client_secret = $secret; scope = $scope; grant_type = 'client_credentials'
            } -ContentType 'application/x-www-form-urlencoded' -ErrorAction Stop
        }
        finally { $secret = $null }
        $context.ClientSecret = $ClientSecret
    }
    elseif ($authenticationType -eq 'Password') {
        $password = ConvertTo-PlainText -SecureString $AdminPassword
        try {
            $response = Invoke-RestMethod -Method Post -Uri $tokenUri -Body @{
                client_id = $ClientId; username = $AdminUserName; password = $password; scope = $scope; grant_type = 'password'
            } -ContentType 'application/x-www-form-urlencoded' -ErrorAction Stop
        }
        finally { $password = $null }
    }
    else {
        $deviceCode = Invoke-RestMethod -Method Post -Uri "$authority/oauth2/v2.0/devicecode" -Body @{
            client_id = $ClientId; scope = $scope
        } -ContentType 'application/x-www-form-urlencoded' -ErrorAction Stop
        if ([string]::IsNullOrWhiteSpace($deviceCode.device_code)) {
            throw [Security.Authentication.AuthenticationException]::new('The device-code endpoint did not return a device code.')
        }
        if ($AdminUserName) {
            Write-Verbose "Authenticate the device-code flow as '$AdminUserName'."
        }
        Write-Host $deviceCode.message
        $deadline = [DateTimeOffset]::UtcNow.AddSeconds([int]$deviceCode.expires_in)
        $pollingInterval = [Math]::Max(1, [int]$deviceCode.interval)
        $response = $null
        do {
            Start-Sleep -Seconds $pollingInterval
            try {
                $response = Invoke-RestMethod -Method Post -Uri $tokenUri -Body @{
                    grant_type = 'urn:ietf:params:oauth:grant-type:device_code'; client_id = $ClientId; device_code = $deviceCode.device_code
                } -ContentType 'application/x-www-form-urlencoded' -ErrorAction Stop
            }
            catch {
                if ($null -eq $_.ErrorDetails -or $_.ErrorDetails.Message -notmatch 'authorization_pending|slow_down') { throw }
                if ($_.ErrorDetails.Message -match 'slow_down') {
                    $pollingInterval += 5
                }
            }
        } while ($null -eq $response -and [DateTimeOffset]::UtcNow -lt $deadline)
        if ($null -eq $response) { throw [TimeoutException]::new('Device-code authentication timed out.') }
    }
    if ([string]::IsNullOrWhiteSpace($response.access_token)) { throw [Security.Authentication.AuthenticationException]::new('The token endpoint did not return an access token.') }
    $context.AccessToken = $response.access_token
    $context.ExpiresOn = [DateTimeOffset]::UtcNow.AddSeconds([int]$response.expires_in)
    $script:AuthContext = $context
}

function Remove-IntuneAuthenticationToken {
    <#
    .SYNOPSIS
    Removes the module authentication context from the current session.
    .DESCRIPTION
    Clears the cached access token and secure client-secret reference held by this module.
    Call this before Set-IntuneAuthenticationToken to force a new sign-in for the
    same application and tenant.
    .EXAMPLE
    Remove-IntuneAuthenticationToken
    #>
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium')]
    param()
    if ($PSCmdlet.ShouldProcess('current PowerShell session', 'Remove Intune authentication context')) {
        $script:AuthContext = $null
    }
}

function New-IntuneUserPfxCertificate {
    <#
    .SYNOPSIS
    Creates a userPFXCertificate object for Graph import.
    .DESCRIPTION
    Loads the PFX with EphemeralKeySet, detects rsa, ecc, or unknown, and encrypts its password with a CNG key or exported public key.
    ProviderName and KeyName are remembered for later calls in the same module session, matching Version 2 behavior.
    KeyAlgorithm is local diagnostic metadata and is not sent to Microsoft Graph.
    .PARAMETER PathToPfxFile
    Path to a PFX file. Use this parameter set or Base64EncodedPfx.
    .PARAMETER Base64EncodedPfx
    Base64-encoded PFX data. Use this parameter set or PathToPfxFile.
    .PARAMETER PfxPassword
    Secure password that protects the PFX private key.
    .PARAMETER UPN
    Existing Microsoft Entra user principal name in the authenticated tenant that receives the certificate.
    When omitted, the module uses the certificate UPN or email name.
    .PARAMETER ProviderName
    CNG provider that contains the password-encryption key when KeyFilePath is not used.
    .PARAMETER KeyName
    Name of the CNG password-encryption key when KeyFilePath is not used.
    .PARAMETER IntendedPurpose
    Intune certificate purpose tag. Version 2 numeric values 0, 1, 2, 4, and 8 remain accepted.
    .PARAMETER PaddingScheme
    RSA OAEP padding scheme used to encrypt the PFX password. Version 2 names and numeric values remain accepted; None (0) is an alias for OaepSha512.
    .PARAMETER KeyFilePath
    Public key file exported by Export-IntunePublicKey, in CNG blob or PEM format.
    .EXAMPLE
    New-IntuneUserPfxCertificate -PathToPfxFile .\user.pfx -PfxPassword $password -UPN user@contoso.com -ProviderName 'Microsoft Software Key Storage Provider' -KeyName PfxImportKey
    #>
    [CmdletBinding(DefaultParameterSetName = 'SinglePFXFile')]
    param(
        [Parameter(Mandatory, Position = 1, ParameterSetName = 'SinglePFXFile')][string]$PathToPfxFile,
        [Parameter(Mandatory, Position = 1, ParameterSetName = 'Base64EncodedPfx')][ValidateNotNullOrEmpty()][string]$Base64EncodedPfx,
        [Parameter(Mandatory, Position = 2)][Security.SecureString]$PfxPassword,
        [Parameter(Position = 3)][string]$UPN,
        [Parameter(Position = 4)][string]$ProviderName,
        [Parameter(Position = 5)][string]$KeyName,
        [Parameter(Position = 6)][object]$IntendedPurpose = 'unassigned',
        [Parameter(Position = 7)][object]$PaddingScheme = 'OaepSha512',
        [Parameter(Position = 8)][string]$KeyFilePath
    )

    $IntendedPurpose = ConvertTo-IntuneIntendedPurposeName -Value $IntendedPurpose

    $paddingText = ConvertTo-IntunePaddingSchemeName -Value $PaddingScheme
    if ($paddingText -ieq 'None') {
        $PaddingScheme = 'OaepSha512'
    }
    elseif ($paddingText -inotin @('OaepSha256', 'OaepSha384', 'OaepSha512')) {
        throw [ArgumentException]::new("PaddingScheme '$paddingText' is invalid. Use OaepSha256, OaepSha384, OaepSha512, or the Version 2 compatibility value None.")
    }
    else {
        $PaddingScheme = $paddingText
    }

    $keyParameters = Resolve-IntuneEncryptionKeyParameters `
        -ProviderName $ProviderName `
        -KeyName $KeyName `
        -ProviderNameWasBound ($PSBoundParameters.ContainsKey('ProviderName')) `
        -KeyNameWasBound ($PSBoundParameters.ContainsKey('KeyName'))
    $ProviderName = $keyParameters.ProviderName
    $KeyName = $keyParameters.KeyName

    [byte[]]$pfxData = $null
    if ($PSCmdlet.ParameterSetName -eq 'SinglePFXFile') {
        $resolvedPfxPath = Resolve-IntuneFileSystemPath -Path $PathToPfxFile -MustExist
        $pfxData = [IO.File]::ReadAllBytes($resolvedPfxPath)
    }
    else {
        $pfxData = [Convert]::FromBase64String($Base64EncodedPfx)
    }
    try {
        $certificate = [Security.Cryptography.X509Certificates.X509Certificate2]::new(
            $pfxData,
            $PfxPassword,
            [Security.Cryptography.X509Certificates.X509KeyStorageFlags]::EphemeralKeySet)
    }
    catch [Security.Cryptography.CryptographicException] {
        throw [ArgumentException]::new('Could not load the PFX. Verify that the PFX data and password are valid.', $_.Exception)
    }

    [byte[]]$passwordBytes = $null
    [byte[]]$encryptedPassword = $null
    try {
        $thumbprint = $certificate.Thumbprint.ToLowerInvariant()
        $oid = $certificate.PublicKey.Oid.Value
        $startDateTime = [DateTimeOffset]$certificate.NotBefore.ToUniversalTime()
        $expirationDateTime = [DateTimeOffset]$certificate.NotAfter.ToUniversalTime()
        if ([string]::IsNullOrWhiteSpace($UPN)) {
            $UPN = $certificate.GetNameInfo([Security.Cryptography.X509Certificates.X509NameType]::UpnName, $false)
            if ([string]::IsNullOrWhiteSpace($UPN)) {
                $UPN = $certificate.GetNameInfo([Security.Cryptography.X509Certificates.X509NameType]::EmailName, $false)
            }
            if ([string]::IsNullOrWhiteSpace($UPN)) {
                throw [ArgumentException]::new('UPN was not supplied and the PFX certificate does not contain a UPN or email name.')
            }
        }
        $passwordBytes = ConvertTo-PasswordBytes -SecureString $PfxPassword
        $encryptedPassword = Invoke-IntunePasswordEncryption -PasswordBytes $passwordBytes -ProviderName $ProviderName -KeyName $KeyName -KeyFilePath $KeyFilePath -PaddingScheme $PaddingScheme
        if ($null -eq $encryptedPassword) {
            throw [Security.Cryptography.CryptographicException]::new('The PFX password encryption operation did not return encrypted data.')
        }
    }
    finally {
        if ($null -ne $passwordBytes) { [Array]::Clear($passwordBytes, 0, $passwordBytes.Length) }
        $certificate.Dispose()
    }
    $keyAlgorithm = if ($oid -eq '1.2.840.113549.1.1.1') { 'rsa' } elseif ($oid -eq '1.2.840.10045.2.1') { 'ecc' } else { 'unknown' }
    $result = New-Object -TypeName $legacyCertificateTypeName
    $result.Thumbprint = $thumbprint
    $result.IntendedPurpose = [Enum]::Parse(
        ('Microsoft.Management.Services.Api.UserPfxIntendedPurpose' -as [type]),
        $IntendedPurpose,
        $true)
    $result.UserPrincipalName = $UPN
    $result.StartDateTime = $startDateTime
    $result.ExpirationDateTime = $expirationDateTime
    $result.ProviderName = $ProviderName
    $result.KeyName = $KeyName
    $result.PaddingScheme = [Enum]::Parse(
        ('Microsoft.Management.Services.Api.UserPfxPaddingScheme' -as [type]),
        $PaddingScheme,
        $true)
    $result.EncryptedPfxPassword = [Convert]::ToBase64String($encryptedPassword)
    $result.EncryptedPfxBlob = $pfxData
    $result.CreatedDateTime = [DateTimeOffset]::UtcNow
    $result.LastModifiedDateTime = [DateTimeOffset]::UtcNow
    $result | Add-Member -NotePropertyName KeyAlgorithm -NotePropertyValue $keyAlgorithm
    return $result
}

function Get-IntuneUserId {
    <#
    .SYNOPSIS
    Gets a Microsoft Graph user ID by UPN.
    .DESCRIPTION
    Uses the authentication context created by Set-IntuneAuthenticationToken.
    .PARAMETER UPN
    User principal name to look up.
    .EXAMPLE
    Get-IntuneUserId -UPN user@contoso.com
    #>
    [CmdletBinding(PositionalBinding = $false)]
    param([Parameter(Mandatory)][ValidateNotNullOrEmpty()][string]$UPN)
    $filter = Escape-IntuneODataLiteral -Value $UPN
    $path = New-IntuneODataFilterPath -Path 'users' -Filter "userPrincipalName eq '$filter'"
    $response = Invoke-IntuneGraphRequest -Method Get -Path $path
    $users = @($response.value)
    $user = if ($users.Count -gt 0) { $users[0] } else { $null }
    if ($null -eq $user -or [string]::IsNullOrWhiteSpace($user.id)) { throw [InvalidOperationException]::new("No user was found for '$UPN'.") }
    return ($user.id -replace '-', '')
}

function Get-IntuneUserPfxCertificate {
    <#
    .SYNOPSIS
    Gets imported Intune PFX certificate records.
    .DESCRIPTION
    Gets all records, records for users, or records matching user/thumbprint pairs.
    .PARAMETER UserThumbprintList
    Objects with User and Thumbprint properties.
    .PARAMETER UserList
    User principal names whose PFX records should be returned.
    .EXAMPLE
    Get-IntuneUserPfxCertificate -UserList user@contoso.com
    #>
    [CmdletBinding(SupportsShouldProcess, PositionalBinding = $false)]
    param(
        [Parameter(ValueFromPipeline)][object[]]$UserThumbprintList,
        [Parameter(ValueFromPipeline)][string[]]$UserList
    )
    process {
        $queries = @()
        if ($UserThumbprintList) {
            foreach ($item in $UserThumbprintList) {
                if ([string]::IsNullOrWhiteSpace($item.User) -or [string]::IsNullOrWhiteSpace($item.Thumbprint)) { throw [ArgumentException]::new('UserThumbprintList items need User and Thumbprint properties.') }
                $user = Escape-IntuneODataLiteral -Value $item.User.ToLowerInvariant()
                $thumbprint = Escape-IntuneODataLiteral -Value $item.Thumbprint.ToLowerInvariant()
                $queries += New-IntuneODataFilterPath -Path 'deviceManagement/userPfxCertificates' -Filter "tolower(userPrincipalName) eq '$user' and tolower(thumbprint) eq '$thumbprint'"
            }
        }
        elseif ($UserList) {
            foreach ($userName in $UserList) {
                $user = Escape-IntuneODataLiteral -Value $userName.ToLowerInvariant()
                $queries += New-IntuneODataFilterPath -Path 'deviceManagement/userPfxCertificates' -Filter "tolower(userPrincipalName) eq '$user'"
            }
        }
        else { $queries = @('deviceManagement/userPfxCertificates') }
        foreach ($query in $queries) {
            do {
                $response = Invoke-IntuneGraphRequest -Method Get -Path $query
                foreach ($certificate in @($response.value)) {
                    Write-Output (ConvertFrom-IntuneUserPfxResponse -Certificate $certificate)
                }
                $nextLink = $response.PSObject.Properties['@odata.nextLink']
                $query = if ($null -ne $nextLink) { $nextLink.Value } else { $null }
            } while (-not [string]::IsNullOrWhiteSpace($query))
        }
    }
}

function Import-IntuneUserPfxCertificate {
    <#
    .SYNOPSIS
    Imports or updates user PFX certificate records in Intune.
    .DESCRIPTION
    Posts a record by default, or patches the matching user ID and thumbprint when IsUpdate is supplied.
    .PARAMETER CertificateList
    userPFXCertificate objects created by New-IntuneUserPfxCertificate.
    .PARAMETER IsUpdate
    Patches the existing user and thumbprint record instead of creating a new record.
    .EXAMPLE
    $certificate | Import-IntuneUserPfxCertificate
    #>
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium', PositionalBinding = $false)]
    param(
        [Parameter(Mandatory, ValueFromPipeline)][ValidateNotNullOrEmpty()][object[]]$CertificateList,
        [switch]$IsUpdate
    )
    process {
        foreach ($certificate in $CertificateList) {
            try {
                $body = ConvertTo-IntuneUserPfxBody -Certificate $certificate
                if ([string]::IsNullOrWhiteSpace($body.thumbprint) -or [string]::IsNullOrWhiteSpace($body.userPrincipalName)) { throw [ArgumentException]::new('Certificate needs thumbprint and userPrincipalName.') }
                $path = 'deviceManagement/userPfxCertificates'
                $method = 'Post'
                if ($IsUpdate) {
                    $userId = Get-IntuneUserId -UPN $body.userPrincipalName
                    $path = "deviceManagement/userPfxCertificates($userId-$($body.thumbprint))"
                    $method = 'Patch'
                }
                if ($PSCmdlet.ShouldProcess("$($body.userPrincipalName)/$($body.thumbprint)", "$method Intune PFX certificate")) {
                    Invoke-IntuneGraphRequest -Method $method -Path $path -Body $body | Out-Null
                }
            }
            catch {
                $PSCmdlet.WriteError($_)
            }
        }
    }
}

function Remove-IntuneUserPfxCertificate {
    <#
    .SYNOPSIS
    Removes user PFX certificate records from Intune.
    .DESCRIPTION
    Removes specified certificate objects, user/thumbprint pairs, or all records for supplied users.
    .PARAMETER CertificateList
    userPFXCertificate objects to remove.
    .PARAMETER UserThumbprintList
    Objects with User and Thumbprint properties identifying records to remove.
    .PARAMETER UserList
    User principal names whose records should be removed. Every imported PFX certificate returned for each supplied user is deleted.
    .EXAMPLE
    Remove-IntuneUserPfxCertificate -UserList user@contoso.com
    #>
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'Medium', DefaultParameterSetName = 'FromUserPFXCertificates', PositionalBinding = $false)]
    param(
        [Parameter(Mandatory, ValueFromPipeline, ParameterSetName = 'FromUserPFXCertificates')][object[]]$CertificateList,
        [Parameter(Mandatory, ValueFromPipeline, ParameterSetName = 'FromThumbprints')][object[]]$UserThumbprintList,
        [Parameter(Mandatory, ValueFromPipeline, ParameterSetName = 'FromUsers')][string[]]$UserList
    )
    process {
        $targets = @()
        if ($PSCmdlet.ParameterSetName -eq 'FromUsers') {
            foreach ($user in $UserList) {
                $targets += Get-IntuneUserPfxCertificate -UserList $user | ForEach-Object { [pscustomobject]@{ User = $_.userPrincipalName; Thumbprint = $_.thumbprint } }
            }
        }
        elseif ($PSCmdlet.ParameterSetName -eq 'FromUserPFXCertificates') {
            $targets = $CertificateList | ForEach-Object { [pscustomobject]@{ User = $_.userPrincipalName; Thumbprint = $_.thumbprint } }
        }
        else { $targets = $UserThumbprintList }

        foreach ($target in $targets) {
            try {
                if ([string]::IsNullOrWhiteSpace($target.User) -or [string]::IsNullOrWhiteSpace($target.Thumbprint)) { throw [ArgumentException]::new('Each removal target needs User and Thumbprint properties.') }
                $userId = if ($target.User -match '^[0-9a-fA-F]{32}$') {
                    $target.User.ToLowerInvariant()
                }
                elseif ($target.User -match '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$') {
                    $target.User.Replace('-', '').ToLowerInvariant()
                }
                else {
                    Get-IntuneUserId -UPN $target.User
                }
                $name = "$userId-$($target.Thumbprint)"
                if ($PSCmdlet.ShouldProcess($name, 'Remove Intune PFX certificate')) {
                    Invoke-IntuneGraphRequest -Method Delete -Path "deviceManagement/userPfxCertificates/$name" | Out-Null
                }
            }
            catch {
                $PSCmdlet.WriteError($_)
            }
        }
    }
}

function Get-IntunePfxGraphResourceAccess {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]$GraphServicePrincipal,
        [Parameter(Mandatory)][ValidateSet('ClientSecret', 'PublicClient', 'Both')][string]$AuthenticationMode
    )

    $required = New-Object Collections.Generic.List[object]
    $requiredValues = New-Object Collections.Generic.List[object]
    if ($AuthenticationMode -in @('ClientSecret', 'Both')) {
        $requiredValues.Add([pscustomobject]@{ Value = 'DeviceManagementConfiguration.ReadWrite.All'; Type = 'Role' })
        $requiredValues.Add([pscustomobject]@{ Value = 'User.Read.All'; Type = 'Role' })
    }
    if ($AuthenticationMode -in @('PublicClient', 'Both')) {
        $requiredValues.Add([pscustomobject]@{ Value = 'DeviceManagementConfiguration.ReadWrite.All'; Type = 'Scope' })
        $requiredValues.Add([pscustomobject]@{ Value = 'User.Read.All'; Type = 'Scope' })
        $requiredValues.Add([pscustomobject]@{ Value = 'User.Read'; Type = 'Scope' })
    }

    foreach ($permission in $requiredValues) {
        if ($permission.Type -eq 'Role') {
            $source = @($GraphServicePrincipal.AppRoles | Where-Object { $_.Value -eq $permission.Value -and $_.IsEnabled })
        }
        else {
            $source = @($GraphServicePrincipal.Oauth2PermissionScopes | Where-Object { $_.Value -eq $permission.Value -and $_.IsEnabled })
        }
        if ($source.Count -ne 1) {
            throw [InvalidOperationException]::new("Microsoft Graph does not expose exactly one enabled $($permission.Type) '$($permission.Value)' in this tenant.")
        }
        $required.Add([pscustomobject]@{ Id = $source[0].Id; Type = $permission.Type; Value = $permission.Value })
    }
    return $required.ToArray()
}

function Merge-IntunePfxRequiredResourceAccess {
    [CmdletBinding()]
    param(
        [AllowNull()]$ExistingRequiredResourceAccess,
        [Parameter(Mandatory)][string]$GraphApplicationId,
        [Parameter(Mandatory)][object[]]$RequiredAccess
    )

    $merged = New-Object Collections.Generic.List[object]
    foreach ($entry in $ExistingRequiredResourceAccess) {
        $resourceAccess = New-Object Collections.Generic.List[object]
        foreach ($access in $entry.ResourceAccess) {
            $resourceAccess.Add(@{ Id = $access.Id; Type = $access.Type })
        }
        $merged.Add(@{ ResourceAppId = $entry.ResourceAppId; ResourceAccess = $resourceAccess.ToArray() })
    }

    $graphEntry = $merged | Where-Object { $_.ResourceAppId -eq $GraphApplicationId } | Select-Object -First 1
    if ($null -eq $graphEntry) {
        $graphEntry = @{ ResourceAppId = $GraphApplicationId; ResourceAccess = @() }
        $merged.Add($graphEntry)
    }
    $graphAccess = New-Object Collections.Generic.List[object]
    foreach ($access in @($graphEntry.ResourceAccess)) {
        $graphAccess.Add(@{ Id = $access.Id; Type = $access.Type })
    }
    foreach ($access in $RequiredAccess) {
        if (@($graphAccess | Where-Object { $_.Id -eq $access.Id -and $_.Type -eq $access.Type }).Count -eq 0) {
            $graphAccess.Add(@{ Id = $access.Id; Type = $access.Type })
        }
    }
    $graphEntry.ResourceAccess = $graphAccess.ToArray()
    return $merged.ToArray()
}

function Test-IntunePfxApplicationConfiguration {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]$Application,
        [Parameter(Mandatory)][object[]]$RequiredAccess,
        [Parameter(Mandatory)][ValidateSet('ClientSecret', 'PublicClient', 'Both')][string]$AuthenticationMode,
        [Parameter(Mandatory)][string]$RedirectUri
    )

    $currentAccess = @($Application.RequiredResourceAccess | Where-Object { $null -ne $_ -and $_.ResourceAppId -eq '00000003-0000-0000-c000-000000000000' } | ForEach-Object { $_.ResourceAccess })
    $missingPermissions = New-Object Collections.Generic.List[object]
    foreach ($requiredPermission in $RequiredAccess) {
        if (@($currentAccess | Where-Object { $_.Id -eq $requiredPermission.Id -and $_.Type -eq $requiredPermission.Type }).Count -eq 0) {
            $missingPermissions.Add($requiredPermission)
        }
    }
    $publicClientNeeded = $AuthenticationMode -in @('PublicClient', 'Both')
    $redirects = if ($null -ne $Application.PublicClient) { @($Application.PublicClient.RedirectUris) } else { @() }
    return [pscustomobject]@{
        MissingPermissions = $missingPermissions.ToArray()
        PublicClientNeedsUpdate = $publicClientNeeded -and ((-not $Application.IsFallbackPublicClient) -or ($redirects -notcontains $RedirectUri))
    }
}

function Get-IntuneMgGraphContext {
    [CmdletBinding()]
    param()
    Get-MgContext
}

function Connect-IntuneMgGraph {
    [CmdletBinding()]
    param([Parameter(Mandatory)][hashtable]$Parameters)
    Connect-MgGraph @Parameters
}

function Get-IntuneMgServicePrincipal {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$Filter,
        [Parameter(Mandatory)][string[]]$Property
    )
    Get-MgServicePrincipal -Filter $Filter -Property $Property -All
}

function Get-IntuneMgApplication {
    [CmdletBinding(DefaultParameterSetName = 'Filter')]
    param(
        [Parameter(Mandatory, ParameterSetName = 'Filter')][string]$Filter,
        [Parameter(Mandatory, ParameterSetName = 'Id')][string]$ApplicationId,
        [Parameter(Mandatory)][string[]]$Property
    )
    if ($PSCmdlet.ParameterSetName -eq 'Filter') {
        Get-MgApplication -Filter $Filter -Property $Property -All
    }
    else {
        Get-MgApplication -ApplicationId $ApplicationId -Property $Property
    }
}

function New-IntuneMgApplication {
    [CmdletBinding()]
    param([Parameter(Mandatory)][hashtable]$Parameters)
    New-MgApplication @Parameters
}

function Update-IntuneMgApplication {
    [CmdletBinding()]
    param([Parameter(Mandatory)][hashtable]$Parameters)
    Update-MgApplication @Parameters
}

function New-IntuneMgServicePrincipal {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$AppId)
    New-MgServicePrincipal -AppId $AppId
}

function Add-IntuneMgApplicationPassword {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$ApplicationId,
        [Parameter(Mandatory)][hashtable]$PasswordCredential
    )
    Add-MgApplicationPassword -ApplicationId $ApplicationId -PasswordCredential $PasswordCredential
}

function Initialize-IntunePfxImportApplication {
    <#
    .SYNOPSIS
    Creates, validates, or updates a Microsoft Entra application for Intune PFX import.
    .DESCRIPTION
    Uses Microsoft Graph PowerShell SDK application cmdlets to configure required Microsoft Graph application and delegated permissions.
    It creates the corresponding service principal, but never claims to grant tenant admin consent. The returned AdminConsentUri must be opened by an authorized tenant administrator.
    .PARAMETER DisplayName
    Display name for a new or discovered application registration.
    .PARAMETER ExistingApplicationId
    Application (client) ID of an existing registration to validate or update.
    .PARAMETER AuthenticationMode
    Configures ClientSecret, PublicClient, or both permission models.
    .PARAMETER ValidateOnly
    Reports missing configuration without creating or changing tenant objects.
    .PARAMETER ConnectGraph
    Connects through Microsoft Graph PowerShell when no existing Graph SDK context is available.
    .PARAMETER TenantId
    Tenant GUID to validate against the active Graph SDK connection.
    .PARAMETER AuthUri
    Entra authority host returned for Set-IntuneAuthenticationToken.
    .PARAMETER GraphUri
    Microsoft Graph resource URI returned for Set-IntuneAuthenticationToken.
    .PARAMETER SchemaVersion
    Microsoft Graph API version returned for Set-IntuneAuthenticationToken.
    .PARAMETER RedirectUri
    Native-client redirect URI configured for public-client authentication.
    .PARAMETER CreateClientSecret
    Creates an application client secret and returns its one-time value as a SecureString.
    .EXAMPLE
    $setup = Initialize-IntunePfxImportApplication -DisplayName 'Intune PFX Import' -AuthenticationMode Both -ConnectGraph
    Set-IntuneAuthenticationToken -Setup $setup
    #>
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High', DefaultParameterSetName = 'ByName')]
    param(
        [Parameter(ParameterSetName = 'ByName')][ValidateNotNullOrEmpty()][string]$DisplayName = 'Intune PFX Import',
        [Parameter(Mandatory, ParameterSetName = 'ByApplicationId')][ValidatePattern('^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$')][string]$ExistingApplicationId,
        [ValidatePattern('^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$')][string]$TenantId,
        [ValidateSet('ClientSecret', 'PublicClient', 'Both')][string]$AuthenticationMode = 'Both',
        [ValidateNotNullOrEmpty()][string]$AuthUri = $script:CommercialAuthUri,
        [ValidatePattern('^https://')][string]$GraphUri = $script:CommercialGraphUri,
        [ValidateNotNullOrEmpty()][string]$SchemaVersion = 'beta',
        [ValidatePattern('^https://')][string]$RedirectUri = $script:DefaultRedirectUri,
        [switch]$CreateClientSecret,
        [switch]$ValidateOnly,
        [switch]$ConnectGraph
    )

    if ($CreateClientSecret -and $AuthenticationMode -eq 'PublicClient') {
        throw [ArgumentException]::new('CreateClientSecret requires AuthenticationMode ClientSecret or Both.')
    }

    $requiredCommands = @('Get-MgContext', 'Get-MgServicePrincipal', 'Get-MgApplication', 'New-MgApplication', 'Update-MgApplication', 'New-MgServicePrincipal')
    foreach ($command in $requiredCommands) {
        if ($null -eq (Get-Command -Name $command -ErrorAction SilentlyContinue)) {
            throw [InvalidOperationException]::new("Microsoft Graph PowerShell SDK command '$command' is unavailable. Install Microsoft.Graph.Applications and Microsoft.Graph.Authentication.")
        }
    }

    $requestedGraphEnvironment = if ($GraphUri -eq $script:CommercialGraphUri) {
        'Global'
    }
    elseif ($GraphUri -eq 'https://graph.microsoft.us') {
        'USGov'
    }
    else {
        throw [ArgumentException]::new("Application onboarding does not support the Microsoft Graph endpoint '$GraphUri'. Connect and configure the application manually for other sovereign clouds.")
    }

    $graphContext = Get-IntuneMgGraphContext
    if ($null -eq $graphContext) {
        if (-not $ConnectGraph) {
            throw [InvalidOperationException]::new('No Microsoft Graph PowerShell SDK connection exists. Run Connect-MgGraph with Application.ReadWrite.All and Application.Read.All, or call this function with -ConnectGraph.')
        }
        $connectTarget = if ($TenantId) { $TenantId } else { 'home tenant' }
        if (-not $PSCmdlet.ShouldProcess($connectTarget, 'Connect Microsoft Graph PowerShell SDK')) { return }
        $connectParameters = @{ Scopes = @('Application.ReadWrite.All', 'Application.Read.All') }
        if ($TenantId) { $connectParameters.TenantId = $TenantId }
        if ($requestedGraphEnvironment -ne 'Global') { $connectParameters.Environment = $requestedGraphEnvironment }
        Connect-IntuneMgGraph -Parameters $connectParameters | Out-Null
        $graphContext = Get-IntuneMgGraphContext
    }
    if ($null -eq $graphContext -or [string]::IsNullOrWhiteSpace($graphContext.TenantId)) {
        throw [InvalidOperationException]::new('Microsoft Graph PowerShell did not return a tenant context after authentication.')
    }
    if ($TenantId -and $TenantId -ne $graphContext.TenantId) {
        throw [InvalidOperationException]::new("Connected Graph tenant '$($graphContext.TenantId)' does not match TenantId '$TenantId'.")
    }
    $connectedGraphEnvironment = if (
        $null -ne $graphContext.PSObject.Properties['Environment'] -and
        -not [string]::IsNullOrWhiteSpace($graphContext.Environment)
    ) { $graphContext.Environment } else { 'Global' }
    if ($connectedGraphEnvironment -ne $requestedGraphEnvironment) {
        throw [InvalidOperationException]::new("Connected Graph environment '$connectedGraphEnvironment' does not match the environment '$requestedGraphEnvironment' selected by GraphUri '$GraphUri'.")
    }
    $tenant = $graphContext.TenantId

    $graphServicePrincipal = @(Get-IntuneMgServicePrincipal -Filter "appId eq '00000003-0000-0000-c000-000000000000'" -Property 'id,appId,appRoles,oauth2PermissionScopes')
    if ($graphServicePrincipal.Count -ne 1) {
        throw [InvalidOperationException]::new('Could not resolve the Microsoft Graph service principal in the connected tenant.')
    }
    $requiredAccess = Get-IntunePfxGraphResourceAccess -GraphServicePrincipal $graphServicePrincipal[0] -AuthenticationMode $AuthenticationMode

    if ($PSCmdlet.ParameterSetName -eq 'ByApplicationId') {
        $application = Get-IntuneMgApplication -Filter "appId eq '$ExistingApplicationId'" -Property 'id,appId,displayName,requiredResourceAccess,publicClient,isFallbackPublicClient'
    }
    else {
        $application = Get-IntuneMgApplication -Filter "displayName eq '$($DisplayName.Replace("'", "''"))'" -Property 'id,appId,displayName,requiredResourceAccess,publicClient,isFallbackPublicClient'
    }
    $application = @($application)
    if ($application.Count -gt 1) {
        throw [InvalidOperationException]::new('More than one application matched. Re-run with -ExistingApplicationId.')
    }
    if ($PSCmdlet.ParameterSetName -eq 'ByApplicationId' -and $application.Count -eq 0) {
        throw [InvalidOperationException]::new("No application registration was found for ExistingApplicationId '$ExistingApplicationId'.")
    }

    $clientSecret = $null
    $changes = New-Object Collections.Generic.List[string]
    if ($application.Count -eq 0) {
        if ($ValidateOnly) {
            throw [InvalidOperationException]::new('ValidateOnly cannot find the requested application registration.')
        }
        if (-not $PSCmdlet.ShouldProcess($DisplayName, 'Create Intune PFX import application registration')) { return }
        $newGraphResourceAccess = @($requiredAccess | ForEach-Object { @{ Id = $_.Id; Type = $_.Type } })
        $newApplicationParameters = @{
            DisplayName = $DisplayName
            SignInAudience = 'AzureADMyOrg'
            RequiredResourceAccess = @(@{
                ResourceAppId = '00000003-0000-0000-c000-000000000000'
                ResourceAccess = $newGraphResourceAccess
            })
        }
        if ($AuthenticationMode -in @('PublicClient', 'Both')) {
            $newApplicationParameters.IsFallbackPublicClient = $true
            $newApplicationParameters.PublicClient = @{ RedirectUris = @($RedirectUri) }
        }
        $application = @(New-IntuneMgApplication -Parameters $newApplicationParameters)
        $changes.Add('ApplicationCreated')
    }
    else {
        $application = $application[0]
        $configuration = Test-IntunePfxApplicationConfiguration -Application $application -RequiredAccess $requiredAccess -AuthenticationMode $AuthenticationMode -RedirectUri $RedirectUri
        if ($configuration.MissingPermissions.Count -gt 0) { $changes.Add('RequiredResourceAccess') }
        if ($configuration.PublicClientNeedsUpdate) { $changes.Add('PublicClient') }
        if ($changes.Count -gt 0 -and -not $ValidateOnly) {
            if ($PSCmdlet.ShouldProcess($application.DisplayName, "Update Intune PFX import application configuration: $($changes -join ', ')")) {
                $updateParameters = @{
                    ApplicationId = $application.Id
                    RequiredResourceAccess = Merge-IntunePfxRequiredResourceAccess -ExistingRequiredResourceAccess $application.RequiredResourceAccess -GraphApplicationId '00000003-0000-0000-c000-000000000000' -RequiredAccess $requiredAccess
                }
                if ($AuthenticationMode -in @('PublicClient', 'Both')) {
                    $updateParameters.IsFallbackPublicClient = $true
                    $redirects = @($RedirectUri)
                    if ($null -ne $application.PublicClient) { $redirects += $application.PublicClient.RedirectUris }
                    $updateParameters.PublicClient = @{ RedirectUris = @($redirects | Select-Object -Unique) }
                }
                Update-IntuneMgApplication -Parameters $updateParameters | Out-Null
                $application = @(Get-IntuneMgApplication -ApplicationId $application.Id -Property 'id,appId,displayName,requiredResourceAccess,publicClient,isFallbackPublicClient')[0]
            }
        }
    }

    $servicePrincipal = @(Get-IntuneMgServicePrincipal -Filter "appId eq '$($application.AppId)'" -Property 'id,appId,displayName')
    if ($servicePrincipal.Count -gt 1) { throw [InvalidOperationException]::new("More than one service principal exists for application '$($application.AppId)'.") }
    if ($servicePrincipal.Count -eq 0) {
        $changes.Add('ServicePrincipal')
        if (-not $ValidateOnly -and $PSCmdlet.ShouldProcess($application.DisplayName, 'Create application service principal')) {
            $servicePrincipal = @(New-IntuneMgServicePrincipal -AppId $application.AppId)
        }
    }

    if ($CreateClientSecret) {
        if ($ValidateOnly) { $changes.Add('ClientSecret') }
        elseif ($PSCmdlet.ShouldProcess($application.DisplayName, 'Create application client secret')) {
            if ($null -eq (Get-Command -Name Add-MgApplicationPassword -ErrorAction SilentlyContinue)) {
                throw [InvalidOperationException]::new("Microsoft Graph PowerShell SDK command 'Add-MgApplicationPassword' is unavailable. Install Microsoft.Graph.Applications.")
            }
            $passwordCredential = Add-IntuneMgApplicationPassword -ApplicationId $application.Id -PasswordCredential @{ DisplayName = "Intune PFX Import $([DateTime]::UtcNow.ToString('yyyy-MM-dd'))" }
            if ([string]::IsNullOrWhiteSpace($passwordCredential.SecretText)) {
                throw [InvalidOperationException]::new('Microsoft Graph did not return the one-time client secret value.')
            }
            $clientSecret = ConvertTo-SecureString $passwordCredential.SecretText -AsPlainText -Force
            $changes.Add('ClientSecret')
        }
    }

    $authority = Get-IntuneAuthorityUri -AuthUri $AuthUri -TenantId $tenant
    $adminConsentUri = "$authority/adminconsent?client_id=$($application.AppId)"
    [pscustomobject]@{
        ApplicationId = $application.AppId
        ObjectId = $application.Id
        ServicePrincipalId = if ($servicePrincipal.Count -eq 1) { $servicePrincipal[0].Id } else { $null }
        TenantId = $tenant
        AuthenticationMode = $AuthenticationMode
        ChangesRequiredOrApplied = $changes.ToArray()
        AdminConsentRequired = $true
        AdminConsentUri = $adminConsentUri
        ClientSecret = $clientSecret
        SetIntuneAuthenticationTokenParameters = [ordered]@{
            ClientId = $application.AppId
            TenantId = $tenant
            AuthUri = $AuthUri
            GraphUri = $GraphUri
            SchemaVersion = $SchemaVersion
            RedirectUri = $RedirectUri
        }
        AdminConsentInstructions = if ($AuthenticationMode -in @('ClientSecret', 'Both')) {
            'This application requests Microsoft Graph application permissions. A Privileged Role Administrator or Global Administrator must review and grant tenant-wide admin consent. Application Administrator and Cloud Application Administrator can configure applications but cannot grant Microsoft Graph application permissions. This command only configures requested permissions; it does not grant consent.'
        }
        else {
            'This application requests delegated permissions. An authorized tenant administrator must review and grant tenant-wide consent when required by the configured scopes. This command only configures requested permissions; it does not grant consent.'
        }
    }
}

Export-ModuleMember -Function @(
    'Add-IntuneKspKey',
    'ConvertTo-IntuneBase64EncodedPfxCertificate',
    'Export-IntunePrivateKey',
    'Export-IntunePublicKey',
    'Get-IntuneUserId',
    'Get-IntuneUserPfxCertificate',
    'Import-IntunePrivateKey',
    'Import-IntuneUserPfxCertificate',
    'Initialize-IntunePfxImportApplication',
    'New-IntuneUserPfxCertificate',
    'Remove-IntuneAuthenticationToken',
    'Remove-IntuneUserPfxCertificate',
    'Set-IntuneAuthenticationToken'
)
