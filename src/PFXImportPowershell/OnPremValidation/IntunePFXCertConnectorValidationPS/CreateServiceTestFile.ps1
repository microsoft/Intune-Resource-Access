param(
	[string] $serviceDirectory = "..\IntunePFXCertConnectorValidationService\bin\x64\Debug\",
	[string] $plainSecret,
	[string] $encryptedSecretBase64,
	[int] $keyLength = 2048,
	[string] $keyName = "PfxImportRecryptionTestValidationKey",
	[string] $hashAlgorithm = "SHA512",
	[int] $paddingFlags = 4,
	[string] $provider = "Microsoft Software Key Storage Provider"
)

Set-Content "$($serviceDirectory)\PFXImportTestFile.txt" "$($plainsecret):$($encryptedSecretBase64):$($provider):$($keyName):$($hashAlgorithm):$($paddingFlags)"