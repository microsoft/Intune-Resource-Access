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

package com.microsoft.intune.scepvalidation;

import java.util.UUID;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/**
 * Exception thrown when the SCEP Service returns an error.
 */
public class IntuneScepServiceException extends IntuneClientException
{
    private static final long serialVersionUID = 2018_04_24_001L;
    
    final Logger log = LoggerFactory.getLogger(IntuneScepServiceException.class);
    
    private UUID activityId = null;
    private String errorCode = null;
    private String errorDescription = null;
    private ErrorCode parsedErrorCode = ErrorCode.Unknown;
    private String transactionId = null;
    
    public enum ErrorCode{
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
        BadUserIdInChallenge;
    };
    
    /**
     * The Unique code that describes the reason for the failure as returned from the server.
     */
    public String getOriginalErrorCode()
    {
        return this.errorCode;
    }
    
    /**
     * The Unique code that describes the reason for the failure parsed from what the server returned.
     */
    public ErrorCode getParsedErrorCode()
    {
        return parsedErrorCode;
    }
    
    /**
     * A short description for the error the service returned.
     */
    public String getErrorDescription()
    {
        return this.errorDescription;
    }
    
    /**
     * The transaction Id used for to correlate all SCEP service parts of the service call.
     */
    public String getTransactionId()
    {
        return this.transactionId;
    }
    
    /**
     * The ID that is provided to Intune to correlate all parts of the service call. 
     */
    public UUID getActivityId()
    {
        return this.activityId;
    }
    
    public IntuneScepServiceException(String errorCode, String errorDescription, String transactionId, UUID activityId)
    {        
        super("ActivityId:" + activityId + "," +
              "TransactionId:" + transactionId + "," +
              "ErrorCode:" + errorCode + "," +
              "ErrorDescription:" + errorDescription);
        
        this.activityId = activityId;
        this.transactionId = transactionId;
        this.errorCode = errorCode;
        try
        {
            parsedErrorCode = ErrorCode.valueOf(this.errorCode);
        }
        catch(IllegalArgumentException e)
        {
            log.warn("Error Code value not expected: " + this.errorCode);
        }
    }
}
