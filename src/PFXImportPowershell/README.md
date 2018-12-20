# PFXImport Powershell Project

*This project is currently in pre-release. Not all features may be functional.*

This project consists of helper Powershell Commandlets for importing PFX certificates to Microsoft Intune.

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
```
Import-Module .\IntunePfxImport.psd1
```

## Create initial Key Example
1. Setup Key -- Convenience method for creating a key. Key's may be created with other tools. If you don't have a dedicated provider, you can use "Microsoft Software Key Storage Provider".
```
Add-IntuneKspKey "<ProviderName>" "<KeyName>"
```
	
## Authenticate to Intune
1. Optionally, create a secure string representing the account administrator password.
```
$secureAdminPassword = ConvertTo-SecureString -String "<admin password>" -AsPlainText -Force
```
2. Authenticate as the account administrator (using the admin UPN) to Intune. If the password is not provided a login dialog will appear.
```
$authResult = Get-IntuneAuthenticationToken -AdminUserName "<Admin-UPN>" [-AdminPassword $secureAdminPassword]
```

## Set up userPFXCertifcate object (including encrypting password)
1. Setup Secure File Password string.
```
$SecureFilePassword = ConvertTo-SecureString -String "<PFXPassword>" -AsPlainText -Force
```
2. Format a Base64 encoded certificate.
```
$Base64Certificate =ConvertTo-IntuneBase64EncodedPfxCertificate -CertificatePath "<FullPathPFXToCert>"
```
3. Create a new UserPfxCertificate record.
```
$userPFXObject = New-IntuneUserPfxCertificate -Base64EncodedPFX $Base64Certificate $SecureFilePassword "<UserUPN>" "<ProviderName>" "<KeyName>" "<IntendedPurpose>" "<PaddingScheme>"
```
or 
```
$userPFXObject = New-IntuneUserPfxCertificate -PathToPfxFile "<FullPathPFXToCert>" $SecureFilePassword "<UserUPN>" "<ProviderName>" "<KeyName>" "<IntendedPurpose>" "<PaddingScheme>"
```

## Import Example
1. Import User PFX
```
Import-IntuneUserPfxCertificate -AuthenticationResult $authResult -CertificateList $userPFXObject
```

## Get PFX Certificate Example
1. Get-PfxCertificates (Specific records)
```
Get-IntuneUserPfxCertificate -AuthenticationResult $authResult -UserThumbprintList <UserThumbprintObjs>
```
2. Get-PfxCertificates (Specific users)
```
Get-IntuneUserPfxCertificate -AuthenticationResult $authResult -UsertList "<UserUPN>"
```
3. Get-PfxCertificates (All records)
```
Get-IntuneUserPfxCertificate -AuthenticationResult $authResult
```

## Remove PFX Certificate Example
1. Remove-PfxCertificates (Specific records)
```
Remove-IntuneUserPfxCertificate -AuthenticationResult $authResult -UserThumbprintList <UserThumbprintObjs>
```
2. Remove-PfxCertificates (Specific users)
```
Remove-IntuneUserPfxCertificate -AuthenticationResult $authResult -UsertList "<UserUPN>"
```

# Graph Usage

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
		"@odata.type": "#microsoft.graph.userPFXCertificate",
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

- The public key used for encryption's equvalent private key must be accessible to the account that is running the "PFX Certificate Connector for Microsoft Intune" service for decryption to work. This is normally the "NT AUTHORITY\System" account.

# Other Useful graph examples

## Lookup up user id from UPN
	
	GET
	https://graph.microsoft.com/beta/users?$filter=userPrincipalName eq '{UPN}'

The user id is found in the id value of the returned object.
