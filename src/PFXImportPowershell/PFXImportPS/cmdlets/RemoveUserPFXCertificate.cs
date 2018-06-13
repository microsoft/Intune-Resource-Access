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
    using System.Linq;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Management.Automation;
    using System.IO;
    using System.Globalization;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Security.Authentication;
    using IdentityModel.Clients.ActiveDirectory;
    using Services.Api;
    using Newtonsoft.Json;

    /// <summary>
    /// Removes existing certificates based on provided PFX Certificates, Thumbprints, or User.
    /// </summary>
    [Cmdlet(VerbsCommon.Remove, "IntuneUserPfxCertificate", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
    public class RemoveUserPFXCertificate : Cmdlet
    {
        private int successCnt;
        private int failureCnt;

        /// <summary>
        ///  AuthenticationResult retrieve from Get-IntuneAuthenticationToken
        /// </summary>
        [Parameter(Mandatory = true)]
        public AuthenticationResult AuthenticationResult
        {
            get;
            set;
        }

        /// <summary>
        /// List of PFX Certificates to remove.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Doesn't work for powershell parameters")]
        [Parameter(ValueFromPipeline = true, Mandatory = true, ParameterSetName = "FromUserPFXCertificates")]
        public List<UserPFXCertificate> CertificateList
        {
            get;
            set;
        }

        /// <summary>
        /// List of thumbprints of the PFX Certificates to remove.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Doesn't work for powershell parameters")]
        [Parameter(ValueFromPipeline = true, Mandatory = true, ParameterSetName = "FromThumbprints")]
        public List<string> ThumbprintList
        {
            get;
            set;
        }

        /// <summary>
        /// List of users to remove all certificates for.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Doesn't work for powershell parameters")]
        [Parameter(ValueFromPipeline = true, Mandatory = true, ParameterSetName = "FromUsers")]
        public List<string> UserList
        {
            get;
            set;
        }

        /// <summary>
        /// Abstracting method to get WebRequest.
        /// </summary>
        /// <param name="url">The graph url for the request.</param>
        /// <returns>The Created HttpWebRequest.</returns>
        public virtual HttpWebRequest CreateWebRequest(string url, AuthenticationResult authRes)
        {
            if(authRes == null)
            {
                throw new ArgumentNullException(nameof(authRes));
            }
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(url));
            request.Method = "DELETE";
            request.Timeout = 30000;
            request.Headers.Add(HttpRequestHeader.Authorization, authRes.CreateAuthorizationHeader());
            return request;
        }

        public virtual GetUserPFXCertificate GetUserPFXCertificate()
        {
            return new GetUserPFXCertificate();
        }

        /// <summary>
        /// ProcessRecord.
        /// </summary>
        protected override void ProcessRecord()
        {
            if (!Authenticate.AuthTokenIsValid(AuthenticationResult))
            {
                this.ThrowTerminatingError(
                    new ErrorRecord(
                        new AuthenticationException("Cannot get Authentication Token"),
                        "Authentication Failure",
                        ErrorCategory.AuthenticationError,
                        AuthenticationResult));
            }

            if ((CertificateList == null || CertificateList.Count == 0) &&
               (ThumbprintList == null || ThumbprintList.Count == 0) &&
               (UserList == null || UserList.Count == 0))
            {
                this.ThrowTerminatingError(
                    new ErrorRecord(
                        new ArgumentException("No Certificates specified"),
                        "Date Input Failure",
                        ErrorCategory.InvalidArgument,
                        AuthenticationResult));
            }

            if (ThumbprintList == null)
            {
                ThumbprintList = new List<string>();
            }

            if (UserList != null)
            {
                GetUserPFXCertificate getCerts = this.GetUserPFXCertificate();
                getCerts.AuthenticationResult = AuthenticationResult;
                getCerts.UserList = UserList;

                foreach (UserPFXCertificate cert in getCerts.Invoke<UserPFXCertificate>())
                {
                    ThumbprintList.Add(cert.Thumbprint);
                }
            }

            if (CertificateList != null && CertificateList.Count > 0)
            {
                ThumbprintList.AddRange(CertificateList.Select(p => p.Thumbprint).ToList());
            }

            successCnt = 0;
            failureCnt = 0;

            foreach (string thumbprint in ThumbprintList)
            {
                string url = string.Format(CultureInfo.InvariantCulture, "{0}/{1}/deviceManagement/userPfxCertificates/{2}?{3}", Authenticate.GraphURI, Authenticate.SchemaVersion, thumbprint, Authenticate.APIVersionString);
                string nonVersionUrl = string.Format(CultureInfo.InvariantCulture, "{0}/{1}/deviceManagement/userPfxCertificates/{2}", Authenticate.GraphURI, Authenticate.SchemaVersion, thumbprint);
                HttpWebRequest request;
                request = CreateWebRequest(url, AuthenticationResult);
                HttpWebRequest nonVersionRequest;
                nonVersionRequest = CreateWebRequest(nonVersionUrl, AuthenticationResult);
                ProcessResponse(request, nonVersionRequest, thumbprint);
            }

            this.WriteCommandDetail(string.Format(LogMessages.RemoveCertificateSuccess, successCnt));
            if (failureCnt > 0)
            {
                this.WriteWarning(string.Format(LogMessages.RemoveCertificateFailure, successCnt));
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:DoNotDisposeObjectsMultipleTimes", 
            Justification = "Not relevant here")]
        private void ProcessResponse(HttpWebRequest request, HttpWebRequest nonVersionRequest, string thumbprint)
        {
            bool needsRetry = false;
            TimeSpan waitTime = TimeSpan.Zero;
            double retryAfter = 60; // TODO: get a good default wait time.
            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode != HttpStatusCode.NoContent)
                    {
                        string responseMessage;
                        using (var rawStream = response.GetResponseStream())
                        {
                            using (var responseReader = new StreamReader(rawStream, Encoding.UTF8))
                            {
                                responseMessage = responseReader.ReadToEnd();
                            }
                        }

                        this.WriteError(
                            new ErrorRecord(
                                new InvalidOperationException(string.Format("Remove failed for {0}: {1}{2}{3}", thumbprint, response.StatusCode, Environment.NewLine, responseMessage)),
                                "Remove Failed",
                                ErrorCategory.InvalidResult,
                                thumbprint));
                        failureCnt++;
                    }
                    else
                    {
                        successCnt++;
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
                    //Need to handle case where we try without the version string
                    if(nonVersionRequest != null)
                    {
                        this.WriteError(
                        new ErrorRecord(
                            we,
                            "Requesting with version failed, trying without version",
                            ErrorCategory.WriteError,
                            null));
                        ProcessResponse(nonVersionRequest, null, thumbprint);
                    }
                    else
                    {
                        var resp = new StreamReader(we.Response.GetResponseStream()).ReadToEnd();

                        dynamic obj = JsonConvert.DeserializeObject(resp);
                        var messageFromServer = obj.error.message;

                        this.WriteDebug(string.Format("Error Message: {0}", messageFromServer));
                        this.WriteError(new ErrorRecord(we, we.Message + messageFromServer + " request-id:" + we.Response.Headers["request-id"], ErrorCategory.InvalidResult, null));

                    }
                }
            }

            // Waiting until response is closed to re-use request
            if (needsRetry)
            {
                this.WriteWarning(string.Format(LogMessages.GetUserPfxTooManyRequests, retryAfter));
                Thread.Sleep(TimeSpan.FromSeconds(retryAfter));
                ProcessResponse(request, nonVersionRequest, thumbprint);
            }
        }
    }
}