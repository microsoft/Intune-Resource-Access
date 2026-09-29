$script:moduleRoot = Join-Path $PSScriptRoot '..\PFXImportPS'
$script:manifestPath = Join-Path $script:moduleRoot 'IntunePfxImport.psd1'
$global:IntunePfxTestGraphServicePrincipal = [pscustomobject]@{
    Id = 'graph-sp'
    AppRoles = @(
        [pscustomobject]@{ Id = 'role-device'; Value = 'DeviceManagementConfiguration.ReadWrite.All'; IsEnabled = $true },
        [pscustomobject]@{ Id = 'role-user'; Value = 'User.Read.All'; IsEnabled = $true }
    )
    Oauth2PermissionScopes = @(
        [pscustomobject]@{ Id = 'scope-device'; Value = 'DeviceManagementConfiguration.ReadWrite.All'; IsEnabled = $true },
        [pscustomobject]@{ Id = 'scope-user-all'; Value = 'User.Read.All'; IsEnabled = $true },
        [pscustomobject]@{ Id = 'scope-user'; Value = 'User.Read'; IsEnabled = $true }
    )
}

Describe 'IntunePfxImport 3.0 script module' {
    BeforeAll {
        Remove-Module IntunePfxImport -ErrorAction SilentlyContinue
        Import-Module $manifestPath -Force
    }

    BeforeEach {
        Remove-IntuneAuthenticationToken -Confirm:$false -ErrorAction SilentlyContinue
    }

    AfterAll {
        Remove-Module IntunePfxImport -Force -ErrorAction SilentlyContinue
        Remove-Variable -Name IntunePfxTestGraphServicePrincipal -Scope Global -ErrorAction SilentlyContinue
        Remove-Variable -Name IntunePfxTestExistingApplication -Scope Global -ErrorAction SilentlyContinue
        Remove-Variable -Name IntunePfxTestSecret -Scope Global -ErrorAction SilentlyContinue
    }

    It 'preserves transport exceptions that do not expose an HTTP response' {
        Mock -CommandName Invoke-RestMethod -ModuleName IntunePfxImport {
            param($Uri)
            if ($Uri -match '/oauth2/v2.0/token$') {
                return [pscustomobject]@{ access_token = 'transport-token'; expires_in = 3600 }
            }
            throw [InvalidOperationException]::new('Network connection failed.')
        }
        $secret = ConvertTo-SecureString 'transport-secret' -AsPlainText -Force
        Set-IntuneAuthenticationToken -ClientId '11111111-1111-1111-1111-111111111111' -TenantId '22222222-2222-2222-2222-222222222222' -ClientSecret $secret -Confirm:$false

        $caught = $null
        try {
            Get-IntuneUserPfxCertificate
        }
        catch {
            $caught = $_.Exception
        }

        if ($caught -isnot [InvalidOperationException] -or $caught.Message -ne 'Network connection failed.') {
            throw "The original transport exception was replaced: $caught"
        }
    }

    It 'loads the Version 2 public certificate class and enums' {
        $certificateType = 'Microsoft.Management.Services.Api.UserPFXCertificate' -as [type]
        $purposeType = 'Microsoft.Management.Services.Api.UserPfxIntendedPurpose' -as [type]
        $paddingType = 'Microsoft.Management.Services.Api.UserPfxPaddingScheme' -as [type]

        if ($null -eq $certificateType -or $null -eq $purposeType -or $null -eq $paddingType) {
            throw 'The Version 2 public CLR types are not available after module import.'
        }
        $purposeValues = @{
            Unassigned = 0
            SmimeEncryption = 1
            SmimeSigning = 2
            VPN = 4
            Wifi = 8
        }
        foreach ($name in $purposeValues.Keys) {
            if ([int][Enum]::Parse($purposeType, $name) -ne $purposeValues[$name]) {
                throw "The Version 2 intended-purpose value '$name' changed."
            }
        }
        $paddingValues = @{
            None = 0
            Pkcs1 = 1
            OaepSha1 = 2
            OaepSha256 = 3
            OaepSha384 = 4
            OaepSha512 = 5
        }
        foreach ($name in $paddingValues.Keys) {
            if ([int][Enum]::Parse($paddingType, $name) -ne $paddingValues[$name]) {
                throw "The Version 2 padding value '$name' changed."
            }
        }
        $propertyTypes = @{
            Id = 'System.String'
            Thumbprint = 'System.String'
            IntendedPurpose = $purposeType.FullName
            UserPrincipalName = 'System.String'
            StartDateTime = 'System.DateTimeOffset'
            ExpirationDateTime = 'System.DateTimeOffset'
            ProviderName = 'System.String'
            KeyName = 'System.String'
            PaddingScheme = $paddingType.FullName
            EncryptedPfxBlob = 'System.Byte[]'
            EncryptedPfxPassword = 'System.String'
            CreatedDateTime = 'System.DateTimeOffset'
            LastModifiedDateTime = 'System.DateTimeOffset'
        }
        foreach ($name in $propertyTypes.Keys) {
            $property = $certificateType.GetProperty($name)
            if ($null -eq $property -or $property.PropertyType.FullName -ne $propertyTypes[$name]) {
                throw "The Version 2 property '$name' changed."
            }
        }
        if (-not $certificateType.IsSealed) { throw 'The Version 2 certificate class is no longer sealed.' }
        $certificate = New-Object Microsoft.Management.Services.Api.UserPFXCertificate
        $certificate.IntendedPurpose = [Microsoft.Management.Services.Api.UserPfxIntendedPurpose]::SmimeEncryption
        if ($certificate.IntendedPurpose -ne [Microsoft.Management.Services.Api.UserPfxIntendedPurpose]::SmimeEncryption) {
            throw 'The Version 2 public certificate class is not usable.'
        }
    }

    It 'rejects public-client secret creation before any Graph operation' {
        Mock -CommandName Get-Command -ModuleName IntunePfxImport {
            throw 'Graph command discovery must not run for invalid onboarding parameters.'
        }

        $errorMessage = $null
        try {
            Initialize-IntunePfxImportApplication -AuthenticationMode PublicClient -CreateClientSecret -Confirm:$false
        }
        catch {
            $errorMessage = $_.Exception.Message
        }

        if ($errorMessage -notlike '*CreateClientSecret requires AuthenticationMode ClientSecret or Both*') {
            throw "Expected early parameter validation but got: $errorMessage"
        }
        Assert-MockCalled -CommandName Get-Command -ModuleName IntunePfxImport -Times 0 -Exactly
    }

    It 'exports the documented public function contract explicitly' {
        $manifest = Import-PowerShellDataFile -Path $manifestPath

        if ($manifest.ModuleVersion -ne '3.0.0') { throw 'ModuleVersion must be 3.0.0.' }
        if ($manifest.RootModule -ne 'IntunePfxImport.psm1') { throw 'The manifest must load the script module.' }
        if ($manifest.FunctionsToExport -contains '*') { throw 'The manifest must not use wildcard function exports.' }
        if (@($manifest.CmdletsToExport).Count -ne 0) { throw 'The manifest must not export compiled cmdlets.' }
        if (@(Compare-Object -ReferenceObject @($manifest.FunctionsToExport) -DifferenceObject @((Get-Command -Module IntunePfxImport).Name)).Count -ne 0) { throw 'Manifest and module exports differ.' }

        $version2Commands = @(
            'Add-IntuneKspKey',
            'ConvertTo-IntuneBase64EncodedPfxCertificate',
            'Export-IntunePrivateKey',
            'Export-IntunePublicKey',
            'Get-IntuneUserId',
            'Get-IntuneUserPfxCertificate',
            'Import-IntunePrivateKey',
            'Import-IntuneUserPfxCertificate',
            'New-IntuneUserPfxCertificate',
            'Remove-IntuneAuthenticationToken',
            'Remove-IntuneUserPfxCertificate',
            'Set-IntuneAuthenticationToken'
        )
        if (@(Compare-Object -ReferenceObject $version2Commands -DifferenceObject @($manifest.FunctionsToExport | Where-Object { $_ -ne 'Initialize-IntunePfxImportApplication' })).Count -ne 0) {
            throw 'The shipped Version 2 command surface is not preserved.'
        }
    }

    It 'restores legacy NuGet packages to the directory used by project HintPaths' {
        $pipelinePath = Join-Path $PSScriptRoot '..\..\..\azure-pipelines.yml'
        $pipeline = Get-Content -LiteralPath $pipelinePath -Raw

        if ($pipeline -notmatch "restoreDirectory:\s*'src/PFXImportPowershell/packages'") {
            throw 'NuGet restoreDirectory does not match the EncryptionUtilities project HintPaths.'
        }
    }

    It 'does not retry ambiguous server errors for non-idempotent POST requests' {
        $global:IntunePfxTestPostCount = 0
        Mock -CommandName Start-Sleep -ModuleName IntunePfxImport {
            throw 'A non-idempotent POST must not be retried after an ambiguous server error.'
        }
        Mock -CommandName Invoke-RestMethod -ModuleName IntunePfxImport {
            param($Uri)
            if ($Uri -match '/oauth2/v2.0/token$') {
                return [pscustomobject]@{ access_token = 'post-retry-token'; expires_in = 3600 }
            }
            $global:IntunePfxTestPostCount++
            $exception = [InvalidOperationException]::new('Ambiguous server failure.')
            $exception | Add-Member -NotePropertyName Response -NotePropertyValue ([pscustomobject]@{
                StatusCode = 500
                Headers = New-Object Net.WebHeaderCollection
            })
            throw $exception
        }
        $secret = ConvertTo-SecureString 'post-retry-secret' -AsPlainText -Force
        $certificate = [pscustomobject]@{
            thumbprint = 'ambiguous'
            userPrincipalName = 'user@contoso.com'
            encryptedPfxBlob = [byte[]](1)
            encryptedPfxPassword = 'AQ=='
        }

        Set-IntuneAuthenticationToken -ClientId '11111111-1111-1111-1111-111111111111' -TenantId '22222222-2222-2222-2222-222222222222' -ClientSecret $secret -Confirm:$false
        Import-IntuneUserPfxCertificate -CertificateList $certificate -Confirm:$false -ErrorAction SilentlyContinue

        if ($global:IntunePfxTestPostCount -ne 1) {
            throw "Expected one POST attempt after an ambiguous server failure but got '$global:IntunePfxTestPostCount'."
        }
    }

    It 'documents all meaningful exported function parameters' {
        $commonParameters = @(
            'Verbose', 'Debug', 'ErrorAction', 'WarningAction', 'InformationAction',
            'ErrorVariable', 'WarningVariable', 'InformationVariable', 'OutVariable',
            'OutBuffer', 'PipelineVariable', 'ProgressAction', 'WhatIf', 'Confirm'
        )
        foreach ($command in Get-Command -Module IntunePfxImport) {
            foreach ($parameterName in @($command.Parameters.Keys | Where-Object { $_ -notin $commonParameters })) {
                $parameterHelp = Get-Help $command.Name -Parameter $parameterName
                if ([string]::IsNullOrWhiteSpace(($parameterHelp.Description.Text -join ' '))) {
                    throw "Missing comment-based help for $($command.Name) parameter '$parameterName'."
                }
            }
        }
    }

    It 'converts a PFX file from TestDrive without importing it into a certificate store' {
        $pfxPath = Join-Path $TestDrive 'certificate.pfx'
        [IO.File]::WriteAllBytes($pfxPath, [byte[]](1, 2, 3, 4))

        if ((ConvertTo-IntuneBase64EncodedPfxCertificate -CertificatePath $pfxPath) -ne 'AQIDBA==') { throw 'PFX bytes were not Base64 encoded correctly.' }
    }

    It 'resolves relative file paths from the callers current location' {
        $pfxPath = Join-Path $TestDrive 'relative-certificate.pfx'
        [IO.File]::WriteAllBytes($pfxPath, [byte[]](1, 2, 3, 4))
        $password = ConvertTo-SecureString 'test' -AsPlainText -Force
        $errorMessage = $null

        Push-Location $TestDrive
        try {
            $base64 = ConvertTo-IntuneBase64EncodedPfxCertificate -CertificatePath '.\relative-certificate.pfx'
            try {
                New-IntuneUserPfxCertificate -PathToPfxFile '.\relative-certificate.pfx' -PfxPassword $password -UPN 'user@contoso.com' -KeyFilePath '.\unused.pem'
            }
            catch {
                $errorMessage = $_.Exception.Message
            }
        }
        finally {
            Pop-Location
        }

        if ($base64 -ne 'AQIDBA==') { throw 'The relative input path was not resolved from the caller location.' }
        if ($errorMessage -notlike '*Could not load the PFX*') {
            throw "New-IntuneUserPfxCertificate did not read the caller-relative PFX path. Error: $errorMessage"
        }
    }

    It 'does not call Graph when an import is simulated with WhatIf' {
        Mock -CommandName Invoke-RestMethod -ModuleName IntunePfxImport { throw 'Graph must not be called for WhatIf.' }
        $certificate = [pscustomobject]@{
            thumbprint = 'abc'
            userPrincipalName = 'user@contoso.com'
            encryptedPfxBlob = [byte[]](1)
            encryptedPfxPassword = 'AQ=='
        }

        Import-IntuneUserPfxCertificate -CertificateList $certificate -WhatIf

        Assert-MockCalled -CommandName Invoke-RestMethod -ModuleName IntunePfxImport -Times 0 -Exactly
    }

    It 'requires an authentication context before a Graph read' {
        Remove-IntuneAuthenticationToken -Confirm:$false

        $errorMessage = $null
        try { Get-IntuneUserPfxCertificate } catch { $errorMessage = $_.Exception.Message }
        if ($errorMessage -notlike '*Call Set-IntuneAuthenticationToken first*') { throw 'Graph reads must require an authentication context.' }
    }

    It 'reports a missing directory user without a strict-mode indexing error' {
        Mock -CommandName Invoke-RestMethod -ModuleName IntunePfxImport {
            param($Uri)
            if ($Uri -match '/oauth2/v2.0/token$') {
                return [pscustomobject]@{ access_token = 'missing-user-token'; expires_in = 3600 }
            }
            return [pscustomobject]@{ value = @() }
        }
        $secret = ConvertTo-SecureString ([Guid]::NewGuid().ToString('N')) -AsPlainText -Force
        Set-IntuneAuthenticationToken `
            -ClientId '99999999-9999-9999-9999-999999999999' `
            -TenantId '88888888-8888-8888-8888-888888888888' `
            -ClientSecret $secret `
            -Confirm:$false

        $errorMessage = $null
        try { Get-IntuneUserId -UPN 'missing@contoso.com' } catch { $errorMessage = $_.Exception.Message }

        if ($errorMessage -ne "No user was found for 'missing@contoso.com'.") {
            throw "Expected a clear missing-user error but got: $errorMessage"
        }
    }

    It 'requires a tenant for client-secret authentication' {
        $clientSecretParameters = (Get-Command Set-IntuneAuthenticationToken).ParameterSets |
            Where-Object Name -eq 'ClientSecret' |
            Select-Object -ExpandProperty Parameters
        $tenantParameter = $clientSecretParameters | Where-Object Name -eq 'TenantId'

        if (-not $tenantParameter.IsMandatory) { throw 'TenantId must be mandatory for client-secret authentication.' }
    }

    It 'preserves the legacy positional certificate-creation contract' {
        $pathParameters = (Get-Command New-IntuneUserPfxCertificate).ParameterSets |
            Where-Object Name -eq 'SinglePFXFile' |
            Select-Object -ExpandProperty Parameters
        $parameterSetNames = @((Get-Command New-IntuneUserPfxCertificate).ParameterSets.Name)
        $expectedPositions = @{
            PathToPfxFile = 1
            PfxPassword = 2
            UPN = 3
            ProviderName = 4
            KeyName = 5
            IntendedPurpose = 6
            PaddingScheme = 7
            KeyFilePath = 8
        }
        if (@(Compare-Object @('SinglePFXFile', 'Base64EncodedPfx') $parameterSetNames).Count -ne 0) {
            throw "Unexpected New parameter sets: $($parameterSetNames -join ', ')"
        }

        foreach ($entry in $expectedPositions.GetEnumerator()) {
            $parameter = $pathParameters | Where-Object Name -eq $entry.Key
            if ($parameter.Position -ne $entry.Value) {
                throw "Expected $($entry.Key) at position $($entry.Value), but found $($parameter.Position)."
            }
        }
    }

    It 'builds the connector key ACL on every supported PowerShell edition' {
        $result = & (Get-Module IntunePfxImport) {
            $parameters = New-Object Security.Cryptography.CngKeyCreationParameters

            Add-IntuneConnectorKeyAccess `
                -Parameters $parameters `
                -ProviderName 'Microsoft Software Key Storage Provider'

            $property = @($parameters.Parameters | Where-Object Name -eq 'Security Descr')[0]
            $descriptor = [Security.AccessControl.RawSecurityDescriptor]::new(
                $property.GetValue(),
                0)
            [pscustomobject]@{
                PropertyCount = $parameters.Parameters.Count
                Sddl = $descriptor.GetSddlForm(
                    [Security.AccessControl.AccessControlSections]::Access)
            }
        }

        if ($result.PropertyCount -ne 1) {
            throw 'The software KSP creation parameters must contain one security descriptor.'
        }
        if ($result.Sddl -ne 'D:(A;;FA;;;BA)(A;;GR;;;SO)(A;;GR;;;SY)') {
            throw "The connector key ACL is incorrect: $($result.Sddl)"
        }
    }

    It 'adds a configured connector service account to the key ACL' {
        $result = & (Get-Module IntunePfxImport) {
            $parameters = New-Object Security.Cryptography.CngKeyCreationParameters

            Add-IntuneConnectorKeyAccess `
                -Parameters $parameters `
                -ProviderName 'Microsoft Software Key Storage Provider' `
                -ConnectorServiceAccount 'NT AUTHORITY\NETWORK SERVICE'

            $property = @($parameters.Parameters | Where-Object Name -eq 'Security Descr')[0]
            $descriptor = [Security.AccessControl.RawSecurityDescriptor]::new(
                $property.GetValue(),
                0)
            $descriptor.GetSddlForm(
                [Security.AccessControl.AccessControlSections]::Access)
        }

        if ($result -notmatch '\(A;;GR;;;NS\)') {
            throw "The connector service account is missing from the key ACL: $result"
        }
    }

    It 'preserves Version 2 positional metadata for local key commands' {
        $expected = @{
            'Add-IntuneKspKey' = @{ ProviderName = 1; KeyName = 2; KeyLength = 3 }
            'ConvertTo-IntuneBase64EncodedPfxCertificate' = @{ CertificatePath = 1 }
            'Export-IntunePublicKey' = @{ ProviderName = 1; KeyName = 2; FilePath = 3; FileFormat = 4 }
            'Export-IntunePrivateKey' = @{ ProviderName = 1; KeyName = 2; FilePath = 3 }
            'Import-IntunePrivateKey' = @{ ProviderName = 1; KeyName = 2; FilePath = 3 }
        }

        foreach ($commandName in $expected.Keys) {
            $parameters = (Get-Command $commandName).ParameterSets[0].Parameters
            foreach ($entry in $expected[$commandName].GetEnumerator()) {
                $parameter = $parameters | Where-Object Name -eq $entry.Key
                if ($parameter.Position -ne $entry.Value) {
                    throw "Expected $commandName -$($entry.Key) at position $($entry.Value), but found $($parameter.Position)."
                }
            }
        }
    }

    It 'preserves Version 2 parameter-set behavior for Graph list and removal commands' {
        $getSets = @((Get-Command Get-IntuneUserPfxCertificate).ParameterSets.Name)
        $removeSets = @((Get-Command Remove-IntuneUserPfxCertificate).ParameterSets.Name)

        if ($getSets.Count -ne 1 -or $getSets[0] -ne '__AllParameterSets') {
            throw "Unexpected Get parameter sets: $($getSets -join ', ')"
        }
        foreach ($name in @('FromUserPFXCertificates', 'FromThumbprints', 'FromUsers')) {
            if ($removeSets -notcontains $name) {
                throw "Remove is missing Version 2 parameter set '$name'."
            }
        }
    }

    It 'accepts Version 2 numeric public-key formats without opening a key under WhatIf' -TestCases @(
        @{ Format = 0; Name = 'CngBlob' },
        @{ Format = 1; Name = 'Pem' }
    ) {
        param($Format, $Name)
        $path = Join-Path $TestDrive "export-$Name.key"
        { Export-IntunePublicKey 'Microsoft Software Key Storage Provider' 'nonexistent-key' $path $Format -WhatIf } |
            Should -Not -Throw
        Test-Path -LiteralPath $path | Should -BeFalse
    }

    It 'preserves Version 2 thumbprint precedence when both Graph list filters are supplied' {
        $filterType = 'Microsoft.Management.Powershell.PFXImport.Cmdlets.UserThumbprint' -as [type]
        $filterType.IsValueType | Should -BeTrue
        $filterType.GetField('User').FieldType | Should -Be ([string])
        $filterType.GetField('Thumbprint').FieldType | Should -Be ([string])
        $global:IntunePfxTestGetUris = @()
        Mock -CommandName Invoke-RestMethod -ModuleName IntunePfxImport {
            param($Uri)
            if ($Uri -match '/oauth2/v2.0/token$') {
                return [pscustomobject]@{ access_token = 'filter-token'; expires_in = 3600 }
            }
            $global:IntunePfxTestGetUris += $Uri
            return [pscustomobject]@{ value = @() }
        }
        $secret = ConvertTo-SecureString 'filter-secret' -AsPlainText -Force
        Set-IntuneAuthenticationToken `
            -ClientId '11111111-1111-1111-1111-111111111111' `
            -TenantId '22222222-2222-2222-2222-222222222222' `
            -ClientSecret $secret `
            -Confirm:$false

        Get-IntuneUserPfxCertificate `
            -UserThumbprintList ([Microsoft.Management.Powershell.PFXImport.Cmdlets.UserThumbprint]@{ User = 'thumbprint@contoso.com'; Thumbprint = 'AABBCC' }) `
            -UserList 'user-only@contoso.com' |
            Out-Null

        if ($global:IntunePfxTestGetUris.Count -ne 1) {
            throw "Expected one thumbprint-filtered request but got '$($global:IntunePfxTestGetUris.Count)'."
        }
        $decodedUri = [uri]::UnescapeDataString($global:IntunePfxTestGetUris[0])
        if ($decodedUri -notmatch 'thumbprint@contoso\.com' -or $decodedUri -notmatch 'aabbcc') {
            throw "The thumbprint filter was not used: $($global:IntunePfxTestGetUris[0])"
        }
        if ($decodedUri -match 'user-only') {
            throw 'UserList incorrectly took precedence over UserThumbprintList.'
        }
    }

    Context 'Version 2 certificate creation' {
        BeforeAll {
            $rsa = New-Object Security.Cryptography.RSACng(2048)
            $request = [Security.Cryptography.X509Certificates.CertificateRequest]::new(
                'CN=Legacy PFX Test',
                $rsa,
                [Security.Cryptography.HashAlgorithmName]::SHA256,
                [Security.Cryptography.RSASignaturePadding]::Pkcs1)
            $certificate = $request.CreateSelfSigned([DateTimeOffset]::UtcNow.AddDays(-1), [DateTimeOffset]::UtcNow.AddDays(1))
            $passwordText = 'test-password'
            $password = ConvertTo-SecureString $passwordText -AsPlainText -Force
            $pfxBytes = $certificate.Export([Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $passwordText)
            $pfxPath = Join-Path $TestDrive 'legacy.pfx'
            $publicKeyPath = Join-Path $TestDrive 'legacy-public.bin'
            [IO.File]::WriteAllBytes($pfxPath, $pfxBytes)
            [IO.File]::WriteAllBytes($publicKeyPath, $rsa.Key.Export([Security.Cryptography.CngKeyBlobFormat]::new('RSAPUBLICBLOB')))
        }

        AfterAll {
            $certificate.Dispose()
            $rsa.Dispose()
            $password.Dispose()
            Remove-Variable -Name IntunePfxTestPasswordBytes -Scope Global -ErrorAction SilentlyContinue
        }

        It 'encrypts a real PFX with legacy purpose <Purpose> and padding <Padding>' -TestCases @(
            @{ Purpose = 0; Padding = 0 },
            @{ Purpose = 1; Padding = 3 },
            @{ Purpose = 2; Padding = 4 },
            @{ Purpose = 4; Padding = 5 },
            @{ Purpose = 8; Padding = 'None' }
        ) {
            param($Purpose, $Padding)

            $result = New-IntuneUserPfxCertificate $pfxPath $password 'user@contoso.com' 'provider' 'key' $Purpose $Padding $publicKeyPath
            $expectedPurpose = [Microsoft.Management.Services.Api.UserPfxIntendedPurpose]$Purpose
            $expectedPadding = if ($Padding -eq 0 -or $Padding -eq 'None') { 'OaepSha512' } else {
                [string][Microsoft.Management.Services.Api.UserPfxPaddingScheme]$Padding
            }
            $result.GetType().FullName | Should -Be 'Microsoft.Management.Services.Api.UserPFXCertificate'
            $result.IntendedPurpose | Should -Be $expectedPurpose
            [string]$result.PaddingScheme | Should -Be $expectedPadding
            $result.KeyAlgorithm | Should -Be 'rsa'
            $paddingAlgorithm = & (Get-Module IntunePfxImport) { param($value) Get-IntuneRsaPadding $value } $expectedPadding
            $decrypted = $rsa.Decrypt([Convert]::FromBase64String($result.EncryptedPfxPassword), $paddingAlgorithm)
            [Text.Encoding]::ASCII.GetString($decrypted) | Should -Be $passwordText
            [Convert]::ToBase64String($result.EncryptedPfxBlob) | Should -Be ([Convert]::ToBase64String($pfxBytes))

            $base64Result = New-IntuneUserPfxCertificate -Base64EncodedPfx ([Convert]::ToBase64String($pfxBytes)) -PfxPassword $password -UPN 'user@contoso.com' -IntendedPurpose $expectedPurpose -PaddingScheme $result.PaddingScheme -KeyFilePath $publicKeyPath
            $base64Result.Thumbprint | Should -Be $result.Thumbprint
            $base64Result.IntendedPurpose | Should -Be $result.IntendedPurpose
            $base64Result.PaddingScheme | Should -Be $result.PaddingScheme
        }

        It 'still rejects obsolete numeric padding values' -TestCases @(@{ Padding = 1 }, @{ Padding = 2 }) {
            param($Padding)
            { New-IntuneUserPfxCertificate -PathToPfxFile $pfxPath -PfxPassword $password -UPN 'user@contoso.com' -PaddingScheme $Padding -KeyFilePath $publicKeyPath } |
                Should -Throw '*PaddingScheme*invalid*'
        }

        It 'clears converted password bytes when encryption fails' {
            Mock -CommandName Invoke-IntunePasswordEncryption -ModuleName IntunePfxImport {
                param($PasswordBytes)
                $global:IntunePfxTestPasswordBytes = $PasswordBytes
                throw [Security.Cryptography.CryptographicException]::new('Encryption failed.')
            }
            { New-IntuneUserPfxCertificate -PathToPfxFile $pfxPath -PfxPassword $password -UPN 'user@contoso.com' -KeyFilePath $publicKeyPath } |
                Should -Throw '*Encryption failed.*'
            $global:IntunePfxTestPasswordBytes.Length | Should -Be $passwordText.Length
            @($global:IntunePfxTestPasswordBytes | Where-Object { $_ -ne 0 }).Count | Should -Be 0
        }
    }

    It 'remembers Version 2 provider and key values within the module session' {
        $module = Get-Module IntunePfxImport
        $values = & $module {
            Resolve-IntuneEncryptionKeyParameters -ProviderName 'provider-a' -KeyName 'key-a' -ProviderNameWasBound $true -KeyNameWasBound $true | Out-Null
            Resolve-IntuneEncryptionKeyParameters -ProviderName '' -KeyName '' -ProviderNameWasBound $false -KeyNameWasBound $false
        }

        if ($values.ProviderName -ne 'provider-a' -or $values.KeyName -ne 'key-a') {
            throw 'Version 2 provider/key carry-forward is not preserved.'
        }
    }

    It 'supports the deprecated Version 2 manifest authentication fallback' {
        Mock -CommandName Invoke-RestMethod -ModuleName IntunePfxImport {
            [pscustomobject]@{ access_token = 'legacy-token'; expires_in = 3600 }
        }
        $module = Get-Module IntunePfxImport
        & $module {
            $ExecutionContext.SessionState.Module.PrivateData.ClientId = '11111111-1111-1111-1111-111111111111'
            $ExecutionContext.SessionState.Module.PrivateData.TenantId = '22222222-2222-2222-2222-222222222222'
            $ExecutionContext.SessionState.Module.PrivateData.ClientSecret = 'legacy-secret'
        }

        try {
            Set-IntuneAuthenticationToken -Confirm:$false
            Assert-MockCalled -CommandName Invoke-RestMethod -ModuleName IntunePfxImport -Times 1 -Exactly -ParameterFilter {
                $Body.client_id -eq '11111111-1111-1111-1111-111111111111' -and
                $Body.grant_type -eq 'client_credentials'
            }
        }
        finally {
            & $module {
                $ExecutionContext.SessionState.Module.PrivateData.ClientId = ''
                $ExecutionContext.SessionState.Module.PrivateData.TenantId = ''
                $ExecutionContext.SessionState.Module.PrivateData.ClientSecret = ''
            }
        }
    }

    It 'continues device-code polling while authorization is pending' {
        $global:IntunePfxTestDevicePoll = 0
        Mock -CommandName Start-Sleep -ModuleName IntunePfxImport {}
        Mock -CommandName Write-Host -ModuleName IntunePfxImport {}
        Mock -CommandName Invoke-RestMethod -ModuleName IntunePfxImport {
            param($Uri)
            if ($Uri -match '/devicecode$') {
                return [pscustomobject]@{
                    device_code = 'device-code'
                    expires_in = 900
                    interval = 1
                    message = 'Authenticate'
                }
            }
            $global:IntunePfxTestDevicePoll++
            if ($global:IntunePfxTestDevicePoll -eq 1) {
                $record = New-Object Management.Automation.ErrorRecord(
                    [InvalidOperationException]::new('Authorization pending.'),
                    'authorization_pending',
                    [Management.Automation.ErrorCategory]::NotSpecified,
                    $null)
                $record.ErrorDetails = New-Object Management.Automation.ErrorDetails('{"error":"authorization_pending"}')
                throw $record
            }
            return [pscustomobject]@{ access_token = 'device-token'; expires_in = 3600 }
        }

        $setup = [pscustomobject]@{
            AuthenticationMode = 'PublicClient'
            ClientSecret = $null
            SetIntuneAuthenticationTokenParameters = [ordered]@{
                ClientId = '11111111-1111-1111-1111-111111111111'
                TenantId = '22222222-2222-2222-2222-222222222222'
                AuthUri = 'login.microsoftonline.com'
                GraphUri = 'https://graph.microsoft.com'
                SchemaVersion = 'beta'
                RedirectUri = 'https://login.microsoftonline.com/common/oauth2/nativeclient'
            }
        }
        Set-IntuneAuthenticationToken -Setup $setup -Confirm:$false

        if ($global:IntunePfxTestDevicePoll -ne 2) { throw 'Device-code authentication did not continue after authorization_pending.' }
    }

    It 'preserves device-code transport errors without ErrorDetails' {
        Mock -CommandName Start-Sleep -ModuleName IntunePfxImport {}
        Mock -CommandName Write-Host -ModuleName IntunePfxImport {}
        Mock -CommandName Invoke-RestMethod -ModuleName IntunePfxImport {
            param($Uri)
            if ($Uri -match '/devicecode$') {
                return [pscustomobject]@{
                    device_code = 'device-code'
                    expires_in = 900
                    interval = 1
                    message = 'Authenticate'
                }
            }
            throw [InvalidOperationException]::new('Device-code connection failed.')
        }

        { Set-IntuneAuthenticationToken -ClientId '11111111-1111-1111-1111-111111111111' -TenantId '22222222-2222-2222-2222-222222222222' -Confirm:$false } |
            Should -Throw '*Device-code connection failed.*'
        Assert-MockCalled -CommandName Invoke-RestMethod -ModuleName IntunePfxImport -Times 1 -Exactly -ParameterFilter {
            $Uri -match '/token$'
        }
    }

    It 'allows delegated authentication without an explicit or manifest tenant' {
        Mock -CommandName Start-Sleep -ModuleName IntunePfxImport {}
        Mock -CommandName Write-Host -ModuleName IntunePfxImport {}
        Mock -CommandName Invoke-RestMethod -ModuleName IntunePfxImport {
            param($Uri)
            if ($Uri -match '/devicecode$') {
                return [pscustomobject]@{
                    device_code = 'device-code'; expires_in = 900; interval = 1; message = 'Authenticate'
                }
            }
            [pscustomobject]@{ access_token = 'tenantless-token'; expires_in = 3600 }
        }
        Set-IntuneAuthenticationToken -ClientId '11111111-1111-1111-1111-111111111111' -Confirm:$false
        Assert-MockCalled -CommandName Invoke-RestMethod -ModuleName IntunePfxImport -Times 1 -Exactly -ParameterFilter {
            $Uri -eq 'https://login.microsoftonline.com/organizations/oauth2/v2.0/token'
        }
    }

    It 'reuses a valid matching token when authentication is requested again' {
        Remove-IntuneAuthenticationToken -Confirm:$false
        $global:IntunePfxTestDeviceCodeRequests = 0
        Mock -CommandName Start-Sleep -ModuleName IntunePfxImport {}
        Mock -CommandName Write-Host -ModuleName IntunePfxImport {}
        Mock -CommandName Invoke-RestMethod -ModuleName IntunePfxImport {
            param($Uri)
            if ($Uri -match '/devicecode$') {
                $global:IntunePfxTestDeviceCodeRequests++
                return [pscustomobject]@{
                    device_code = 'device-code'
                    expires_in = 900
                    interval = 1
                    message = 'Authenticate'
                }
            }
            return [pscustomobject]@{ access_token = 'sticky-device-token'; expires_in = 3600 }
        }

        $parameters = @{
            ClientId = '11111111-1111-1111-1111-111111111111'
            TenantId = '22222222-2222-2222-2222-222222222222'
            Confirm = $false
        }
        try {
            Set-IntuneAuthenticationToken @parameters
            Set-IntuneAuthenticationToken @parameters

            if ($global:IntunePfxTestDeviceCodeRequests -ne 1) {
                throw "Expected one device-code prompt for repeated matching authentication but got '$global:IntunePfxTestDeviceCodeRequests'."
            }
        }
        finally {
            Remove-IntuneAuthenticationToken -Confirm:$false
        }
    }

    It 'increases the device-code polling interval after slow_down' {
        $global:IntunePfxTestDevicePoll = 0
        $global:IntunePfxTestSleepIntervals = @()
        Mock -CommandName Start-Sleep -ModuleName IntunePfxImport {
            param($Seconds)
            $global:IntunePfxTestSleepIntervals += $Seconds
        }
        Mock -CommandName Write-Host -ModuleName IntunePfxImport {}
        Mock -CommandName Invoke-RestMethod -ModuleName IntunePfxImport {
            param($Uri)
            if ($Uri -match '/devicecode$') {
                return [pscustomobject]@{
                    device_code = 'device-code'
                    expires_in = 900
                    interval = 2
                    message = 'Authenticate'
                }
            }
            $global:IntunePfxTestDevicePoll++
            if ($global:IntunePfxTestDevicePoll -eq 1) {
                $record = New-Object Management.Automation.ErrorRecord(
                    [InvalidOperationException]::new('Slow down.'),
                    'slow_down',
                    [Management.Automation.ErrorCategory]::NotSpecified,
                    $null)
                $record.ErrorDetails = New-Object Management.Automation.ErrorDetails('{"error":"slow_down"}')
                throw $record
            }
            return [pscustomobject]@{ access_token = 'device-token'; expires_in = 3600 }
        }

        Set-IntuneAuthenticationToken `
            -ClientId '11111111-1111-1111-1111-111111111111' `
            -TenantId '22222222-2222-2222-2222-222222222222' `
            -Confirm:$false

        if (
            $global:IntunePfxTestSleepIntervals.Count -ne 2 -or
            $global:IntunePfxTestSleepIntervals[0] -ne 2 -or
            $global:IntunePfxTestSleepIntervals[1] -ne 7
        ) {
            throw "Expected polling intervals 2 and 7 seconds but got '$($global:IntunePfxTestSleepIntervals -join ', ')'."
        }
    }

    It 'preserves empty and one-character password byte arrays and rejects non-ASCII passwords' {
        $module = Get-Module IntunePfxImport
        $emptyPassword = New-Object Security.SecureString
        $oneCharacterPassword = New-Object Security.SecureString
        $oneCharacterPassword.AppendChar('a')
        $nonAsciiPassword = New-Object Security.SecureString
        $nonAsciiPassword.AppendChar([char]0x00e9)
        $highUnicodePassword = New-Object Security.SecureString
        $highUnicodePassword.AppendChar([char]0xff21)

        $emptyBytes = & $module { param($value) ConvertTo-PasswordBytes -SecureString $value } $emptyPassword
        $oneCharacterBytes = & $module { param($value) ConvertTo-PasswordBytes -SecureString $value } $oneCharacterPassword
        $errorMessage = $null
        try {
            & $module { param($value) ConvertTo-PasswordBytes -SecureString $value } $nonAsciiPassword
        }
        catch {
            $errorMessage = $_.Exception.Message
        }
        $highUnicodeErrorMessage = $null
        try {
            & $module { param($value) ConvertTo-PasswordBytes -SecureString $value } $highUnicodePassword
        }
        catch {
            $highUnicodeErrorMessage = $_.Exception.Message
        }

        if ($emptyBytes -isnot [byte[]] -or $emptyBytes.Length -ne 0) { throw 'An empty password must remain an empty byte array.' }
        if ($oneCharacterBytes -isnot [byte[]] -or $oneCharacterBytes.Length -ne 1 -or $oneCharacterBytes[0] -ne 97) {
            throw 'A one-character password must remain a one-byte array.'
        }
        if ($errorMessage -notlike '*only ASCII characters*') { throw 'Non-ASCII passwords must fail explicitly.' }
        if ($highUnicodeErrorMessage -notlike '*only ASCII characters*') { throw 'High Unicode passwords must fail explicitly.' }
        $moduleText = Get-Content -LiteralPath (Join-Path $moduleRoot 'IntunePfxImport.psm1') -Raw
        $passwordFunction = [regex]::Match(
            $moduleText,
            '(?s)function ConvertTo-PasswordBytes \{.*?^}',
            [Text.RegularExpressions.RegexOptions]::Multiline).Value
        if ($passwordFunction -match 'ConvertTo-PlainText|PtrToStringBSTR') {
            throw 'PFX password conversion creates an immutable managed plaintext string.'
        }
    }

    It 'reports a missing machine encryption key without cascading errors' {
        $module = Get-Module IntunePfxImport
        $missingKeyName = "MissingIntunePfxKey-$([Guid]::NewGuid())"
        $errorMessage = $null

        try {
            & $module {
                param($keyName)
                Invoke-IntunePasswordEncryption `
                    -PasswordBytes ([byte[]](1, 2, 3)) `
                    -ProviderName 'Microsoft Software Key Storage Provider' `
                    -KeyName $keyName `
                    -PaddingScheme OaepSha512
            } $missingKeyName
        }
        catch {
            $errorMessage = $_.Exception.Message
        }

        if ($errorMessage -notlike "*Machine CNG key '$missingKeyName' was not found*") {
            throw "Missing machine key error was not actionable. Error: $errorMessage"
        }
        if ($errorMessage -like '*encryptedPassword*') { throw 'A missing machine key produced a cascading encryptedPassword error.' }
    }

    It 'uses command-line configuration for client-secret auth and subsequent Graph requests' {
        Mock -CommandName Invoke-RestMethod -ModuleName IntunePfxImport {
            param($Method, $Uri, $Body)
            if ($Uri -match '/oauth2/v2.0/token$') {
                return [pscustomobject]@{ access_token = 'test-token'; expires_in = 3600 }
            }
            return [pscustomobject]@{ value = @([pscustomobject]@{ thumbprint = 'abc'; userPrincipalName = 'user@contoso.com' }) }
        }
        $secret = ConvertTo-SecureString ([Guid]::NewGuid().ToString('N')) -AsPlainText -Force

        Set-IntuneAuthenticationToken -ClientId '11111111-1111-1111-1111-111111111111' -TenantId '22222222-2222-2222-2222-222222222222' -ClientSecret $secret -Confirm:$false
        $result = Get-IntuneUserPfxCertificate

        if ($result.thumbprint -ne 'abc') { throw 'Graph response was not returned.' }
        Assert-MockCalled -CommandName Invoke-RestMethod -ModuleName IntunePfxImport -Times 1 -Exactly -ParameterFilter {
            $Uri -eq 'https://login.microsoftonline.com/22222222-2222-2222-2222-222222222222/oauth2/v2.0/token' -and
            $Body.client_id -eq '11111111-1111-1111-1111-111111111111' -and
            $Body.grant_type -eq 'client_credentials'
        }
        Assert-MockCalled -CommandName Invoke-RestMethod -ModuleName IntunePfxImport -Times 1 -Exactly -ParameterFilter {
            $Method -eq 'Get' -and
            $Uri -eq 'https://graph.microsoft.com/beta/deviceManagement/userPfxCertificates' -and
            $Headers.Authorization -eq 'Bearer test-token'
        }
    }

    It 'writes safe verbose Graph request lifecycle messages' {
        Mock -CommandName Invoke-RestMethod -ModuleName IntunePfxImport {
            param($Uri)
            if ($Uri -match '/oauth2/v2.0/token$') {
                return [pscustomobject]@{ access_token = 'verbose-secret-token'; expires_in = 3600 }
            }
            return [pscustomobject]@{ value = @() }
        }
        $secret = ConvertTo-SecureString 'verbose-client-secret' -AsPlainText -Force

        Set-IntuneAuthenticationToken `
            -ClientId '11111111-1111-1111-1111-111111111111' `
            -TenantId '22222222-2222-2222-2222-222222222222' `
            -ClientSecret $secret `
            -Confirm:$false
        $messages = @(
            Get-IntuneUserPfxCertificate -Verbose 4>&1 |
                Where-Object { $_ -is [Management.Automation.VerboseRecord] } |
                ForEach-Object Message
        )

        if ($messages -notcontains 'Sending Graph request (attempt 1 of 3): Get https://graph.microsoft.com/beta/deviceManagement/userPfxCertificates') {
            throw "Graph request start was not logged. Messages: $($messages -join '; ')"
        }
        if ($messages -notcontains 'Graph request succeeded: Get https://graph.microsoft.com/beta/deviceManagement/userPfxCertificates') {
            throw "Graph request success was not logged. Messages: $($messages -join '; ')"
        }
        if (($messages -join ' ') -match 'verbose-secret-token|verbose-client-secret') {
            throw 'Graph request verbose logging exposed authentication material.'
        }
    }

    It 'refreshes an expired client-secret token before sending a Graph request' {
        $global:IntunePfxTestTokenRequestCount = 0
        $global:IntunePfxTestGraphAuthorization = $null
        Mock -CommandName Invoke-RestMethod -ModuleName IntunePfxImport {
            param($Method, $Uri, $Headers)
            if ($Uri -match '/oauth2/v2.0/token$') {
                $global:IntunePfxTestTokenRequestCount++
                if ($global:IntunePfxTestTokenRequestCount -eq 1) {
                    return [pscustomobject]@{ access_token = 'expired-token'; expires_in = 0 }
                }
                return [pscustomobject]@{ access_token = 'refreshed-token'; expires_in = 3600 }
            }
            $global:IntunePfxTestGraphAuthorization = $Headers.Authorization
            return [pscustomobject]@{ value = @() }
        }
        $secret = ConvertTo-SecureString ([Guid]::NewGuid().ToString('N')) -AsPlainText -Force

        Set-IntuneAuthenticationToken -ClientId '11111111-1111-1111-1111-111111111111' -TenantId '22222222-2222-2222-2222-222222222222' -ClientSecret $secret -Confirm:$false
        Get-IntuneUserPfxCertificate | Out-Null

        if ($global:IntunePfxTestTokenRequestCount -ne 2) { throw 'An expired client-secret token was not refreshed.' }
        if ($global:IntunePfxTestGraphAuthorization -ne 'Bearer refreshed-token') { throw 'Graph request did not use the refreshed bearer token.' }
    }

    It 'serializes binary and date certificate fields for the Graph contract' {
        $global:IntunePfxTestRequestBody = $null
        Mock -CommandName Invoke-RestMethod -ModuleName IntunePfxImport {
            param($Uri, $Body)
            if ($Uri -match '/oauth2/v2.0/token$') {
                return [pscustomobject]@{ access_token = 'serialization-token'; expires_in = 3600 }
            }
            $global:IntunePfxTestRequestBody = $Body
            return [pscustomobject]@{ id = 'created-certificate' }
        }
        $secret = ConvertTo-SecureString ([Guid]::NewGuid().ToString('N')) -AsPlainText -Force
        $start = [DateTime]::SpecifyKind([DateTime]'2025-01-02T03:04:05', [DateTimeKind]::Utc)
        $certificate = [pscustomobject]@{
            thumbprint = 'abc'
            userPrincipalName = 'user@contoso.com'
            startDateTime = $start
            expirationDateTime = $start.AddDays(1)
            encryptedPfxBlob = [byte[]](1, 2, 3, 255)
            encryptedPfxPassword = 'AQ=='
        }

        Set-IntuneAuthenticationToken -ClientId '11111111-1111-1111-1111-111111111111' -TenantId '22222222-2222-2222-2222-222222222222' -ClientSecret $secret -Confirm:$false
        Import-IntuneUserPfxCertificate -CertificateList $certificate -Confirm:$false | Out-Null
        $requestBody = $global:IntunePfxTestRequestBody | ConvertFrom-Json

        if ($requestBody.encryptedPfxBlob -ne 'AQID/w==') { throw 'encryptedPfxBlob must be serialized as Base64.' }
        if ($null -ne $requestBody.PSObject.Properties['keyAlgorithm']) { throw 'keyAlgorithm is local metadata and is not valid in the Graph request body.' }
        if ($global:IntunePfxTestRequestBody -notmatch '"startDateTime"\s*:\s*"2025-01-02T03:04:05(\.0+)?Z"') {
            throw "startDateTime must be serialized as ISO-8601 UTC. Body: $global:IntunePfxTestRequestBody"
        }
        if ($global:IntunePfxTestRequestBody -notmatch '"expirationDateTime"\s*:\s*"2025-01-03T03:04:05(\.0+)?Z"') {
            throw "expirationDateTime must be serialized as ISO-8601 UTC. Body: $global:IntunePfxTestRequestBody"
        }
    }

    It 'continues an import batch after a per-record Graph failure' {
        $global:IntunePfxTestImportCount = 0
        Mock -CommandName Invoke-RestMethod -ModuleName IntunePfxImport {
            param($Uri)
            if ($Uri -match '/oauth2/v2.0/token$') {
                return [pscustomobject]@{ access_token = 'batch-token'; expires_in = 3600 }
            }
            $global:IntunePfxTestImportCount++
            if ($global:IntunePfxTestImportCount -eq 1) {
                throw [InvalidOperationException]::new('First record failed.')
            }
            return [pscustomobject]@{ id = 'second-record' }
        }
        $secret = ConvertTo-SecureString 'batch-secret' -AsPlainText -Force
        $certificates = @(
            [pscustomobject]@{ thumbprint = 'first'; userPrincipalName = 'first@contoso.com'; encryptedPfxBlob = [byte[]](1); encryptedPfxPassword = 'AQ==' },
            [pscustomobject]@{ thumbprint = 'second'; userPrincipalName = 'second@contoso.com'; encryptedPfxBlob = [byte[]](2); encryptedPfxPassword = 'Ag==' }
        )
        Set-IntuneAuthenticationToken -ClientId '11111111-1111-1111-1111-111111111111' -TenantId '22222222-2222-2222-2222-222222222222' -ClientSecret $secret -Confirm:$false

        Import-IntuneUserPfxCertificate -CertificateList $certificates -Confirm:$false -ErrorAction SilentlyContinue -ErrorVariable importErrors

        if ($global:IntunePfxTestImportCount -ne 2) { throw 'Import stopped after the first record failure.' }
        if (@($importErrors).Count -lt 1) { throw 'Import did not surface the per-record error.' }
    }

    It 'follows Graph continuation links without changing endpoints' {
        $global:IntunePfxTestPage = 0
        Mock -CommandName Invoke-RestMethod -ModuleName IntunePfxImport {
            param($Uri)
            if ($Uri -match '/oauth2/v2.0/token$') {
                return [pscustomobject]@{ access_token = 'paging-token'; expires_in = 3600 }
            }
            $global:IntunePfxTestPage++
            if ($global:IntunePfxTestPage -eq 1) {
                return [pscustomobject]@{
                    value = @([pscustomobject]@{ thumbprint = 'first' })
                    '@odata.nextLink' = 'https://graph.microsoft.com/beta/deviceManagement/userPfxCertificates?$skiptoken=next'
                }
            }
            return [pscustomobject]@{ value = @([pscustomobject]@{ thumbprint = 'second' }) }
        }
        $secret = ConvertTo-SecureString ([Guid]::NewGuid().ToString('N')) -AsPlainText -Force

        Set-IntuneAuthenticationToken -ClientId '11111111-1111-1111-1111-111111111111' -TenantId '22222222-2222-2222-2222-222222222222' -ClientSecret $secret -Confirm:$false
        $result = @(Get-IntuneUserPfxCertificate)

        if ($result.Count -ne 2 -or $result[0].thumbprint -ne 'first' -or $result[1].thumbprint -ne 'second') {
            throw 'Graph continuation pages were not returned in order.'
        }
    }

    It 'normalizes Graph responses to the Version 2 PascalCase object shape' {
        Mock -CommandName Invoke-RestMethod -ModuleName IntunePfxImport {
            param($Uri)
            if ($Uri -match '/oauth2/v2.0/token$') {
                return [pscustomobject]@{ access_token = 'shape-token'; expires_in = 3600 }
            }
            return [pscustomobject]@{ value = @([pscustomobject]@{
                thumbprint = 'abc'
                userPrincipalName = 'user@contoso.com'
                intendedPurpose = 'smimeEncryption'
                paddingScheme = 'oaepSha512'
                encryptedPfxBlob = 'AQID'
                startDateTime = '2025-01-02T03:04:05Z'
            }) }
        }
        $secret = ConvertTo-SecureString 'shape-secret' -AsPlainText -Force
        Set-IntuneAuthenticationToken -ClientId '11111111-1111-1111-1111-111111111111' -TenantId '22222222-2222-2222-2222-222222222222' -ClientSecret $secret -Confirm:$false

        $result = Get-IntuneUserPfxCertificate

        if ($result -isnot [Microsoft.Management.Services.Api.UserPFXCertificate]) { throw 'Legacy CLR type is missing.' }
        if ($result.PSObject.Properties.Name -notcontains 'UserPrincipalName') { throw 'PascalCase properties are missing.' }
        if ($result.IntendedPurpose -ne [Microsoft.Management.Services.Api.UserPfxIntendedPurpose]::SmimeEncryption) { throw 'IntendedPurpose was not restored to the legacy enum.' }
        if ($result.PaddingScheme -ne [Microsoft.Management.Services.Api.UserPfxPaddingScheme]::OaepSha512) { throw 'PaddingScheme was not restored to the legacy enum.' }
        if ($result.EncryptedPfxBlob -isnot [byte[]] -or $result.EncryptedPfxBlob.Length -ne 3) { throw 'Graph Base64 was not restored to byte[].' }
        if ($result.StartDateTime -isnot [DateTimeOffset]) { throw 'Graph dates were not restored to DateTimeOffset.' }
    }

    It 'normalizes intended-purpose casing before creating records' {
        $module = Get-Module IntunePfxImport
        $cases = @(
            @{ Value = 'SMIMEENCRYPTION'; Expected = 'smimeEncryption' },
            @{ Value = [Microsoft.Management.Services.Api.UserPfxIntendedPurpose]::SmimeEncryption; Expected = 'smimeEncryption' },
            @{ Value = 2; Expected = 'smimeSigning' },
            @{ Value = 'VPN'; Expected = 'vpn' }
        )
        foreach ($case in $cases) {
            $canonical = & $module {
                param($value)
                ConvertTo-IntuneIntendedPurposeName -Value $value
            } $case.Value
            if ($canonical -ne $case.Expected) {
                throw "Expected '$($case.Expected)' but got '$canonical'."
            }
        }

        $body = & $module {
            ConvertTo-IntuneUserPfxBody -Certificate ([pscustomobject]@{
                IntendedPurpose = [Microsoft.Management.Services.Api.UserPfxIntendedPurpose]::SmimeEncryption
                PaddingScheme = [Microsoft.Management.Services.Api.UserPfxPaddingScheme]::OaepSha512
            })
        }
        if ($body.intendedPurpose -ne 'smimeEncryption' -or $body.paddingScheme -ne 'oaepSha512') {
            throw 'Legacy enum properties were not serialized to canonical Graph values.'
        }

        $errorMessage = $null
        try {
            & $module {
                ConvertTo-IntuneUserPfxBody -Certificate ([pscustomobject]@{
                    IntendedPurpose = 'invalid'
                    PaddingScheme = 'OaepSha512'
                })
            }
        }
        catch {
            $errorMessage = $_.Exception.Message
        }
        if ($errorMessage -notlike "*IntendedPurpose 'invalid' is invalid*") {
            throw 'Invalid intended-purpose values do not fail explicitly.'
        }
    }

    It 'accepts a Version 2 directory user ID for direct removal' -TestCases @(
        @{ Typed = $false }, @{ Typed = $true }
    ) {
        param($Typed)
        $global:IntunePfxTestDeleteUri = $null
        Mock -CommandName Invoke-RestMethod -ModuleName IntunePfxImport {
            param($Method, $Uri)
            if ($Uri -match '/oauth2/v2.0/token$') {
                return [pscustomobject]@{ access_token = 'remove-token'; expires_in = 3600 }
            }
            $global:IntunePfxTestDeleteUri = $Uri
        }
        $secret = ConvertTo-SecureString 'remove-secret' -AsPlainText -Force
        Set-IntuneAuthenticationToken -ClientId '11111111-1111-1111-1111-111111111111' -TenantId '22222222-2222-2222-2222-222222222222' -ClientSecret $secret -Confirm:$false

        $filter = @{
            User = '0123456789abcdef0123456789abcdef'
            Thumbprint = 'aabbcc'
        }
        if ($Typed) { $filter = [Microsoft.Management.Powershell.PFXImport.Cmdlets.UserThumbprint]$filter }
        Remove-IntuneUserPfxCertificate -UserThumbprintList $filter -Confirm:$false

        if ($global:IntunePfxTestDeleteUri -notlike '*/0123456789abcdef0123456789abcdef-aabbcc') {
            throw "Directory user ID was not used directly: $global:IntunePfxTestDeleteUri"
        }
    }

    It 'ships a parseable, clearly marked non-production E2E sample' {
        $samplePath = Join-Path $PSScriptRoot '..\Examples\Test-IntunePfxImportE2E.ps1'
        $tokens = $null
        $errors = $null
        $sampleAst = [Management.Automation.Language.Parser]::ParseFile($samplePath, [ref]$tokens, [ref]$errors)
        $sampleText = Get-Content -LiteralPath $samplePath -Raw

        if (@($errors).Count -ne 0) { throw "E2E sample parser errors: $($errors.Message -join '; ')" }
        if ($sampleText -notmatch 'NOT FOR PRODUCTION USE') { throw 'E2E sample lacks the non-production disclaimer.' }
        if ($sampleText -notmatch "\[version\]'3\.0\.0'") { throw 'E2E sample does not require Version 3.0.0.' }
        if ($sampleText -notmatch 'ConnectorServiceAccount') { throw 'E2E sample cannot grant its connector service account access to the test key.' }
        if ($sampleText -notmatch "\.Parameters\.ContainsKey\('ConnectorServiceAccount'\)") { throw 'E2E sample does not detect a stale module that lacks connector service-account support.' }
        if ($sampleText -notmatch '\$keyParameters\.ConnectorServiceAccount = \$ConnectorServiceAccount') { throw 'E2E sample does not conditionally pass the connector service account to key creation.' }
        if ($sampleText -notmatch '\[string\]\$ApplicationId') { throw 'E2E sample does not accept an existing application ID for single sign-in.' }
        if ($sampleText -notmatch '\[psobject\]\$Setup') { throw 'E2E sample does not accept a reusable setup object.' }
        if ($sampleText -notmatch 'Set-IntuneAuthenticationToken -Setup \$Setup') { throw 'E2E sample does not authenticate directly from the reusable setup object.' }
        if ($sampleText -notmatch 'For future single-sign-in runs, use -Setup \$setup') { throw 'E2E sample does not explain setup-object reuse.' }
        if ($sampleText -notmatch 'Set-IntuneAuthenticationToken @authenticationParameters') { throw 'E2E sample does not authenticate directly with an existing application.' }
        if ($sampleText -notmatch 'openid-configuration') { throw 'E2E sample does not resolve the tenant for single sign-in when TenantId is omitted.' }
        if ($sampleText -notmatch 'TenantId = \$authenticationTenantId') { throw 'E2E sample does not pass the resolved tenant to module authentication.' }
        if ($sampleText -notmatch "' is the tenant ID, not an application client ID") { throw 'E2E sample does not reject a tenant ID supplied as ApplicationId.' }
        if ($sampleText -notmatch '\$effectiveApplicationId = \$Setup\.ApplicationId') { throw 'E2E sample does not retain the onboarded application ID.' }
        if ($sampleText -match 'Application ID: \$\(\$Setup\.ApplicationId\)') { throw 'E2E sample logs application ID through a setup object that is absent in single-sign-in mode.' }
        if ($sampleText -notmatch 'Application ID: \$effectiveApplicationId') { throw 'E2E sample does not consistently log the effective application ID.' }
        if ($sampleText -notmatch 'Tenant ID: \$effectiveTenantId') { throw 'E2E sample does not log the effective tenant ID.' }
        if ($sampleText -notmatch "For future single-sign-in runs, use -ApplicationId") { throw 'E2E sample does not identify the reusable application ID after onboarding.' }
        if ($sampleText -notmatch "'-GrantAdminConsent cannot be used with -ApplicationId") { throw 'E2E sample silently ignores GrantAdminConsent in application-ID mode.' }
        if ($sampleText -notmatch "'Specify either -Setup or -ApplicationId, not both\.'") { throw 'E2E sample does not reject ambiguous reusable authentication inputs.' }
        if ($sampleText -notmatch "'-ApplicationDisplayName is available only during application onboarding\.'") { throw 'E2E sample silently ignores ApplicationDisplayName outside onboarding.' }
        if ($sampleText -notmatch "' does not match the tenant '") { throw 'E2E sample does not reject a TenantId that conflicts with Setup.' }
        if ($sampleText -notmatch '\[string\]\$TenantId') { throw 'E2E sample does not expose an optional tenant ID.' }
        if ($sampleText -match '\[Parameter\(Mandatory\)\]\s*\r?\n\s*\[ValidatePattern\([^\r\n]+\)\]\s*\r?\n\s*\[string\]\$TenantId') {
            throw 'E2E sample still requires operators to know the tenant ID before signing in.'
        }
        $sampleParameters = @{}
        foreach ($parameter in $sampleAst.ParamBlock.Parameters) {
            $sampleParameters[$parameter.Name.VariablePath.UserPath] = $parameter
        }
        if ($sampleParameters.Algorithm.DefaultValue.SafeGetValue() -ne 'rsa') { throw 'E2E sample does not default to RSA.' }
        if ($sampleParameters.KeySize.DefaultValue.SafeGetValue() -ne 2048) { throw 'E2E sample does not default to an RSA 2048-bit key.' }
        if ($sampleParameters.Curve.DefaultValue.SafeGetValue() -ne 'nistP384') { throw 'E2E sample does not provide the expected ECC curve default.' }
        if ($sampleParameters.IntendedPurpose.DefaultValue.SafeGetValue() -ne 'smimeEncryption') { throw 'E2E sample does not default to S/MIME encryption.' }
        if ($null -eq $sampleParameters.CertificateSubject) { throw 'E2E sample does not expose a configurable certificate subject.' }
        if ($sampleParameters.CertificateSubject.DefaultValue.Extent.Text -ne '"CN=$TargetUpn"') { throw 'E2E sample does not preserve the target UPN as the default certificate common name.' }
        if (([regex]::Matches($sampleText, '\$CertificateSubject,\s*\r?\n\s*\$certificateKey')).Count -ne 2) { throw 'E2E sample does not use the configured certificate subject for both RSA and ECC generation.' }
        if ($sampleText -notmatch '\$san\.AddEmailAddress\(\$TargetUpn\)') { throw 'E2E sample does not retain the target UPN in the certificate SAN.' }
        if ($sampleText -notmatch "\[ValidateSet\('rsa', 'ecc'\)\]") { throw 'E2E sample does not restrict the certificate algorithm to RSA or ECC.' }
        if ($sampleText -notmatch "\[ValidateSet\('nistP256', 'nistP384', 'nistP521'\)\]") { throw 'E2E sample does not expose supported ECC curves.' }
        if ($sampleText -notmatch '\[ValidateSet\(2048, 3072, 4096\)\]') { throw 'E2E sample does not expose supported RSA key sizes.' }
        if ($sampleText -notmatch "\[ValidateSet\('unassigned', 'smimeEncryption', 'smimeSigning', 'vpn', 'wifi'\)\]") { throw 'E2E sample does not expose supported intended purposes.' }
        if ($sampleText -notmatch '-IntendedPurpose \$IntendedPurpose') { throw 'E2E sample does not pass the selected intended purpose to record creation.' }
        if ($sampleText -notmatch "'-Curve is valid only when -Algorithm is ecc\.'") { throw 'E2E sample can silently accept an ECC curve for RSA generation.' }
        if ($sampleText -notmatch "'-KeySize is valid only when -Algorithm is rsa\.'") { throw 'E2E sample can silently accept an RSA key size for ECC generation.' }
        if ($sampleText -notmatch "ContextScope\s*=\s*'Process'") { throw 'E2E sample does not isolate the Graph SDK context to the current process.' }
        if ($sampleText -notmatch '\$effectiveTenantId = \$graphContext\.TenantId') { throw 'E2E sample does not discover the tenant selected during Graph sign-in.' }
        if ($sampleText -notmatch '-TenantId \$effectiveTenantId') { throw 'E2E sample does not use the signed-in tenant for application onboarding.' }
        if ($sampleText -notmatch 'Disconnect-MgGraph') { throw 'E2E sample does not clean up the Graph SDK context.' }
        if ($sampleText -match 'Remove-IntuneUserPfxCertificate') { throw 'E2E sample removes the record before asynchronous connector processing can complete.' }
        if ($sampleText -match '\$cleanupKey\.Delete\(\)') { throw 'E2E sample removes the key before asynchronous connector processing can complete.' }
        if ($sampleText -notmatch '\$recordSubmissionAttempted\s*=\s*\$true\s*\r?\n\s*Import-IntuneUserPfxCertificate') {
            throw 'E2E sample can delete the connector key after an ambiguously successful record submission.'
        }
        if ($sampleText -notmatch 'try\s*\{\s*if \(-not \[Security\.Cryptography\.CngKey\]::Exists\(') {
            throw 'E2E sample creates the machine key outside the protected cleanup region.'
        }
        if ($sampleText -notmatch '\$createdKey -and -not \$recordSubmissionAttempted') { throw 'E2E sample leaves a newly created key behind when the run fails before record submission.' }
        if ($sampleText -notmatch '\$failedRunKey\.Delete\(\)') { throw 'E2E sample does not remove a newly created key after a pre-submission failure.' }
    }

    It 'preserves sovereign cloud AuthUri and GraphUri selections' {
        Mock -CommandName Invoke-RestMethod -ModuleName IntunePfxImport {
            [pscustomobject]@{ access_token = 'government-token'; expires_in = 3600 }
        }
        $secret = ConvertTo-SecureString ([Guid]::NewGuid().ToString('N')) -AsPlainText -Force

        Set-IntuneAuthenticationToken -ClientId '11111111-1111-1111-1111-111111111111' -TenantId '22222222-2222-2222-2222-222222222222' -ClientSecret $secret -AuthUri 'login.microsoftonline.us' -GraphUri 'https://graph.microsoft.us' -Confirm:$false

        Assert-MockCalled -CommandName Invoke-RestMethod -ModuleName IntunePfxImport -Times 1 -Exactly -ParameterFilter {
            $Uri -eq 'https://login.microsoftonline.us/22222222-2222-2222-2222-222222222222/oauth2/v2.0/token' -and
            $Body.scope -eq 'https://graph.microsoft.us/.default'
        }
    }

    if ($PSVersionTable.PSEdition -eq 'Core') {
        It 'reads retry headers from PowerShell 7 HTTP responses' {
            $module = Get-Module IntunePfxImport
            $response = [Net.Http.HttpResponseMessage]::new([Net.HttpStatusCode]::TooManyRequests)
            $response.Headers.TryAddWithoutValidation('x-ms-retry-after-ms', '1500') | Out-Null
            $exception = [pscustomobject]@{ Response = $response }

            $delay = & $module { param($value) Get-IntuneRetryDelay -Exception $value } $exception

            if ($delay -ne 2) { throw "Expected a two-second retry delay but got '$delay'." }
            $response.Dispose()
        }
    }

    It 'rejects an existing Graph SDK context for the wrong cloud' {
        Mock -CommandName Get-Command -ModuleName IntunePfxImport {
            param($Name)
            [pscustomobject]@{ Name = $Name }
        }
        Mock -CommandName Get-IntuneMgGraphContext -ModuleName IntunePfxImport {
            [pscustomobject]@{
                TenantId = '22222222-2222-2222-2222-222222222222'
                Environment = 'Global'
            }
        }

        $errorMessage = $null
        try {
            Initialize-IntunePfxImportApplication -GraphUri 'https://graph.microsoft.us' -AuthUri 'login.microsoftonline.us' -Confirm:$false
        }
        catch {
            $errorMessage = $_.Exception.Message
        }

        if ($errorMessage -notlike "*does not match the environment 'USGov'*") {
            throw 'Onboarding must reject a Graph SDK context connected to the wrong cloud.'
        }
    }

    It 'escapes single quotes in a UPN before issuing Graph requests' {
        Mock -CommandName Invoke-RestMethod -ModuleName IntunePfxImport {
            param($Uri)
            if ($Uri -match '/oauth2/v2.0/token$') {
                return [pscustomobject]@{ access_token = 'quoted-upn-token'; expires_in = 3600 }
            }
            [pscustomobject]@{ value = @() }
        }
        $secret = ConvertTo-SecureString 'quoted-upn-secret' -AsPlainText -Force
        Set-IntuneAuthenticationToken `
            -ClientId '11111111-1111-1111-1111-111111111111' `
            -TenantId '22222222-2222-2222-2222-222222222222' `
            -ClientSecret $secret `
            -Confirm:$false

        Get-IntuneUserPfxCertificate -UserList "o'hara@contoso.com"

        Assert-MockCalled -CommandName Invoke-RestMethod -ModuleName IntunePfxImport -Times 1 -Exactly -ParameterFilter {
            [uri]::UnescapeDataString(([uri]$Uri).Query) -like "*o''hara@contoso.com*"
        }
    }

    It 'encodes legal URI delimiter characters in UPN filters' {
        $global:IntunePfxTestUserUri = $null
        Mock -CommandName Invoke-RestMethod -ModuleName IntunePfxImport {
            param($Uri)
            if ($Uri -match '/oauth2/v2.0/token$') {
                return [pscustomobject]@{ access_token = 'uri-token'; expires_in = 3600 }
            }
            $global:IntunePfxTestUserUri = [uri]$Uri
            return [pscustomobject]@{ value = @() }
        }
        $secret = ConvertTo-SecureString ([Guid]::NewGuid().ToString('N')) -AsPlainText -Force

        Set-IntuneAuthenticationToken -ClientId '11111111-1111-1111-1111-111111111111' -TenantId '22222222-2222-2222-2222-222222222222' -ClientSecret $secret -Confirm:$false
        Get-IntuneUserPfxCertificate -UserList 'a#b@contoso.com' | Out-Null

        if (-not [string]::IsNullOrEmpty($global:IntunePfxTestUserUri.Fragment)) { throw 'The UPN was interpreted as a URI fragment.' }
        if ([uri]::UnescapeDataString($global:IntunePfxTestUserUri.Query) -notlike '*a#b@contoso.com*') {
            throw 'The complete UPN was not preserved in the Graph filter.'
        }
    }

    It 'creates an idempotent Graph SDK application and returns splattable authentication settings' {
        $graphApplicationId = '00000003-0000-0000-c000-000000000000'
        $applicationId = '33333333-3333-3333-3333-333333333333'
        $tenantId = '22222222-2222-2222-2222-222222222222'
        $graphServicePrincipal = [pscustomobject]@{
            Id = 'graph-sp'
            AppRoles = @(
                [pscustomobject]@{ Id = 'role-device'; Value = 'DeviceManagementConfiguration.ReadWrite.All'; IsEnabled = $true },
                [pscustomobject]@{ Id = 'role-user'; Value = 'User.Read.All'; IsEnabled = $true }
            )
            Oauth2PermissionScopes = @(
                [pscustomobject]@{ Id = 'scope-device'; Value = 'DeviceManagementConfiguration.ReadWrite.All'; IsEnabled = $true },
                [pscustomobject]@{ Id = 'scope-user-all'; Value = 'User.Read.All'; IsEnabled = $true },
                [pscustomobject]@{ Id = 'scope-user'; Value = 'User.Read'; IsEnabled = $true }
            )
        }
        Mock -CommandName Get-Command -ModuleName IntunePfxImport {
            param($Name)
            [pscustomobject]@{ Name = $Name }
        }
        Mock -CommandName Get-IntuneMgGraphContext -ModuleName IntunePfxImport { [pscustomobject]@{ TenantId = '22222222-2222-2222-2222-222222222222' } }
        Mock -CommandName Get-IntuneMgServicePrincipal -ModuleName IntunePfxImport {
            param($Filter)
            if ($Filter -like '*00000003-0000-0000-c000-000000000000*') { return $global:IntunePfxTestGraphServicePrincipal }
            return @()
        }
        Mock -CommandName Get-IntuneMgApplication -ModuleName IntunePfxImport { return @() }
        Mock -CommandName New-IntuneMgApplication -ModuleName IntunePfxImport {
            param($Parameters)
            [pscustomobject]@{
                Id = 'application-object-id'
                AppId = '33333333-3333-3333-3333-333333333333'
                DisplayName = 'Intune PFX Import Test'
                RequiredResourceAccess = $Parameters.RequiredResourceAccess
                PublicClient = $Parameters.PublicClient
                IsFallbackPublicClient = $Parameters.IsFallbackPublicClient
            }
        }
        Mock -CommandName New-IntuneMgServicePrincipal -ModuleName IntunePfxImport {
            param($AppId)
            [pscustomobject]@{ Id = 'client-sp'; AppId = $AppId }
        }

        $result = Initialize-IntunePfxImportApplication -DisplayName 'Intune PFX Import Test' -AuthenticationMode Both -Confirm:$false

        if ($result.ApplicationId -ne $applicationId) { throw 'Application ID was not returned.' }
        if ($result.SetIntuneAuthenticationTokenParameters.ClientId -ne $applicationId) { throw 'Output is not splattable into Set-IntuneAuthenticationToken.' }
        if ($result.SetIntuneAuthenticationTokenParameters.TenantId -ne $tenantId) { throw 'Tenant ID was not returned.' }
        if ($result.AdminConsentUri -notlike "*$applicationId*") { throw 'Admin consent instructions do not identify the application.' }
        Assert-MockCalled -CommandName New-IntuneMgApplication -ModuleName IntunePfxImport -Times 1 -Exactly
        Assert-MockCalled -CommandName New-IntuneMgServicePrincipal -ModuleName IntunePfxImport -Times 1 -Exactly
    }

    It 'validates an existing application without changing Graph tenant objects' {
        $graphApplicationId = '00000003-0000-0000-c000-000000000000'
        $applicationId = '33333333-3333-3333-3333-333333333333'
        $tenantId = '22222222-2222-2222-2222-222222222222'
        $graphServicePrincipal = [pscustomobject]@{
            Id = 'graph-sp'
            AppRoles = @([pscustomobject]@{ Id = 'role-device'; Value = 'DeviceManagementConfiguration.ReadWrite.All'; IsEnabled = $true })
            Oauth2PermissionScopes = @(
                [pscustomobject]@{ Id = 'scope-device'; Value = 'DeviceManagementConfiguration.ReadWrite.All'; IsEnabled = $true },
                [pscustomobject]@{ Id = 'scope-user-all'; Value = 'User.Read.All'; IsEnabled = $true },
                [pscustomobject]@{ Id = 'scope-user'; Value = 'User.Read'; IsEnabled = $true }
            )
        }
        $existingApplication = [pscustomobject]@{
            Id = 'application-object-id'
            AppId = $applicationId
            DisplayName = 'Existing Intune PFX Import'
            RequiredResourceAccess = @()
            PublicClient = [pscustomobject]@{ RedirectUris = @() }
            IsFallbackPublicClient = $false
        }
        Mock -CommandName Get-Command -ModuleName IntunePfxImport {
            param($Name)
            [pscustomobject]@{ Name = $Name }
        }
        Mock -CommandName Get-IntuneMgGraphContext -ModuleName IntunePfxImport { [pscustomobject]@{ TenantId = '22222222-2222-2222-2222-222222222222' } }
        Mock -CommandName Get-IntuneMgServicePrincipal -ModuleName IntunePfxImport {
            param($Filter)
            if ($Filter -like '*00000003-0000-0000-c000-000000000000*') { return $global:IntunePfxTestGraphServicePrincipal }
            return [pscustomobject]@{ Id = 'client-sp'; AppId = '33333333-3333-3333-3333-333333333333' }
        }
        $global:IntunePfxTestExistingApplication = $existingApplication
        Mock -CommandName Get-IntuneMgApplication -ModuleName IntunePfxImport { return $global:IntunePfxTestExistingApplication }
        Mock -CommandName New-IntuneMgApplication -ModuleName IntunePfxImport { throw 'ValidateOnly must not create an application.' }
        Mock -CommandName Update-IntuneMgApplication -ModuleName IntunePfxImport { throw 'ValidateOnly must not update an application.' }
        Mock -CommandName New-IntuneMgServicePrincipal -ModuleName IntunePfxImport { throw 'ValidateOnly must not create a service principal.' }

        $result = Initialize-IntunePfxImportApplication -ExistingApplicationId $applicationId -AuthenticationMode PublicClient -ValidateOnly -Confirm:$false

        if ($result.ChangesRequiredOrApplied -notcontains 'RequiredResourceAccess') { throw 'ValidateOnly did not report missing required permissions.' }
        if ($result.ChangesRequiredOrApplied -notcontains 'PublicClient') { throw 'ValidateOnly did not report public-client configuration.' }
    }

    It 'updates an existing application without losing permissions or redirect URIs' -TestCases @(
        @{ AccessShape = 'null'; PublicClientPresent = $false },
        @{ AccessShape = 'empty'; PublicClientPresent = $true },
        @{ AccessShape = 'other'; PublicClientPresent = $true },
        @{ AccessShape = 'graph'; PublicClientPresent = $true }
    ) {
        param($AccessShape, $PublicClientPresent)

        $otherAccess = [pscustomobject]@{
            ResourceAppId = '44444444-4444-4444-4444-444444444444'
            ResourceAccess = @([pscustomobject]@{ Id = 'other-role'; Type = 'Role' })
        }
        $access = switch ($AccessShape) {
            'null' { $null }
            'empty' { ,@() }
            'other' { @($otherAccess) }
            'graph' {
                @($otherAccess, [pscustomobject]@{
                    ResourceAppId = '00000003-0000-0000-c000-000000000000'
                    ResourceAccess = @([pscustomobject]@{ Id = 'scope-user'; Type = 'Scope' })
                })
            }
        }
        $global:IntunePfxTestExistingApplication = [pscustomobject]@{
            Id = 'application-object-id'
            AppId = '33333333-3333-3333-3333-333333333333'
            DisplayName = 'Existing application'
            RequiredResourceAccess = $access
            PublicClient = if ($PublicClientPresent) {
                [pscustomobject]@{ RedirectUris = @('https://localhost/existing') }
            } else { $null }
            IsFallbackPublicClient = $false
        }
        Mock -CommandName Get-Command -ModuleName IntunePfxImport { [pscustomobject]@{ Name = 'available' } }
        Mock -CommandName Get-IntuneMgGraphContext -ModuleName IntunePfxImport {
            [pscustomobject]@{ TenantId = '22222222-2222-2222-2222-222222222222' }
        }
        Mock -CommandName Get-IntuneMgServicePrincipal -ModuleName IntunePfxImport {
            param($Filter)
            if ($Filter -like '*00000003-0000-0000-c000-000000000000*') {
                return $global:IntunePfxTestGraphServicePrincipal
            }
            [pscustomobject]@{ Id = 'client-sp' }
        }
        Mock -CommandName Get-IntuneMgApplication -ModuleName IntunePfxImport { $global:IntunePfxTestExistingApplication }
        Mock -CommandName New-IntuneMgApplication -ModuleName IntunePfxImport { throw 'Must reuse the existing application.' }
        Mock -CommandName Update-IntuneMgApplication -ModuleName IntunePfxImport {
            param($Parameters)
            $global:IntunePfxTestExistingApplication.RequiredResourceAccess = $Parameters.RequiredResourceAccess
            $global:IntunePfxTestExistingApplication.PublicClient = [pscustomobject]$Parameters.PublicClient
            $global:IntunePfxTestExistingApplication.IsFallbackPublicClient = $Parameters.IsFallbackPublicClient
        }

        $setup = Initialize-IntunePfxImportApplication -ExistingApplicationId '33333333-3333-3333-3333-333333333333' -AuthenticationMode PublicClient -Confirm:$false
        $setup.ApplicationId | Should -Be '33333333-3333-3333-3333-333333333333'
        $updated = $global:IntunePfxTestExistingApplication
        $graphAccess = @($updated.RequiredResourceAccess | Where-Object ResourceAppId -eq '00000003-0000-0000-c000-000000000000')
        $graphAccess.Count | Should -Be 1
        @($graphAccess[0].ResourceAccess).Count | Should -Be 3
        foreach ($id in 'scope-device', 'scope-user-all', 'scope-user') {
            $graphAccess[0].ResourceAccess.Id | Should -Contain $id
        }
        if ($AccessShape -in 'other', 'graph') {
            $preserved = $updated.RequiredResourceAccess | Where-Object ResourceAppId -eq $otherAccess.ResourceAppId
            $preserved.ResourceAccess[0].Id | Should -Be 'other-role'
        }
        $updated.PublicClient.RedirectUris | Should -Contain 'https://login.microsoftonline.com/common/oauth2/nativeclient'
        if ($PublicClientPresent) {
            $updated.PublicClient.RedirectUris | Should -Contain 'https://localhost/existing'
        }
        Assert-MockCalled -CommandName Update-IntuneMgApplication -ModuleName IntunePfxImport -Times 1 -Exactly
        Initialize-IntunePfxImportApplication -ExistingApplicationId $setup.ApplicationId -AuthenticationMode PublicClient -Confirm:$false | Out-Null
        Assert-MockCalled -CommandName Update-IntuneMgApplication -ModuleName IntunePfxImport -Times 1 -Exactly
    }

    It 'fails when an explicit existing application ID is not found' {
        Mock -CommandName Get-Command -ModuleName IntunePfxImport {
            param($Name)
            [pscustomobject]@{ Name = $Name }
        }
        Mock -CommandName Get-IntuneMgGraphContext -ModuleName IntunePfxImport {
            [pscustomobject]@{ TenantId = '22222222-2222-2222-2222-222222222222' }
        }
        Mock -CommandName Get-IntuneMgServicePrincipal -ModuleName IntunePfxImport {
            return $global:IntunePfxTestGraphServicePrincipal
        }
        Mock -CommandName Get-IntuneMgApplication -ModuleName IntunePfxImport { return @() }
        Mock -CommandName New-IntuneMgApplication -ModuleName IntunePfxImport {
            throw 'A missing explicit application must not create a replacement.'
        }

        $errorMessage = $null
        try {
            Initialize-IntunePfxImportApplication `
                -ExistingApplicationId '33333333-3333-3333-3333-333333333333' `
                -AuthenticationMode PublicClient `
                -Confirm:$false
        }
        catch {
            $errorMessage = $_.Exception.Message
        }

        if ($errorMessage -notlike "*No application registration was found for ExistingApplicationId*") {
            throw "Expected a missing-application error but got: $errorMessage"
        }
        Assert-MockCalled -CommandName New-IntuneMgApplication -ModuleName IntunePfxImport -Times 0 -Exactly
    }

    It 'creates a secure one-time client secret only when requested' {
        $graphApplicationId = '00000003-0000-0000-c000-000000000000'
        $applicationId = '33333333-3333-3333-3333-333333333333'
        $tenantId = '22222222-2222-2222-2222-222222222222'
        $graphServicePrincipal = [pscustomobject]@{
            Id = 'graph-sp'
            AppRoles = @(
                [pscustomobject]@{ Id = 'role-device'; Value = 'DeviceManagementConfiguration.ReadWrite.All'; IsEnabled = $true },
                [pscustomobject]@{ Id = 'role-user'; Value = 'User.Read.All'; IsEnabled = $true }
            )
            Oauth2PermissionScopes = @()
        }
        $existingApplication = [pscustomobject]@{
            Id = 'application-object-id'
            AppId = $applicationId
            DisplayName = 'Existing Intune PFX Import'
            RequiredResourceAccess = @(@{ ResourceAppId = $graphApplicationId; ResourceAccess = @(
                [pscustomobject]@{ Id = 'role-device'; Type = 'Role' },
                [pscustomobject]@{ Id = 'role-user'; Type = 'Role' }
            ) })
            PublicClient = [pscustomobject]@{ RedirectUris = @() }
            IsFallbackPublicClient = $false
        }
        $oneTimeSecret = [Guid]::NewGuid().ToString('N')
        Mock -CommandName Get-Command -ModuleName IntunePfxImport {
            param($Name)
            [pscustomobject]@{ Name = $Name }
        }
        Mock -CommandName Get-IntuneMgGraphContext -ModuleName IntunePfxImport { [pscustomobject]@{ TenantId = '22222222-2222-2222-2222-222222222222' } }
        Mock -CommandName Get-IntuneMgServicePrincipal -ModuleName IntunePfxImport {
            param($Filter)
            if ($Filter -like '*00000003-0000-0000-c000-000000000000*') { return $global:IntunePfxTestGraphServicePrincipal }
            return [pscustomobject]@{ Id = 'client-sp'; AppId = '33333333-3333-3333-3333-333333333333' }
        }
        $global:IntunePfxTestExistingApplication = $existingApplication
        $global:IntunePfxTestSecret = $oneTimeSecret
        Mock -CommandName Get-IntuneMgApplication -ModuleName IntunePfxImport { return $global:IntunePfxTestExistingApplication }
        Mock -CommandName Add-IntuneMgApplicationPassword -ModuleName IntunePfxImport {
            param($ApplicationId, $PasswordCredential)
            [pscustomobject]@{ SecretText = $global:IntunePfxTestSecret }
        }
        Mock -CommandName Invoke-RestMethod -ModuleName IntunePfxImport {
            param($Uri)
            if ($Uri -match '/oauth2/v2.0/token$') {
                return [pscustomobject]@{ access_token = 'onboarding-test-token'; expires_in = 3600 }
            }
            return [pscustomobject]@{ value = @() }
        }

        $result = Initialize-IntunePfxImportApplication -ExistingApplicationId $applicationId -AuthenticationMode ClientSecret -CreateClientSecret -Confirm:$false

        if ($result.ClientSecret -isnot [Security.SecureString]) { throw 'The one-time client secret must be returned as a SecureString.' }
        if ($result.AdminConsentInstructions -notmatch 'Privileged Role Administrator or Global Administrator') { throw 'Application-mode consent guidance must require Privileged Role Administrator or Global Administrator.' }
        Set-IntuneAuthenticationToken -Setup $result -Confirm:$false
        Get-IntuneUserPfxCertificate | Out-Null
        Assert-MockCalled -CommandName Add-IntuneMgApplicationPassword -ModuleName IntunePfxImport -Times 1 -Exactly
    }

    It 'does not mutate tenant objects when onboarding is simulated with WhatIf' {
        Mock -CommandName Get-Command -ModuleName IntunePfxImport {
            param($Name)
            [pscustomobject]@{ Name = $Name }
        }
        Mock -CommandName Get-IntuneMgGraphContext -ModuleName IntunePfxImport { [pscustomobject]@{ TenantId = '22222222-2222-2222-2222-222222222222' } }
        Mock -CommandName Get-IntuneMgServicePrincipal -ModuleName IntunePfxImport {
            param($Filter)
            if ($Filter -like '*00000003-0000-0000-c000-000000000000*') { return $global:IntunePfxTestGraphServicePrincipal }
            return @()
        }
        Mock -CommandName Get-IntuneMgApplication -ModuleName IntunePfxImport { return @() }
        Mock -CommandName New-IntuneMgApplication -ModuleName IntunePfxImport { throw 'WhatIf must not create an application.' }
        Mock -CommandName New-IntuneMgServicePrincipal -ModuleName IntunePfxImport { throw 'WhatIf must not create a service principal.' }

        Initialize-IntunePfxImportApplication -DisplayName 'WhatIf Intune PFX Import' -AuthenticationMode Both -WhatIf

    }

    if ($PSVersionTable.PSEdition -eq 'Core') {
        It 'creates an ECC record with ephemeral PFX loading and a TestDrive public key' {
            $ecdsa = [Security.Cryptography.ECDsa]::Create()
            $ecdsa.GenerateKey([Security.Cryptography.ECCurve]::CreateFromFriendlyName('nistP384'))
            $request = [Security.Cryptography.X509Certificates.CertificateRequest]::new(
                'CN=ECC PFX Test',
                $ecdsa,
                [Security.Cryptography.HashAlgorithmName]::SHA384)
            $subjectAlternativeName = [Security.Cryptography.X509Certificates.SubjectAlternativeNameBuilder]::new()
            $subjectAlternativeName.AddEmailAddress('user@contoso.com')
            $request.CertificateExtensions.Add($subjectAlternativeName.Build())
            $certificate = $request.CreateSelfSigned([DateTimeOffset]::UtcNow.AddDays(-1), [DateTimeOffset]::UtcNow.AddDays(1))
            $pfxPath = Join-Path $TestDrive 'ecc.pfx'
            $passwordText = [Guid]::NewGuid().ToString('N')
            [IO.File]::WriteAllBytes($pfxPath, $certificate.Export([Security.Cryptography.X509Certificates.X509ContentType]::Pfx, $passwordText))

            $rsa = [Security.Cryptography.RSA]::Create(2048)
            $base64 = [Convert]::ToBase64String($rsa.ExportSubjectPublicKeyInfo())
            $pem = "-----BEGIN PUBLIC KEY-----`n" + (($base64 -split '(.{1,64})' | Where-Object { $_ }) -join "`n") + "`n-----END PUBLIC KEY-----`n"
            $keyPath = Join-Path $TestDrive 'public.key'
            [IO.File]::WriteAllText($keyPath, $pem)
            $password = ConvertTo-SecureString $passwordText -AsPlainText -Force

            $result = New-IntuneUserPfxCertificate -PathToPfxFile $pfxPath -PfxPassword $password -KeyFilePath $keyPath -IntendedPurpose 1 -PaddingScheme None

            if ($result.keyAlgorithm -ne 'ecc') { throw "Expected ECC keyAlgorithm but got '$($result.keyAlgorithm)'." }
            if ($result.userPrincipalName -ne 'user@contoso.com') { throw 'The UPN was not inferred from the certificate email name.' }
            if ($result.paddingScheme -ne 'oaepSha512') { throw 'Expected OAEP SHA-512 padding.' }
            if ($result.intendedPurpose -ne 'smimeEncryption') { throw 'Version 2 numeric intended purpose was not normalized.' }
            if ([string]::IsNullOrWhiteSpace($result.encryptedPfxPassword)) { throw 'The PFX password was not encrypted.' }
            if ($result.StartDateTime -isnot [DateTimeOffset] -or $result.ExpirationDateTime -isnot [DateTimeOffset]) {
                throw 'Version 2 certificate dates must remain DateTimeOffset values.'
            }
        }
    }
}
