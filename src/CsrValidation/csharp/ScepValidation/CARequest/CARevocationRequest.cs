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

namespace Microsoft.Management.Services.Api
{
    using Newtonsoft.Json;

    /// <summary>
    /// Class defining a Certificate Authority Request
    /// </summary>
    public class CARevocationRequest
    {
        /// <summary>
        /// Context for this request
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string RequestContext { get; set; }
        
        /// <summary>
        /// Serial number for the certificate to revoke
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string SerialNumber { get; set; }

        /// <summary>
        /// Issuer Name for the certficate to be revoked.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string IssuerName { get; set; }

        /// <summary>
        /// CA Configuration for the certficate to be revoked
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string CaConfiguration { get; set; }
    } 
}
