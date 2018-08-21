#Uninstalls the test service (rather, the one with the given serviceName) from the machine
params(
	[string]$serviceName = "DecryptTest"
)

$service = Get-WmiObject -Class Win32_Service -Filter "name='$serviceName'"
$service.stopservice()
$service.delete()
