# PFXImport Powershell Project

This project consists of helper Powershell Commandlets for importing PFX certificates to Microsoft Intune. Prior to running these scripts you will need to create the PFX files to import. Further documentation of the feature can be found [here](https://docs.microsoft.com/en-us/intune/certificates-s-mime-encryption-sign).

These scripts provide a baseline for the actions that can take place to import your PFX Certificates to Intune. They can be modified and adapted to fit your workflow. Most of the cmdlets are wrappers of Intune Graph calls.

## What's New?
### Version 1.1
- Added functionality to make private keys exportable, a cmdlet to export the key, and a cmdlet to import a key.
	- Allows migrating connectors when using the Microsoft Software Key Storage Provider.
	- Serious security considerations needs to be taken when transferring keys between machines.
- Deprecated the Get-IntuneAuthenticationToken cmdlet in favore of the new Set-IntuneAuthenticationToken to store the authentication token so that it isn't required as a parameter on every call that interacts with Intune.
	- Calling Remove-IntuneAuthenticationToken or closing the session is recommended when calls to Intune are complete.

# Building the Commandlets
## Prerequisite
Visual Studio 2015 (or above)

[Graph Permissions](https://developer.microsoft.com/en-us/graph/docs/concepts/permissions_reference) required:

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


## Building
1. Load .\PFXImportPS.sln in Visual Studio
2. Select the appropriate build configuration (Debug or Release)
3. Build solution

# Example Powershell Usage

## Prerequisite:
1. Import the built powershell module. This usually is found in the "bin\debug" or "bin\release" directory.
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
1. Optionally, create a secure string representing the account administrator password.
```
$secureAdminPassword = ConvertTo-SecureString -String "<admin password>" -AsPlainText -Force
```
2. Authenticate as the account administrator (using the admin UPN) to Intune. If the password is not provided a login dialog will appear.
```
Set-IntuneAuthenticationToken -AdminUserName "<Admin-UPN>" [-AdminPassword $secureAdminPassword]
```
3. Make sure the call Remove-IntuneAuthenticationToken to clear the token cache when all interation with Intune is complete.

## Set up userPFXCertifcate object (scenario: encrypting password from a location that has acccess to the private key in the key store) 
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
```
Remove-IntuneAuthenticationToken
```

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
