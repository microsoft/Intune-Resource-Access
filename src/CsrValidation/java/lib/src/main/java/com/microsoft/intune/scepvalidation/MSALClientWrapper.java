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
import java.net.Proxy;
import java.util.Properties;
import java.util.Set;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Executors;

import javax.naming.ServiceUnavailableException;
import javax.net.ssl.SSLSocketFactory;

import com.microsoft.aad.msal4j.ClientCredentialFactory;
import com.microsoft.aad.msal4j.ClientCredentialParameters;
import com.microsoft.aad.msal4j.ConfidentialClientApplication;
import com.microsoft.aad.msal4j.ConfidentialClientApplication.Builder;
import com.microsoft.aad.msal4j.IAuthenticationResult;
/**
 * MSAL Authentication Client
 */
public class MSALClientWrapper 
{

    private String authority = "https://login.microsoftonline.com/";
    private String azureAppId = null;
    private ExecutorService service = null;
    private Builder builder = null;
    
    /**
     * MSAL Authentication Client
     * @param aadTenant - Azure tenant
     * @throws IllegalArgumentException
     */
    public MSALClientWrapper(String aadTenant, Properties props) throws IllegalArgumentException
    {
        if(aadTenant == null || aadTenant.isEmpty())
        {
            throw new IllegalArgumentException("The argument 'aadTenant' is missing");
        }
        
        if(props == null)
        {
            throw new IllegalArgumentException("The argument 'props' is missing");
        }
        
        this.authority = props.getProperty("AUTH_AUTHORITY",this.authority);
        
        this.azureAppId = props.getProperty("AAD_APP_ID");
        if(this.azureAppId == null || this.azureAppId.isEmpty())
        {
            throw new IllegalArgumentException("The argument 'AAD_APP_ID' is missing");
        }
        
        String azureAppKey = props.getProperty("AAD_APP_KEY");
        if(azureAppKey == null || azureAppKey.isEmpty())
        {
            throw new IllegalArgumentException("The argument 'AAD_APP_KEY' is missing");
        }
        
        this.service = Executors.newFixedThreadPool(1);

        try 
        {
            builder = ConfidentialClientApplication
                    .builder(azureAppId, ClientCredentialFactory.createFromSecret(azureAppKey))
                    .authority(authority + aadTenant);
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
    public void SetSslSocketFactory(SSLSocketFactory factory) throws IllegalArgumentException
    {
        if(factory == null)
        {
            throw new IllegalArgumentException("The argument 'factory' is missing.");
        }
        
        this.builder.sslSocketFactory(factory);
    }
    
    /**
     * Sets the proxy to be used by the client for any HTTP or HTTPS calls
     * @param proxy
     */
    public void SetProxy(Proxy proxy)
    {
        this.builder.proxy(proxy);
    }
    
    /**
     * Gets an access token from MSAL for the specified scopes.
     * @param sopes Scopes to request access for.
     * @return
     * @throws MalformedURLException 
     * @throws ServiceUnavailableException 
     */    
    public String getAccessToken(Set<String> scopes) throws MalformedURLException, ServiceUnavailableException {

        IAuthenticationResult result;

        ClientCredentialParameters params = ClientCredentialParameters.builder(scopes).build();

        ConfidentialClientApplication app = builder.build();
        result = app.acquireToken(params).join();

        if (result == null) 
        {
            throw new ServiceUnavailableException("Authentication result was null");
        }
        
        return result.accessToken();
    }
    
    @Override
    public void finalize()
    {
        service.shutdown();
    }
}