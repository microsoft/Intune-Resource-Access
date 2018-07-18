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

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Intune
{
    public interface IIntuneClient
    {

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
        Task<JObject> PostAsync(string serviceName, string urlSuffix, string apiVersion, JObject json, Guid activityId, Dictionary<string, string> additionalHeaders = null);
    }
}