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
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Identity.Client;

namespace Microsoft.Intune
{
    /// <summary>
    /// Authentication Client
    /// </summary>
    public class MsalClient
    {
        public const string DEFAULT_AUTHORITY = "https://login.microsoftonline.com/";

        private TraceSource trace = new TraceSource(nameof(MsalClient));

        /// <summary>
        /// The authority that we are requesting access from.
        /// </summary>
        private string authority = null;
        private string tenant = null;

        private IConfidentialClientApplication app = null;

        /// <summary>
        /// Constructor meant to be used for dependency injection of authContext
        /// </summary>
        /// <param name="configProperties">Configuration properties for this class.</param>
        /// <param name="trace">Trace</param>
        [SuppressMessage("Microsoft.Usage", "CA2208", Justification = "Using a parameter coming from an object.")]
        public MsalClient(Dictionary<string,string> configProperties, TraceSource trace = null)
        {
            // Required Parameters
            if (configProperties == null)
            {
                throw new ArgumentNullException(nameof(configProperties));
            }

            configProperties.TryGetValue("TENANT", out tenant);
            if (string.IsNullOrWhiteSpace(tenant))
            {
                throw new ArgumentNullException(nameof(tenant));
            }

            string appId = null;
            configProperties.TryGetValue("AAD_APP_ID", out appId);
            if (string.IsNullOrWhiteSpace(appId))
            {
                throw new ArgumentNullException(nameof(appId));
            }

            string appKey = null;
            configProperties.TryGetValue("AAD_APP_KEY", out appKey);
            if (string.IsNullOrWhiteSpace(appKey))
            {
                throw new ArgumentNullException(nameof(appKey));
            }

            // Optional Parameters
            if(trace != null)
            {
                this.trace = trace;
            }

            configProperties.TryGetValue("AUTH_AUTHORITY", out authority);
            if(authority == null)
            {
                authority = DEFAULT_AUTHORITY;
            }

            app = ConfidentialClientApplicationBuilder.Create(appId)
                .WithClientSecret(appKey)
                .WithTenantId(tenant)
                .Build();
        }

        /// <summary>
        /// Gets an access token from AAD for the specified resource using the ClientCredential passed in.
        /// </summary>
        /// <param name="resource">Resource to get token for.</param>
        /// <returns></returns>
        public async Task<string> AcquireTokenAsync(string[] scopes)
        {
            if (scopes == null)
            {
                throw new ArgumentNullException(nameof(scopes));
            }

            var res = await app.AcquireTokenForClient(scopes)
                 .WithAuthority($"{authority}{tenant}")
                 .ExecuteAsync();

            if (res == null || res.AccessToken == null)
            {
                throw new IntuneClientException("Authentication result was null");
            }

            return res.AccessToken;
        }
    }
}