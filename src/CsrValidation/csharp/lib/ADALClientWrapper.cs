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

namespace lib
{
    /**
    * Azure Active Directory Authentication Client
    */
    public class ADALClientWrapper : IADALClientWrapper
    {

        private string authority = "https://login.microsoftonline.com/";
        private ClientCredential credential = null;
        private IAuthenticationContext context = null;

        protected TraceSource trace = new TraceSource(typeof(ADALClientWrapper).Name);

        /// <summary>
        /// Constructor meant to be used for dependency injection of authContext
        /// </summary>
        /// <param name="aadTenant">Azure Active Directory tenant</param>
        /// <param name="credential">Credential to use for authentication</param>
        /// <param name="authAuthority">If specified authentication will use this Auth Authority.</param> 
        public ADALClientWrapper(string aadTenant, ClientCredential credential, string authAuthority = null, TraceSource trace = null)
        {
            initialize(aadTenant, credential, authAuthority);

            context = new AuthenticationContextWrapper(new AuthenticationContext(this.authority + aadTenant, false));

            this.trace = trace ?? this.trace;
        }

        /// <summary>
        /// Constructor meant to be used for dependency injection of authContext
        /// </summary>
        /// <param name="aadTenant">Azure Active Directory tenant</param>
        /// <param name="credential">Credential to use for authentication</param>
        /// <param name="authAuthority">If specified authentication will use this Auth Authority.</param>
        /// <param name="authContext">AuthenticationContext to use.</param>
        public ADALClientWrapper(string aadTenant, ClientCredential credential, IAuthenticationContext authContext, string authAuthority = null)
        {
            initialize(aadTenant, credential, authAuthority);
            this.context = authContext;
        }

        private void initialize(string aadTenant, ClientCredential credential, string authAuthority = null)
        {
            if (string.IsNullOrEmpty(aadTenant))
            {
                throw new ArgumentException(nameof(aadTenant));
            }

            this.authority = authAuthority ?? this.authority;
            this.credential = credential ?? throw new ArgumentException(nameof(credential));

        }

        /// <summary>
        /// Gets an access token from AAD for the specified resource using the ClientCredential passed in.
        /// </summary>
        /// <param name="resource">Resource to get token for.</param>
        /// <returns></returns>
        public async Task<AuthenticationResult> GetAccessTokenFromCredentialAsync(String resource)
        {
            if (string.IsNullOrEmpty(resource))
            {
                throw new ArgumentException(nameof(resource));
            }

            AuthenticationResult result = await context.AcquireTokenAsync(resource, credential);

            if (result == null)
            {
                throw new IntuneClientException("Authentication result was null");
            }

            return result;
        }
    }
}