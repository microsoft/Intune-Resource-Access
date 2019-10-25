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
    using Microsoft.DirectoryServices;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Serialization;
    using System;
    using System.Collections;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Management.Automation;
    using System.Net;
    using System.Security.Authentication;

    [Cmdlet(VerbsCommon.Get, "IntuneUserId")]
    [OutputType(typeof(Guid))]
    public class GetUserId : PSCmdlet
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
        /// List of users with PFX Certificates to be retrieved.
        /// </summary>
        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty()]
        public string UPN
        {
            get;
            set;
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
            string userId = GetUserId.GetUserIdFromUpn(UPN, graphURI, schemaVersion, AuthenticationResult);
            this.WriteObject(userId);
        }

        /// <summary>
        /// Uses a graph call to return the UserId for a specified UPN
        /// </summary>
        /// <param name="user">The User Principal Name.</param>
        /// <returns>The Azuer UserId.</returns>
        public static string GetUserIdFromUpn(string user, string graphURI, string schemaVersion, AuthenticationResult authenticationResult)
        {
            string url = string.Format(CultureInfo.InvariantCulture, "{0}/{1}/users?$filter=userPrincipalName eq '{2}'", graphURI, schemaVersion, user);
            HttpWebRequest request;
            request = GetUserPFXCertificate.CreateWebRequest(url, authenticationResult);

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
                    return userObj.Id.Replace("-", string.Empty);
                }
                else
                {
                    throw new InvalidOperationException(response.StatusDescription);
                }
            }
        }

    }
}
