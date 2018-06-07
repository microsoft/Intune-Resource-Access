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
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace lib
{
    /// <summary>
    /// Client to access the ScepRequestValidationFEService in Intune
    /// </summary>
    public class IntuneScepServiceClient
    {
        private readonly string serviceVersion = "2018-02-20";
        private readonly string VALIDATION_SERVICE_NAME = "ScepRequestValidationFEService";
        private readonly string VALIDATION_URL = "ScepActions/validateRequest";
        private readonly string NOTIFY_SUCCESS_URL = "ScepActions/successNotification";
        private readonly string NOTIFY_FAILURE_URL = "ScepActions/failureNotification";

        private string providerNameAndVersion = null;
        private Dictionary<String, String> additionalHeaders = new Dictionary<String, String>();
        private IntuneClient intuneClient = null;

        protected TraceSource trace = new TraceSource(typeof(IntuneScepServiceClient).Name);

        /// <summary>
        /// IntuneSceptService Client constructor
        /// </summary>
        public IntuneScepServiceClient(string providerNameAndVersion, string azureAppId, string azureAppKey, string intuneTenant, string serviceVersion = null, string intuneAppId = null, string intuneResourceUrl = null, string graphApiVersion = null, string graphResourceUrl = null, string authAuthority = null, IntuneClient intuneClient = null, TraceSource trace = null)
        {
            this.serviceVersion = serviceVersion ?? this.serviceVersion;

            if (string.IsNullOrEmpty(providerNameAndVersion))
            {
                throw new ArgumentException(nameof(providerNameAndVersion));
            }

            additionalHeaders.Add("UserAgent", providerNameAndVersion);
            this.providerNameAndVersion = providerNameAndVersion;

            if(intuneClient == null)
            {
                intuneClient = new IntuneClient(azureAppId, azureAppKey, intuneTenant, intuneAppId, intuneResourceUrl, graphApiVersion, graphResourceUrl, authAuthority);
            }
            this.intuneClient = intuneClient;

            this.trace = trace ?? this.trace;
        }

        /// <summary>
        /// Validates whether the given Certificate Request is a valid and from Microsoft Intune.
        /// If the request is not valid an exception will be thrown.
        /// 
        /// IMPORTANT: If an exception is thrown the SCEP server should not issue a certificate to the client.
        /// </summary>
        /// <param name="transactionId">The transactionId of the Certificate Request</param>
        /// <param name="certificateRequest">Base 64 encoded PKCS10 packet</param>
        /// <returns></returns>
        public async Task ValidateRequestAsync(String transactionId, String certificateRequest)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                throw new ArgumentException(nameof(transactionId));
            }

            if (string.IsNullOrEmpty(certificateRequest))
            {
                throw new ArgumentException(nameof(certificateRequest));
            }

            JObject requestBody = new JObject(
                new JProperty("request", new JObject( 
                    new JProperty("transactionId", transactionId),
                    new JProperty("certificateRequest", certificateRequest),
                    new JProperty("callerInfo", this.providerNameAndVersion))));

            await PostAsync(requestBody, VALIDATION_URL, transactionId);
        }

        /// <summary>
        /// Send a Success notification to the SCEP Service.
        /// 
        /// IMPORTANT: If an exception is thrown the SCEP server should not issue a certificate to the client.
        /// </summary>
        /// <param name="transactionId">The transactionId of the CSR</param>
        /// <param name="certificateRequest">Base 64 encoded PKCS10 packet</param>
        /// <param name="certThumbprint">Thumbprint of the certificate issued.</param>
        /// <param name="certSerialNumber">Serial number of the certificate issued.</param>
        /// <param name="certExpirationDate">The date time string should be formated as web UTC time (YYYY-MM-DDThh:mm:ss.sssTZD) ISO 8601. </param>
        /// <param name="certIssuingAuthority">Issuing Authority that issued the certificate.</param>
        /// <returns></returns>
        public async Task SendSuccessNotificationAsync(String transactionId, String certificateRequest, String certThumbprint, String certSerialNumber, String certExpirationDate, String certIssuingAuthority)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                throw new ArgumentException(nameof(transactionId));
            }

            if (string.IsNullOrEmpty(certificateRequest))
            {
                throw new ArgumentException(nameof(certificateRequest));
            }

            if (string.IsNullOrEmpty(certThumbprint))
            {
                throw new ArgumentException(nameof(certThumbprint));
            }

            if (string.IsNullOrEmpty(certSerialNumber))
            {
                throw new ArgumentException(nameof(certSerialNumber));
            }

            if (string.IsNullOrEmpty(certExpirationDate))
            {
                throw new ArgumentException(nameof(certExpirationDate));
            }

            if (string.IsNullOrEmpty(certIssuingAuthority))
            {
                throw new ArgumentException(nameof(certIssuingAuthority));
            }

            JObject requestBody = new JObject(
                new JProperty("notification", new JObject(
                    new JProperty("transactionId", transactionId),
                    new JProperty("certificateRequest", certificateRequest),
                    new JProperty("certificateThumbprint", certThumbprint),
                    new JProperty("certificateSerialNumber", certSerialNumber),
                    new JProperty("certificateExpirationDateUtc", certExpirationDate),
                    new JProperty("issuingCertificateAuthority", certIssuingAuthority),
                    new JProperty("callerInfo", this.providerNameAndVersion))));

            await PostAsync(requestBody, NOTIFY_SUCCESS_URL, transactionId);
        }

        /// <summary>
        /// Send a Failure notification to the SCEP service. 
        /// 
        /// IMPORTANT: If this method is called the SCEP server should not issue a certificate to the client.
        /// </summary>
        /// <param name="transactionId">The transactionId of the CSR</param>
        /// <param name="certificateRequest">Base 64 encoded PKCS10 packet</param>
        /// <param name="hResult">hResult 32-bit error code formulated using the instructions specified in https://msdn.microsoft.com/en-us/library/cc231198.aspx. 
        /// The value specified will be reported in the Intune management console and will be used by the administrator to troubleshoot the issue.
        /// It is recommended that your product provide documentation about the meaning of the error codes reported.</param>
        /// <param name="errorDescription">Description of what error occurred. Max length = 255 chars</param>
        /// <returns></returns>
        public async Task SendFailureNotificationAsync(String transactionId, String certificateRequest, long hResult, String errorDescription)
        {
            if (string.IsNullOrEmpty(transactionId))
            {
                throw new ArgumentException(nameof(transactionId));
            }

            if (string.IsNullOrEmpty(certificateRequest))
            {
                throw new ArgumentException(nameof(certificateRequest));
            }

            if (string.IsNullOrEmpty(errorDescription))
            {
                throw new ArgumentException(nameof(errorDescription));
            }

            JObject requestBody = new JObject(
                new JProperty("notification", new JObject(
                    new JProperty("transactionId", transactionId),
                    new JProperty("certificateRequest", certificateRequest),
                    new JProperty("hResult", hResult),
                    new JProperty("errorDescription", errorDescription),
                    new JProperty("callerInfo", this.providerNameAndVersion))));

            await PostAsync(requestBody, NOTIFY_FAILURE_URL, transactionId);
        }

        private async Task PostAsync(JObject requestBody, String urlSuffix, String transactionId)
        {
            Guid activityId = Guid.NewGuid();

            try
            {
                JObject result = await intuneClient.PostRequestAsync(VALIDATION_SERVICE_NAME,
                         urlSuffix,
                         serviceVersion,
                         requestBody,
                         activityId,
                         additionalHeaders);

                trace.TraceEvent(TraceEventType.Information,0,"Activity " + activityId + " has completed.");
                trace.TraceEvent(TraceEventType.Information, 0,result.ToString());

                string code = (string)result["code"];
                string errorDescription = (string)result["errorDescription"];

                IntuneScepServiceException e = new IntuneScepServiceException(code, errorDescription, transactionId, activityId);

                if (e.getParsedErrorCode() != IntuneScepServiceException.ErrorCode.Success)
                {
                    trace.TraceEvent(TraceEventType.Warning, 0, e.Message);
                    throw e;
                }
            }
            catch (Exception e)
            {
                if (!(e is IntuneScepServiceException))
                {
                    trace.TraceEvent(TraceEventType.Error, 0,
                        "ActivityId:" + activityId + "," +
                        "TransactionId:" + transactionId + "," +
                        "ExceptionMessage:" + e.Message);
                }
                throw e;
            }
        }
    }
}