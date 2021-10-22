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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Microsoft.Intune
{
    /// <summary>
    /// IntuneClient - A client which can be used to make requests to Intune services.
    /// This object uses ADAL libraries and tokens for authentication with Intune.
    /// </summary>
    public class IntuneClient : IIntuneClient
    {
        public const string DEFAULT_INTUNE_RESOURCE_URL = "https://api.manage.microsoft.com/";
        private TraceSource trace = new TraceSource(nameof(IntuneClient));

        /// <summary>
        /// The resource URL of Intune that we are requesting access from ADAL for.
        /// </summary>
        private string intuneResourceUrl = null;

        /// <summary>
        /// The active directory authentication library client to request tokens from
        /// </summary>
        private MsalClient authClient;

        /// <summary>
        /// HttpClient to utilize when making requests to Intune
        /// </summary>
        private IHttpClient httpClient;

        /// <summary>
        /// The API that provides ocations for services in Intune.
        /// </summary>
        private IIntuneServiceLocationProvider locationProvider;

        /// <summary>
        /// Constructs an IntuneClient object which can be used to make requests to Intune services.
        /// </summary>
        /// <param name="configProperties">Configuration properties for this class.</param>
        /// <param name="authClient">Authorization Client.</param>
        /// <param name="locationProvider">Service Location provider to be used for service discovery.</param>
        /// <param name="httpClient">HttpClient to use for all requests.</param>
        /// <param name="trace">Trace</param>
        public IntuneClient(Dictionary<string, string> configProperties, MsalClient authClient, IIntuneServiceLocationProvider locationProvider, IHttpClient httpClient = null, TraceSource trace = null)
        {
            // Required Parameters/Dependencies
            if (configProperties == null)
            {
                throw new ArgumentNullException(nameof(configProperties));
            }

            if(locationProvider == null)
            {
                throw new ArgumentNullException(nameof(locationProvider));
            }
            this.locationProvider = locationProvider;
            if(authClient == null)
            {
                throw new ArgumentNullException(nameof(authClient));
            }
            this.authClient = authClient;

            // Optional Prameters/Dependencies
            configProperties.TryGetValue("INTUNE_RESOURCE_URL", out intuneResourceUrl);
            if (string.IsNullOrWhiteSpace(intuneResourceUrl))
            {
                intuneResourceUrl = DEFAULT_INTUNE_RESOURCE_URL;
            }

            if (trace != null)
            {
                this.trace = trace;
            }

            this.httpClient = httpClient ?? new HttpClient(new System.Net.Http.HttpClient());
        }

        /// <inheritdoc />
        public async Task<JObject> PostAsync(string serviceName, string urlSuffix, string apiVersion, JObject json, Guid activityId, Dictionary<string, string> additionalHeaders = null)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName));
            }

            if (string.IsNullOrWhiteSpace(urlSuffix))
            {
                throw new ArgumentNullException(nameof(urlSuffix));
            }

            if (string.IsNullOrWhiteSpace(apiVersion))
            {
                throw new ArgumentNullException(nameof(apiVersion));
            }

            if (json == null)
            {
                throw new ArgumentNullException(nameof(json));
            }

            string intuneServiceEndpoint = await this.locationProvider.GetServiceEndpointAsync(serviceName);
            if (string.IsNullOrWhiteSpace(intuneServiceEndpoint))
            {
                IntuneServiceNotFoundException exception = new IntuneServiceNotFoundException(serviceName);
                trace.TraceEvent(TraceEventType.Error, 0, exception.Message);
                throw exception;
            }

            string token = await authClient.AcquireTokenAsync(new[] { intuneResourceUrl + "/.default" });

            string intuneRequestUrl = intuneServiceEndpoint + "/" + urlSuffix;

            IHttpClient client = this.httpClient;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            client.DefaultRequestHeaders.Add("client-request-id", activityId.ToString());
            client.DefaultRequestHeaders.Add("api-version", apiVersion);
            var httpContent = new StringContent(json.ToString(), Encoding.UTF8, "application/json");

            if (additionalHeaders != null)
            {
                foreach (KeyValuePair<string, string> entry in additionalHeaders)
                {
                    client.DefaultRequestHeaders.Add(entry.Key, entry.Value);
                }
            }

            HttpResponseMessage response = null;
            string result = null;
            try
            {
                response = await client.PostAsync(intuneRequestUrl, httpContent);
                result = await response.Content.ReadAsStringAsync();
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                trace.TraceEvent(TraceEventType.Error, 0, $"Failed to contact intune service with URL: {intuneRequestUrl};\r\n{e.Message}");
                trace.TraceEvent(TraceEventType.Error, 0, result);
                this.locationProvider.Clear(); // clear contents in case the service location has changed and we cached the value
                throw;
            }
            
            try
            {
                return JObject.Parse(result);
            }
            catch (JsonReaderException e)
            {
                throw new IntuneClientException($"Failed to parse JSON response from Intune. Response {result}", e);
            }
        }
    }
}