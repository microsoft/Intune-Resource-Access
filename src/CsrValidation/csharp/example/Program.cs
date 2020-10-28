using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Microsoft.Intune;
using Microsoft.Management.Services.Api;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            // If scenario requires the use of a proxy with authentication then you need to supply your own WebProxy like the following.
            string proxyHost = "http://localhost";
            string proxyPort = "8888";
            string proxyUser = "proxyuser";
            string proxyPass = "proxypass";

            var proxy = new WebProxy()
            {
                Address = new Uri($"{proxyHost}:{proxyPort}"),
                UseDefaultCredentials = false,

                // *** These creds are given to the proxy server, not the web server ***
                Credentials = new NetworkCredential(
                    userName: proxyUser,
                    password: proxyPass)
            };

            // Uncomment the following line to use a proxy
            //System.Net.WebRequest.DefaultWebProxy = proxy;

            // Populate properties dictionary with properties needed for API.  
            // This example uses a simple Java like properties file to pass in the settings to maintain consistency.
            var configProperties = SimpleIniParser.Parse("com.microsoft.intune.props");

            var trace = new TraceSource("log");

            var validator = new IntuneScepValidator(
                configProperties,
                trace: trace
            );

            var transactionId = Guid.NewGuid(); // A GUID that will uniquley identify the entire transaction to allow for log correlation accross Validate and Notification calls.

            // The CSR should be in Base64 encoding format
            string csr = "";

            // This validates the request
            (validator.ValidateRequestAsync(transactionId.ToString(), csr)).Wait();
            Console.WriteLine("No exceptions means CSR was validated!");

            // This notification is to be sent on successful issuance of certificate
            (validator.SendSuccessNotificationAsync(transactionId.ToString(), csr, "certThumbprint", "certSerial", (DateTime.Now + TimeSpan.FromDays(365)).ToLongDateString(), "certIssuingAuthority")).Wait();
            Console.WriteLine("No exceptions means success notification was recorded succesfully!");

            // This notification is to be sent on failure to issue certificate
            (validator.SendFailureNotificationAsync(transactionId.ToString(), csr, 0xFFFF, "Short description of error that occurred.")).Wait();
            Console.WriteLine("No exceptions means failure notification was recorded succesfully!");


            // CARequest Client - Cert Revocation Example
            var caRequestClient = new IntuneCARequestClient(
                configProperties,
                trace: trace
            );

            // Download CARequests from Intune
            List<CARequest> caRequests = (caRequestClient.DownloadCARequestsAsync(transactionId.ToString(), 10)).Result;

            // Process CARequest List
            List<CARequestResult> caRequestResults = new List<CARequestResult>();
            foreach (CARequest request in caRequests)
            {
                switch (request.RequestType)
                {
                    case CARequestType.RevokeCertificate:

                        var revokeParams = CARequestRevokeParameters.CreateFromRevokeRequest(request);

                        // Revoke the certificate
                        RevokeCertificate(
                            revokeParams.SerialNumber, 
                            revokeParams.RevocationScenario, 
                            out bool succeeded, 
                            out CARequestErrorCode errorCode, 
                            out string errorMessage);

                        // Add result to list
                        caRequestResults.Add(CARequestResult.CreateFromCARequest(request, succeeded, errorCode, errorMessage);

                        break;
                    default:
                        // Unsupported
                        caRequestResults.Add(CARequestResult.CreateFromCARequest(request, false, CARequestErrorCode.NotSupportedError, $"Client does not support request type: {request.RequestType}");
                        break;
                }
            }

            // Upload Results
            (caRequestClient.UploadCARequestResults(transactionId.ToString(), caRequestResults)).Wait();
        }

        private static void RevokeCertificate(string serialNumber, CARequestRevocationScenario revocationScenario, out bool succeeded, out CARequestErrorCode errorCode, out string errorMessage)
        {
            Console.WriteLine($"Revoking Certificate on the Certificate Authority. Serial Number {serialNumber}, Reason: {revocationScenario}");

            succeeded = true;
            errorCode = CARequestErrorCode.None;
            errorMessage = null;

            throw new NotImplementedException();
        }
    }
}
