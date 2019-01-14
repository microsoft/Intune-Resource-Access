This part of the project is to verify that your on prem infrastucture properly interfaces with our service.  Note that these tests require installing a service as well as running scripts as admin, so make sure to verify their contents before building and running them.

The PFX Certificate Connector runs on the server as a System Account, but needs access to the Keys that were used to encrypt PFX Certificates.  These tests verify that, once installed, the PFX Certificate Connector will be able to do that.  The tests consist of three steps:

1. Creating a key and encrypting some test data
2. Dropping the encrypted data and what was used to encrypt
3. Installing and running the service, verifying that the service can correctly decrypt the data

Before running the script you will need to build the test executables and libraries used for the test. This can be done by opening the PFXImportOnPremValidation.sln solution with Visual Studio found in the PFXImportPowershell parent folder and buiding it.

There are two ways to handle encrypting the data.  If using our scripts, we've provided a helper script below (please note this script deletes any previous keys with the given name!)
```powershell
$serviceDirectory = "{PathToTheServiceBinaries}"

$encryptedPasswordResults = .\EncryptPlaintextPassword.ps1 -serviceDirectory $serviceDirectory #[-keyLength {LengthOfKey}] [-keyName {NameOfKey}] [-hashAlgorithm {TypeOfHashingAlgorithm}] [-paddingFlags {PaddingFlags}] [-provider {KeyStorageProvider}] [-plainSecret {SecretPlaintextString}]

$secretAsPlain = $encryptedPasswordResults[0] #This is the password that was encrypted as a plaintext string, for verification purposes

$secretAsEncrypted = $encryptedPasswordResults[1] #This is the password after encryption as a Base64 encoded string
```

If you're using a different method to encrypt for PFX Import, use your own scripts and keep track of all the options used so they can be passed to the service in the test file.  Make sure the encrypted password is in Base64 format.

To write the encrypted data to a test value the service can check, run:
```powershell
.\CreateServiceTestFile.ps1 -serviceDirectory $serviceDirectory -plainSecret $secretAsPlain -encryptedSecretBase64 $secretAsEncrypted #[-keyLength {LengthOfKey}] [-keyName {NameOfKey}] [-hashAlgorithm {TypeOfHashingAlgorithm}] [-paddingFlags {PaddingFlags}] [-provider {KeyStorageProvider}]
```

To install the service and start it running:
```powershell
.\InstallAndRunTestService.ps1 -serviceDirectory $serviceDirectory #[-testResultsFileName {FileNameOfTheTestResults}] [-serviceName {ServiceFriendlyName}] [-serviceDescription {FriendlyDescriptionForTheService}]
```

The script will wait for the service to check the file, and log out whether the service passed or failed.

To remove the service after finishing testing:
```powershell
.\UninstallService.ps1 [-serviceName {ServiceFriendlyName}]
```
