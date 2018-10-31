param(
	[string] $serviceDirectory = "..\IntunePFXCertConnectorValidationService\bin\x64\Debug\",
	[int] $keyLength = 2048,
	[string] $keyName = "PfxImportRecryptionTestValidationKey",
	[string] $hashAlgorithm = "SHA512",
	[int] $paddingFlags = 4,
	[string] $provider = "Microsoft Software Key Storage Provider",
	[string] $plainSecret = "This is a test secret"
)

#encode plaintext password to byte configuration
$encoder = [system.Text.Encoding]::UTF8
$plainSecretBytes = $encoder.GetBytes($plainSecret)

Add-Type -Path (Join-Path $serviceDirectory "Microsoft.Intune.EncryptionUtilities.dll")

[Microsoft.Intune.EncryptionUtilities.ManagedRSAEncryption] $manRSAObj = [Microsoft.Intune.EncryptionUtilities.ManagedRSAEncryption]::new()

try {
	#Generate the key.
	if(!$manRSAObj.TryGenerateLocalRSAKey($provider, $keyName, $keylength))
	{
		Write-Warning "Key Creation failed, it likely already exists"
	}
}
catch {
    Write-Warning $_
    Write-Warning $StackTrace
}

try {
	$encryptedSecret = $manRSAObj.EncryptWithLocalKey($provider, $keyName, $plainSecretBytes, $hashAlgorithm, $paddingFlags)
}
catch {
	Write-Warning $_
	Write-Warning $StackTrace
}
$encryptedSecretb64 = [System.Convert]::ToBase64String($encryptedSecret)

return $plainSecret,$encryptedSecretb64