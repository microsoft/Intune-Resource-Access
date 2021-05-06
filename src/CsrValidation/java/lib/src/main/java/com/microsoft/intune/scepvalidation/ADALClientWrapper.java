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
import java.util.concurrent.ExecutionException;
import java.util.concurrent.ExecutorService;
import java.util.concurrent.Future;

import javax.naming.ServiceUnavailableException;
import javax.net.ssl.SSLSocketFactory;

import com.microsoft.aad.adal4j.AsymmetricKeyCredential;
import com.microsoft.aad.adal4j.AuthenticationContext;
import com.microsoft.aad.adal4j.AuthenticationResult;
import com.microsoft.aad.adal4j.ClientCredential;

/**
 * Azure Active Directory Authentication Client
 */
public class ADALClientWrapper {

    private String authority = "https://login.microsoftonline.com/";
    private ClientCredential credential = null;
    private AsymmetricKeyCredential asymmetricCredential = null;
    private ExecutorService service = null;
    private AuthenticationContext context = null;

    /**
     * Azure Active Directory Authentication Client
     * 
     * @param aadTenant
     *            - Azure Active Directory tenant
     * @param credential
     *            - Credential to use for authentication
     * @throws IllegalArgumentException
     */
    public ADALClientWrapper(String aadTenant, ClientCredential credential, Properties props) throws IllegalArgumentException {
        if (aadTenant == null || aadTenant.isEmpty()) {
            throw new IllegalArgumentException("The argument 'aadTenant' is missing");
        }

        if (credential == null) {
            throw new IllegalArgumentException("The argument 'credential' is missing");
        }

        if (props != null) {
            this.authority = props.getProperty("AUTH_AUTHORITY", this.authority);
        }

        this.credential = credential;
        this.service = new CurrentThreadExecutor();

        try {
            context = new AuthenticationContext(this.authority + aadTenant, false, service);
        } catch (MalformedURLException e) {
            throw new IllegalArgumentException("AUTH_AUTHORITY parameter was not formatted correctly which resulted in a MalformedURLException", e);
        }
    }

    /**
     * Azure Active Directory Authentication Client
     * 
     * @param aadTenant
     *            - Azure Active Directory tenant
     * @param credential
     *            - Credential to use for authentication
     * @throws IllegalArgumentException
     */
    public ADALClientWrapper(String aadTenant, AsymmetricKeyCredential credential, Properties props) throws IllegalArgumentException {
        if (aadTenant == null || aadTenant.isEmpty()) {
            throw new IllegalArgumentException("The argument 'aadTenant' is missing");
        }

        if (credential == null) {
            throw new IllegalArgumentException("The argument 'credential' is missing");
        }

        if (props != null) {
            this.authority = props.getProperty("AUTH_AUTHORITY", this.authority);
        }

        this.asymmetricCredential = credential;
        this.service = new CurrentThreadExecutor();

        try {
            context = new AuthenticationContext(this.authority + aadTenant, false, service);
        } catch (MalformedURLException e) {
            throw new IllegalArgumentException("AUTH_AUTHORITY parameter was not formatted correctly which resulted in a MalformedURLException", e);
        }
    }

    /**
     * Sets the SSL factory to be used on the HTTP client for authentication.
     * 
     * @param factory
     */
    public void SetSslSocketFactory(SSLSocketFactory factory) throws IllegalArgumentException {
        if (factory == null) {
            throw new IllegalArgumentException("The argument 'factory' is missing.");
        }

        this.context.setSslSocketFactory(factory);
    }

    /**
     * Sets the proxy to be used by the ADAL library for any HTTP or HTTPS calls
     * 
     * @param proxy
     */
    public void SetProxy(Proxy proxy) {
        this.context.setProxy(proxy);
    }

    /**
     * Gets an access token from AAD for the specified resource using the
     * ClientCredential passed in.
     * 
     * @param resource
     *            Resource to get token for.
     * @return
     * @throws ExecutionException
     * @throws IllegalArgumentException
     * @throws InterruptedException
     * @throws ServiceUnavailableException
     */
    public AuthenticationResult getAccessTokenFromCredential(String resource)
            throws ServiceUnavailableException, InterruptedException, ExecutionException, IllegalArgumentException {
        if (resource == null || resource.isEmpty()) {
            throw new IllegalArgumentException("The argument 'resource' is missing");
        }

        AuthenticationResult result = null;

        Future<AuthenticationResult> future;
        if (credential != null) {
            future = context.acquireToken(resource, credential, null);
        } else {
            future = context.acquireToken(resource, asymmetricCredential, null);
        }
        result = future.get();

        if (result == null) {
            throw new ServiceUnavailableException("Authentication result was null");
        }

        return result;
    }
}