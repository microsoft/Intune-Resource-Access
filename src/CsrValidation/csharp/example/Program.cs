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
using System.Diagnostics;
using System.Net;
using Microsoft.Intune;

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

            var validator = new IntuneScepValidator(
                configProperties,
                trace: new TraceSource("log")
            );

            var transactionId = Guid.NewGuid(); // A GUID that will uniquley identify the entire transaction to allow for log correlation accross Validate and Notification calls.

            // The CSR should be in Base64 encoding format
            string csr = "test";

            // This validates the request
            (validator.ValidateRequestAsync(transactionId.ToString(), csr)).Wait();
            Console.WriteLine("No exceptions means CSR was validated!");

            // This notification is to be sent on successful issuance of certificate
            (validator.SendSuccessNotificationAsync(transactionId.ToString(), csr, "certThumbprint", "certSerial", (DateTime.Now + TimeSpan.FromDays(365)).ToLongDateString(), "certIssuingAuthority", "caConfiguration", "certificateAuthority")).Wait();
            Console.WriteLine("No exceptions means success notification was recorded succesfully!");

            // This notification is to be sent on failure to issue certificate
            (validator.SendFailureNotificationAsync(transactionId.ToString(), csr, 0xFFFF, "Short description of error that occurred.")).Wait();
            Console.WriteLine("No exceptions means failure notification was recorded succesfully!");
        }
    }
}
