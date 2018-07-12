using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using lib;

namespace example
{
    class Program
    {
        static void Main(string[] args)
        {
            IntuneScepServiceClient client = new IntuneScepServiceClient(
                providerNameAndVersion: "",
                intuneTenant: "",
                azureAppId: "",
                azureAppKey: "",
                trace:new TraceSource("log")
            );

            Guid transactionId = Guid.NewGuid();
            String csr = "";

            (client.ValidateRequestAsync(transactionId.ToString(), csr)).Wait();

            Console.WriteLine("No exceptions mean CSR was validated! Press any key to continue...");
            Console.ReadKey();
        }
    }
}
