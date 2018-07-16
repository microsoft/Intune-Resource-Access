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

using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.Intune
{
    public class IntuneServiceLocationProvider : IIntuneServiceLocationProvider
    {
        private const string DEFAULT_INTUNE_APP_ID = "0000000a-0000-0000-c000-000000000000";
        private const string DEFAULT_RESOURCE_URL = "https://graph.windows.net/";
        private const string DEFAULT_GRAPH_VERSION = "1.6";

        protected TraceSource trace = new TraceSource(typeof(IntuneServiceLocationProvider).Name);

        /// <summary>
        /// The specific graph service version that we are choosing to make a request to.
        /// </summary>
        protected string graphApiVersion = DEFAULT_GRAPH_VERSION;

        /// <summary>
        /// The graph resource URL that we are requesting a token to access from ADAL
        /// </summary>
        protected string graphResourceUrl = DEFAULT_RESOURCE_URL;

        /// <summary>
        /// The App Identifier of Intune to be used in call to graph for service discovery
        /// </summary>
        protected string intuneAppId = DEFAULT_INTUNE_APP_ID;

        /// <summary>
        /// The tenant identifier i.e. contoso.onmicrosoft.com
        /// </summary>
        protected string intuneTenant;
        
        /// <summary>
        /// Cached Map of service locations
        /// </summary>
        protected Dictionary<string, string> serviceMap = new Dictionary<string, string>();

        // Dependencies
        protected AdalClient authClient;
        protected IHttpClient httpClient;

        public IntuneServiceLocationProvider(string intuneTenant, AdalClient authClient, string graphApiVersion = DEFAULT_GRAPH_VERSION, string graphResourceUrl = DEFAULT_RESOURCE_URL, string intuneAppId = DEFAULT_INTUNE_APP_ID, IHttpClient httpClient = null, TraceSource trace = null)
        {
            // Required Parameters
            if (string.IsNullOrEmpty(intuneTenant))
            {
                throw new ArgumentNullException(nameof(intuneTenant));
            }
            this.intuneTenant = intuneTenant;

            // Optional Parameters
            this.graphApiVersion = string.IsNullOrEmpty(graphApiVersion) ? DEFAULT_GRAPH_VERSION : graphApiVersion;
            this.graphResourceUrl = string.IsNullOrEmpty(graphResourceUrl) ? DEFAULT_RESOURCE_URL : graphResourceUrl;
            this.intuneAppId = string.IsNullOrEmpty(intuneAppId) ? DEFAULT_INTUNE_APP_ID : intuneAppId;
            this.trace = trace ?? this.trace;

            // Dependencies
            this.authClient = authClient;
            this.httpClient = httpClient ?? new HttpClient(new System.Net.Http.HttpClient());
        }

        public void Clear()
        {
            this.serviceMap.Clear();
        }

        public async Task<string> GetServiceEndpointAsync(string serviceName)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException(nameof(serviceName));
            }

            string serviceNameLower = serviceName.ToLowerInvariant();

            // Pull down the service map if we haven't populated it OR we are forcing a refresh
            if (serviceMap.Count <= 0)
            {
                trace.TraceEvent(TraceEventType.Information, 0, "Refreshing service map from Microsoft.Graph");
                await RefreshServiceMapAsync();
            }

            if (serviceMap.ContainsKey(serviceNameLower))
            {
                return serviceMap[serviceNameLower];
            }

            // LOG Cache contents
            trace.TraceEvent(TraceEventType.Information, 0, "Could not find endpoint for service '" + serviceName + "'");
            trace.TraceEvent(TraceEventType.Information, 0, "ServiceMap: ");
            foreach (KeyValuePair<string, string> entry in serviceMap)
            {
                trace.TraceEvent(TraceEventType.Information, 0, $"{entry.Key}:{entry.Value}");
            }

            return null;
        }

        private async Task RefreshServiceMapAsync()
        {
            AuthenticationResult authResult = await this.authClient.AcquireTokenAsync(this.graphResourceUrl);

            string graphRequest = this.graphResourceUrl + intuneTenant + "/servicePrincipalsByAppId/" + this.intuneAppId + "/serviceEndpoints?api-version=" + this.graphApiVersion;

            Guid activityId = Guid.NewGuid();
            IHttpClient client = null;
            
            client = this.httpClient;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
            client.DefaultRequestHeaders.Add("client-request-id", activityId.ToString());

            HttpResponseMessage response = null;
            string result = null;
            try
            {
                response = await client.GetAsync(graphRequest);
                result = await response.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                trace.TraceEvent(TraceEventType.Error, 0, $"Failed to contact intune service with URL: {graphRequest};\r\n{e.Message}");
                throw;
            }

            if (response != null && response.IsSuccessStatusCode)
            {
                JObject jsonResponse;
                try
                {
                    jsonResponse = JObject.Parse(result);
                }
                catch (JsonReaderException e)
                {
                    throw new IntuneClientException($"Failed to parse JSON response during Service Discovery from Graph. Response {result}", e);
                }

                JToken serviceEndpoints = null;
                if (jsonResponse.TryGetValue("value", out serviceEndpoints))
                {
                    serviceMap.Clear(); // clear map now that we ideally have a good response

                    foreach (var service in serviceEndpoints)
                    {
                        serviceMap.Add(service["serviceName"].ToString().ToLowerInvariant(), service["uri"].ToString());
                    }
                }
                else
                {
                    throw new IntuneClientException($"Failed to parse JSON response during Service Discovery from Graph. Response {jsonResponse.ToString()}");
                }
            }
            else if(response == null)
            {
                throw new IntuneClientException($"ServiceDiscovery failed for an unknown reason");
            }
            else
            {
                throw new IntuneClientException($"ServiceDiscovery returned unsuccesfully with HTTP StatusCode:{response.StatusCode} and Response:{result}");
            }

        }
    }
}
