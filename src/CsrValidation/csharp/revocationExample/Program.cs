// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portionas of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using Microsoft.Intune;
using Microsoft.Management.Services.Api;

namespace RevocationExample
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

            var transactionId = Guid.NewGuid(); // A GUID that will uniquley identify the entire transaction to allow for log correlation accross Validate and Notification calls.

            // Create CARevocationRequest Client 
            var revocationClient = new IntuneRevocationClient(
                configProperties,
                trace: trace
            );

            // Set Download Parameters
            int maxRequests = 100; // Maximum number of Revocation requests to download at a time
            string certificateProviderName = null; // Optional Parameter: Set this value if you want to filter 
                                                   //   the request to only download request matching this CA Name
            string issuerName = null; // Optional Parameter: Set this value if you want to filter 
                                      //   the request to only download request matching this Issuer Name

            // Download CARevocationRequests from Intune
            List<CARevocationRequest> caRevocationRequests = (revocationClient.DownloadCARevocationRequestsAsync(transactionId.ToString(), maxRequests, certificateProviderName, issuerName)).Result;
            Console.WriteLine($"Downloaded {caRevocationRequests.Count} number of Revocation requests from Intune.");

            // Process CARevocationRequest List
            List<CARevocationResult> revocationResults = new List<CARevocationResult>();
            foreach (CARevocationRequest request in caRevocationRequests)
            {
                // Revoke the certificate
                RevokeCertificate(
                    request.SerialNumber,
                    out bool succeeded,
                    out CARequestErrorCode errorCode,
                    out string errorMessage);

                // Add result to list
                revocationResults.Add(new CARevocationResult(request.RequestContext, succeeded, errorCode, errorMessage));
            }

            if (revocationResults.Count > 0)
            {
                try
                {
                    Console.WriteLine($"Uploading {revocationResults.Count} Revocation Results to Intune.");

                    // Upload Results to Intune
                    (revocationClient.UploadRevocationResultsAsync(transactionId.ToString(), revocationResults)).Wait();
                }
                catch(AggregateException e)
                {
                    Console.WriteLine($"Upload of results to to Intune Failed. Exception: {e.InnerException}");
                }
            }
        }

        private static void RevokeCertificate(string serialNumber, out bool succeeded, out CARequestErrorCode errorCode, out string errorMessage)
        {
            Console.WriteLine($"Revoking Certificate on the Certificate Authority. Serial Number {serialNumber}");

            succeeded = true;
            errorCode = CARequestErrorCode.None;
            errorMessage = "";

            // throw new NotImplementedException();
        }
    }
}
