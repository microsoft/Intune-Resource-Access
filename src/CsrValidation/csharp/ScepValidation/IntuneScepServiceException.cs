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
using System.Diagnostics;

namespace Microsoft.Intune
{

    /// <summary>
    /// Exception thrown when the SCEP Service returns an error.
    /// </summary>
    public class IntuneScepServiceException : IntuneClientException
    {
        public enum ErrorCode
        {
            Unknown,
            Success,
            CertificateRequestDecodingFailed,
            ChallengePasswordMissing,
            ChallengeDeserializationError,
            ChallengeDecryptionError,
            ChallengeDecodingError,
            ChallengeInvalidTimestamp,
            ChallengeExpired,
            SubjectNameMissing,
            SubjectNameMismatch,
            SubjectAltNameMissing,
            SubjectAltNameMismatch,
            KeyUsageMismatch,
            KeyLengthMismatch,
            EnhancedKeyUsageMissing,
            EnhancedKeyUsageMismatch,
            AadKeyIdentifierListMissing,
            RegisteredKeyMismatch,
            SigningCertThumbprintMismatch,
            ScepProfileNoLongerTargetedToTheClient,
            SignatureValidationFailed,
            BadCertificateRequestIdInChallenge,
            BadDeviceIdInChallenge,
            BadUserIdInChallenge
        };

        /// <summary>
        /// The Unique code that describes the reason for the failure as returned from the server.
        /// </summary>
        /// <returns></returns>
        public string OriginalErrorCode { get; } = null;

        /// <summary>
        /// The Unique code that describes the reason for the failure parsed from what the server returned.
        /// </summary>
        /// <returns></returns>
        public ErrorCode ParsedErrorCode { get; } = ErrorCode.Unknown;

        /// <summary>
        /// A short description for the error the service returned.
        /// </summary>
        /// <returns></returns>
        public string ErrorDescription { get; } = null;

        /// <summary>
        /// The transaction Id used for to correlate all SCEP service parts of the service call.
        /// </summary>
        /// <returns></returns>
        public string TransactionId { get; } = null;

        /// <summary>
        /// The ID that is provided to Intune to correlate all parts of the service call. 
        /// </summary>
        /// <returns></returns>
        public Guid ActivityId { get; } = Guid.Empty;

        public IntuneScepServiceException(string errorCode, string errorDescription, string transactionId, Guid activityId, TraceSource trace) : base(
            "ActivityId:" + activityId + "," +
            "TransactionId:" + transactionId + "," +
            "ErrorCode:" + errorCode + "," +
            "ErrorDescription:" + errorDescription)
        {
            this.ActivityId = activityId;
            this.TransactionId = transactionId;
            this.OriginalErrorCode = errorCode;
            this.ErrorDescription = errorDescription;

            try
            {
                ParsedErrorCode = (ErrorCode)Enum.Parse(typeof(ErrorCode), this.OriginalErrorCode);
            }
            catch(ArgumentException)
            {
                trace.TraceEvent(TraceEventType.Error, 0, $"Error Code value not expected: {this.OriginalErrorCode}");
                throw;
            }
        }
    }
}
