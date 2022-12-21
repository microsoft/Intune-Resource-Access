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

import static org.mockito.ArgumentMatchers.anyString;
import static org.mockito.ArgumentMatchers.argThat;
import static org.mockito.Mockito.mock;
import static org.mockito.Mockito.when;

import java.io.ByteArrayInputStream;
import java.io.IOException;
import java.net.MalformedURLException;
import java.util.Properties;
import java.util.concurrent.ExecutionException;

import javax.naming.ServiceUnavailableException;

import org.apache.http.HttpEntity;
import org.apache.http.StatusLine;
import org.apache.http.client.ClientProtocolException;
import org.apache.http.client.methods.CloseableHttpResponse;
import org.apache.http.client.methods.HttpUriRequest;
import org.apache.http.impl.client.CloseableHttpClient;
import org.apache.http.impl.client.HttpClientBuilder;
import org.mockito.ArgumentMatcher;
import org.mockito.ArgumentMatchers;

import com.microsoft.aad.adal4j.AuthenticationResult;
import com.microsoft.intune.scepvalidation.ADALClientWrapper;
import com.microsoft.intune.scepvalidation.IntuneRevocationClient;
import com.microsoft.intune.scepvalidation.IntuneScepServiceClient;
import com.microsoft.intune.scepvalidation.IntuneScepServiceException;
import com.microsoft.intune.scepvalidation.MSALClientWrapper;

public class Helper
{
    public static final String GRAPH_URL = "graph.windows.net";
    public static final String MSAL_URL = "graph.microsoft.com";
    public static final String GOOD_GRAPH_SERVICE_DISCOVERY_RESPONSE = "{"
            + "value: ["
            + "{"
                + "serviceName:" + IntuneScepServiceClient.VALIDATION_SERVICE_NAME + ","
                + "uri:'https://fef.dmsua01.manage-dogfood.microsoft.com/RACerts/ScepRequestValidationFEService/Gateway/StatelessScepRequestValidationService'"
            + "},"
            + "{"
            	+ "serviceName:" + IntuneRevocationClient.CONNECTOR_SERVICE_NAME + ","
            	+ "uri:'https://fef.dmsua01.manage-dogfood.microsoft.com/RACerts/StatelessPkiConnectorService/Gateway/StatelessPkiConnectorService'"
            + "}"
        + "]}";
    public static final String NO_SERVICE_DISCOVERY_RESPONSE = "{"
            + "value: ["
            + "{"
                + "serviceName:nonExistant,"
                + "uri:'https://fef.dmsua01.manage-dogfood.microsoft.com/RACerts/ScepRequestValidationFEService/Gateway/StatelessScepRequestValidationService'"
            + "}"        
        + "]}";
    public static final String GOOD_MSAL_SERVICE_DISCOVERY_RESPONSE = "{"
            + "value: ["
            + "{"
                + "providerName:" + IntuneScepServiceClient.VALIDATION_SERVICE_NAME + ","
                + "uri:'https://fef.dmsua01.manage-dogfood.microsoft.com/RACerts/ScepRequestValidationFEService/Gateway/StatelessScepRequestValidationService'"
            + "},"
            + "{"
                + "providerName:" + IntuneRevocationClient.CONNECTOR_SERVICE_NAME + ","
                + "uri:'https://fef.dmsua01.manage-dogfood.microsoft.com/RACerts/StatelessPkiConnectorService/Gateway/StatelessPkiConnectorService'"
            + "}"
        + "]}";
    public static final String SERVICE_URL = "fef.dmsua01.manage-dogfood.microsoft.com";
    public static final String VALID_SCEP_RESPONSE = "{code:"+IntuneScepServiceException.ErrorCode.Success.name()+",errorDescription:''}";
    public static final String ERROR_SCEP_RESPONSE = "{code:"+IntuneScepServiceException.ErrorCode.ChallengeDecodingError.name()+",errorDescription:''}";
    
    CloseableHttpClient httpClient = mock(CloseableHttpClient.class);
    HttpClientBuilder httpBuilder = mock(HttpClientBuilder.class);
    HttpEntity graphResponseEntity = mock(HttpEntity.class);
    CloseableHttpResponse graphResponse = mock(CloseableHttpResponse.class);
    StatusLine graphStatus = mock(StatusLine.class);
    
    CloseableHttpResponse msalResponse = mock(CloseableHttpResponse.class);
    HttpEntity msalResponseEntity = mock(HttpEntity.class);
    StatusLine msalStatus = mock(StatusLine.class);

    CloseableHttpResponse intuneResponse = mock(CloseableHttpResponse.class);
    HttpEntity intuneResponseEntity = mock(HttpEntity.class);
    StatusLine intuneStatus = mock(StatusLine.class);
    ADALClientWrapper adal;
    MSALClientWrapper msal;
    
    public Properties properties;
    
    public Helper() throws ClientProtocolException, IOException, ServiceUnavailableException, IllegalArgumentException, InterruptedException, ExecutionException
    {
        when(httpBuilder.build()).thenReturn(httpClient);

        when(httpClient.execute(
            argThat(new ArgumentMatcher<HttpUriRequest>() {
                @Override
                public boolean matches(HttpUriRequest resp) {
                    if(resp == null)
                        return false;
                    return resp.getURI().getHost().equals(MSAL_URL);
                }})))
        .thenReturn(msalResponse);
        
        when(msalResponse.getStatusLine())
            .thenReturn(msalStatus);
        when(msalStatus.getStatusCode())
            .thenReturn(200);
        when(msalResponse.getEntity())
            .thenReturn(msalResponseEntity);
        when(msalResponseEntity.getContent())
            .thenReturn(new ByteArrayInputStream(GOOD_MSAL_SERVICE_DISCOVERY_RESPONSE.getBytes()));
        when(msalResponseEntity.getContentLength())
            .thenReturn((long)GOOD_MSAL_SERVICE_DISCOVERY_RESPONSE.length());

        when(httpClient.execute(
                argThat(new ArgumentMatcher<HttpUriRequest>() {
                    @Override
                    public boolean matches(HttpUriRequest resp) {
                        if(resp == null)
                            return false;
                        return resp.getURI().getHost().equals(GRAPH_URL);
                    }})))
            .thenReturn(graphResponse);
        
        when(graphResponse.getEntity())
            .thenReturn(graphResponseEntity);
        when(graphResponse.getStatusLine())
            .thenReturn(graphStatus);
        when(graphStatus.getStatusCode())
            .thenReturn(200);
        when(graphResponseEntity.getContent())
            .thenReturn(new ByteArrayInputStream(GOOD_GRAPH_SERVICE_DISCOVERY_RESPONSE.getBytes()));
        when(graphResponseEntity.getContentLength())
            .thenReturn((long)GOOD_GRAPH_SERVICE_DISCOVERY_RESPONSE.length());

        when(httpClient.execute(
                argThat(new ArgumentMatcher<HttpUriRequest>() {
                    @Override
                    public boolean matches(HttpUriRequest resp) {
                        if(resp == null)
                            return false;
                        return resp.getURI().getHost().equals(SERVICE_URL);
                    }})))
            .thenReturn(intuneResponse);
        
        when(intuneResponse.getEntity())
            .thenReturn(intuneResponseEntity);
        when(intuneResponse.getStatusLine())
            .thenReturn(intuneStatus);
        when(intuneStatus.getStatusCode())
            .thenReturn(200);
        when(intuneResponseEntity.getContent())
            .thenReturn(new ByteArrayInputStream(VALID_SCEP_RESPONSE.getBytes()));
        when(intuneResponseEntity.getContentLength())
            .thenReturn((long)VALID_SCEP_RESPONSE.length());

        adal = getDefaultAdalMock();
        msal = getDefaultMsalMock();
        
        properties = new Properties();
        properties.setProperty("AAD_APP_ID", "1234");
        properties.setProperty("AAD_APP_KEY", "1234");
        properties.setProperty("TENANT", "1234");
        properties.setProperty("PROVIDER_NAME_AND_VERSION", "1234");
    }
    
    public void resetGraphRequest() throws UnsupportedOperationException, IOException
    {
        when(graphResponseEntity.getContent())
            .thenReturn(new ByteArrayInputStream(GOOD_GRAPH_SERVICE_DISCOVERY_RESPONSE.getBytes()));
    }
    
    public void resetMsalRequest() throws UnsupportedOperationException, IOException
    {
        when(msalResponseEntity.getContent())
            .thenReturn(new ByteArrayInputStream(GOOD_MSAL_SERVICE_DISCOVERY_RESPONSE.getBytes()));
    }
    
    private ADALClientWrapper getDefaultAdalMock() throws ServiceUnavailableException, IllegalArgumentException, InterruptedException, ExecutionException
    {
        ADALClientWrapper adalMock = mock(ADALClientWrapper.class);
        when(adalMock.getAccessTokenFromCredential(anyString()))
            .thenReturn(new AuthenticationResult(
                    "accessTokenType", 
                    "accessToken", 
                    "refreshToken", 
                    2000, 
                    "idToken", 
                    null, 
                    true));
        return adalMock;
    }
    
    private MSALClientWrapper getDefaultMsalMock() throws MalformedURLException, ServiceUnavailableException
    {
        MSALClientWrapper adalMock = mock(MSALClientWrapper.class);
        when(adalMock.getAccessToken(ArgumentMatchers.<String>anySet()))
            .thenReturn("accessToken");
        return adalMock;
    }
}