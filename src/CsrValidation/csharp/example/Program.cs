using System;
using System.Collections.Generic;
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
                "", 
                "", 
                "", 
                "", 
                graphResourceUrl: "https://graph.ppe.windows.net/", 
                intuneResourceUrl: "https://api.manage-dogfood.microsoft.com/", 
                authAuthority:"https://login.windows-ppe.net/"
            );

            Guid transactionId = Guid.NewGuid();
            (client.ValidateRequestAsync(transactionId.ToString(), "testing")).Wait();

            Console.ReadKey();
        }
    }
}
