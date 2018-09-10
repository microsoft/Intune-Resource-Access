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
import java.net.Authenticator;
import java.net.InetSocketAddress;
import java.net.PasswordAuthentication;
import java.net.Proxy;
import java.net.UnknownHostException;
import java.util.HashMap;
import java.util.Map;
import java.util.Map.Entry;
import java.util.Properties;
import java.util.UUID;
import java.util.concurrent.ExecutionException;

import javax.naming.ServiceUnavailableException;
import javax.net.ssl.SSLSocketFactory;

import org.apache.http.HttpEntity;
import org.apache.http.HttpHost;
import org.apache.http.StatusLine;
import org.apache.http.auth.AuthScope;
import org.apache.http.auth.Credentials;
import org.apache.http.auth.UsernamePasswordCredentials;
import org.apache.http.client.ClientProtocolException;
import org.apache.http.client.CredentialsProvider;
import org.apache.http.client.methods.CloseableHttpResponse;
import org.apache.http.client.methods.HttpGet;
import org.apache.http.client.methods.HttpPost;
import org.apache.http.config.Registry;
import org.apache.http.config.RegistryBuilder;
import org.apache.http.conn.HttpClientConnectionManager;
import org.apache.http.conn.socket.ConnectionSocketFactory;
import org.apache.http.conn.socket.PlainConnectionSocketFactory;
import org.apache.http.conn.ssl.DefaultHostnameVerifier;
import org.apache.http.conn.ssl.SSLConnectionSocketFactory;
import org.apache.http.entity.StringEntity;
import org.apache.http.impl.client.BasicCredentialsProvider;
import org.apache.http.impl.client.CloseableHttpClient;
import org.apache.http.impl.client.HttpClientBuilder;
import org.apache.http.impl.client.HttpClients;
import org.apache.http.impl.conn.BasicHttpClientConnectionManager;
import org.apache.http.util.EntityUtils;
import org.json.JSONException;
import org.json.JSONObject;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

import com.microsoft.aad.adal4j.AuthenticationException;
import com.microsoft.aad.adal4j.AuthenticationResult;
import com.microsoft.aad.adal4j.ClientCredential;

/**
 * IntuneClient - A client which can be used to make requests to Intune services.
 * This object uses ADAL libraries and tokens for authentication with Intune.  
 */
class IntuneClient 
{
    protected String intuneAppId = "0000000a-0000-0000-c000-000000000000";
    protected String intuneResourceUrl = "https://api.manage.microsoft.com/";
    protected String graphApiVersion = "1.6";
    protected String graphResourceUrl = "https://graph.windows.net/";
    
    protected String intuneTenant;
    protected ClientCredential aadCredential;
    protected ADALClientWrapper authClient;
    
    protected SSLSocketFactory sslSocketFactory = null;
    protected HttpClientBuilder httpClientBuilder = null;
    
    protected String proxyHost = null;
    protected Integer proxyPort = null;
    protected String proxyUser = null;
    protected String proxyPass = null;
    
    private HashMap<String,String> serviceMap = new HashMap<String,String>();
    
    final Logger log = LoggerFactory.getLogger(IntuneClient.class);
    
    /**
     * Constructs an IntuneClient object which can be used to make requests to Intune services.
     * @param configProperties Properties object containing client configuration information.
     * @throws IllegalArgumentException
     */
    public IntuneClient(Properties configProperties) throws IllegalArgumentException
    {
        this(configProperties, null, null);
    }
    
    /**
     * Constructs an IntuneClient object.  This is meant to be used for unit tests for dependency injection.
     * @param configProperties
     * @param authClient
     * @param httpClientBuilder
     * @throws IllegalArgumentException
     */
    public IntuneClient(Properties configProperties, ADALClientWrapper authClient, HttpClientBuilder httpClientBuilder) throws IllegalArgumentException
    {        
        if(configProperties == null)
        {
            throw new IllegalArgumentException("The argument 'configProperties' is missing"); 
        }
        
        // Read required properties
        String azureAppId = configProperties.getProperty("AAD_APP_ID");
        if(azureAppId == null || azureAppId.isEmpty())
        {
            throw new IllegalArgumentException("The argument 'AAD_APP_ID' is missing");
        }
        
        String azureAppKey = configProperties.getProperty("AAD_APP_KEY");
        if(azureAppKey == null || azureAppKey.isEmpty())
        {
            throw new IllegalArgumentException("The argument 'AAD_APP_KEY' is missing");
        }
        
        this.intuneTenant = configProperties.getProperty("TENANT");
        if(this.intuneTenant == null || this.intuneTenant.isEmpty())
        {
            throw new IllegalArgumentException("The argument 'TENANT' is missing");
        }
        
        // Read optional properties
        this.intuneAppId = configProperties.getProperty("INTUNE_APP_ID", this.intuneAppId);
        this.intuneResourceUrl = configProperties.getProperty("INTUNE_RESOURCE_URL", this.intuneResourceUrl);
        this.graphApiVersion = configProperties.getProperty("GRAPH_API_VERSION", this.graphApiVersion);
        this.graphResourceUrl = configProperties.getProperty("GRAPH_RESOURCE_URL", this.graphResourceUrl);
        
        // Instantiate ADAL Client
        this.aadCredential = new ClientCredential(azureAppId, azureAppKey);
        
        this.authClient = authClient == null ? new ADALClientWrapper(this.intuneTenant, this.aadCredential, configProperties) : authClient;
        this.httpClientBuilder = httpClientBuilder == null ? this.httpClientBuilder : httpClientBuilder;
        
        proxyHost = configProperties.getProperty("PROXY_HOST");
        try
        {
            proxyPort = Integer.parseInt(configProperties.getProperty("PROXY_PORT"));
        }
        catch(NumberFormatException e)
        {
            throw new IllegalArgumentException("'PROXY_PORT' must be a value that can be converted to an integer.", e);
        }
        
        if(!(proxyPort >= 0 && proxyPort <= 65535))
        {
            throw new IllegalArgumentException("'PROXY_PORT' must be in the range of available ports 0-65535");
        }
        
        if((this.proxyHost != null && !this.proxyHost.isEmpty()) && 
           (this.proxyPort == null))
        {
            throw new IllegalArgumentException("If the argument 'PROXY_HOST' is set then 'PROXY_PORT' must also be set.");
        }
        if((this.proxyPort != null) && 
           (this.proxyHost == null || this.proxyHost.isEmpty()))
        {
            throw new IllegalArgumentException("If the argument 'PROXY_PORT' is set then 'PROXY_HOST' must also be set.");
        }
        
        proxyUser = configProperties.getProperty("PROXY_USER");
        proxyPass = configProperties.getProperty("PROXY_PASS");
        if((this.proxyUser != null && !this.proxyUser.isEmpty()) && 
           (this.proxyPass == null || this.proxyPass.isEmpty()))
        {
            throw new IllegalArgumentException("If the argument 'PROXY_USER' is set then 'PROXY_PASS' must also be set.");
        }
        if((this.proxyPass != null && !this.proxyPass.isEmpty()) && 
           (this.proxyUser == null || this.proxyUser.isEmpty()))
        {
            throw new IllegalArgumentException("If the argument 'PROXY_PASS' is set then 'PROXY_USER' must also be set.");
        }
        
        setProxy();
    }
    
    /**
     * Sets the SSL factory to be used for all HTTP clients.
     * @param factory
     */
    public void SetSslSocketFactory(SSLSocketFactory factory) throws IllegalArgumentException
    {
        if(factory == null)
        {
            throw new IllegalArgumentException("The argument 'factory' is missing.");
        }
        
        this.log.info("Setting SSL Socket Factory");
        
        this.authClient.SetSslSocketFactory(factory);
        
        this.sslSocketFactory = factory;
               
        this.httpClientBuilder = HttpClientBuilder.create();
        SSLConnectionSocketFactory sslConnectionFactory = new SSLConnectionSocketFactory(this.sslSocketFactory, new String[] { "TLSv1.2" }, null, new DefaultHostnameVerifier());
        this.httpClientBuilder.setSSLSocketFactory(sslConnectionFactory);
        
        setProxy();
        
        Registry<ConnectionSocketFactory> registry = RegistryBuilder.<ConnectionSocketFactory>create()
                .register("https", sslConnectionFactory)
                .register("http", PlainConnectionSocketFactory.getSocketFactory())
                .build();
        
        HttpClientConnectionManager ccm = new BasicHttpClientConnectionManager(registry);
        
        this.httpClientBuilder.setConnectionManager(ccm);
    }
    
    /**
     * Post a Request to an Intune rest service.
     * @param serviceName The name of the service to post to.
     * @param urlSuffix The end of the url to tack onto the request.
     * @param apiVersion API Version of service to use.
     * @param json The body of the request.
     * @param activityId Client generated ID for correlation of this activity
     * @return JSON response from service
     * @throws AuthenticationException
     * @throws ExecutionException 
     * @throws InterruptedException 
     * @throws ServiceUnavailableException 
     * @throws IOException 
     * @throws ClientProtocolException 
     * @throws IllegalArgumentException 
     * @throws IntuneClientException 
     */
    public JSONObject PostRequest(String serviceName, String urlSuffix, String apiVersion, JSONObject json, UUID activityId) throws ServiceUnavailableException, InterruptedException, ExecutionException, ClientProtocolException, IOException, AuthenticationException, IllegalArgumentException, IntuneClientException
    {
        return this.PostRequest(serviceName, urlSuffix, apiVersion, json, activityId, null);
    }
    
    /**
     * Post a Request to an Intune rest service.
     * @param serviceName The name of the service to post to.
     * @param urlSuffix The end of the url to tack onto the request.
     * @param apiVersion API Version of service to use.
     * @param json The body of the request.
     * @param activityId Client generated ID for correlation of this activity
     * @param additionalHeaders key value pairs of additional header values to add to the request
     * @return JSON response from service
     * @throws AuthenticationException
     * @throws ExecutionException 
     * @throws InterruptedException 
     * @throws ServiceUnavailableException 
     * @throws IOException 
     * @throws ClientProtocolException 
     * @throws IllegalArgumentException 
     * @throws IntuneClientException 
     */
    public JSONObject PostRequest(String serviceName, String urlSuffix, String apiVersion, JSONObject json, UUID activityId, Map<String,String> additionalHeaders) throws ServiceUnavailableException, InterruptedException, ExecutionException, ClientProtocolException, IOException, AuthenticationException, IllegalArgumentException, IntuneClientException
    {
        if(serviceName == null || serviceName.isEmpty())
        {
            throw new IllegalArgumentException("The argument 'serviceName' is missing");
        }
        
        if(urlSuffix == null || urlSuffix.isEmpty())
        {
            throw new IllegalArgumentException("The argument 'urlSuffix' is missing");
        }
        
        if(apiVersion == null || apiVersion.isEmpty())
        {
            throw new IllegalArgumentException("The argument 'apiVersion' is missing");
        }
        
        if(json == null)
        {
            throw new IllegalArgumentException("The argument 'json' is missing");
        }
        
        
        String intuneServiceEndpoint = GetServiceEndpoint(serviceName);
        if(intuneServiceEndpoint == null || intuneServiceEndpoint.isEmpty())
        {
            IntuneServiceNotFoundException ex = new IntuneServiceNotFoundException(serviceName);
            this.log.error(ex.getMessage(), ex);
            throw ex;
        }
        
        AuthenticationResult authResult = this.authClient.getAccessTokenFromCredential(this.intuneResourceUrl);
        
        String intuneRequestUrl = intuneServiceEndpoint + "/" + urlSuffix;
        CloseableHttpClient httpclient = this.getCloseableHttpClient();
        HttpPost httpPost = new HttpPost(intuneRequestUrl);
        httpPost.addHeader("Authorization", "Bearer " + authResult.getAccessToken());
        httpPost.addHeader("content-type", "application/json");
        httpPost.addHeader("client-request-id", activityId.toString());
        httpPost.addHeader("api-version", apiVersion);
        
        if(additionalHeaders != null)
        {
            for (Map.Entry<String, String> entry : additionalHeaders.entrySet())
            {
                httpPost.addHeader(entry.getKey(), entry.getValue());
            }
        }
        
        httpPost.setEntity(new StringEntity(json.toString()));
        
        CloseableHttpResponse intuneResponse = null;
        JSONObject jsonResult = null;
        try 
        {
            intuneResponse = httpclient.execute(httpPost);
            jsonResult = ParseResponseToJSON(intuneResponse, intuneRequestUrl, activityId);
        }
        catch(UnknownHostException e)
        {
            this.log.error("Failed to contact intune service with URL: " + intuneRequestUrl, e);
            serviceMap.clear(); // clear contents in case the service location has changed and we cached the value
            throw e;
        }
        finally 
        {    
            if(intuneResponse != null)
                intuneResponse.close();
        }
        return jsonResult;
    }
    
    private synchronized String GetServiceEndpoint(String serviceName) throws ServiceUnavailableException, ClientProtocolException, AuthenticationException, InterruptedException, ExecutionException, IOException, IntuneClientException
    {
        if(serviceName == null || serviceName.isEmpty())
        {
            throw new IllegalArgumentException("The argument 'serviceName' is missing");
        }
        
        String serviceNameLower = serviceName.toLowerCase();
        
        // Pull down the service map if we haven't populated it OR we are forcing a refresh
        if(serviceMap.size() <= 0)
        {
            this.log.info("Refreshing service map from Microsoft.Graph");
            RefreshServiceMap();
        }

        if(serviceMap.containsKey(serviceNameLower))
        {
            return serviceMap.get(serviceNameLower);
        }
        
        // LOG Cache contents
        this.log.info("Could not find endpoint for service '" + serviceName + "'");
        this.log.info("ServiceMap: ");
        for(Entry<String, String> entry:serviceMap.entrySet())
        {
            this.log.info(entry.getKey() + ":" + entry.getValue());
        }
        
        return null;
    }
    
    private void RefreshServiceMap() throws ServiceUnavailableException, InterruptedException, ExecutionException, ClientProtocolException, IOException, AuthenticationException, IntuneClientException
    {
        AuthenticationResult authResult = this.authClient.getAccessTokenFromCredential(this.graphResourceUrl);
        
        String graphRequest = this.graphResourceUrl + intuneTenant + "/servicePrincipalsByAppId/" + this.intuneAppId + "/serviceEndpoints?api-version=" + this.graphApiVersion;
        
        UUID activityId = UUID.randomUUID();
        CloseableHttpClient httpclient = this.getCloseableHttpClient();
        HttpGet httpGet = new HttpGet(graphRequest);
        httpGet.addHeader("Authorization", "Bearer " + authResult.getAccessToken());
        httpGet.addHeader("client-request-id", activityId.toString());
        CloseableHttpResponse graphResponse = null;
        try 
        {
            graphResponse = httpclient.execute(httpGet);

            JSONObject jsonResult = ParseResponseToJSON(graphResponse, graphRequest, activityId);
            
            for(Object obj:jsonResult.getJSONArray("value"))
            {
                JSONObject jObj = (JSONObject)obj;
                serviceMap.put(jObj.getString("serviceName").toLowerCase(), jObj.getString("uri"));
            } 
        } 
        finally 
        {
            if(graphResponse != null)
                graphResponse.close();
        }
    }
    
    private JSONObject ParseResponseToJSON(CloseableHttpResponse response, String requestUrl, UUID activityId) throws IntuneClientException, IOException
    {
        JSONObject jsonResult = null;
        HttpEntity httpEntity = null;
        try 
        {
            httpEntity = response.getEntity();
            if(httpEntity == null)
            {
                throw new IntuneClientException("ActivityId: " + activityId + " Unable to get httpEntity from response getEntity returned null.");
            }
            

            String httpEntityStr = null;
            try
            {
                httpEntityStr = EntityUtils.toString(httpEntity);
            }
            catch(IllegalArgumentException|IOException e)
            {
                throw new IntuneClientException("ActivityId: " + activityId + " Unable to convert httpEntity from response to string", e);
            }
            
            try
            {
                jsonResult = new JSONObject(httpEntityStr);
            }
            catch(JSONException e)
            {
                throw new IntuneClientException("ActivityId: " + activityId + " Unable to parse response from Intune to JSON", e);
            }
            
            StatusLine statusLine = response.getStatusLine();
            if(statusLine == null)
            {
                throw new IntuneClientException("ActivityId: " + activityId + " Unable to retrieve status line from intune response");
            }
            
            int statusCode = statusLine.getStatusCode();
            if(statusCode < 200 || statusCode >= 300)
            {
                String msg = "Request to: " + requestUrl + " returned: " + statusLine;
                IntuneClientHttpErrorException ex = new IntuneClientHttpErrorException(statusLine, jsonResult, activityId);
                this.log.error(msg, ex);
                throw ex;
            }
        } 
        finally 
        {
            if(httpEntity != null)
                EntityUtils.consume(httpEntity);
        }
        
        return jsonResult;
    }
    
    private CloseableHttpClient getCloseableHttpClient() 
    {
        if(this.httpClientBuilder == null)
        {
            return HttpClients.createDefault();
        }

        return this.httpClientBuilder.build();
    }
    
    private void setProxy()
    {
        if(proxyHost != null && !proxyHost.isEmpty() &&
           proxyPort != null)
         {
            this.log.info("Setting IntuneClient ProxyHost:" + proxyHost + " ProxyPort:" + proxyPort);
            this.authClient.SetProxy(new Proxy(Proxy.Type.HTTP, new InetSocketAddress(proxyHost, proxyPort)));

            if(this.httpClientBuilder == null)
            {
                this.httpClientBuilder = HttpClients.custom();
            }
            this.log.info("Setting IntuneClient ProxyHost:" + proxyHost + " ProxyPort:" + proxyPort);
            this.httpClientBuilder.setProxy(new HttpHost(proxyHost, proxyPort));
             
            if(proxyUser != null && !proxyUser.isEmpty() &&
               proxyPass != null && !proxyPass.isEmpty())
            {
               this.log.info("Setting IntuneClient Proxy to use Basic Authentication.");
                 
               Credentials credentials = new UsernamePasswordCredentials(proxyUser, proxyPass);
               CredentialsProvider credsProvider = new BasicCredentialsProvider();
               credsProvider.setCredentials( new AuthScope(proxyHost, proxyPort), credentials);
                 
               httpClientBuilder.setDefaultCredentialsProvider(credsProvider);
                 
                System.setProperty("jdk.http.auth.tunneling.disabledSchemes", "");
                Authenticator.setDefault(new Authenticator() {
                    @Override
                    protected PasswordAuthentication getPasswordAuthentication() {
                        return new PasswordAuthentication(proxyUser, proxyPass.toCharArray());
                    }
                });
            }
         }
    }
}
