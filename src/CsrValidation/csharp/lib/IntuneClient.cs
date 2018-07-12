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
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Diagnostics;
using Newtonsoft.Json;

namespace lib
{
    /// <summary>
    /// IntuneClient - A client which can be used to make requests to Intune services.
    /// This object uses ADAL libraries and tokens for authentication with Intune.
    /// </summary>
    public class IntuneClient
    {
        protected string intuneAppId = "0000000a-0000-0000-c000-000000000000";
        protected string intuneResourceUrl = "https://api.manage.microsoft.com/";
        protected string graphApiVersion = "1.6";
        protected string graphResourceUrl = "https://graph.windows.net/";

        protected string intuneTenant;
        protected ClientCredential aadCredential;
        protected IADALClientWrapper authClient;

        private Dictionary<String, String> serviceMap = new Dictionary<String, String>();

        protected TraceSource trace = new TraceSource(typeof(IntuneClient).Name);

        /// <summary>
        /// Constructs an IntuneClient object which can be used to make requests to Intune services.
        /// </summary>
        /// <param name="azureAppId"></param>
        /// <param name="azureAppKey"></param>
        /// <param name="intuneTenant"></param>
        /// <param name="intuneAppId"></param>
        /// <param name="intuneResourceUrl"></param>
        /// <param name="graphApiVersion"></param>
        /// <param name="graphResourceUrl"></param>
        public IntuneClient(string azureAppId, string azureAppKey, string intuneTenant, string intuneAppId = null, string intuneResourceUrl = null, string graphApiVersion = null, string graphResourceUrl = null, string authAuthority = null, IADALClientWrapper authClient = null, TraceSource trace = null)
        {
            initialize(azureAppId, azureAppKey, intuneTenant, intuneAppId, intuneResourceUrl, graphApiVersion, graphResourceUrl);

            // Instantiate ADAL Client
            this.aadCredential = new ClientCredential(azureAppId, azureAppKey);

            if (authClient == null)
            {
                authClient = new ADALClientWrapper(this.intuneTenant, this.aadCredential, authAuthority: authAuthority, trace:trace);
            }
            this.authClient = authClient;

            this.trace = trace ?? this.trace;
        }

        private void initialize(string azureAppId, string azureAppKey, string intuneTenant, string intuneAppId = null, string intuneResourceUrl = null, string graphApiVersion = null, string graphResourceUrl = null)
        {

            if (string.IsNullOrEmpty(azureAppId))
            {
                throw new ArgumentException(nameof(azureAppId));
            }

            if (string.IsNullOrEmpty(azureAppKey))
            {
                throw new ArgumentException(nameof(azureAppKey));
            }

            this.intuneTenant = string.IsNullOrEmpty(intuneTenant) ? throw new ArgumentException(nameof(intuneTenant)) : intuneTenant;

            // Read optional properties
            this.intuneAppId = intuneAppId ?? this.intuneAppId;
            this.intuneResourceUrl = intuneResourceUrl ?? this.intuneResourceUrl;
            this.graphApiVersion = graphApiVersion ?? this.graphApiVersion;
            this.graphResourceUrl = graphResourceUrl ?? this.graphResourceUrl;
        }

        /// <summary>
        /// Post a Request to an Intune rest service.
        /// </summary>
        /// <param name="serviceName">The name of the service to post to.</param>
        /// <param name="urlSuffix">The end of the url to tack onto the request.</param>
        /// <param name="apiVersion">API Version of service to use.</param>
        /// <param name="json">The body of the request.</param>
        /// <param name="activityId">Client generated ID for correlation of this activity</param>
        /// <param name="additionalHeaders">key value pairs of additional header values to add to the request</param>
        /// <returns>JSON response from service</returns>
        public async Task<JObject> PostRequestAsync(String serviceName, String urlSuffix, String apiVersion, JObject json, Guid activityId, Dictionary<String, String> additionalHeaders = null)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException(nameof(serviceName));
            }

            if (string.IsNullOrEmpty(urlSuffix))
            {
                throw new ArgumentException(nameof(urlSuffix));
            }

            if (string.IsNullOrEmpty(apiVersion))
            {
                throw new ArgumentException(nameof(apiVersion));
            }

            if (json == null)
            {
                throw new ArgumentException(nameof(json));
            }


            string intuneServiceEndpoint = await GetServiceEndpointAsync(serviceName);
            if (string.IsNullOrEmpty(intuneServiceEndpoint))
            {
                IntuneServiceNotFoundException ex = new IntuneServiceNotFoundException(serviceName);
                trace.TraceEvent(TraceEventType.Error, 0, ex.Message);
                throw ex;
            }

            AuthenticationResult authResult = await authClient.GetAccessTokenFromCredentialAsync(intuneResourceUrl);

            string intuneRequestUrl = intuneServiceEndpoint + "/" + urlSuffix;

            HttpClient client = null;
            JObject jsonResponse = new JObject();
            try
            {
                client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
                client.DefaultRequestHeaders.Add("client-request-id", activityId.ToString());
                client.DefaultRequestHeaders.Add("api-version", apiVersion);
                var httpContent = new StringContent(json.ToString(), Encoding.UTF8, "application/json");

                if (additionalHeaders != null)
                {
                    foreach(KeyValuePair<string,string> entry in additionalHeaders)
                    {
                        client.DefaultRequestHeaders.Add(entry.Key, entry.Value);
                    }
                }

                HttpResponseMessage response = await client.PostAsync(intuneRequestUrl, httpContent);
                if (response.IsSuccessStatusCode)
                {
                    string result = response.Content.ReadAsStringAsync().Result;
                    try
                    {
                        jsonResponse = JObject.Parse(result);
                    }
                    catch (JsonReaderException e)
                    {
                        throw new IntuneClientException($"Failed to parse JSON response during Service Discovery from Graph. Response {result}");
                    }
                    
                }
                else
                {
                    String msg = "Request to: " + intuneRequestUrl + " returned: " + response.StatusCode.ToString();
                    IntuneClientHttpErrorException ex = new IntuneClientHttpErrorException(response.StatusCode, jsonResponse, activityId);
                    trace.TraceEvent(TraceEventType.Error, 0, ex.Message);
                    throw ex;
                }
            }
            catch (HttpRequestException e)
            {
                trace.TraceEvent(TraceEventType.Error, 0, $"Failed to contact intune service with URL: {intuneRequestUrl};\r\n{e.Message}");
                serviceMap.Clear(); // clear contents in case the service location has changed and we cached the value
                throw e;
            }
            finally
            {
                if (client != null)
                    client.Dispose();
            }

            return jsonResponse;
        }

        private async Task<string> GetServiceEndpointAsync(String serviceName)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentException(nameof(serviceName));
            }

            String serviceNameLower = serviceName.ToLowerInvariant();

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
            foreach (KeyValuePair<String, String> entry in serviceMap)
            {
                trace.TraceEvent(TraceEventType.Information, 0, $"{entry.Key}:{entry.Value}");
            }

            return null;
        }

        private async Task RefreshServiceMapAsync()
        {
            AuthenticationResult authResult = await this.authClient.GetAccessTokenFromCredentialAsync(this.graphResourceUrl);

            String graphRequest = this.graphResourceUrl + intuneTenant + "/servicePrincipalsByAppId/" + this.intuneAppId + "/serviceEndpoints?api-version=" + this.graphApiVersion;

            Guid activityId = Guid.NewGuid();
            HttpClient client = null;
            try
            {
                client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
                client.DefaultRequestHeaders.Add("client-request-id", activityId.ToString());

                HttpResponseMessage response = await client.GetAsync(graphRequest);
                String result = response.Content.ReadAsStringAsync().Result;

                if (response.IsSuccessStatusCode)
                {
                    JObject jsonResponse;
                    try
                    {
                        jsonResponse = JObject.Parse(result);
                    }
                    catch(JsonReaderException e)
                    {
                        throw new IntuneClientException($"Failed to parse JSON response during Service Discovery from Graph. Response {result}");
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
                else
                {
                    throw new IntuneClientException($"ServiceDiscovery returned unsuccesfully with HTTP StatusCode:{response.StatusCode} and Response:{result}");
                }
            }
            catch (HttpRequestException e)
            {
                trace.TraceEvent(TraceEventType.Error,0,$"Failed to contact intune service with URL: {graphRequest};\r\n{e.Message}");
                throw e;
            }
            finally
            {
                if(client != null)
                    client.Dispose();
            }
        }
    }
}