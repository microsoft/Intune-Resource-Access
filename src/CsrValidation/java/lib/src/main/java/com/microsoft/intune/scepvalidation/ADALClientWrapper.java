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

import java.net.MalformedURLException;
import java.util.Properties;
import java.util.concurrent.ExecutionException;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;
import java.util.concurrent.Future;

import javax.naming.ServiceUnavailableException;
import javax.net.ssl.SSLSocketFactory;

import com.microsoft.aad.adal4j.AuthenticationContext;
import com.microsoft.aad.adal4j.AuthenticationResult;
import com.microsoft.aad.adal4j.ClientCredential;

/**
 * Azure Active Directory Authentication Client
 */
class ADALClientWrapper 
{

    private String authority = "https://login.windows.net/";
    private ClientCredential credential = null;
    private ExecutorService service = null;
    private AuthenticationContext context = null;
    
    /**
     * Azure Active Directory Authentication Client
     * @param aadTenant - Azure Active Directory tenant
     * @param credential - Credential to use for authentication
     * @throws IllegalArgumentException
     */
    public ADALClientWrapper(String aadTenant, ClientCredential credential, Properties props) throws IllegalArgumentException
    {
        if(aadTenant == null || aadTenant.isEmpty())
        {
            throw new IllegalArgumentException("The argument 'aadTenant' is missing");
        }
        
        if(credential == null)
        {
            throw new IllegalArgumentException("The argument 'credential' is missing");    
        }
        
        if(props != null)
        {
            this.authority = props.getProperty("AUTH_AUTHORITY",this.authority);
        }
        
        this.credential = credential;
        this.service = Executors.newFixedThreadPool(1);
        
        try 
        {
            context = new AuthenticationContext(this.authority + aadTenant, false, service);
        }
        catch(MalformedURLException e)
        {
            throw new IllegalArgumentException("AUTH_AUTHORITY parameter was not formatted correctly which resulted in a MalformedURLException", e);
        }
    }
    
    /**
     * Sets the SSL factory to be used on the HTTP client for authentication.
     * @param factory
     */
    public void setSslSocketFactory(SSLSocketFactory factory) throws IllegalArgumentException
    {
        if(factory == null)
        {
            throw new IllegalArgumentException("The argument 'factory' is missing.");
        }
        
        this.context.setSslSocketFactory(factory);
    }
    
    /**
     * Gets an access token from AAD for the specified resource using the ClientCredential passed in.
     * @param resource Resource to get token for.
     * @param credential Credential to use to acquire token.
     * @return
     * @throws ExecutionException 
     * @throws IllegalArgumentException
     * @throws InterruptedException 
     * @throws ServiceUnavailableException 
     */
    public AuthenticationResult getAccessTokenFromCredential(String resource) 
            throws ServiceUnavailableException, InterruptedException, ExecutionException, IllegalArgumentException
    {
        if(resource == null || resource.isEmpty())
        {
            throw new IllegalArgumentException("The argument 'resource' is missing");
        }
        
        AuthenticationResult result = null;
        
        Future<AuthenticationResult> future = context.acquireToken(resource, credential, null);
        result = future.get();

        if (result == null) 
        {
            throw new ServiceUnavailableException("Authentication result was null");
        }
        
        return result;
    }
    
    @Override
    public void finalize()
    {
        service.shutdown();
    }
}