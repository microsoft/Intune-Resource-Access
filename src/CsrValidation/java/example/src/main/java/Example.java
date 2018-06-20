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
import java.util.Properties;
import java.util.UUID;

import com.microsoft.intune.scepvalidation.IntuneScepServiceClient;
import com.microsoft.intune.scepvalidation.IntuneScepServiceException;

public class Example 
{
    public static void main(String args[]) throws Exception 
    {        
        // *** IMPORTANT ***: This property file contains a parameter named AAD_APP_KEY.  This parameter is a secret and needs to be secured.
        //                    Please secure this file properly on your file system.
        InputStream in = Example.class.getResourceAsStream("com.microsoft.intune.props");
        Properties props = new Properties();
        props.load(in);
        in.close();
		
        UUID transactionId = UUID.randomUUID();
        String csr = "BASE64 Encoded CSR would go here";
        
        IntuneScepServiceClient client = new IntuneScepServiceClient(props);
        
        // ** IMPORTANT ***: If the customer's environment goes through a proxy you will need to provide a custom socket factory
        //                   Below is an example of a factory which handles proxy with or without authentication.
        //SSLTunnelSocketFactory sslFactory = new SSLTunnelSocketFactory("127.0.0.1", new Integer(8888).toString(), "proxyUser", "proxyPass");
        //SSLTunnelSocketFactory sslFactory = new SSLTunnelSocketFactory("127.0.0.1", new Integer(8888).toString());
        //client.SetSslSocketFactory(sslFactory);
        
        try 
        {
        	client.ValidateRequest(transactionId.toString(), csr);
            
            client.SendSuccessNotification(transactionId.toString(), csr, "thumbprint", "serial", "2018-06-11T16:11:20.0904778Z", "authority");
            
            client.SendFailureNotification(transactionId.toString(), csr, 0x8000ffff, "description");	
        }
        catch(IntuneScepServiceException e)
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
 }
