using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Intune;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using System.Net.Http;

namespace UnitTests
{
    [TestClass]
    public class IntuneScepValidatorTests
    {
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
                Task.FromResult<JObject>(validResponse)
            );

            Microsoft.Intune.IntuneScepValidator client = new Microsoft.Intune.IntuneScepValidator("test", "test", "test", "test", intuneClient: mock.Object);

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
                Task.FromResult<JObject>(invalidResponse)
            );

            Microsoft.Intune.IntuneScepValidator client = new Microsoft.Intune.IntuneScepValidator("test", "test", "test", "test", intuneClient: mock.Object);

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
                Task.FromResult<JObject>(validResponse)
            );

            Microsoft.Intune.IntuneScepValidator client = new Microsoft.Intune.IntuneScepValidator("test", "test", "test", "test", intuneClient: mock.Object);

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
                Task.FromResult<JObject>(invalidResponse)
            );

            Microsoft.Intune.IntuneScepValidator client = new Microsoft.Intune.IntuneScepValidator("test", "test", "test", "test", intuneClient: mock.Object);

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
                Task.FromResult<JObject>(validResponse)
            );

            Microsoft.Intune.IntuneScepValidator client = new Microsoft.Intune.IntuneScepValidator("test", "test", "test", "test", intuneClient: mock.Object);

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
                Task.FromResult<JObject>(invalidResponse)
            );

            Microsoft.Intune.IntuneScepValidator client = new Microsoft.Intune.IntuneScepValidator("test", "test", "test", "test", intuneClient: mock.Object);

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


            var adalClient = new AdalClient("test", new ClientCredential("test", "test"));
            var intuneClient = new IntuneClient(adalClient: adalClient, locationProvider: locationProviderMock.Object);
            var scepClient = new Microsoft.Intune.IntuneScepValidator("test", "test", "test", "test", intuneClient: intuneClient);

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

            var adalClient = new AdalClient("test", new ClientCredential("test", "test"));
            var intuneClient = new IntuneClient(adalClient: adalClient, locationProvider: locationProviderMock.Object, httpClient:httpClientMock.Object);
            var scepClient = new Microsoft.Intune.IntuneScepValidator("test", "test", "test", "test", intuneClient: intuneClient);

            Guid transactionId = Guid.NewGuid();
            string csr = "testing";

            await scepClient.SendFailureNotificationAsync(transactionId.ToString(), csr, 1, "description");

            locationProviderMock.Verify(foo => foo.GetServiceEndpointAsync(IntuneScepValidator.VALIDATION_SERVICE_NAME), Times.Once);
            locationProviderMock.Verify(foo => foo.Clear(), Times.Once);
        }

        [TestMethod]
        public void TestConstructorParameterNulls()
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

            ConstructorNullThrows(null, azureAppId, azureAppKey, intuneTenant, serviceVersion, intuneAppId, intuneResourceUrl, graphApiVersion, graphResourceUrl, authAuthority);
            ConstructorNullThrows(providerNameAndVersion, null, azureAppKey, intuneTenant, serviceVersion, intuneAppId, intuneResourceUrl, graphApiVersion, graphResourceUrl, authAuthority);
            ConstructorNullThrows(providerNameAndVersion, azureAppId, null, intuneTenant, serviceVersion, intuneAppId, intuneResourceUrl, graphApiVersion, graphResourceUrl, authAuthority);
            ConstructorNullThrows(providerNameAndVersion, azureAppId, azureAppKey, null, serviceVersion, intuneAppId, intuneResourceUrl, graphApiVersion, graphResourceUrl, authAuthority);
            ConstructorNullThrows(providerNameAndVersion, azureAppId, azureAppKey, intuneTenant, null, intuneAppId, intuneResourceUrl, graphApiVersion, graphResourceUrl, authAuthority);
            ConstructorNullThrows(providerNameAndVersion, azureAppId, azureAppKey, intuneTenant, serviceVersion, null, intuneResourceUrl, graphApiVersion, graphResourceUrl, authAuthority);
            ConstructorNullThrows(providerNameAndVersion, azureAppId, azureAppKey, intuneTenant, serviceVersion, intuneAppId, null, graphApiVersion, graphResourceUrl, authAuthority);
            ConstructorNullThrows(providerNameAndVersion, azureAppId, azureAppKey, intuneTenant, serviceVersion, intuneAppId, intuneResourceUrl, null, graphResourceUrl, authAuthority);
            ConstructorNullThrows(providerNameAndVersion, azureAppId, azureAppKey, intuneTenant, serviceVersion, intuneAppId, intuneResourceUrl, graphApiVersion, null, authAuthority);
            ConstructorNullThrows(providerNameAndVersion, azureAppId, azureAppKey, intuneTenant, serviceVersion, intuneAppId, intuneResourceUrl, graphApiVersion, graphResourceUrl, null);
        }

        private void ConstructorNullThrows(
            string providerNameAndVersion,
            string azureAppId,
            string azureAppKey,
            string intuneTenant,
            string serviceVersion,
            string intuneAppId,
            string intuneResourceUrl,
            string graphApiVersion,
            string graphResourceUrl,
            string authAuthority)
        {
            try
            {
                new IntuneScepValidator(providerNameAndVersion, azureAppId, azureAppKey, intuneTenant, serviceVersion, intuneAppId, intuneResourceUrl, graphApiVersion, graphResourceUrl, authAuthority);
            }
            catch(ArgumentNullException)
            {
                return;
            }
            throw new Exception("Failed to catch argument Null exception");
        }


    }
}
