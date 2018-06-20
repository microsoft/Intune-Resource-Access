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

import java.util.HashMap;
import java.util.Properties;
import java.util.UUID;

import org.apache.http.impl.client.HttpClientBuilder;
import org.json.JSONObject;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import com.microsoft.intune.scepvalidation.IntuneScepServiceException.ErrorCode;

/**
 * Client to access the ScepRequestValidationFEService in Intune
 */
public class IntuneScepServiceClient extends IntuneClient
{
    private String serviceVersion = "2018-02-20";
    public final static String VALIDATION_SERVICE_NAME = "ScepRequestValidationFEService";
    private final static String VALIDATION_URL = "ScepActions/validateRequest";
    private final static String NOTIFY_SUCCESS_URL = "ScepActions/successNotification";
    private final static String NOTIFY_FAILURE_URL = "ScepActions/failureNotification";
    private final static String SERVICE_VERSION_PROP_NAME = VALIDATION_SERVICE_NAME + "Version";
    private final static String PROVIDER_NAME_AND_VERSION_NAME = "PROVIDER_NAME_AND_VERSION";
    
    private String providerNameAndVersion = null;
    private HashMap<String,String> additionalHeaders = new HashMap<String, String>();;
    
    final Logger log = LoggerFactory.getLogger(IntuneScepServiceClient.class);
    
    /**
     * IntuneScepService Client constructor
     * @param configProperties Properties object containing client configuration information.
     * @throws IllegalArgumentException
     */
    public IntuneScepServiceClient(Properties configProperties) throws IllegalArgumentException 
    {
        this(configProperties, null, null);
    }
    
    /**
     * IntuneScepService Client constructor meant for dependency injection
     * @param configProperties
     * @param adalClient
     * @param httpClientBuilder
     * @throws IllegalArgumentException
     */
    public IntuneScepServiceClient(Properties configProperties, ADALClientWrapper adalClient, HttpClientBuilder httpClientBuilder) throws IllegalArgumentException 
    {
        super(configProperties, adalClient, httpClientBuilder);
        
        if(configProperties == null)
        {
            throw new IllegalArgumentException("The argument 'configProperties' is missing"); 
        }
        
        configProperties.getProperty(SERVICE_VERSION_PROP_NAME,this.serviceVersion);
        
        providerNameAndVersion = configProperties.getProperty(PROVIDER_NAME_AND_VERSION_NAME);
        if(providerNameAndVersion == null)
        {
            throw new IllegalArgumentException("The property '" + PROVIDER_NAME_AND_VERSION_NAME + "' is missing from the property file.");
        }
        
        additionalHeaders.put("UserAgent", providerNameAndVersion);
    }

    /**
     * Validates whether the given Certificate Request is a valid and from Microsoft Intune.
     * If the request is not valid an exception will be thrown.  
     * 
     * IMPORTANT: If an exception is thrown the SCEP server should not issue a certificate to the client.
     *  
     * @param transactionId The transactionId of the Certificate Request
     * @param certificateRequest Base 64 encoded PKCS10 packet
     * @throws IntuneScepServiceException The Certificate Request failed validation
     * @throws Exception Unexpected validation
     */
    public void ValidateRequest(String transactionId, String certificateRequest) throws IntuneScepServiceException, Exception
    {
        if(transactionId == null || transactionId.isEmpty())
        {
            throw new IllegalArgumentException("The argument 'transactionId' is missing");
        }     
        
        if(certificateRequest == null || certificateRequest.isEmpty())
        {
            throw new IllegalArgumentException("The argument 'certificateRequest' is missing");
        }     
        
        JSONObject requestBody = new JSONObject().put(
                "request", (new JSONObject())
                    .put("transactionId", transactionId)
                    .put("certificateRequest", certificateRequest)
                    .put("callerInfo", this.providerNameAndVersion));
        
        Post(requestBody, VALIDATION_URL, transactionId);
    }
    
    /**
     * Send a Success notification to the SCEP Service.
     * 
     * IMPORTANT: If an exception is thrown the SCEP server should not issue a certificate to the client.
     * 
     * @param transactionId The transactionId of the CSR
     * @param certificateRequest Base 64 encoded PKCS10 packet
     * @param certThumbprint Thumbprint of the certificate issued.
     * @param certSerialNumber Serial number of the certificate issued.
     * @param certExpirationDate The date time string should be formated as web UTC time (YYYY-MM-DDThh:mm:ss.sssTZD) ISO 8601. 
     * @param certIssuingAuthority Issuing Authority that issued the certificate.
     * @throws IntuneScepServiceException The service reported a failure in processing the notification examine the exception error code.
     * @throws Exception Unexpected error
     */
    public void SendSuccessNotification(String transactionId, String certificateRequest, String certThumbprint, String certSerialNumber, String certExpirationDate, String certIssuingAuthority) throws IntuneScepServiceException, Exception
    {
        if(transactionId == null || transactionId.isEmpty())
        {
            throw new IllegalArgumentException("The argument 'transactionId' is missing");
        }     
        
        if(certificateRequest == null || certificateRequest.isEmpty())
        {
            throw new IllegalArgumentException("The argument 'certificateRequest' is missing");
        }     
        
        if(certThumbprint == null || certThumbprint.isEmpty())
        {
            throw new IllegalArgumentException("The argument 'certThumbprint' is missing");
        }     
        
        if(certSerialNumber == null || certSerialNumber.isEmpty())
        {
            throw new IllegalArgumentException("The argument 'certSerialNumber' is missing");
        }     
        
        if(certExpirationDate == null || certExpirationDate.isEmpty())
        {
            throw new IllegalArgumentException("The argument 'certExpirationDate' is missing");
        }     
        
        if(certIssuingAuthority == null || certIssuingAuthority.isEmpty())
        {
            throw new IllegalArgumentException("The argument 'certIssuingAuthority' is missing");
        }     
        
        JSONObject requestBody = new JSONObject().put(
                "notification", (new JSONObject())
                    .put("transactionId", transactionId)
                    .put("certificateRequest", certificateRequest)
                    .put("certificateThumbprint", certThumbprint)
                    .put("certificateSerialNumber", certSerialNumber)
                    .put("certificateExpirationDateUtc", certExpirationDate)
                    .put("issuingCertificateAuthority", certIssuingAuthority)
                    .put("callerInfo", this.providerNameAndVersion));
        
        Post(requestBody, NOTIFY_SUCCESS_URL, transactionId);
    }
    
    /**
     * Send a Failure notification to the SCEP service. 
     * 
     * IMPORTANT: If this method is called the SCEP server should not issue a certificate to the client.
     * 
     * @param transactionId The transactionId of the CSR
     * @param certificateRequest Base 64 encoded PKCS10 packet
     * @param hResult 32-bit error code formulated using the instructions specified in https://msdn.microsoft.com/en-us/library/cc231198.aspx. 
     * The value specified will be reported in the Intune management console and will be used by the administrator to troubleshoot the issue. 
     * It is recommended that your product provide documentation about the meaning of the error codes reported.
     * @param errorDescription Description of what error occurred. Max length = 255 chars
     * @throws IntuneScepServiceException The service reported a failure in processing the notification examine the exception error code.
     * @throws Exception Unexpected error
     */
    public void SendFailureNotification(String transactionId, String certificateRequest, long hResult, String errorDescription) throws IntuneScepServiceException, Exception
    {
        if(transactionId == null || transactionId.isEmpty())
        {
            throw new IllegalArgumentException("The argument 'transactionId' is missing");
        }     
        
        if(certificateRequest == null || certificateRequest.isEmpty())
        {
            throw new IllegalArgumentException("The argument 'certificateRequest' is missing");
        }       
        
        if(errorDescription == null || errorDescription.isEmpty())
        {
            throw new IllegalArgumentException("The argument 'errorDescription' is missing");
        }  
        
        JSONObject requestBody = new JSONObject().put(
                "notification", (new JSONObject())
                    .put("transactionId", transactionId)
                    .put("certificateRequest", certificateRequest)
                    .put("hResult", hResult)
                    .put("errorDescription", errorDescription)
                    .put("callerInfo", this.providerNameAndVersion));
        
        Post(requestBody, NOTIFY_FAILURE_URL, transactionId);
    }
    
    private void Post(JSONObject requestBody, String urlSuffix, String transactionId) throws IntuneScepServiceException, Exception
    {
        UUID activityId = UUID.randomUUID();
        
        try 
        {
            JSONObject result = this.PostRequest(VALIDATION_SERVICE_NAME, 
                     urlSuffix, 
                     serviceVersion, 
                     requestBody,
                     activityId,
                     additionalHeaders);
            
            log.info("Activity " + activityId + " has completed.");
            log.info(result.toString());
            
            String code = result.getString("code");
            String errorDescription = result.getString("errorDescription");
            
            IntuneScepServiceException e = new IntuneScepServiceException(code, errorDescription, transactionId, activityId);

            if (e.getParsedErrorCode() != ErrorCode.Success)
            {
                
                log.warn(e.getMessage());
                throw e;
            }
        }
        catch(Exception e)
        { 
            if (!(e instanceof IntuneScepServiceException))
            {
                log.error(
                        "ActivityId:" + activityId + "," +
                        "TransactionId:" + transactionId + "," +
                        "ExceptionMessage:" + e.getMessage()
                        , e);
            }
            throw e;
        }
    }
}
