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

import java.io.IOException;
import java.util.Properties;

import org.json.JSONObject;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/**
 * Client to access the ScepRequestValidationFEService in Intune
 */
public class IntuneScepServiceClient extends IntuneClient
{
	private String serviceVersion = "5018-02-20";
	private final static String VALIDATION_SERVICE_NAME = "ScepRequestValidationFEService";
	private final static String VALIDATION_COLLECTION = "ScepRequestValidations";
	private final static String SERVICE_VERSION_PROP_NAME = VALIDATION_SERVICE_NAME + "Version";
	
	final Logger log = LoggerFactory.getLogger(IntuneScepServiceClient.class);
	
	/**
	 * IntuneSceptService Client constructor
     * @param configProperties Properties object containing client configuration information.
	 * @throws IllegalArgumentException
	 * @throws IOException
	 */
	public IntuneScepServiceClient(Properties configProperties) throws IllegalArgumentException, IOException {
		super(configProperties);

		if(configProperties == null)
		{
			throw new IllegalArgumentException("The argument 'configProperties' is missing"); 
		}
		
		configProperties.getProperty(SERVICE_VERSION_PROP_NAME,this.serviceVersion);
	}
		
	/**
	 * Validates whether the given CSR is a valid certificate request from Microsoft Intune.
	 * If the CSR is not valid an exception will be thrown.
	 * @param csr Base 64 encoded PKCS10 packet
	 * @throws Exception 
	 */
    public void ValidateCsr(String csr) throws Exception
    {
    	if(csr == null || csr.isEmpty())
    	{
    		throw new IllegalArgumentException("The argument csr is missing");
    	}     
    	
    	JSONObject requestBody = new JSONObject("{certificateRequest:" + csr + "}");
    	
    	try 
    	{
    		JSONObject result = this.PostRequest(VALIDATION_SERVICE_NAME, 
					 VALIDATION_COLLECTION, 
					 serviceVersion, 
					 requestBody);
    		
    		// TODO: Parse result and look for error and throw
    		log.info(result.toString());
    	}
    	catch(Exception e)
    	{ 
    		this.log.error(e.getMessage(), e);
    		throw e; // TODO: Should we throw a IntuneClientException?
    	}
    }
}
