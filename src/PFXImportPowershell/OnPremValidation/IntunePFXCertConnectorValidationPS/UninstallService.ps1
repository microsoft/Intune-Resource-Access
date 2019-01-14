#Uninstalls the test service (rather, the one with the given serviceName) from the machine
param(
	[string]$serviceName = "PFXImportDecryptTest"
)

$service = Get-WmiObject -Class Win32_Service -Filter "name='$serviceName'"
$service.stopservice()
$service.delete()
