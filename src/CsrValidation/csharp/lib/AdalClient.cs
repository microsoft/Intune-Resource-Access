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

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Intune
{
    /**
    * Azure Active Directory Authentication Client
    */
    public class AdalClient
    {
        protected TraceSource trace = new TraceSource(typeof(AdalClient).Name);
        private const string DEFAULT_AUTHORITY = "https://login.microsoftonline.com/";
        private string authority = DEFAULT_AUTHORITY;
        private ClientCredential clientCredential = null;

        // Dependencies
        private IAuthenticationContext context = null;

        

        /// <summary>
        /// Constructor meant to be used for dependency injection of authContext
        /// </summary>
        /// <param name="aadTenant">Azure Active Directory tenant</param>
        /// <param name="authAuthority">If specified authentication will use this Auth Authority.</param> 
        public AdalClient(string aadTenant, ClientCredential clientCredential, string authAuthority = DEFAULT_AUTHORITY, TraceSource trace = null, IAuthenticationContext authContext = null)
        {
            // Required Parameters
            if (string.IsNullOrEmpty(aadTenant))
            {
                throw new ArgumentException(nameof(aadTenant));
            }
            this.clientCredential = clientCredential ?? throw new ArgumentNullException(nameof(clientCredential));

            // Optional Parameters
            this.authority = authAuthority ?? this.authority;
            this.trace = trace ?? this.trace;

            // Instantiate Dependencies
            context = authContext ?? new AuthenticationContextWrapper(new AuthenticationContext(this.authority + aadTenant, false));
        }

        /// <summary>
        /// Gets an access token from AAD for the specified resource using the ClientCredential passed in.
        /// </summary>
        /// <param name="clientCredential">Credential to use for authentication.</param>
        /// <param name="resource">Resource to get token for.</param>
        /// <returns></returns>
        public async Task<AuthenticationResult> AcquireTokenAsync(string resource)
        {
            if (string.IsNullOrEmpty(resource))
            {
                throw new ArgumentException(nameof(resource));
            }

            AuthenticationResult result = await context.AcquireTokenAsync(resource, clientCredential);

            if (result == null)
            {
                throw new IntuneClientException("Authentication result was null");
            }

            return result;
        }
    }
}