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
// all copies or substantial portionas of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.


namespace Microsoft.Management.Powershell.PFXImport.UnitTests
{
    #region using
    using Cmdlets;
    using Moq;
    using VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.IO;
    using System.Text;
    using System.Collections;
    using IdentityModel.Clients.ActiveDirectory;
    using System.Security.Authentication;
    using Services.Api;
    using System.Security.Cryptography.X509Certificates;
    using Intune.EncryptionUtilities;
    #endregion using

    [TestClass]
    public class ImportPFXCertificatesUnitTests
    {


        private Mock<ImportUserPFXCertificate> mockCmdlet;

        private int webExRethowCnt;

        public TestContext TestContext { get; set; }


        [TestInitialize]
        public void Initialize()
        {
            string httpResponseContent = "";
            byte[] httpResponseContentBytes = Encoding.UTF8.GetBytes(httpResponseContent);
            MemoryStream stream = new MemoryStream(httpResponseContentBytes);

            Mock<HttpWebResponse> response = new Mock<HttpWebResponse>();
            response.Setup(c => c.GetResponseStream()).Returns(stream);
            string responseStatus = TestContext.Properties["httpResponseStatus"] as string;
            response.Setup(c => c.StatusCode).Returns((HttpStatusCode)Enum.Parse(typeof(HttpStatusCode), responseStatus));

            Mock<HttpWebRequest> request = new Mock<HttpWebRequest>();
            if (TestContext.Properties["throwExceptionWithStatus429"] == null)
            {
                request.Setup(c => c.GetResponse()).Returns(response.Object);
            }
            else
            {
                webExRethowCnt = 0;

                Mock<HttpWebResponse> exResponse = new Mock<HttpWebResponse>();
                exResponse.Setup(c => c.GetResponseStream()).Returns(stream);
                exResponse.Setup(c => c.StatusCode).Returns((HttpStatusCode)429);
                WebHeaderCollection headers = new WebHeaderCollection();
                if(TestContext.Properties["retryAfterHeader"] != null)
                {
                    headers.Add("x-ms-retry-after-ms", TestContext.Properties["retryAfterHeader"] as string);
                }
                exResponse.Setup(p => p.Headers).Returns(headers);
                Mock<WebException> webEx = new Mock<WebException>("WebException", null, WebExceptionStatus.ProtocolError, exResponse.Object);
                int exCnt = int.Parse(TestContext.Properties["throwExceptionWithStatus429"] as String);
                request.When(() => webExRethowCnt < exCnt).Setup(c => c.GetResponse()).Callback(() => webExRethowCnt++).Throws(webEx.Object);
                request.When(() => webExRethowCnt >= exCnt).Setup(c => c.GetResponse()).Returns(response.Object);
            }

            Mock<MemoryStream> requestStream = new Mock<MemoryStream>();
            request.Setup(c => c.GetRequestStream()).Returns(requestStream.Object);

            mockCmdlet = new Mock<ImportUserPFXCertificate>();
            mockCmdlet.CallBase = true;
            mockCmdlet.Setup(c => c.CreateWebRequest(It.IsAny<string>(), It.IsAny<AuthenticationResult>())).Returns(request.Object);
        }

        [TestMethod]
        [TestProperty("httpResponseStatus", "Created")]
        public void TestImportUserPfxCertificateByPost()
        {
            UserPFXCertificate userPFXCert2Import = new UserPFXCertificate();
            userPFXCert2Import.Thumbprint = "f6f51856-1856-f6f5-5618-f5f65618f5f6";
            userPFXCert2Import.IntendedPurpose = UserPfxIntendedPurpose.SmimeEncryption;
            userPFXCert2Import.UserPrincipalName = "User Principal Name value";
            userPFXCert2Import.StartDateTime = DateTime.Parse("2016-12-31T23:58:46.7156189-07:00");
            userPFXCert2Import.ExpirationDateTime = DateTime.Parse("2016-12-31T23:57:57.2481234-07:00");
            userPFXCert2Import.ProviderName = "Provider Name value";
            userPFXCert2Import.KeyName = "Key Name value";
            userPFXCert2Import.EncryptedPfxBlob = GetEncryptedPFXBlob();
            userPFXCert2Import.EncryptedPfxPassword = Guid.NewGuid().ToString();
            userPFXCert2Import.CreatedDateTime = DateTime.Parse("2017-01-01T00:02:43.5775965-07:00");
            userPFXCert2Import.LastModifiedDateTime = DateTime.Parse("2017-01-01T00:00:35.1329464-07:00");


            Authenticate.AuthTokenIsValid = (AuthRes) =>
            {
                return true;
            };

            List<UserPFXCertificate> thumbprintList = new List<UserPFXCertificate>();
            thumbprintList.Add(userPFXCert2Import);

            mockCmdlet.Object.CertificateList = thumbprintList;

            IEnumerator result = mockCmdlet.Object.Invoke().GetEnumerator();
            Assert.IsFalse(result.MoveNext());
        }


        [TestMethod]
        [TestProperty("httpResponseStatus", "OK")]
        public void TestImportUserPfxCertificateByPatch()
        {
            UserPFXCertificate userPFXCert2Import = new UserPFXCertificate();
            userPFXCert2Import.Id = "f6f51856-1856-f6f5-5618-f5f65618f5f6";
            userPFXCert2Import.Thumbprint = "f6f51856-1856-f6f5-5618-f5f65618f5f6";
            userPFXCert2Import.IntendedPurpose = UserPfxIntendedPurpose.SmimeEncryption;
            userPFXCert2Import.UserPrincipalName = "User Principal Name value";
            userPFXCert2Import.StartDateTime = DateTime.Parse("2016-12-31T23:58:46.7156189-07:00");
            userPFXCert2Import.ExpirationDateTime = DateTime.Parse("2016-12-31T23:57:57.2481234-07:00");
            userPFXCert2Import.ProviderName = "Provider Name value";
            userPFXCert2Import.KeyName = "Key Name value";
            userPFXCert2Import.EncryptedPfxBlob = GetEncryptedPFXBlob();
            userPFXCert2Import.EncryptedPfxPassword = Guid.NewGuid().ToString();
            userPFXCert2Import.CreatedDateTime = DateTime.Parse("2017-01-01T00:02:43.5775965-07:00");
            userPFXCert2Import.LastModifiedDateTime = DateTime.Parse("2017-01-01T00:00:35.1329464-07:00");


            Authenticate.AuthTokenIsValid = (AuthRes) =>
            {
                return true;
            };
            mockCmdlet.Setup<string>(a => a.GetUserIdFromUpn(It.IsAny<string>())).Returns<string>((a) => { return "1"; });

            List<UserPFXCertificate> thumbprintList = new List<UserPFXCertificate>();
            thumbprintList.Add(userPFXCert2Import);

            mockCmdlet.Object.CertificateList = thumbprintList;
            mockCmdlet.Object.IsUpdate = new System.Management.Automation.SwitchParameter(true);

            IEnumerator result = mockCmdlet.Object.Invoke().GetEnumerator();
            Assert.IsFalse(result.MoveNext());
        }



        [TestMethod]
        [TestProperty("httpResponseStatus", "Created")]
        [TestProperty("throwExceptionWithStatus429", "3")]
        [TestProperty("retryAfterHeader", "3")]
        public void TestImportUserPfxCertificateThatsThrottled()
        {
            UserPFXCertificate userPFXCert2Import = new UserPFXCertificate();
            userPFXCert2Import.Thumbprint = "f6f51856-1856-f6f5-5618-f5f65618f5f6";
            userPFXCert2Import.IntendedPurpose = UserPfxIntendedPurpose.SmimeEncryption;
            userPFXCert2Import.UserPrincipalName = "User Principal Name value";
            userPFXCert2Import.StartDateTime = DateTime.Parse("2016-12-31T23:58:46.7156189-07:00");
            userPFXCert2Import.ExpirationDateTime = DateTime.Parse("2016-12-31T23:57:57.2481234-07:00");
            userPFXCert2Import.ProviderName = "Provider Name value";
            userPFXCert2Import.KeyName = "Key Name value";
            userPFXCert2Import.EncryptedPfxBlob = GetEncryptedPFXBlob();
            userPFXCert2Import.EncryptedPfxPassword = Guid.NewGuid().ToString();
            userPFXCert2Import.CreatedDateTime = DateTime.Parse("2017-01-01T00:02:43.5775965-07:00");
            userPFXCert2Import.LastModifiedDateTime = DateTime.Parse("2017-01-01T00:00:35.1329464-07:00");


            Authenticate.AuthTokenIsValid = (AuthRes) =>
            {
                return true;
            };

            List<UserPFXCertificate> thumbprintList = new List<UserPFXCertificate>();
            thumbprintList.Add(userPFXCert2Import);

            mockCmdlet.Object.CertificateList = thumbprintList;

            IEnumerator result = mockCmdlet.Object.Invoke().GetEnumerator();
            Assert.IsFalse(result.MoveNext());
        }


        [TestMethod]
        [TestProperty("httpResponseStatus", "OK")]
        [ExpectedException(typeof(ArgumentException))]
        public void TestImportUserPfxCertificateNoCerts()
        {
            Authenticate.AuthTokenIsValid = (AuthRes) =>
            {
                return true;
            };

            IEnumerator result = mockCmdlet.Object.Invoke().GetEnumerator();
            Assert.IsFalse(result.MoveNext());
        }

        [TestMethod]
        [TestProperty("httpResponseStatus", "OK")]
        [ExpectedException(typeof(AuthenticationException))]
        public void TestImportUserPfxCertificateNoAuth()
        {
            Authenticate.AuthTokenIsValid = (AuthRes) =>
            {
                return false;
            };

            IEnumerator result = mockCmdlet.Object.Invoke().GetEnumerator();
            Assert.IsFalse(result.MoveNext());
        }

        private byte[] GetEncryptedPFXBlob()
        {
            string testPW = "1234";
            X509Certificate2 testCert = CertificateTestUtil.CreateSelfSignedCertificate("TestCertSN", testPW, PaddingHashAlgorithmNames.SHA512);
            byte[] exportedTestCert = testCert.Export(X509ContentType.Pfx, testPW);

            return exportedTestCert;
        }

    }
}
