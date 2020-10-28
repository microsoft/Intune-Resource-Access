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
    using System;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Class defining the results of a Certificate Authority Request
    /// </summary>
    public class CARequestResult
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
        /// The Type for this Request
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public CARequestType RequestType { get; set; }

        /// <summary>
        /// Boolean for whether the request was successful
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public bool Succeeded { get; set; }

        /// <summary>
        /// The Error Code for a failed request
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public CARequestErrorCode ErrorCode { get; set; }

        /// <summary>
        /// The Error Message string describing why the request failed
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Static helper function for creating a new result message from a CARequest
        /// </summary>
        /// <param name="request">CARequest to create the result from.</param>
        /// <param name="succeeded">Whether the request was successful (true/false).</param>
        /// <param name="errorCode">Error code for the failure.</param>
        /// <param name="errorMessage">Error message for the failure.</param>
        /// <returns>A new CARequestResult</returns>
        public static CARequestResult CreateFromCARequest(CARequest request, bool succeeded, CARequestErrorCode errorCode = CARequestErrorCode.None, string errorMessage = null)
        {
            if (succeeded && errorCode != CARequestErrorCode.None)
            {
                throw new ArgumentException($"ErrorCodes are not allowed if 'Succeeded' is set to true");
            }

            if (succeeded && errorMessage != null)
            {
                throw new ArgumentException($"ErrorMessages are not allowed if 'Succeeded' is set to true");
            }

            return new CARequestResult()
            {
                TenantId = request.TenantId,
                UserId = request.UserId,
                DeviceId = request.DeviceId,
                RequestId = request.RequestId,
                RequestType = request.RequestType,
                Succeeded = succeeded,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }
    }
}
