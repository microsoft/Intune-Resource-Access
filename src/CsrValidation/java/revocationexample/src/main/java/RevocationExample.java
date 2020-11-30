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

import java.io.InputStream;
import java.util.List;
import java.util.ArrayList;
import java.util.Properties;
import java.util.UUID;

import com.microsoft.intune.scepvalidation.IntuneRevocationClient;
import com.microsoft.intune.carequest.CARequestErrorCodes;
import com.microsoft.intune.carequest.CARevocationRequest;
import com.microsoft.intune.carequest.CARevocationResult;
import com.microsoft.intune.scepvalidation.IntuneClientException;

public class RevocationExample 
{
    public static void main(String args[]) throws Exception 
    {        
        // *** IMPORTANT ***: This property file contains a parameter named AAD_APP_KEY.  This parameter is a secret and needs to be secured.
        //                    Please secure this file properly on your file system.
        InputStream in = RevocationExample.class.getResourceAsStream("com.microsoft.intune.props");
        Properties props = new Properties();
        props.load(in);
        in.close();
        
        UUID transactionId = UUID.randomUUID();        

        // Create IntuneRevocationClient 
        IntuneRevocationClient client = new IntuneRevocationClient(props);
        
        // Set Download Parameters
        int maxRequests = 10; // Maximum number of Revocation requests to download at a time
        String certificateProviderName = null; // Optional Parameter: Set this value if you want to filter 
                                               //   the request to only download request matching this CA Name
        String issuerName = null; // Optional Parameter: Set this value if you want to filter 
                                  //   the request to only download request matching this Issuer Name
        
        try 
        {
        	// Download CARevocationRequests from Intune
        	List<CARevocationRequest> revocationRequests = client.DownloadCARevocationRequests(transactionId.toString(), maxRequests, certificateProviderName, issuerName);
        	
        	// Process CARevocationRequest List
        	List<CARevocationResult> caRevocationResults = new ArrayList<CARevocationResult>();
        	for (CARevocationRequest revocationRequest : revocationRequests)
        	{
        		// Revoke Certificate
        		CARevocationResult result = revokeCertificate(revocationRequest);

                // Add result to list
        		caRevocationResults.add(result);
        	}
        	
        	 if (caRevocationResults.size() > 0)
             {
        		 System.out.println("Uploading" + caRevocationResults.size() + " Revocation Results to Intune.");

                 // Upload Results to Intune
                 client.UploadRevocationResults(transactionId.toString(), caRevocationResults);
             }
        }
        catch(IntuneClientException e)
        {
            // ERROR Handling for known exception scenario here
            System.exit(1);
        }
        catch(Exception e)
        {
            // ERROR Handling for unknown exception here
            System.exit(1);
        }
        
        System.exit(0);
    }
    
    private static CARevocationResult revokeCertificate(CARevocationRequest revocationRequest)
    {
    	// PLACEHOLDER: Add Revoke Certificate Handling
    	
    	return new CARevocationResult(revocationRequest.requestContext, true, CARequestErrorCodes.None, null);
    }
 }
