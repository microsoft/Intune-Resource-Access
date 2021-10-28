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

package com.microsoft.intune.carequest;

/**
 * CARequestErrorCodes - Error Codes for CA Request Results.
 */
public enum CARequestErrorCodes 
{
	// No Errors Occurred
	None("0"),

    // General Non-Retryable Service error
    NonRetryableServiceException("4000"),
    // Data failed to deserialize correctly (non-retryable).
    DataSerializationError("4001"),
    // Data contained invalid parameters (non-retryable).
    ParameterDataInvalidError("4002"),
    // Cryptography error attempting to fulfill request (non-retryable).
    CryptographyError("4003"),
    // Could not locate the requested Certificate (non-retryable).
    CertificateNotFoundError("4004"),
    // Conflict processing request"), Ex. trying to revoke an already revoked certificate (non-retryable)
    ConflictError("4005"),
    // Request  Not Supported (non-retryable).
    NotSupportedError("4006"),
    // Request is larger than what is allowed by the requesting service (non-retryable).
    PayloadTooLargeError("4007"),

    // General Retryable Service error
    RetryableServiceException("4100"),
    // Service Unavailable Exception (retryable).
    ServiceUnavailableException("4101"),   
    // Service Too Busy Exception (retryable).
    ServiceTooBusyException("4102"),
    // Authentication Failure Exception (retryable).
    AuthenticationException("4103");
	
	public final String Value;
	
	private CARequestErrorCodes(String value) 
	{
		this.Value = value;
	}
}
