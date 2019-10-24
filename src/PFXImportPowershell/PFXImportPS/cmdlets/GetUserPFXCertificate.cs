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

[module: System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:FileHeaderFileNameDocumentationMustMatchTypeName", Justification = "Have a simple struct that doesn't justify a new file.")]

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
    using System.Threading;
    using IdentityModel.Clients.ActiveDirectory;
    using Services.Api;
    using Serialization;
    using DirectoryServices;
    using System.Collections;

    public struct UserThumbprint
    {
        //The User Id guid associate with the user certificate.
        public string User;
        //Thumbprint assoicated with the imported PFX certificate
        public string Thumbprint;
    }

    /// <summary>
    /// Retrieves existing certificates based on provided Users or Thumbprints.
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "IntuneUserPfxCertificate", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
    [OutputType(typeof(List<UserPFXCertificate>))]
    public class GetUserPFXCertificate : PSCmdlet
    {


        /// <summary>
        /// AuthenticationResult retrieved from Get-IntuneAuthenticationToken
        /// </summary>
        [Parameter(DontShow = true)]//Deprecated
        [ValidateNotNullOrEmpty()]
        public AuthenticationResult AuthenticationResult
        {
            get;
            set;
        }

        /// <summary>
        /// List of thumbprints of the PFX Certificates to be retrieved.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Doesn't work for powershell parameters")]
        [Parameter(ValueFromPipeline = true)]
        public List<UserThumbprint> UserThumbprintList
        {
            get;
            set;
        }

        /// <summary>
        /// List of users with PFX Certificates to be retrieved.
        /// </summary>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Doesn't work for powershell parameters")]
        [Parameter(ValueFromPipeline = true)]
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
        public static HttpWebRequest CreateWebRequest(string url, AuthenticationResult authRes)
        {
            if(authRes == null)
            {
                throw new ArgumentNullException(nameof(authRes));
            }
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(url));
            request.Method = "Get";
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

            string graphURI = Authenticate.GetGraphURI(modulePrivateData);
            string schemaVersion = Authenticate.GetSchemaVersion(modulePrivateData);

            string urlbase = string.Format(CultureInfo.InvariantCulture, "{0}/{1}/deviceManagement/userPfxCertificates", graphURI, schemaVersion);

            if (UserThumbprintList != null && UserThumbprintList.Count > 0)
            {
                foreach (UserThumbprint userthumbprint in UserThumbprintList)
                {
                    string url = string.Format("{0}?$filter=tolower(userPrincipalName) eq '{1}' and tolower(thumbprint) eq '{2}'", urlbase, userthumbprint.User.ToLowerInvariant(), userthumbprint.Thumbprint.ToLowerInvariant());
                    HttpWebRequest request;
                    try
                    {
                        request = CreateWebRequest(url, AuthenticationResult);

                        // Returns a single record and comes back in a different format than the other requests.
                        ProcessSingleResponse(request, "User:" + userthumbprint.User + " Thumbprint:" + userthumbprint.Thumbprint);
                    }
                    catch (WebException we)
                    {
                        this.WriteError(new ErrorRecord(we, we.Message + " request-id:" + we.Response.Headers["request-id"], ErrorCategory.InvalidResult, userthumbprint));
                    }
                }
            }
            else if (UserList != null && UserList.Count > 0)
            {
                foreach (string user in UserList)
                {
                    string url = string.Format("{0}?$filter=tolower(userPrincipalName) eq '{1}'", urlbase, user.ToLowerInvariant());
                    HttpWebRequest request;
                    try
                    {
                        request = CreateWebRequest(url, AuthenticationResult);
                        ProcessCollectionResponse(request, user);
                    }
                    catch (WebException we)
                    {
                        this.WriteError(new ErrorRecord(we, we.Message + " request-id:" + we.Response.Headers["request-id"], ErrorCategory.InvalidResult, user));
                    }
                }
            }
            else
            {
                HttpWebRequest request;
                try
                {
                    request = CreateWebRequest(urlbase, AuthenticationResult);
                    ProcessCollectionResponse(request, null);
                }
                catch (WebException we)
                {
                    this.WriteError(new ErrorRecord(we, we.Message + " request-id:" + we.Response.Headers["request-id"], ErrorCategory.InvalidResult, null));
                }
            }
        }

        private void ProcessCollectionResponse(HttpWebRequest request, string filter)
        {
            bool needsRetry = false;
            TimeSpan waitTime = TimeSpan.Zero;
            double retryAfter = 60; // TODO: get a good default wait time.
            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseMessage = string.Empty;
                        using (StreamReader rs = new StreamReader(response.GetResponseStream()))
                        {
                            responseMessage = rs.ReadToEnd();
                        }

                        List<UserPFXCertificate> certList = SerializationHelpers.DeserializeUserPFXCertificateList(responseMessage);
                        foreach (UserPFXCertificate cert in certList)
                        {
                            this.WriteObject(cert);
                        }
                    }
                    else
                    {
                        this.WriteError(new ErrorRecord(new InvalidOperationException(response.StatusDescription), response.StatusCode.ToString(), ErrorCategory.InvalidResult, filter));
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
                    this.WriteError(new ErrorRecord(we, we.Message + " request-id:" + we.Response.Headers["request-id"], ErrorCategory.InvalidResult, filter));
                }
            }

            // Waiting until response is closed to re-use request
            if (needsRetry)
            {
                this.WriteWarning(string.Format(LogMessages.GetUserPfxTooManyRequests, retryAfter));
                Thread.Sleep(TimeSpan.FromSeconds(retryAfter));
                ProcessCollectionResponse(request, filter);
            }
        }

        private void ProcessSingleResponse(HttpWebRequest request, string filter)
        {
            bool needsRetry = false;
            TimeSpan waitTime = TimeSpan.Zero;
            double retryAfter = 60; // TODO: get a good default wait time.
            try
            {
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseMessage = string.Empty;
                        using (StreamReader rs = new StreamReader(response.GetResponseStream()))
                        {
                            responseMessage = rs.ReadToEnd();
                        }

                        List<UserPFXCertificate> certList = SerializationHelpers.DeserializeUserPFXCertificateList(responseMessage);
                        if (certList.Count != 1)
                        {
                            this.WriteError(new ErrorRecord(new InvalidOperationException("Multiple records returned"), certList.Count.ToString(), ErrorCategory.InvalidResult, filter));
                        }
                        this.WriteObject(certList[0]);
                    }
                    else
                    {
                        this.WriteError(new ErrorRecord(new InvalidOperationException(response.StatusDescription), response.StatusCode.ToString(), ErrorCategory.InvalidResult, filter));
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
                    this.WriteError(new ErrorRecord(we, we.Message + " request-id:" + we.Response.Headers["request-id"], ErrorCategory.InvalidResult, filter));
                }
            }

            // Waiting until response is closed to re-use request
            if (needsRetry)
            {
                this.WriteWarning(string.Format(LogMessages.GetUserPfxTooManyRequests, retryAfter));
                Thread.Sleep(TimeSpan.FromSeconds(retryAfter));
                ProcessSingleResponse(request, filter);
            }
        }
    }
}
