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

namespace Microsoft.Intune
{
    /// <summary>
    /// IntuneClient - A client which can be used to make requests to Intune services.
    /// This object uses ADAL libraries and tokens for authentication with Intune.
    /// </summary>
    public class IntuneClient : IIntuneClient
    {
        protected TraceSource trace = new TraceSource(typeof(IntuneClient).Name);

        /// <summary>
        /// The resource URL of Intune that we are requesting access from ADAL for.
        /// </summary>
        protected string intuneResourceUrl = "https://api.manage.microsoft.com/";

        /// <summary>
        /// The active directory authentication library client to request tokens from
        /// </summary>
        protected AdalClient adalClient;

        /// <summary>
        /// HttpClient to utilize when making requests to Intune
        /// </summary>
        protected IHttpClient httpClient;

        /// <summary>
        /// The API that provides ocations for services in Intune.
        /// </summary>
        protected IIntuneServiceLocationProvider locationProvider;

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
        public IntuneClient(string azureAppId, string azureAppKey, string intuneTenant, AdalClient adalClient, IIntuneServiceLocationProvider locationProvider, IHttpClient httpClient = null, string intuneResourceUrl = null, TraceSource trace = null)
        {
            // Required parameters
            if (string.IsNullOrEmpty(azureAppId))
            {
                throw new ArgumentException(nameof(azureAppId));
            }

            if (string.IsNullOrEmpty(azureAppKey))
            {
                throw new ArgumentException(nameof(azureAppKey));
            }

            if (string.IsNullOrEmpty(intuneTenant))
            {
                throw new ArgumentException(nameof(intuneTenant));
            }

            // Optional parameters
            this.intuneResourceUrl = intuneResourceUrl ?? this.intuneResourceUrl;
            this.trace = trace ?? this.trace;

            // Instantiate Dependencies
            this.locationProvider = locationProvider;
            this.adalClient = adalClient;
            this.httpClient = httpClient ?? new HttpClient(new System.Net.Http.HttpClient());
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
        public async Task<JObject> PostAsync(string serviceName, string urlSuffix, string apiVersion, JObject json, Guid activityId, Dictionary<string, string> additionalHeaders = null)
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


            string intuneServiceEndpoint = await this.locationProvider.GetServiceEndpointAsync(serviceName);
            if (string.IsNullOrEmpty(intuneServiceEndpoint))
            {
                IntuneServiceNotFoundException ex = new IntuneServiceNotFoundException(serviceName);
                trace.TraceEvent(TraceEventType.Error, 0, ex.Message);
                throw ex;
            }

            AuthenticationResult authResult = await adalClient.AcquireTokenAsync(intuneResourceUrl);

            string intuneRequestUrl = intuneServiceEndpoint + "/" + urlSuffix;

            IHttpClient client = null;
            JObject jsonResponse = new JObject();

            client = this.httpClient;
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
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
            try
            {
                response = await client.PostAsync(intuneRequestUrl, httpContent);
            }
            catch (HttpRequestException e)
            {
                trace.TraceEvent(TraceEventType.Error, 0, $"Failed to contact intune service with URL: {intuneRequestUrl};\r\n{e.Message}");
                this.locationProvider.Clear(); // clear contents in case the service location has changed and we cached the value
                throw;
            }

            if (response != null && response.IsSuccessStatusCode)
            {
                string result = await response.Content.ReadAsStringAsync();
                try
                {
                    jsonResponse = JObject.Parse(result);
                }
                catch (JsonReaderException e)
                {
                    throw new IntuneClientException($"Failed to parse JSON response from Intune. Response {result}", e);
                }

            }
            else if(response == null)
            {
                string msg = "Request to: " + intuneRequestUrl + " failed for an unknown reason.";
                IntuneClientException ex = new IntuneClientException(msg);
                trace.TraceEvent(TraceEventType.Error, 0, ex.Message);
                throw ex;
            }
            else
            {
                string msg = "Request to: " + intuneRequestUrl + " returned: " + response.StatusCode.ToString();
                IntuneClientHttpErrorException ex = new IntuneClientHttpErrorException(response.StatusCode, jsonResponse, activityId);
                trace.TraceEvent(TraceEventType.Error, 0, ex.Message);
                throw ex;
            }
   
            return jsonResponse;
        }
    }
}