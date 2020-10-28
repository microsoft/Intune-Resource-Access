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

namespace Microsoft.Management.Services.Api
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Class defining a Certificate Authority Request
    /// </summary>
    public class CARequest
    {
        /// <summary>
        /// Tenant ID
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public Guid TenantId { get; set; }

        /// <summary>
        /// User ID
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public Guid UserId { get; set; }

        /// <summary>
        /// Device ID
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public Guid DeviceId { get; set; }

        /// <summary>
        /// Unique Identifier for this request
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string RequestId { get; set; }

        /// <summary>
        /// The type for this request
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public CARequestType RequestType { get; set; }

        /// <summary>
        /// JSON serialized parameter data for this request. The format of this data is determined by the Request type.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Parameters { get; set; }
    } 
}
