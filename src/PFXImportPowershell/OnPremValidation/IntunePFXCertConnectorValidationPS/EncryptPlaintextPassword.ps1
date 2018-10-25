param(
	[string] $serviceDirectory,
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
		Write-Error "Key Creation failed, it likely already exists"
	}
}
catch {
    Write-Warning $_ 
}

$encryptedSecret = $manRSAObj.EncryptWithLocalKey($provider, $keyName, $plainSecretBytes, $hashAlgorithm, $paddingFlags)
$encryptedSecretb64 = [System.Convert]::ToBase64String($encryptedSecret)

return $plainSecret,$encryptedSecretb64