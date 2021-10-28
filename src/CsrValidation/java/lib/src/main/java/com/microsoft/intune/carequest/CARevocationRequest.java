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
 * CARevocationRequest - Class defining a Certificate Authority Request.
 */
public class CARevocationRequest 
{
	public String requestContext;
	public String serialNumber;
	public String issuerName;
	public String caConfiguration;
	
	/**
	 * Default Constructor
	 */
	public CARevocationRequest()
	{
		
	}
	
	/**
	 * CARevocationRequest Constructor
     * @param requestContext
     * @param serialNumber
     * @param issuerName
     * @param caConfig
     * @throws IllegalArgumentException
	 */
	public CARevocationRequest(String requestContext, String serialNumber, String issuerName, String caConfig)
	{
        if(requestContext == null)
        {
            throw new IllegalArgumentException("The argument 'requestContext' may not be 'null'"); 
        }
        
        if(serialNumber == null)
        {
            throw new IllegalArgumentException("The argument 'serialNumber' may not be 'null'"); 
        }
		
		this.requestContext = requestContext;
		this.serialNumber = serialNumber;
		this.issuerName = issuerName;
		this.caConfiguration = caConfig;
	}
}