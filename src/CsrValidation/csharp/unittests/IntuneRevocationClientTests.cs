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

namespace UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Intune;
    using Microsoft.Management.Services.Api;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;

    [TestClass]
    public class IntuneRevocationClientTests
    {
        Dictionary<string, string> configProperties = new Dictionary<string, string>()
        {
            {"AAD_APP_ID","appId"},
            {"AAD_APP_KEY","appKey"},
            {"TENANT","tenant"},
            {"PROVIDER_NAME_AND_VERSION","providerName"}
        };

        private JObject validDownloadResponse;
        private List<CARevocationResult> validRequestResults;

        [TestInitialize]
        public void Initialize()
        {
            // Initialize a sample valid response for download
            List<CARevocationRequest> revocationRequests = new List<CARevocationRequest>()
            {
                new CARevocationRequest()
                {
                    RequestContext = "test-context",
                    IssuerName = "contoso-ca",
                    SerialNumber = "1234567890",
                }
            };
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings()
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            validDownloadResponse = new JObject(
                new JProperty("@odata.context", "https://manage.microsoft.com/RACerts/StatelessPkiConnectorService/$metadata#Collection(microsoft.management.services.api.caRevocationRequest)"),
                new JProperty("value", JToken.FromObject(revocationRequests)));

            // Initialize a sample valid result list for upload
            validRequestResults = new List<CARevocationResult>()
            {
                new CARevocationResult("test-context", true, CARequestErrorCode.None, null)
            };
        }

        [TestMethod]
        public async Task DownloadCARevocationRequestsAsync_ValidResponseTest()
        {
            Mock<IIntuneClient> mock = CreateDownloadMock(validDownloadResponse);
            IntuneRevocationClient client = new IntuneRevocationClient(configProperties, intuneClient: mock.Object);
            string transactionId = Guid.NewGuid().ToString();
            await client.DownloadCARevocationRequestsAsync(transactionId, 10);
        }

        [TestMethod]
        [ExpectedException(typeof(IntuneClientException))]
        public async Task DownloadCARevocationRequestsAsync_InvalidResponseTest()
        {
            string invalidResponse = "This is an invalid response that should fail deserialization";
            Mock<IIntuneClient> mock = CreateDownloadMock(new JObject(new JProperty("value", invalidResponse)));
            IntuneRevocationClient client = new IntuneRevocationClient(configProperties, intuneClient: mock.Object);
            string transactionId = Guid.NewGuid().ToString();
            await client.DownloadCARevocationRequestsAsync(transactionId, 10);
        }

        [TestMethod]
        [ExpectedException(typeof(IntuneClientException))]
        public async Task DownloadCARevocationRequestsAsync_InvalidResponseTest_NoValue()
        {
            Mock<IIntuneClient> mock = CreateDownloadMock(new JObject(new JProperty("not_value", "test")));
            IntuneRevocationClient client = new IntuneRevocationClient(configProperties, intuneClient: mock.Object);
            string transactionId = Guid.NewGuid().ToString();
            await client.DownloadCARevocationRequestsAsync(transactionId, 10);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task DownloadCARevocationRequestsAsync_InvalidTransactionId()
        {
            Mock<IIntuneClient> mock = CreateDownloadMock(validDownloadResponse);
            IntuneRevocationClient client = new IntuneRevocationClient(configProperties, intuneClient: mock.Object);
            string transactionId = null;
            await client.DownloadCARevocationRequestsAsync(transactionId, 10);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public async Task DownloadCARevocationRequestsAsync_NegativeMaxRequests()
        {
            Mock<IIntuneClient> mock = CreateDownloadMock(validDownloadResponse);
            IntuneRevocationClient client = new IntuneRevocationClient(configProperties, intuneClient: mock.Object);
            string transactionId = Guid.NewGuid().ToString();
            await client.DownloadCARevocationRequestsAsync(transactionId, -1);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public async Task DownloadCARevocationRequestsAsync_OverMaxRequests()
        {
            Mock<IIntuneClient> mock = CreateDownloadMock(validDownloadResponse);
            IntuneRevocationClient client = new IntuneRevocationClient(configProperties, intuneClient: mock.Object);
            string transactionId = Guid.NewGuid().ToString();
            await client.DownloadCARevocationRequestsAsync(transactionId, IntuneRevocationClient.MAXREQUESTS_MAXVALUE + 1);
        }

        [TestMethod]
        public async Task UploadCARequestResults_ValidResultsTest()
        {
            Mock<IIntuneClient> mock = CreateUploadMock(new JObject(new JProperty("value", true)));
            IntuneRevocationClient client = new IntuneRevocationClient(configProperties, intuneClient: mock.Object);
            string transactionId = Guid.NewGuid().ToString();
            await client.UploadRevocationResultsAsync(transactionId, validRequestResults);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task UploadCARequestResults_InvalidResultsTest()
        {
            Mock<IIntuneClient> mock = CreateUploadMock(new JObject(new JProperty("value", true)));
            IntuneRevocationClient client = new IntuneRevocationClient(configProperties, intuneClient: mock.Object);
            string transactionId = Guid.NewGuid().ToString();
            await client.UploadRevocationResultsAsync(transactionId, new List<CARevocationResult>());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task UploadCARequestResults_NullResultsTest()
        {
            Mock<IIntuneClient> mock = CreateUploadMock(new JObject(new JProperty("value", true)));
            IntuneRevocationClient client = new IntuneRevocationClient(configProperties, intuneClient: mock.Object);
            string transactionId = Guid.NewGuid().ToString();
            await client.UploadRevocationResultsAsync(transactionId, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task UploadCARequestResults_InvalidTransactionId()
        {
            Mock<IIntuneClient> mock = CreateUploadMock(new JObject(new JProperty("value", true)));
            IntuneRevocationClient client = new IntuneRevocationClient(configProperties, intuneClient: mock.Object);
            string transactionId = null;
            await client.UploadRevocationResultsAsync(transactionId, validRequestResults);
        }

        [TestMethod]
        [ExpectedException(typeof(IntuneClientException))]
        public async Task UploadCARequestResults_FalseResponse()
        {
            Mock<IIntuneClient> mock = CreateUploadMock(new JObject(new JProperty("value", false)));
            IntuneRevocationClient client = new IntuneRevocationClient(configProperties, intuneClient: mock.Object);
            string transactionId = Guid.NewGuid().ToString();
            await client.UploadRevocationResultsAsync(transactionId, validRequestResults);
        }

        [TestMethod]
        [ExpectedException(typeof(IntuneClientException))]
        public async Task UploadCARequestResults_NonBooleanResponse()
        {
            Mock<IIntuneClient> mock = CreateUploadMock(new JObject(new JProperty("value", "test")));
            IntuneRevocationClient client = new IntuneRevocationClient(configProperties, intuneClient: mock.Object);
            string transactionId = Guid.NewGuid().ToString();
            await client.UploadRevocationResultsAsync(transactionId, validRequestResults);
        }

        [TestMethod]
        [ExpectedException(typeof(IntuneClientException))]
        public async Task UploadCARequestResults_NoValueResponse()
        {
            Mock<IIntuneClient> mock = CreateUploadMock(new JObject(new JProperty("not_value", true)));
            IntuneRevocationClient client = new IntuneRevocationClient(configProperties, intuneClient: mock.Object);
            string transactionId = Guid.NewGuid().ToString();
            await client.UploadRevocationResultsAsync(transactionId, validRequestResults);
        }

        [TestMethod]
        public void TestConstructorParameters()
        {
            string providerNameAndVersion = "non-empty";
            string azureAppId = "non-empty";
            string azureAppKey = "non-empty";
            string intuneTenant = "non-empty";
            string intuneAppId = "non-empty";
            string intuneResourceUrl = "non-empty";
            string serviceVersion = "non-empty";
            string graphApiVersion = "non-empty";
            string graphResourceUrl = "non-empty";
            string authAuthority = "https://www.localhost.com/path";
            
            ConstructorNullThrows(null, azureAppId, azureAppKey, intuneTenant);
            ConstructorNullThrows(providerNameAndVersion, null, azureAppKey, intuneTenant);
            ConstructorNullThrows(providerNameAndVersion, azureAppId, null, intuneTenant);
            ConstructorNullThrows(providerNameAndVersion, azureAppId, azureAppKey, null);

            ConstructorNotPresentThrows(null, azureAppId, azureAppKey, intuneTenant);
            ConstructorNotPresentThrows(providerNameAndVersion, null, azureAppKey, intuneTenant);
            ConstructorNotPresentThrows(providerNameAndVersion, azureAppId, null, intuneTenant);
            ConstructorNotPresentThrows(providerNameAndVersion, azureAppId, azureAppKey, null);

            ConstructorNullDoesntThrow(null, intuneAppId, intuneResourceUrl, graphApiVersion, graphResourceUrl, authAuthority);
            ConstructorNullDoesntThrow(serviceVersion, null, intuneResourceUrl, graphApiVersion, graphResourceUrl, authAuthority);
            ConstructorNullDoesntThrow(serviceVersion, intuneAppId, null, graphApiVersion, graphResourceUrl, authAuthority);
            ConstructorNullDoesntThrow(serviceVersion, intuneAppId, intuneResourceUrl, null, graphResourceUrl, authAuthority);
            ConstructorNullDoesntThrow(serviceVersion, intuneAppId, intuneResourceUrl, graphApiVersion, null, authAuthority);
            ConstructorNullDoesntThrow(serviceVersion, intuneAppId, intuneResourceUrl, graphApiVersion, graphResourceUrl, null);

            ConstructorNotPresentDoesntThrow(null, intuneAppId, intuneResourceUrl, graphApiVersion, graphResourceUrl, authAuthority);
            ConstructorNotPresentDoesntThrow(serviceVersion, null, intuneResourceUrl, graphApiVersion, graphResourceUrl, authAuthority);
            ConstructorNotPresentDoesntThrow(serviceVersion, intuneAppId, null, graphApiVersion, graphResourceUrl, authAuthority);
            ConstructorNotPresentDoesntThrow(serviceVersion, intuneAppId, intuneResourceUrl, null, graphResourceUrl, authAuthority);
            ConstructorNotPresentDoesntThrow(serviceVersion, intuneAppId, intuneResourceUrl, graphApiVersion, null, authAuthority);
            ConstructorNotPresentDoesntThrow(serviceVersion, intuneAppId, intuneResourceUrl, graphApiVersion, graphResourceUrl, null);
        }

        private void ConstructorNullThrows(
            string providerNameAndVersion,
            string azureAppId,
            string azureAppKey,
            string intuneTenant)
        {
            Dictionary<string, string> props = new Dictionary<string, string>()
            {
                {"PROVIDER_NAME_AND_VERSION",providerNameAndVersion},
                {"AAD_APP_ID",azureAppId},
                {"AAD_APP_KEY",azureAppKey},
                {"TENANT",intuneTenant}
            };

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                new IntuneRevocationClient(props);
            });
        }

        private void ConstructorNullDoesntThrow(
            string serviceVersion,
            string intuneAppId,
            string intuneResourceUrl,
            string graphApiVersion,
            string graphResourceUrl,
            string authAuthority)
        {
            Dictionary<string, string> props = new Dictionary<string, string>()
            {
                {"PROVIDER_NAME_AND_VERSION","test"},
                {"AAD_APP_ID","test"},
                {"AAD_APP_KEY","test"},
                {"TENANT","test"},
                {"StatelessPkiConnectorServiceVersion",serviceVersion},
                {"INTUNE_APP_ID",intuneAppId},
                {"INTUNE_RESOURCE_URL",intuneResourceUrl},
                {"GRAPH_API_VERSION",graphApiVersion},
                {"GRAPH_RESOURCE_URL",graphResourceUrl},
                {"AUTH_AUTHORITY",authAuthority},
            };

            new IntuneRevocationClient(props);
        }

        private void ConstructorNotPresentThrows(
            string providerNameAndVersion,
            string azureAppId,
            string azureAppKey,
            string intuneTenant)
            
        {
            Dictionary<string, string> props = new Dictionary<string, string>();

            if(providerNameAndVersion != null)
            {
                props.Add("PROVIDER_NAME_AND_VERSION", providerNameAndVersion);
            }

            if (azureAppId != null)
            {
                props.Add("AAD_AAP_ID", azureAppId);
            }

            if (azureAppKey != null)
            {
                props.Add("AAD_APP_KEY", azureAppKey);
            }

            if (intuneTenant != null)
            {
                props.Add("TENANT", intuneTenant);
            }

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                new IntuneRevocationClient(props);
            });
        }


        private void ConstructorNotPresentDoesntThrow(
            string serviceVersion,
            string intuneAppId,
            string intuneResourceUrl,
            string graphApiVersion,
            string graphResourceUrl,
            string authAuthority)
        {
            Dictionary<string, string> props = new Dictionary<string, string>()
            {
                {"PROVIDER_NAME_AND_VERSION","test"},
                {"AAD_APP_ID","test"},
                {"AAD_APP_KEY","test"},
                {"TENANT","test"},
            };

            if (serviceVersion != null)
            {
                props.Add("StatelessPkiConnectorServiceVersion", serviceVersion);
            }

            if (intuneAppId != null)
            {
                props.Add("INTUNE_APP_ID", intuneAppId);
            }

            if (intuneResourceUrl != null)
            {
                props.Add("INTUNE_RESOURCE_URL", intuneResourceUrl);
            }

            if (graphApiVersion != null)
            {
                props.Add("GRAPH_API_VERSION", graphApiVersion);
            }

            if (graphResourceUrl != null)
            {
                props.Add("GRAPH_RESOURCE_URL", graphResourceUrl);
            }

            if (authAuthority != null)
            {
                props.Add("AUTH_AUTHORITY", authAuthority);
            }
            
            new IntuneRevocationClient(props);
        }
        
        private Mock<IIntuneClient> CreateDownloadMock(JObject response)
        {
            var mock = new Mock<IIntuneClient>();
            mock.Setup(foo => foo.PostAsync(
                IntuneRevocationClient.CAREQUEST_SERVICE_NAME,
                IntuneRevocationClient.DOWNLOADREVOCATIONREQUESTS_URL,
                IntuneRevocationClient.DEFAULT_SERVICE_VERSION,
                It.IsAny<JObject>(),
                It.IsAny<Guid>(),
                It.IsAny<Dictionary<string, string>>())
            ).Returns(
                Task.FromResult<JObject>(response)
            );
            return mock;
        }

        private Mock<IIntuneClient> CreateUploadMock(JObject response)
        {
            var mock = new Mock<IIntuneClient>();
            mock.Setup(foo => foo.PostAsync(
                IntuneRevocationClient.CAREQUEST_SERVICE_NAME,
                IntuneRevocationClient.UPLOADREVOCATIONRESULTS_URL,
                IntuneRevocationClient.DEFAULT_SERVICE_VERSION,
                It.IsAny<JObject>(),
                It.IsAny<Guid>(),
                It.IsAny<Dictionary<string, string>>())
            ).Returns(
                Task.FromResult<JObject>(response)
            );
            return mock;
        }
    }
}
