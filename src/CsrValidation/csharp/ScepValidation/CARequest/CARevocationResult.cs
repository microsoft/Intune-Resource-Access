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
    using Newtonsoft.Json.Converters;
    using System;

    /// <summary>
    /// Class defining the results of a Certificate Authority Request
    /// </summary>
    public class CARevocationResult
    {
        /// <summary>
        /// Context for this request
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string RequestContext { get; set; }

        /// <summary>
        /// Boolean for whether the request was successful
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public bool Succeeded { get; set; }

        /// <summary>
        /// The Error Code for a failed request
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        [JsonConverter(typeof(StringEnumConverter))]
        public CARequestErrorCode ErrorCode { get; set; }

        /// <summary>
        /// The Error Message string describing why the request failed
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="succeeded"></param>
        /// <param name="errorCode"></param>
        /// <param name="errorMessage"></param>
        public CARevocationResult(string requestContext, bool succeeded, CARequestErrorCode errorCode, string errorMessage)
        {
            // Validation of params
            if (string.IsNullOrWhiteSpace(requestContext))
            {
                throw new ArgumentNullException(nameof(requestContext));
            }
            if (succeeded && (errorCode != CARequestErrorCode.None || !string.IsNullOrWhiteSpace(errorMessage)))
            {
                throw new ArgumentException($"CARevocationResult be set to Succeeded=true along with an error code or error message. Error Code: {errorCode}; Error Message: {errorMessage};");
            }

            this.RequestContext = requestContext;
            this.Succeeded = succeeded;
            this.ErrorCode = errorCode;
            this.ErrorMessage = errorMessage;
        }
    }
}
