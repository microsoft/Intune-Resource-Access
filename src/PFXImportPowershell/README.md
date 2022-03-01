# PFXImport Powershell Project

This project consists of helper Powershell Commandlets for importing PFX certificates to Microsoft Intune. Prior to running these scripts you will need to create the PFX files to import. Further documentation of the feature can be found [here](https://docs.microsoft.com/en-us/intune/certificates-s-mime-encryption-sign).

These scripts provide a baseline for the actions that can take place to import your PFX Certificates to Intune. They can be modified and adapted to fit your workflow. Most of the cmdlets are wrappers of Intune Graph calls.

## What's New?

### Version 1.2
- Breaking changes:
	- The global Intune app registration is deprecated and its client ID has been removed from these scripts.  A tenant-specific app registration must be created and its client ID added to your IntunePfxImport.psd1 file.
	- The previously deprecated Get-IntuneAuthenticationToken command has been removed.  Use Set-IntuneAuthenticationToken instead.  The associated AuthenticationResult parameter has also been removed from the other various commands.
	- Changed the default redirect uri to https://login.microsoftonline.com/common/oauth2/nativeclient as recommended by Microsoft Azure.  
- Added the ability to authenticate using a client secret instead of user authentication. This is configured in the app registration and IntunePfxImport.psd1 file.  
- Switched the underlying authentication library from ADAL (which will soon be unsupported) to MSAL.

### Version 1.1
- Added functionality to make private keys exportable, a cmdlet to export the key, and a cmdlet to import a key.
	- Allows migrating connectors when using the Microsoft Software Key Storage Provider.
	- Serious security considerations needs to be taken when transferring keys between machines.
- Deprecated the Get-IntuneAuthenticationToken cmdlet in favore of the new Set-IntuneAuthenticationToken to store the authentication token so that it isn't required as a parameter on every call that interacts with Intune.
	- Calling Remove-IntuneAuthenticationToken or closing the session is recommended when calls to Intune are complete.

# Configure a Microsoft Azure App Registration

An app registration must be configured for your tenant.  Create the app registration in the Microsoft Azure portal.

Set the redirect uri for the Public client/native (mobile & desktop) platform to https://login.microsoftonline.com/common/oauth2/nativeclient
- The redirect uri used by scripts can optionally be modified by adding the "RedirectURI" setting to the PrivateData section of your IntunePfxImport.psd1 file

The following Microsoft Graph API permissions are required:

1. DeviceManagementServiceConfig.ReadWrite.All
2. DeviceManagementServiceConfig.Read.All
3. DeviceManagementConfiguration.ReadWrite.All
4. DeviceManagementConfiguration.Read.All
5. DeviceManagementApps.ReadWrite.All
6. DeviceManagementApps.Read.All
7. DeviceManagementRBAC.ReadWrite.All
8. DeviceManagementRBAC.Read.All
9. DeviceManagementManagedDevices.PriviligedOperation.All
10. DeviceManagementManagedDevices.ReadWrite.All
11. DeviceManagementManagedDevices.Read.All

Additionally required when using user-based authentication:

12. User.Read

Additionally required for Remove-IntuneUserPfxCertificate:

13. User.ReadWrite.All

Add these permissions as delegated permissions when using user-based authentication or application permissions when using an application client secret.  Grant admin consent for the permissions.

If using user-based authentication in a non-interactive session (by specifying the password on the PowerShell command line), you must enable the “Allow public client flows” setting on the app registration "Authentication" page.

If using an application client secret for authentication, create the secret under "Certificates & secrets".  Save the secret before leaving the page, as you will not be able to view it again later.  The secret value will be provided in the IntunePfxImport.psd1 file (see details below).

# Building the Commandlets
## Prerequisite
Visual Studio 2019 (or above)

## Building
1. Load .\PFXImportPS.sln in Visual Studio
2. Select the appropriate build configuration (Debug or Release)
3. Build solution

# Example Powershell Usage

## Prerequisite:

1. Update the IntunePfxImport.psd1 file with details about your app registration. This usually is found in the "bin\debug" or "bin\release" directory.

- Set the ClientId setting to the "application (client) Id" from the app registration "Overview" page.
- If using an application client secret:
	- Create the secret in your app registration and specify it in the ClientSecret setting in IntunePfxImport.psd1.  Be sure to keep this file secure.
	- The TenantId setting is also required when using a client secret.  This value can be found on the app registration "Overview" page.

2. Import the built powershell module. 
```
Import-Module .\IntunePfxImport.psd1
```

## Create initial Key Example
1. Setup Key -- Convenience method for creating a key. Key's may be created with other tools. If you don't have a dedicated provider, you can use "Microsoft Software Key Storage Provider". Only include the MakeExportable switch if you must have the ability to move the key to another machine.
```
Add-IntuneKspKey "<ProviderName>" "<KeyName>" {-MakeExportable}
```

## Export the public key to a file
1. Export the public key. Used to encrypt in an independent location from where the private key is accessed. Set "Set up userPFXCertificate object (scenario: encrypting password with the public key that has been exported to a file)" below.
```
Export-IntunePublicKey -ProviderName "<ProviderName>" -KeyName "<KeyName>" -FilePath "<File path to write to>"
```

## Export the private key to a file 
1. Export the private key. For use when migrating connector and moving keys between machines.
```
Export-IntunePublicKey -ProviderName "<ProviderName>" -KeyName "<KeyName>" -FilePath "<File path to write to>" {-MakeExportable}
```

## Import the private key from a file
1. Import the private key. For use when migrating connector and moving keys between machines.
```
Import-IntunePublicKey -ProviderName "<ProviderName>" -KeyName "<KeyName>" -FilePath "<File path to write to>"
```

## Authenticate to Intune

### User Authentication with interactive login

1. Authenticate as the account administrator (using the admin UPN) to Intune. Specify the AdminUserName on the command line, but not the AdminPassword.  An interactive login dialog will appear.
```
Set-IntuneAuthenticationToken -AdminUserName "<Admin-UPN>" 
```
2. Make sure the call Remove-IntuneAuthenticationToken to clear the token cache when all interation with Intune is complete.  Close the PowerShell session to remove credentials cached by the interactive browser.


### User authentication with non-interactive login

Prerequisite: to use this option, enable “Allow public client flows” setting on the app registration "Authentication" page.

1. Optionally, create a secure string representing the account administrator password.
```
$secureAdminPassword = ConvertTo-SecureString -String "<admin password>" -AsPlainText -Force
```
2. Authenticate as the account administrator (using the admin UPN) to Intune.  Provide both the user name and the password on the command line.
```
Set-IntuneAuthenticationToken -AdminUserName "<Admin-UPN>" [-AdminPassword $secureAdminPassword]
```
3. Make sure the call Remove-IntuneAuthenticationToken to clear the token cache when all interation with Intune is complete.

### Application client secret authentication

Prerequisite: create the client secret in the app registration and configure the IntunePfxImport.psd1 file.

1. Set-IntuneAuthenticationToken will use configured client secret settings if it is called with no parameters
```
Set-IntuneAuthenticationToken
```

## Set up userPFXCertificate object (scenario: encrypting password from a location that has acccess to the private key in the key store) 
1. Setup Secure File Password string.
```
$SecureFilePassword = ConvertTo-SecureString -String "<PFXPassword>" -AsPlainText -Force
```
2. (Optional) Format a Base64 encoded certificate.
```
$Base64Certificate =ConvertTo-IntuneBase64EncodedPfxCertificate -CertificatePath "<FullPathPFXToCert>"
```
3. Create a new UserPfxCertificate record.
```
$userPFXObject = New-IntuneUserPfxCertificate -Base64EncodedPFX $Base64Certificate -PfxPassword $SecureFilePassword -UPN "<UserUPN>" -ProviderName "<ProviderName>" -KeyName "<KeyName>" -IntendedPurpose "<IntendedPurpose>" {-PaddingScheme "<PaddingScheme>"}
```
or 
```
$userPFXObject = New-IntuneUserPfxCertificate -PathToPfxFile "<FullPathPFXToCert>" -PfxPassword $SecureFilePassword -UPN "<UserUPN>" -ProviderName "<ProviderName>" -KeyName "<KeyName>" -IntendedPurpose "<IntendedPurpose>" {-PaddingScheme "<PaddingScheme>"}
```

## Set up userPFXCertificate object (scenario: encrypting password with the public key that has been exported to a file) 
1. Setup Secure File Password string.
```
$SecureFilePassword = ConvertTo-SecureString -String "<PFXPassword>" -AsPlainText -Force
```
2. (Optional) Format a Base64 encoded certificate.
```
$Base64Certificate =ConvertTo-IntuneBase64EncodedPfxCertificate -CertificatePath "<FullPathPFXToCert>"
```
3. Create a new UserPfxCertificate record.
```
$userPFXObject = New-IntuneUserPfxCertificate -Base64EncodedPFX $Base64Certificate -PfxPassword $SecureFilePassword -UPN "<UserUPN>" -ProviderName "<ProviderName>" -KeyName "<KeyName>" -IntendedPurpose "<IntendedPurpose>" -KeyFilePath "<File path to public key file>"
```
or 
```
$userPFXObject = New-IntuneUserPfxCertificate -PathToPfxFile "<FullPathPFXToCert>" -PfxPassword $SecureFilePassword -UPN "<UserUPN>" -ProviderName "<ProviderName>" -KeyName "<KeyName>" -IntendedPurpose "<IntendedPurpose>" -KeyFilePath "<File path to public key file>"
```

## Import Example
1. Import User PFX
```
Import-IntuneUserPfxCertificate -CertificateList $userPFXObject
```

## Get PFX Certificate Example
1. Get-PfxCertificates (Specific records)
```
Get-IntuneUserPfxCertificate -UserThumbprintList <UserThumbprintObjs>
```
2. Get-PfxCertificates (Specific users)
```
Get-IntuneUserPfxCertificate -UsertList "<UserUPN>"
```
3. Get-PfxCertificates (All records)
```
Get-IntuneUserPfxCertificate
```

## Remove PFX Certificate Example
1. Remove-PfxCertificates (Specific records)
```
Remove-IntuneUserPfxCertificate -UserThumbprintList <UserThumbprintObjs>
```
2. Remove-PfxCertificates (Specific users)
```
Remove-IntuneUserPfxCertificate -UsertList "<UserUPN>"
```

## Remove Authentication Token from session (logout)
To unselect the authentication token:
```
Remove-IntuneAuthenticationToken
```
Note: To clear all caches used internally by MSAL APIs, also close the PowerShell session.

# Graph Usage
See [UserPFXCertificate Graph resource type](https://docs.microsoft.com/en-us/graph/api/resources/intune-raimportcerts-userpfxcertificate?view=graph-rest-beta)

## GET
A specific record
```
https://graph.microsoft.com/beta/deviceManagement/userPfxCertificates('{Userid}-{Thumbprint}')  
```
A specific User
```
https://graph.microsoft.com/beta/deviceManagement/userPfxCertificates/?$filter=tolower(userPrincipalName) eq '{lowercase UPN}'
```
All records
```
https://graph.microsoft.com/beta/deviceManagement/userPfxCertificates
```

## POST
	
	https://graph.microsoft.com/beta/deviceManagement/userPfxCertificates
 
with an example payload:
 
	{
		"id": "",
		"thumbprint": "f6f5f8f6-f8f6-f6f5-f6f8-f5f6f6f8f5f6",
		"intendedPurpose": "smimeEncryption",
		"userPrincipalName": "User1@contoso.onmicrosoft.com",
		"startDateTime": "2016-12-31T23:58:46.7156189-07:00",
		"expirationDateTime": "2016-12-31T23:57:57.2481234-07:00",
		"providerName": "Microsoft Software Key Storage Provider",
		"keyName": "KeyNameValue",
		"paddingScheme": "oaepSha512",
		"encryptedPfxBlob": "MIIaHR0cHM6Ly93d3cuYmFzZTY0ZW5jb2RlLm.......",
		"encryptedPfxPassword": ".......0dHBzOi8vd3d3LmJhc2U2NGVuY29kZS5vcm==",
		"createdDateTime": "2017-01-01T00:02:43.5775965-07:00",
		"lastModifiedDateTime": "2017-01-01T00:00:35.1329464-07:0"
	}

## PATCH

	https://graph.microsoft.com/beta/deviceManagement/userPfxCertificates('{UserId}-{Thumbprint}')

For payload, see above example.

## DELETE
	
	https://graph.microsoft.com/beta/deviceManagement/userPfxCertificates('{UserId}-{Thumbprint}')


# Notes
- While encryptedPfxBlob and encryptedPfxPassword must be provided when a UserPFXCertificate record is imported, those values will be returned empty in any get call.

	A returned json object will be similar to this:

		{
			"id": "5ffff976dffffe49affff8978fffff25-0ffff8962ffffdea9ffff8e83ffff1d83ffff6ae",
			"thumbprint": "0ffff8962ffffdea9ffff8e83ffff1d83ffff6ae",
			"intendedPurpose": "smimeEncryption",
			"userPrincipalName": "User1@contoso.onmicrosoft.com",
			"startDateTime": "2016-12-31T23:58:46.7156189-07:00",
			"expirationDateTime": "2016-12-31T23:57:57.2481234-07:00",
			"providerName": "Microsoft Software Key Storage Provider",
			"keyName": "KeyNameValue",
			"paddingScheme": "oaepSha512",
			"encryptedPfxBlob": "AA==",
			"encryptedPfxPassword": "",
			"createdDateTime": "2017-01-01T00:02:43.5775965-07:00",
			"lastModifiedDateTime": "2017-01-01T00:00:35.1329464-07:0"
		}

- The public key used for encryption's equivalent private key must be accessible to the account that is running the "PFX Certificate Connector for Microsoft Intune" service for decryption to work. This is normally the "NT AUTHORITY\System" account. See the [OnPremValidation project](OnPremValidation) for testing access.

# Other Useful graph examples

## Lookup up user id from UPN
	
	GET
	https://graph.microsoft.com/beta/users?$filter=userPrincipalName eq '{UPN}'

The user id is found in the id value of the returned object.
