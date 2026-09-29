@{
    RootModule = 'IntunePfxImport.psm1'
    ModuleVersion = '3.0.0'
    GUID = '40fbdf09-4bde-43a1-9a37-151640986ccc'
    Author = 'Microsoft'
    CompanyName = 'Microsoft'
    Copyright = '(c) Microsoft. All rights reserved.'
    Description = 'PowerShell functions for importing PFX certificates into Microsoft Intune.'
    PowerShellVersion = '5.1'
    DotNetFrameworkVersion = '4.7.2'
    CompatiblePSEditions = @('Desktop', 'Core')
    FunctionsToExport = @(
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
    CmdletsToExport = @()
    VariablesToExport = @()
    AliasesToExport = @()
    PrivateData = @{
        # Version 2 compatibility only. Prefer command-line parameters or -Setup.
        ClientId = ''
        ClientSecret = ''
        TenantId = ''
        AuthURI = 'login.microsoftonline.com'
        GraphURI = 'https://graph.microsoft.com'
        SchemaVersion = 'beta'
        RedirectURI = 'https://login.microsoftonline.com/common/oauth2/nativeclient'
    }
}
