# PFXImport Powershell Project

This project consists of helper Powershell Commandlets for importing PFX certificates to Microsoft Intune.

# Example Usage

## Prerequisite:
	Import Microsoft.Management.Powershell.PFXImport.dll

## Create initial Key Example

	# 1. Setup Key
	Add-IntuneKspKey "Microsoft Software Key Storage Provider" "RSA" "<KeyName>"
	
## Get Base64 String Certificate Example

	# 1. Setup Secure File Password
	$SecureFilePassword = ConvertTo-SecureString -String "<PFXPassword>" -AsPlainText -Force
	# 2. Get Base64 String Certificate
	$Base64Certificate =ConvertTo-IntuneBase64EncodedPfxCertificate -CertificatePath "<FullPathPFXToCert>"
	# 3. Base64 String
	$userPFXObject = New-IntuneUserPfxCertificate -Base64EncodedPFX $Base64Certificate $SecureFilePassword "<UserUPN>" "Microsoft Software Key Storage Provider" "<KeyName>"
	

## Encrypt + Import Example

	# 1. Get-AuthToken
	$authResult = Get-IntuneAuthenticationToken -AdminUserName "<UserUPN>"
	# 2. Setup Secure File Password
	$SecureFilePassword = ConvertTo-SecureString -String "<PFXPassword>" -AsPlainText -Force
	# 3. Encrypt PFX File
	$userPFXObject = New-IntuneUserPfxCertificate "<FullPathPFXToCert>" $SecureFilePassword "<UserUPN>" "Microsoft Software Key Storage Provider" "<KeyName>"
	# 4. Import User PFX
	Import-IntuneUserPfxCertificate -AuthenticationResult $authResult -CertificateList $userPFXObject
	
## Get PFX Certificate By Thumbprint Example

	# 1. Get-AuthToken
	$authResult = Get-IntuneAuthenticationToken  -AdminUserName "<UserUPN>"
	# 2. Get-PfxCertificates
	Get-IntuneUserPfxCertificate -AuthenticationResult $authResult -ThumbprintList "<PFXThumbprint>"


## Remove PFX Certificate By Thumbprint Example

	# 1. Get-AuthToken
	$authResult = Get-IntuneAuthToken -AdminUserName "<UserUPN>"
	# 2. Remove-PfxCertificates
	Remove-IntuneUserPfxCertificate -AuthenticationResult $authResult -ThumbprintList "<PFXThumbprint>"
