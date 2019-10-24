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

namespace Microsoft.Management.Powershell.PFXImport.Cmdlets
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Management.Automation;
    using System.Net;
    using System.Security.Authentication;
    using System.Text;
    using System.Threading;
    using IdentityModel.Clients.ActiveDirectory;
    using Serialization;
    using Services.Api;
    using Newtonsoft.Json;
    using DirectoryServices;
    using System.Collections;

    /// <summary>
    /// Imports PFX certificates for delivery to user devices.
    /// </summary>
    [Cmdlet(VerbsData.Import, "IntuneUserPfxCertificate", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
    public class ImportUserPFXCertificate : PSCmdlet
    {
        private int successCnt;
        private int failureCnt;

        /// <summary>
        ///  AuthenticationResult retrieve from Get-IntuneAuthenticationToken
        /// </summary>
        [Parameter(DontShow = true)]//Deprecated
        public AuthenticationResult AuthenticationResult
        {
            get;
            set;
        }

        /// <summary>
        /// List of PFX certificates to import.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Doesn't work for powershell parameters")]
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        [ValidateNotNullOrEmpty()]
        public List<UserPFXCertificate> CertificateList
        {
            get;
            set;
        }

        /// <summary>
        /// Marks this command as an update to existing records.
        /// </summary>
        [Parameter]
        public SwitchParameter IsUpdate
        {
            get;
            set;
        }

        /// <summary>
        /// Abstracting method to get WebRequest.
        /// </summary>
        /// <param name="url">The graph url for the request.</param>
        /// <returns>The Created HttpWebRequest.</returns>
        [ExcludeFromCodeCoverage]
        public virtual HttpWebRequest CreateWebRequest(string url, AuthenticationResult authRes)
        {
            if(authRes == null)
            {
                throw new ArgumentNullException(nameof(authRes));
            }
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(url));
            request.ContentType = "application/json";
            if (IsUpdate.IsPresent)
            {
                request.Method = "PATCH";
            }
            else
            {
                request.Method = "POST";
            }

            request.Timeout = 30000;
            request.Headers.Add(HttpRequestHeader.Authorization, authRes.CreateAuthorizationHeader());
            return request;
        }

        /// <summary>
        /// ProcessRecord.
        /// </summary>
        protected override void ProcessRecord()
        {
            Hashtable modulePrivateData = this.MyInvocation.MyCommand.Module.PrivateData as Hashtable;
            if (AuthenticationResult == null)
            {
                AuthenticationResult = Authenticate.GetAuthToken(modulePrivateData);
            }
            if (!Authenticate.AuthTokenIsValid(AuthenticationResult))
            {
                this.ThrowTerminatingError(
                    new ErrorRecord(
                        new AuthenticationException("Cannot get Authentication Token"),
                        "Authentication Failure",
                        ErrorCategory.AuthenticationError,
                        AuthenticationResult));
            }

            successCnt = 0;
            failureCnt = 0;

            string graphURI = Authenticate.GetGraphURI(modulePrivateData);
            string schemaVersion = Authenticate.GetSchemaVersion(modulePrivateData);

            if (CertificateList == null || CertificateList.Count == 0)
            {
                this.ThrowTerminatingError(
                    new ErrorRecord(
                        new ArgumentException("No Certificates specified"),
                        "Date Input Failure",
                        ErrorCategory.InvalidArgument,
                        AuthenticationResult));
            }

            foreach (UserPFXCertificate cert in CertificateList)
            {
                string url;
                if (IsUpdate.IsPresent)
                {
                    string userId = GetUserId.GetUserIdFromUpn(cert.UserPrincipalName, graphURI, schemaVersion, AuthenticationResult);
                    url = string.Format(CultureInfo.InvariantCulture, "{0}/{1}/deviceManagement/userPfxCertificates({2}-{3})", graphURI, schemaVersion, userId, cert.Thumbprint);
                }
                else
                {
                    url = string.Format(CultureInfo.InvariantCulture, "{0}/{1}/deviceManagement/userPfxCertificates", graphURI, schemaVersion);
                }

                HttpWebRequest request = CreateWebRequest(url, AuthenticationResult);

                string certJson = SerializationHelpers.SerializeUserPFXCertificate(cert);
                byte[] contentBytes = Encoding.UTF8.GetBytes(certJson);

                request.ContentLength = contentBytes.Length;

                using (Stream reqStream = request.GetRequestStream())
                {
                    reqStream.Write(contentBytes, 0, contentBytes.Length);
                }

                ProcessResponse(request, cert);
            }

            this.WriteCommandDetail(string.Format(LogMessages.ImportCertificatesSuccess, successCnt));
            if (failureCnt > 0)
            {
                this.WriteWarning(string.Format(LogMessages.ImportCertificatesFailure, failureCnt));
            }
        }

        /// <summary>
        /// Uses a graph call to return the UserId for a specified UPN
        /// </summary>
        /// <param name="user">The User Principal Name.</param>
        /// <returns>The Azuer UserId.</returns>
        public virtual string GetUserIdFromUpn(string user)
        {
            string url = string.Format(CultureInfo.InvariantCulture, "{0}/{1}/users?$filter=userPrincipalName eq '{2}'", Authenticate.GraphURI, Authenticate.SchemaVersion, user);
            HttpWebRequest request;
            request = CreateWebRequest(url, AuthenticationResult);

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string responseMessage = string.Empty;
                    using (StreamReader rs = new StreamReader(response.GetResponseStream()))
                    {
                        responseMessage = rs.ReadToEnd();
                    }

                    User userObj = SerializationHelpers.DeserializeUser(responseMessage);
                    return userObj.Id;
                }
                else
                {
                    this.WriteError(new ErrorRecord(new InvalidOperationException(response.StatusDescription), response.StatusCode.ToString(), ErrorCategory.InvalidResult, user));
                }
            }

            return null;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:DoNotDisposeObjectsMultipleTimes", 
            Justification = "Not relevant here")]
        private void ProcessResponse(HttpWebRequest request, UserPFXCertificate cert)
        {
            bool needsRetry = false;
            TimeSpan waitTime = TimeSpan.Zero;
            double retryAfter = 60; // TODO: get a good default wait time.
            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if ((int)response.StatusCode >= 200 && (int)response.StatusCode <= 299)
                    {
                        successCnt++;
                    }
                    else
                    {
                        string responseMessage;
                        using (var rawStream = response.GetResponseStream())
                        using (var responseReader = new StreamReader(rawStream, Encoding.UTF8))
                        {
                            responseMessage = responseReader.ReadToEnd();
                        }

                        failureCnt++;

                        this.WriteError(
                            new ErrorRecord(
                                new InvalidOperationException(string.Format(LogMessages.ImportCertificateFailureWithThumbprint, cert.Thumbprint, response.StatusCode, Environment.NewLine, responseMessage)),
                                "Import Failure:" + responseMessage,
                                ErrorCategory.WriteError,
                                cert));
                    }
                }
            }
            catch (WebException we)
            {
                HttpWebResponse response = we.Response as HttpWebResponse;
                if (we.Status == WebExceptionStatus.ProtocolError && response.StatusCode == (HttpStatusCode)429)
                {
                    needsRetry = true;
                    if (response.Headers["x-ms-retry-after-ms"] != null)
                    {
                        retryAfter = double.Parse(response.Headers["x-ms-retry-after-ms"]);
                    }

                    if (response.Headers["Retry-After"] != null)
                    {
                        retryAfter = double.Parse(response.Headers["Retry-After"]);
                    }
                }
                else
                {

                    failureCnt++;

                    var resp = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();

                    dynamic obj = JsonConvert.DeserializeObject(resp);

                    string messageFromServer;
                    if (obj.error != null)
                    {
                        messageFromServer = obj.error.message.ToString();
                    }
                    else
                    {
                        messageFromServer = String.Format("Failed to deserialize response {0}", resp);
                    }

                    this.WriteDebug(string.Format("Error Message: {0}", messageFromServer));

                    this.WriteError(
                        new ErrorRecord(
                            we,
                            "\n\n Error Message" + messageFromServer + "\n\n request-id:" + we.Response.Headers["request-id"],
                            ErrorCategory.WriteError,
                            cert));
                    
                }
            }

            // Waiting until response is closed to re-use request
            if (needsRetry)
            {
                this.WriteWarning(string.Format(LogMessages.GetUserPfxTooManyRequests, retryAfter));
                Thread.Sleep(TimeSpan.FromSeconds(retryAfter));
                ProcessResponse(request, cert);
            }
        }
    }
}
