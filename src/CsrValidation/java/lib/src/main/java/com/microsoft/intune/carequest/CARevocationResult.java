//Copyright (c) Microsoft Corporation.
//All rights reserved.
//
//This code is licensed under the MIT License.
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files(the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions :
//
//The above copyright notice and this permission notice shall be included in
//all copies or substantial portions of the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//THE SOFTWARE.

package com.microsoft.intune.carequest;

/**
* CARevocationRequest - Class defining a Certificate Authority Request.
*/
public class CARevocationResult 
{
	public String RequestContext;
	public boolean Succeeded;
	public int ErrorCode;
	public String ErrorMessage;
		
	/**
	 * CARevocationResult Constructor
	 * @param requestContext Context for the request
	 * @param succeeded Whether the revocation on the CA was successful
	 * @param errorCode Error Code in the case of a failure
	 * @param errorMessage Error message in the case of a failure
	 * @throws IllegalArgumentException
	 */
	public CARevocationResult(String requestContext, boolean succeeded, CARequestErrorCodes errorCode, String errorMessage)
	{
		if (requestContext == null)
		{
		    throw new IllegalArgumentException("The argument 'requestContext' may not be 'null'"); 
		}
		
		if (succeeded && errorCode != CARequestErrorCodes.None)
		{
		    throw new IllegalArgumentException("The argument 'errorCode' must be set to 0 ('None') if succeeded is set to true"); 
		}
		
		if (!succeeded && errorCode == CARequestErrorCodes.None)
		{
		    throw new IllegalArgumentException("The argument 'errorCode' may not be set to 0 ('None') if succeeded is set to true"); 
		}
			
		this.RequestContext = requestContext;
		this.Succeeded = succeeded;
		this.ErrorCode = errorCode.Value;
		this.ErrorMessage = errorMessage;
	}
}
