using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Intune;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;

namespace UnitTests
{
    [TestClass]
    public class IntuneScepServiceClientTests
    {
        [TestMethod]
        public async Task TestValidationSucceedsAsync()
        {
            var validResponse = new JObject();
            validResponse.Add("code", IntuneScepServiceException.ErrorCode.Success.ToString());
            validResponse.Add("errorDescription", "");

            var mock = new Mock<IIntuneClient>();
            mock.Setup(foo => foo.PostAsync(
                IntuneScepValidator.VALIDATION_SERVICE_NAME, 
                IntuneScepValidator.VALIDATION_URL, 
                IntuneScepValidator.DEFAULT_SERVICE_VERSION, 
                It.IsAny<JObject>(), 
                It.IsAny<Guid>(),
                It.IsAny<Dictionary<string,string>>())
            ).Returns(
                Task.FromResult<JObject>(validResponse)
            );

            IntuneScepValidator client = new IntuneScepValidator("test", "test", "test", "test", intuneClient: mock.Object);

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
                IntuneScepValidator.VALIDATION_SERVICE_NAME,
                IntuneScepValidator.VALIDATION_URL,
                IntuneScepValidator.DEFAULT_SERVICE_VERSION,
                It.IsAny<JObject>(),
                It.IsAny<Guid>(),
                It.IsAny<Dictionary<string, string>>())
            ).Returns(
                Task.FromResult<JObject>(invalidResponse)
            );

            IntuneScepValidator client = new IntuneScepValidator("test", "test", "test", "test", intuneClient: mock.Object);

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
                IntuneScepValidator.VALIDATION_SERVICE_NAME,
                IntuneScepValidator.NOTIFY_SUCCESS_URL,
                IntuneScepValidator.DEFAULT_SERVICE_VERSION,
                It.IsAny<JObject>(),
                It.IsAny<Guid>(),
                It.IsAny<Dictionary<string, string>>())
            ).Returns(
                Task.FromResult<JObject>(validResponse)
            );

            IntuneScepValidator client = new IntuneScepValidator("test", "test", "test", "test", intuneClient: mock.Object);

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
                IntuneScepValidator.VALIDATION_SERVICE_NAME,
                IntuneScepValidator.NOTIFY_SUCCESS_URL,
                IntuneScepValidator.DEFAULT_SERVICE_VERSION,
                It.IsAny<JObject>(),
                It.IsAny<Guid>(),
                It.IsAny<Dictionary<string, string>>())
            ).Returns(
                Task.FromResult<JObject>(invalidResponse)
            );

            IntuneScepValidator client = new IntuneScepValidator("test", "test", "test", "test", intuneClient: mock.Object);

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
                IntuneScepValidator.VALIDATION_SERVICE_NAME,
                IntuneScepValidator.NOTIFY_FAILURE_URL,
                IntuneScepValidator.DEFAULT_SERVICE_VERSION,
                It.IsAny<JObject>(),
                It.IsAny<Guid>(),
                It.IsAny<Dictionary<string, string>>())
            ).Returns(
                Task.FromResult<JObject>(validResponse)
            );

            IntuneScepValidator client = new IntuneScepValidator("test", "test", "test", "test", intuneClient: mock.Object);

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
                IntuneScepValidator.VALIDATION_SERVICE_NAME,
                IntuneScepValidator.NOTIFY_FAILURE_URL,
                IntuneScepValidator.DEFAULT_SERVICE_VERSION,
                It.IsAny<JObject>(),
                It.IsAny<Guid>(),
                It.IsAny<Dictionary<string, string>>())
            ).Returns(
                Task.FromResult<JObject>(invalidResponse)
            );

            IntuneScepValidator client = new IntuneScepValidator("test", "test", "test", "test", intuneClient: mock.Object);

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
            locationProviderMock.Setup(foo => foo.GetServiceEndpointAsync(IntuneScepValidator.VALIDATION_SERVICE_NAME))
                .Returns(Task.FromResult<string>(@"http://localhost/"));


            var adalClient = new AdalClient("test", new ClientCredential("test", "test"));
            var intuneClient = new IntuneClient("test", "test", "test", adalClient: adalClient, locationProvider: locationProviderMock.Object);
            var scepClient = new IntuneScepValidator("test", "test", "test", "test", intuneClient: intuneClient);

            Guid transactionId = Guid.NewGuid();
            string csr = "testing";

            await scepClient.SendFailureNotificationAsync(transactionId.ToString(), csr, 1, "description");
        }
    }
}
