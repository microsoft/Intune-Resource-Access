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

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Intune;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;

namespace UnitTests
{
    [TestClass]
    public class IntuneScepValidatorTests
    {
        Dictionary<string, string> configProperties = new Dictionary<string, string>()
        {
            {"AAD_APP_ID","appId"},
            {"AAD_APP_KEY","appKey"},
            {"TENANT","tenant"},
            {"PROVIDER_NAME_AND_VERSION","providerName"}
        };

        [TestMethod]
        public async Task TestValidationSucceedsAsync()
        {
            var validResponse = new JObject();
            validResponse.Add("code", IntuneScepServiceException.ErrorCode.Success.ToString());
            validResponse.Add("errorDescription", "");

            var mock = new Mock<IIntuneClient>();
            mock.Setup(foo => foo.PostAsync(
                Microsoft.Intune.IntuneScepValidator.VALIDATION_SERVICE_NAME,
                Microsoft.Intune.IntuneScepValidator.VALIDATION_URL,
                Microsoft.Intune.IntuneScepValidator.DEFAULT_SERVICE_VERSION,
                It.IsAny<JObject>(),
                It.IsAny<Guid>(),
                It.IsAny<Dictionary<string, string>>())
            ).Returns(
                Task.FromResult<string>(validResponse.ToString())
            );

            Microsoft.Intune.IntuneScepValidator client = new Microsoft.Intune.IntuneScepValidator(configProperties, intuneClient: mock.Object);

            Guid transactionId = Guid.NewGuid();
            string csr = "testing";

            await client.ValidateRequestAsync(transactionId.ToString(), csr);
        }

        [TestMethod]
        [ExpectedException(typeof(IntuneScepServiceException))]
        public async Task TestValidationFailsAsync()
        {
            var invalidResponse = new JObject();
            invalidResponse.Add("code", IntuneScepServiceException.ErrorCode.ChallengeDecryptionError.ToString());
            invalidResponse.Add("errorDescription", "");

            var mock = new Mock<IIntuneClient>();
            mock.Setup(foo => foo.PostAsync(
                Microsoft.Intune.IntuneScepValidator.VALIDATION_SERVICE_NAME,
                Microsoft.Intune.IntuneScepValidator.VALIDATION_URL,
                Microsoft.Intune.IntuneScepValidator.DEFAULT_SERVICE_VERSION,
                It.IsAny<JObject>(),
                It.IsAny<Guid>(),
                It.IsAny<Dictionary<string, string>>())
            ).Returns(
                Task.FromResult<string>(invalidResponse.ToString())
            );

            Microsoft.Intune.IntuneScepValidator client = new Microsoft.Intune.IntuneScepValidator(configProperties, intuneClient: mock.Object);

            Guid transactionId = Guid.NewGuid();
            string csr = "testing";

            await client.ValidateRequestAsync(transactionId.ToString(), csr);
        }

        [TestMethod]
        public async Task TestSendSuccessNotificationSucceedsAsync()
        {
            var validResponse = new JObject();
            validResponse.Add("code", IntuneScepServiceException.ErrorCode.Success.ToString());
            validResponse.Add("errorDescription", "");

            var mock = new Mock<IIntuneClient>();
            mock.Setup(foo => foo.PostAsync(
                Microsoft.Intune.IntuneScepValidator.VALIDATION_SERVICE_NAME,
                Microsoft.Intune.IntuneScepValidator.NOTIFY_SUCCESS_URL,
                Microsoft.Intune.IntuneScepValidator.DEFAULT_SERVICE_VERSION,
                It.IsAny<JObject>(),
                It.IsAny<Guid>(),
                It.IsAny<Dictionary<string, string>>())
            ).Returns(
                Task.FromResult<string>(validResponse.ToString())
            );

            Microsoft.Intune.IntuneScepValidator client = new Microsoft.Intune.IntuneScepValidator(configProperties, intuneClient: mock.Object);

            Guid transactionId = Guid.NewGuid();
            string csr = "testing";

            await client.SendSuccessNotificationAsync(transactionId.ToString(), csr, "thumpbrint", "serial", "expire", "auth");
        }

        [TestMethod]
        [ExpectedException(typeof(IntuneScepServiceException))]
        public async Task TestSendSuccessNotificationFailsAsync()
        {
            var invalidResponse = new JObject();
            invalidResponse.Add("code", IntuneScepServiceException.ErrorCode.ChallengeDecryptionError.ToString());
            invalidResponse.Add("errorDescription", "");

            var mock = new Mock<IIntuneClient>();
            mock.Setup(foo => foo.PostAsync(
                Microsoft.Intune.IntuneScepValidator.VALIDATION_SERVICE_NAME,
                Microsoft.Intune.IntuneScepValidator.NOTIFY_SUCCESS_URL,
                Microsoft.Intune.IntuneScepValidator.DEFAULT_SERVICE_VERSION,
                It.IsAny<JObject>(),
                It.IsAny<Guid>(),
                It.IsAny<Dictionary<string, string>>())
            ).Returns(
                Task.FromResult<string>(invalidResponse.ToString())
            );

            Microsoft.Intune.IntuneScepValidator client = new Microsoft.Intune.IntuneScepValidator(configProperties, intuneClient: mock.Object);

            Guid transactionId = Guid.NewGuid();
            string csr = "testing";

            await client.SendSuccessNotificationAsync(transactionId.ToString(), csr, "thumpbrint", "serial", "expire", "auth");
        }

        [TestMethod]
        public async Task TestSendFailureNotificationSucceedsAsync()
        {
            var validResponse = new JObject();
            validResponse.Add("code", IntuneScepServiceException.ErrorCode.Success.ToString());
            validResponse.Add("errorDescription", "");

            var mock = new Mock<IIntuneClient>();
            mock.Setup(foo => foo.PostAsync(
                Microsoft.Intune.IntuneScepValidator.VALIDATION_SERVICE_NAME,
                Microsoft.Intune.IntuneScepValidator.NOTIFY_FAILURE_URL,
                Microsoft.Intune.IntuneScepValidator.DEFAULT_SERVICE_VERSION,
                It.IsAny<JObject>(),
                It.IsAny<Guid>(),
                It.IsAny<Dictionary<string, string>>())
            ).Returns(
                Task.FromResult<string>(validResponse.ToString())
            );

            Microsoft.Intune.IntuneScepValidator client = new Microsoft.Intune.IntuneScepValidator(configProperties, intuneClient: mock.Object);

            Guid transactionId = Guid.NewGuid();
            string csr = "testing";

            await client.SendFailureNotificationAsync(transactionId.ToString(), csr, 1, "description");
        }

        [TestMethod]
        [ExpectedException(typeof(IntuneScepServiceException))]
        public async Task TestSendFailureNotificationFailsAsync()
        {
            var invalidResponse = new JObject();
            invalidResponse.Add("code", IntuneScepServiceException.ErrorCode.ChallengeDecryptionError.ToString());
            invalidResponse.Add("errorDescription", "");

            var mock = new Mock<IIntuneClient>();
            mock.Setup(foo => foo.PostAsync(
                Microsoft.Intune.IntuneScepValidator.VALIDATION_SERVICE_NAME,
                Microsoft.Intune.IntuneScepValidator.NOTIFY_FAILURE_URL,
                Microsoft.Intune.IntuneScepValidator.DEFAULT_SERVICE_VERSION,
                It.IsAny<JObject>(),
                It.IsAny<Guid>(),
                It.IsAny<Dictionary<string, string>>())
            ).Returns(
                Task.FromResult<string>(invalidResponse.ToString())
            );

            Microsoft.Intune.IntuneScepValidator client = new Microsoft.Intune.IntuneScepValidator(configProperties, intuneClient: mock.Object);

            Guid transactionId = Guid.NewGuid();
            string csr = "testing";

            await client.SendFailureNotificationAsync(transactionId.ToString(), csr, 1, "description");
        }

        [TestMethod]
        [ExpectedException(typeof(AdalServiceException))]
        public async Task TestAuthFailureAsync()
        {
            var invalidResponse = new JObject();
            invalidResponse.Add("code", IntuneScepServiceException.ErrorCode.ChallengeDecryptionError.ToString());
            invalidResponse.Add("errorDescription", "");

            var authContextMock = new Mock<IAuthenticationContext>();
            authContextMock.Setup(foo => foo.AcquireTokenAsync(
                It.IsAny<string>(), It.IsAny<ClientCredential>())
            ).Throws(
                new AdalServiceException("","")
            );

            var locationProviderMock = new Mock<IIntuneServiceLocationProvider>();
            locationProviderMock.Setup(foo => foo.GetServiceEndpointAsync(Microsoft.Intune.IntuneScepValidator.VALIDATION_SERVICE_NAME))
                .Returns(Task.FromResult<string>(@"http://localhost/"));


            var adalClient = new AdalClient(configProperties);
            var intuneClient = new IntuneClient(configProperties, adalClient: adalClient, locationProvider: locationProviderMock.Object);
            var scepClient = new Microsoft.Intune.IntuneScepValidator(configProperties, intuneClient: intuneClient);

            Guid transactionId = Guid.NewGuid();
            string csr = "testing";

            await scepClient.SendFailureNotificationAsync(transactionId.ToString(), csr, 1, "description");
        }

        [TestMethod]
        [ExpectedException(typeof(AdalServiceException))]
        public async Task TestURLChangesAsync()
        {
            var invalidResponse = new JObject();
            invalidResponse.Add("code", IntuneScepServiceException.ErrorCode.ChallengeDecryptionError.ToString());
            invalidResponse.Add("errorDescription", "");

            var authContextMock = new Mock<IAuthenticationContext>();
            authContextMock.Setup(foo => foo.AcquireTokenAsync(
                It.IsAny<string>(), It.IsAny<ClientCredential>())
            ).Throws(
                new AdalServiceException("", "")
            );

            var locationProviderMock = new Mock<IIntuneServiceLocationProvider>();
            locationProviderMock.Setup(foo => foo.GetServiceEndpointAsync(IntuneScepValidator.VALIDATION_SERVICE_NAME))
                .Returns(Task.FromResult<string>(@"http://localhost/"));

            var httpClientMock = new Mock<IHttpClient>();
            httpClientMock.Setup(foo => foo.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
                .Throws(new HttpRequestException());

            var adalClient = new AdalClient(configProperties);
            var intuneClient = new IntuneClient(configProperties, adalClient: adalClient, locationProvider: locationProviderMock.Object, httpClient:httpClientMock.Object);
            var scepClient = new Microsoft.Intune.IntuneScepValidator(configProperties, intuneClient: intuneClient);

            Guid transactionId = Guid.NewGuid();
            string csr = "testing";

            await scepClient.SendFailureNotificationAsync(transactionId.ToString(), csr, 1, "description");

            locationProviderMock.Verify(foo => foo.GetServiceEndpointAsync(IntuneScepValidator.VALIDATION_SERVICE_NAME), Times.Once);
            locationProviderMock.Verify(foo => foo.Clear(), Times.Once);
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
            try
            {
                new IntuneScepValidator(props);
            }
            catch(ArgumentNullException)
            {
                return;
            }
            throw new Exception("Failed to catch argument Null exception");
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
                {"ScepRequestValidationFEServiceVersion",serviceVersion},
                {"INTUNE_APP_ID",intuneAppId},
                {"INTUNE_RESOURCE_URL",intuneResourceUrl},
                {"GRAPH_API_VERSION",graphApiVersion},
                {"GRAPH_RESOURCE_URL",graphResourceUrl},
                {"AUTH_AUTHORITY",authAuthority},
            };

            new IntuneScepValidator(props);
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

            try
            {
                new IntuneScepValidator(props);
            }
            catch (ArgumentNullException)
            {
                return;
            }
            throw new Exception("Failed to catch argument Null exception");
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
                props.Add("ScepRequestValidationFEServiceVersion", serviceVersion);
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
            
            new IntuneScepValidator(props);
        }


    }
}
