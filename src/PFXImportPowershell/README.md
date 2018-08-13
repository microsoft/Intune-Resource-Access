# PFXImport Powershell Project

*This project is currently in pre-release. Not all features may be functional.*

This project consists of helper Powershell Commandlets for importing PFX certificates to Microsoft Intune.

# Example Powershell Usage

## Prerequisite:
	Import-Module IntunePfxImport.psd1

## Create initial Key Example

	# 1. Setup Key (if you don't have a dedicated provider, you can use "Microsoft Software Key Storage Provider")
	Add-IntuneKspKey "<ProviderName>" "<KeyAlgoritm>" "<KeyName>"
	
## Authenticate to Intune
    # 1. Optionally, create a secure string representing the account administrator password.
    $secureAdminPassword = ConvertTo-SecureString -String "<admin password>" -AsPlainText -Force
    # 2. Authenticate as the account administrator (using the admin UPN) to Intune.
    $authResult = Get-IntuneAuthenticationToken -AdminUserName "<Admin-UPN>" [-AdminPassword $secureAdminPassword]

## Set up userPFXCertifcate object (including encrypting password)

	# 1. Setup Secure File Password
	$SecureFilePassword = ConvertTo-SecureString -String "<PFXPassword>" -AsPlainText -Force
	# 2. Get Base64 String Certificate
	$Base64Certificate =ConvertTo-IntuneBase64EncodedPfxCertificate -CertificatePath "<FullPathPFXToCert>"
	# 3. Base64 String
	$userPFXObject = New-IntuneUserPfxCertificate -Base64EncodedPFX $Base64Certificate $SecureFilePassword "<UserUPN>" "<ProviderName>" "<KeyName>" "IntendedPurpose" "PaddingScheme"
	

## Import Example

	# 1. Import User PFX
	Import-IntuneUserPfxCertificate -AuthenticationResult $authResult -CertificateList $userPFXObject
	
## Get PFX Certificate By Thumbprint Example

	# 1. Get-PfxCertificates (Specific records)
	Get-IntuneUserPfxCertificate -AuthenticationResult $authResult -UserThumbprintList <UserThumbprintObjs>
	# 2. Get-PfxCertificates (Specific users)
	Get-IntuneUserPfxCertificate -AuthenticationResult $authResult -UsertList "<UserUPN>"
	# 3. Get-PfxCertificates (All records)
	Get-IntuneUserPfxCertificate -AuthenticationResult $authResult


## Remove PFX Certificate By Thumbprint Example

	# 1. Remove-PfxCertificates (Specific records)
	Remove-IntuneUserPfxCertificate -AuthenticationResult $authResult -UserThumbprintList <UserThumbprintObjs>
	# 2. Remove-PfxCertificates (Specific users)
	Remove-IntuneUserPfxCertificate -AuthenticationResult $authResult -UsertList "<UserUPN>"



# Graph Usage

## GET
	https://graph.microsoft.com/beta/deviceManagement/userPfxCertificates('{Userid}-{Thumbprint}')  --A specific record
	https://graph.microsoft.com/beta/deviceManagement/userPfxCertificates/?$filter=userPrincipalName eq '{UPN}' –-A specific User
	https://graph.microsoft.com/beta/deviceManagement/userPfxCertificates --All records

## POST
	https://graph.microsoft.com/beta/deviceManagement/userPfxCertificates
 
	with an example payload:
 
	{
		"@odata.type": "#microsoft.graph.userPFXCertificate",
		"id": "",
		"thumbprint": "f6f51856-1856-f6f5-5618-f5f65618f5f6",
		"intendedPurpose": "smimeEncryption",
		"userPrincipalName": "User1@contoso.onmicrosoft.com",
		"startDateTime": "2016-12-31T23:58:46.7156189-07:00",
		"expirationDateTime": "2016-12-31T23:57:57.2481234-07:00",
		"providerName": "Provider Name value",
		"keyName": "Key Name value",
		"encryptedPfxBlob": "{Base64Encrypted Blob}",
		"encryptedPfxPassword": "{Base64Encrypted Blob}",
		"createdDateTime": "2017-01-01T00:02:43.5775965-07:00",
		"lastModifiedDateTime": "2017-01-01T00:00:35.1329464-07:0"
	}

## PATCH
	https://graph.microsoft.com/beta/deviceManagement/userPfxCertificates('{UserId}-{Thumbprint}')

	For payload, see above example.

## DELETE
	https://graph.microsoft.com/beta/deviceManagement/userPfxCertificates('{UserId}-{Thumbprint}')


# Other Useful graph examples

## Lookup up user id from UPN
	GET
	https://graph.microsoft.com/test_intune_1806/users?$filter=userPrincipalName eq '{UPN}'

	The user id is found in the id value of the returned object.
