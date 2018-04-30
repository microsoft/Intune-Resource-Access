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
import java.net.MalformedURLException;
import java.net.UnknownHostException;
import java.util.HashMap;
import java.util.Map.Entry;
import java.util.Properties;
import java.util.UUID;
import java.util.concurrent.ExecutionException;

import javax.naming.ServiceUnavailableException;
import javax.net.ssl.SSLSocketFactory;

import org.apache.http.HttpEntity;
import org.apache.http.StatusLine;
import org.apache.http.client.ClientProtocolException;
import org.apache.http.client.methods.CloseableHttpResponse;
import org.apache.http.client.methods.HttpGet;
import org.apache.http.client.methods.HttpPost;
import org.apache.http.config.Registry;
import org.apache.http.config.RegistryBuilder;
import org.apache.http.conn.HttpClientConnectionManager;
import org.apache.http.conn.socket.ConnectionSocketFactory;
import org.apache.http.conn.ssl.SSLConnectionSocketFactory;
import org.apache.http.conn.ssl.DefaultHostnameVerifier;
import org.apache.http.entity.StringEntity;
import org.apache.http.impl.client.CloseableHttpClient;
import org.apache.http.impl.client.HttpClientBuilder;
import org.apache.http.impl.client.HttpClients;
import org.apache.http.impl.conn.BasicHttpClientConnectionManager;
import org.apache.http.util.EntityUtils;
import org.json.JSONException;
import org.json.JSONObject;

import com.microsoft.aad.adal4j.AuthenticationException;
import com.microsoft.aad.adal4j.AuthenticationResult;
import com.microsoft.aad.adal4j.ClientCredential;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/**
 * IntuneClient - A client which can be used to make requests to Intune services.
 * This object uses ADAL libraries and tokens for authentication with Intune.  
 */
class IntuneClient 
{

    protected String intuneAppId = "0000000a-0000-0000-c000-000000000000";
    protected String intuneResourceUrl = "https://api.manage.microsoft.com/";
    protected String graphApiVersion = "1.6";
    protected String graphResourceUrl = "https://graph.microsoft.com/";
    
    protected String intuneTenant;
    protected ClientCredential aadCredential;
    protected ADALClientWrapper authClient;
    
    protected SSLSocketFactory sslSocketFactory = null;
    protected HttpClientBuilder httpClientBuilder = null;
    
    private HashMap<String,String> serviceMap = new HashMap<String,String>();
    
    final Logger log = LoggerFactory.getLogger(IntuneClient.class);
    
    /**
     * Constructs an IntuneClient object which can be used to make requests to Intune services.
     * @param configProperties Properties object containing client configuration information.
     * @throws IllegalArgumentException
     * @throws IOException 
     */
    public IntuneClient(Properties configProperties) throws IllegalArgumentException, IOException
    {
    	if(configProperties == null)
		{
			throw new IllegalArgumentException("The argument 'configProperties' is missing"); 
		}
    	
    	// Read required properties
    	String azureAppId = configProperties.getProperty("AAD_APP_ID");
    	if(azureAppId == null || azureAppId.isEmpty())
    	{
    		throw new IllegalArgumentException("The argument 'azureAppId' is missing");
    	}
    	
    	String azureAppKey = configProperties.getProperty("AAD_APP_KEY");
    	if(azureAppKey == null || azureAppKey.isEmpty())
    	{
    		throw new IllegalArgumentException("The argument 'azureAppKey' is missing");
    	}
    	
    	this.intuneTenant = configProperties.getProperty("TENANT");
    	if(this.intuneTenant == null || this.intuneTenant.isEmpty())
    	{
    		throw new IllegalArgumentException("The argument 'intuneTenant' is missing");
    	}
    	
    	// Read optional properties
    	this.intuneAppId = configProperties.getProperty("INTUNE_APP_ID", this.intuneAppId);
        this.intuneResourceUrl = configProperties.getProperty("INTUNE_RESOURCE_URL", this.intuneResourceUrl);
        this.graphApiVersion = configProperties.getProperty("GRAPH_API_VERSION", this.graphApiVersion);
        this.graphResourceUrl = configProperties.getProperty("GRAPH_RESOURCE_URL", this.graphResourceUrl);
        
        // Instantiate ADAL Client
    	this.aadCredential = new ClientCredential(azureAppId, azureAppKey);
    	this.authClient = new ADALClientWrapper(this.intuneTenant, this.aadCredential, configProperties);
    }    
    
    /**
     * Sets the SSL factory to be used for all HTTP clients.
     * @param factory
     */
    public void setSslSocketFactory(SSLSocketFactory factory) throws IllegalArgumentException
    {
    	if(factory == null)
    	{
    		throw new IllegalArgumentException("The argument 'factory' is missing.");
    	}
    	
    	this.authClient.setSslSocketFactory(factory);
    	
    	this.sslSocketFactory = factory;
    	
		String[] cipherSuiteList = {
			    "TLS_RSA_WITH_AES_128_CBC_SHA",
				"TLS_DHE_RSA_WITH_AES_128_CBC_SHA",
				"TLS_RSA_WITH_AES_256_CBC_SHA",
				"TLS_DHE_RSA_WITH_AES_256_CBC_SHA",
				"TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA",
				"TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA"
			};  
			   
		this.httpClientBuilder = HttpClientBuilder.create();
		SSLConnectionSocketFactory sslConnectionFactory = new SSLConnectionSocketFactory(this.sslSocketFactory, new String[] { "TLSv1.2" }, cipherSuiteList, new DefaultHostnameVerifier());
		this.httpClientBuilder.setSSLSocketFactory(sslConnectionFactory);
		
		Registry<ConnectionSocketFactory> registry = RegistryBuilder.<ConnectionSocketFactory>create()
		        .register("https", sslConnectionFactory)
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
    	CloseableHttpClient httpclient = this.getCloseableHttpClient(intuneRequestUrl);
        HttpPost httpPost = new HttpPost(intuneRequestUrl);
        httpPost.addHeader("Authorization", "Bearer " + authResult.getAccessToken());
        httpPost.addHeader("content-type", "application/json");
        httpPost.addHeader("client-request-id", activityId.toString());
        httpPost.addHeader("api-version", apiVersion);
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
        CloseableHttpClient httpclient = this.getCloseableHttpClient(graphRequest);
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
	
	private CloseableHttpClient getCloseableHttpClient(String serviceUrl) throws MalformedURLException 
	{
		if(this.httpClientBuilder == null)
		{
			return HttpClients.createDefault();
		}

		return this.httpClientBuilder.build();
	}
}
