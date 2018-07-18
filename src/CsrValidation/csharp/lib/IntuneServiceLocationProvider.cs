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
        public const string DEFAULT_INTUNE_APP_ID = "0000000a-0000-0000-c000-000000000000";
        public const string DEFAULT_RESOURCE_URL = "https://graph.windows.net/";
        public const string DEFAULT_GRAPH_VERSION = "1.6";

        private TraceSource trace = new TraceSource(nameof(IntuneServiceLocationProvider));

        /// <summary>
        /// The specific graph service version that we are choosing to make a request to.
        /// </summary>
        private string graphApiVersion = null;

        /// <summary>
        /// The graph resource URL that we are requesting a token to access from ADAL
        /// </summary>
        private string graphResourceUrl = null;

        /// <summary>
        /// The App Identifier of Intune to be used in call to graph for service discovery
        /// </summary>
        private string intuneAppId = null;

        /// <summary>
        /// The tenant identifier i.e. contoso.onmicrosoft.com
        /// </summary>
        private string intuneTenant;
        
        /// <summary>
        /// Cached Map of service locations
        /// </summary>
        private Dictionary<string, string> serviceMap = new Dictionary<string, string>();

        // Dependencies
        private AdalClient authClient;
        private IHttpClient httpClient;

        /// <summary>
        /// Instantiates a new instance.
        /// </summary>
        /// <param name="intuneTenant">Tenant name.</param>
        /// <param name="authClient">Authorization client.</param>
        /// <param name="graphApiVersion">Specific version of graph to use.</param>
        /// <param name="graphResourceUrl">Graph resource url to request access to.</param>
        /// <param name="intuneAppId">Application Id of Intune in graph.</param>
        /// <param name="httpClient">HttpClient to use for requests.</param>
        /// <param name="trace">Trace</param>
        public IntuneServiceLocationProvider(string intuneTenant, AdalClient authClient, string graphApiVersion = DEFAULT_GRAPH_VERSION, string graphResourceUrl = DEFAULT_RESOURCE_URL, string intuneAppId = DEFAULT_INTUNE_APP_ID, IHttpClient httpClient = null, TraceSource trace = null)
        {
            // Required Parameters
            if (string.IsNullOrWhiteSpace(intuneTenant))
            {
                throw new ArgumentNullException(nameof(intuneTenant));
            }
            this.intuneTenant = intuneTenant;

            if (string.IsNullOrWhiteSpace(graphApiVersion))
            {
                throw new ArgumentNullException(nameof(graphApiVersion));
            }
            this.graphApiVersion = graphApiVersion;

            if (string.IsNullOrWhiteSpace(graphResourceUrl))
            {
                throw new ArgumentNullException(nameof(graphResourceUrl));
            }
            this.graphResourceUrl = graphResourceUrl;

            if (string.IsNullOrWhiteSpace(intuneAppId))
            {
                throw new ArgumentNullException(nameof(intuneAppId));
            }
            this.intuneAppId = intuneAppId;

            this.authClient = authClient ?? throw new ArgumentNullException(nameof(authClient));

            // Optional Parameters
            if (trace != null)
            {
                this.trace = trace;
            }

            // Dependencies
            this.httpClient = httpClient ?? new HttpClient(new System.Net.Http.HttpClient());
        }

        public void Clear()
        {
            this.serviceMap.Clear();
        }

        public async Task<string> GetServiceEndpointAsync(string serviceName)
        {
            if (string.IsNullOrWhiteSpace(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName));
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

            IHttpClient client = this.httpClient;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
            client.DefaultRequestHeaders.Add("client-request-id", activityId.ToString());

            HttpResponseMessage response = null;
            try
            {
                response = await client.GetAsync(graphRequest);
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                trace.TraceEvent(TraceEventType.Error, 0, $"Failed to contact intune service with URL: {graphRequest};\r\n{e.Message}");
                throw;
            }

            string result = await response.Content.ReadAsStringAsync();

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
    }
}
