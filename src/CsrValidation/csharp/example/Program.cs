using System;
using System.Diagnostics;
using Microsoft.Intune;

namespace example
{
    class Program
    {
        static void Main(string[] args)
        {
            IntuneScepValidator client = new IntuneScepValidator(
                providerNameAndVersion: "",   // A string that uniquely identifies your Certificate Authority and any version info for your app.
                intuneTenant: "",             // Tenant name i.e. contoso.onmicrosoft.com
                azureAppId: "",               // Application ID of Active Directory application in the tenants azure subscription.
                azureAppKey: "",              // Application secret to be used for authentication of tenant.
                trace:new TraceSource("log")
            );

            Guid transactionId = Guid.NewGuid();

            // NOTE: The CSR should be in Base64 encoding format
            String csr = "";

            (client.ValidateRequestAsync(transactionId.ToString(), csr)).Wait();

            Console.WriteLine("No exceptions mean CSR was validated! Press any key to continue...");
            Console.ReadKey();
        }
    }
}
