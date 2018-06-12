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
    using Services.Api;
    using System.Collections.Generic;
    using System.Net;
    using System.IO;
    using System.Text;
    using System.Collections;
    using IdentityModel.Clients.ActiveDirectory;
    #endregion using

    [TestClass]
    public class GetPFXCertificatesUnitTests
    {

        private Mock<GetUserPFXCertificate> mockCmdlet;

        private int webExRethowCnt;

        public TestContext TestContext { get; set; }


        [TestInitialize]
        public void Initialize()
        {
            string httpResponseContent = TestContext.Properties["httpResponseContent"] as string;
            byte[] httpResponseContentBytes = Encoding.UTF8.GetBytes(httpResponseContent);
            MemoryStream stream = new MemoryStream(httpResponseContentBytes);

            Mock<HttpWebResponse> response = new Mock<HttpWebResponse>();
            response.Setup(c => c.GetResponseStream()).Returns(stream);
            response.Setup(c => c.StatusCode).Returns(HttpStatusCode.OK);

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
                if (TestContext.Properties["retryAfterHeader"] != null)
                {
                    headers.Add("x-ms-retry-after-ms", TestContext.Properties["retryAfterHeader"] as string);
                }
                exResponse.Setup(p => p.Headers).Returns(headers);
                Mock<WebException> webEx = new Mock<WebException>("WebException", null, WebExceptionStatus.ProtocolError, exResponse.Object);
                int exCnt = int.Parse(TestContext.Properties["throwExceptionWithStatus429"] as string);
                request.When(() => webExRethowCnt < exCnt).Setup(c => c.GetResponse()).Callback(() => webExRethowCnt++).Throws(webEx.Object);
                request.When(() => webExRethowCnt >= exCnt).Setup(c => c.GetResponse()).Returns(response.Object);
            }

            mockCmdlet = new Mock<GetUserPFXCertificate>();
            mockCmdlet.CallBase = true;
            mockCmdlet.Setup(c => c.CreateWebRequest(It.IsAny<string>(), It.IsAny<AuthenticationResult>())).Returns(request.Object);
            mockCmdlet.Setup(c => c.GetUserIdFromUpn(It.IsAny<string>())).Returns("user-id");
        }

        [TestMethod]
        [TestProperty("httpResponseContent", @"{
        ""@odata.type"": ""#microsoft.graph.userPFXCertificate"",
        ""id"": ""f6f51856-1856-f6f5-5618-f5f65618f5f6"",
        ""thumbprint"": ""f6f51856-1856-f6f5-5618-f5f65618f5f6"",
        ""intendedPurpose"": ""smimeEncryption"",
        ""userPrincipalName"": ""User1"",
        ""startDateTime"": ""2016-12-31T23:58:46.7156189-07:00"",
        ""expirationDateTime"": ""2016-12-31T23:57:57.2481234-07:00"",
        ""providerName"": ""Provider Name value"",
        ""keyName"": ""Key Name value"",
        ""encryptedPfxBlob"": """",
        ""encryptedPfxPassword"": """",
        ""createdDateTime"": ""2017-01-01T00:02:43.5775965-07:00"",
        ""lastModifiedDateTime"": ""2017-01-01T00:00:35.1329464-07:00""
}")]
        public void TestGetPFXCertificatesGetByUserThumbprint()
        {

            Authenticate.AuthTokenIsValid = (AuthRes) =>
            {
                return true;
            };

            List<UserThumbprint> userThumbprintList = new List<UserThumbprint>();
            UserThumbprint userThumbprint = new UserThumbprint();
            userThumbprint.User = "User1";
            userThumbprint.Thumbprint = "f6f51856-1856-f6f5-5618-f5f65618f5f6";
            userThumbprintList.Add(userThumbprint);

            mockCmdlet.Object.UserThumbprintList = userThumbprintList;

            IEnumerator result = mockCmdlet.Object.Invoke().GetEnumerator();
            Assert.IsTrue(result.MoveNext());
            Assert.IsTrue(result.Current is UserPFXCertificate);
            UserPFXCertificate cert = ((UserPFXCertificate)result.Current);
            Assert.AreEqual("f6f51856-1856-f6f5-5618-f5f65618f5f6", cert.Thumbprint);

        }

        [TestMethod]
        [TestProperty("httpResponseContent", @"{
        ""@odata.type"": ""#microsoft.graph.userPFXCertificate"",
        ""id"": ""f6f51856-1856-f6f5-5618-f5f65618f5f6"",
        ""thumbprint"": ""f6f51856-1856-f6f5-5618-f5f65618f5f6"",
        ""intendedPurpose"": ""smimeEncryption"",
        ""userPrincipalName"": ""User1"",
        ""startDateTime"": ""2016-12-31T23:58:46.7156189-07:00"",
        ""expirationDateTime"": ""2016-12-31T23:57:57.2481234-07:00"",
        ""providerName"": ""Provider Name value"",
        ""keyName"": ""Key Name value"",
        ""encryptedPfxBlob"": """",
        ""encryptedPfxPassword"": """",
        ""createdDateTime"": ""2017-01-01T00:02:43.5775965-07:00"",
        ""lastModifiedDateTime"": ""2017-01-01T00:00:35.1329464-07:00""
}")]
        [TestProperty("throwExceptionWithStatus429", "3")]
        [TestProperty("retryAfterHeader", "3")]
        public void TestGetPFXCertificatesGetByUserThumbprintThrottled()
        {

            Authenticate.AuthTokenIsValid = (AuthRes) =>
            {
                return true;
            };

            List<UserThumbprint> userThumbprintList = new List<UserThumbprint>();
            UserThumbprint userThumbprint = new UserThumbprint();
            userThumbprint.User = "User1";
            userThumbprint.Thumbprint = "f6f51856-1856-f6f5-5618-f5f65618f5f6";
            userThumbprintList.Add(userThumbprint);

            mockCmdlet.Object.UserThumbprintList = userThumbprintList;

            IEnumerator result = mockCmdlet.Object.Invoke().GetEnumerator();
            Assert.IsTrue(result.MoveNext());
            Assert.IsTrue(result.Current is UserPFXCertificate);
            UserPFXCertificate cert = ((UserPFXCertificate)result.Current);
            Assert.AreEqual("f6f51856-1856-f6f5-5618-f5f65618f5f6", cert.Thumbprint);

        }

        [TestMethod]
        [TestProperty("httpResponseContent", @"{
  ""value"": [
      {
        ""@odata.type"": ""#microsoft.graph.userPFXCertificate"",
        ""id"": ""f6f51856-1856-f6f5-5618-f5f65618f5f6"",
        ""thumbprint"": ""f6f51856-1856-f6f5-5618-f5f65618f5f6"",
        ""intendedPurpose"": ""smimeEncryption"",
        ""userPrincipalName"": ""User1"",
        ""startDateTime"": ""2016-12-31T23:58:46.7156189-07:00"",
        ""expirationDateTime"": ""2016-12-31T23:57:57.2481234-07:00"",
        ""providerName"": ""Provider Name value"",
        ""keyName"": ""Key Name value"",
        ""encryptedPfxBlob"": """",
        ""encryptedPfxPassword"": """",
        ""createdDateTime"": ""2017-01-01T00:02:43.5775965-07:00"",
        ""lastModifiedDateTime"": ""2017-01-01T00:00:35.1329464-07:00""
      },
      {
        ""@odata.type"": ""#microsoft.graph.userPFXCertificate"",
        ""id"": ""58cedeed-a9c4-4bcb-8b91-61640a1e3fe0"",
        ""thumbprint"": ""58cedeed-a9c4-4bcb-8b91-61640a1e3fe0"",
        ""intendedPurpose"": ""smimeEncryption"",
        ""userPrincipalName"": ""User1"",
        ""startDateTime"": ""2016-12-31T23:58:46.7156189-07:00"",
        ""expirationDateTime"": ""2016-12-31T23:57:57.2481234-07:00"",
        ""providerName"": ""Provider Name value"",
        ""keyName"": ""Key Name value"",
        ""encryptedPfxBlob"": """",
        ""encryptedPfxPassword"": """",
        ""createdDateTime"": ""2017-01-01T00:02:43.5775965-07:00"",
        ""lastModifiedDateTime"": ""2017-01-01T00:00:35.1329464-07:00""
      }
    ]
}")]
       public void TestGetPFXCertificatesGetByUser()
        {
 
            Authenticate.AuthTokenIsValid = (AuthRes) =>
            {
                return true;
            };

            List<string> userList = new List<string>();
            userList.Add("User1");

            mockCmdlet.Object.UserList = userList;

            IEnumerator result = mockCmdlet.Object.Invoke().GetEnumerator();
            Assert.IsTrue(result.MoveNext());
            Assert.IsTrue(result.Current is UserPFXCertificate);
            UserPFXCertificate cert = (UserPFXCertificate)result.Current;
            Assert.AreEqual("f6f51856-1856-f6f5-5618-f5f65618f5f6", cert.Thumbprint);
            Assert.AreEqual("User1", cert.UserPrincipalName);
            Assert.IsTrue(result.MoveNext());
            Assert.IsTrue(result.Current is UserPFXCertificate);
            cert = (UserPFXCertificate)result.Current;
            Assert.AreEqual("58cedeed-a9c4-4bcb-8b91-61640a1e3fe0", cert.Thumbprint);
            Assert.AreEqual("User1", cert.UserPrincipalName);
        }

        [TestMethod]
        [TestProperty("httpResponseContent", @"{
  ""value"": [
      {
        ""@odata.type"": ""#microsoft.graph.userPFXCertificate"",
        ""id"": ""f6f51856-1856-f6f5-5618-f5f65618f5f6"",
        ""thumbprint"": ""f6f51856-1856-f6f5-5618-f5f65618f5f6"",
        ""intendedPurpose"": ""smimeEncryption"",
        ""userPrincipalName"": ""User1"",
        ""startDateTime"": ""2016-12-31T23:58:46.7156189-07:00"",
        ""expirationDateTime"": ""2016-12-31T23:57:57.2481234-07:00"",
        ""providerName"": ""Provider Name value"",
        ""keyName"": ""Key Name value"",
        ""encryptedPfxBlob"": """",
        ""encryptedPfxPassword"": """",
        ""createdDateTime"": ""2017-01-01T00:02:43.5775965-07:00"",
        ""lastModifiedDateTime"": ""2017-01-01T00:00:35.1329464-07:00""
      },
      {
        ""@odata.type"": ""#microsoft.graph.userPFXCertificate"",
        ""id"": ""58cedeed-a9c4-4bcb-8b91-61640a1e3fe0"",
        ""thumbprint"": ""58cedeed-a9c4-4bcb-8b91-61640a1e3fe0"",
        ""intendedPurpose"": ""smimeEncryption"",
        ""userPrincipalName"": ""User1"",
        ""startDateTime"": ""2016-12-31T23:58:46.7156189-07:00"",
        ""expirationDateTime"": ""2016-12-31T23:57:57.2481234-07:00"",
        ""providerName"": ""Provider Name value"",
        ""keyName"": ""Key Name value"",
        ""encryptedPfxBlob"": """",
        ""encryptedPfxPassword"": """",
        ""createdDateTime"": ""2017-01-01T00:02:43.5775965-07:00"",
        ""lastModifiedDateTime"": ""2017-01-01T00:00:35.1329464-07:00""
      }
    ]
}")]
        [TestProperty("throwExceptionWithStatus429", "3")]
        [TestProperty("retryAfterHeader", "3")]
        public void TestGetPFXCertificatesGetByUserThrottled()
        {

            Authenticate.AuthTokenIsValid = (AuthRes) =>
            {
                return true;
            };

            List<string> userList = new List<string>();
            userList.Add("User1");

            mockCmdlet.Object.UserList = userList;

            IEnumerator result = mockCmdlet.Object.Invoke().GetEnumerator();
            Assert.IsTrue(result.MoveNext());
            Assert.IsTrue(result.Current is UserPFXCertificate);
            UserPFXCertificate cert = (UserPFXCertificate)result.Current;
            Assert.AreEqual("f6f51856-1856-f6f5-5618-f5f65618f5f6", cert.Thumbprint);
            Assert.AreEqual("User1", cert.UserPrincipalName);
            Assert.IsTrue(result.MoveNext());
            Assert.IsTrue(result.Current is UserPFXCertificate);
            cert = (UserPFXCertificate)result.Current;
            Assert.AreEqual("58cedeed-a9c4-4bcb-8b91-61640a1e3fe0", cert.Thumbprint);
            Assert.AreEqual("User1", cert.UserPrincipalName);
        }


        [TestMethod]
        [TestProperty("httpResponseContent", @"{
  ""value"": [
      {
        ""@odata.type"": ""#microsoft.graph.userPFXCertificate"",
        ""id"": ""f6f51856-1856-f6f5-5618-f5f65618f5f6"",
        ""thumbprint"": ""f6f51856-1856-f6f5-5618-f5f65618f5f6"",
        ""intendedPurpose"": ""smimeEncryption"",
        ""userPrincipalName"": ""User1"",
        ""startDateTime"": ""2016-12-31T23:58:46.7156189-07:00"",
        ""expirationDateTime"": ""2016-12-31T23:57:57.2481234-07:00"",
        ""providerName"": ""Provider Name value"",
        ""keyName"": ""Key Name value"",
        ""encryptedPfxBlob"": """",
        ""encryptedPfxPassword"": """",
        ""createdDateTime"": ""2017-01-01T00:02:43.5775965-07:00"",
        ""lastModifiedDateTime"": ""2017-01-01T00:00:35.1329464-07:00""
      },
      {
        ""@odata.type"": ""#microsoft.graph.userPFXCertificate"",
        ""id"": ""58cedeed-a9c4-4bcb-8b91-61640a1e3fe0"",
        ""thumbprint"": ""58cedeed-a9c4-4bcb-8b91-61640a1e3fe0"",
        ""intendedPurpose"": ""smimeEncryption"",
        ""userPrincipalName"": ""User1"",
        ""startDateTime"": ""2016-12-31T23:58:46.7156189-07:00"",
        ""expirationDateTime"": ""2016-12-31T23:57:57.2481234-07:00"",
        ""providerName"": ""Provider Name value"",
        ""keyName"": ""Key Name value"",
        ""encryptedPfxBlob"": """",
        ""encryptedPfxPassword"": """",
        ""createdDateTime"": ""2017-01-01T00:02:43.5775965-07:00"",
        ""lastModifiedDateTime"": ""2017-01-01T00:00:35.1329464-07:00""
      }
    ]
}")]
        public void TestGetPFXCertificatesGetAll()
        {

            Authenticate.AuthTokenIsValid = (AuthRes) =>
            {
                return true;
            };

            IEnumerator result = mockCmdlet.Object.Invoke().GetEnumerator();
            Assert.IsTrue(result.MoveNext());
            Assert.IsTrue(result.Current is UserPFXCertificate);
            UserPFXCertificate cert = (UserPFXCertificate)result.Current;
            Assert.AreEqual("f6f51856-1856-f6f5-5618-f5f65618f5f6", cert.Thumbprint);
            Assert.AreEqual("User1", cert.UserPrincipalName);
            Assert.IsTrue(result.MoveNext());
            Assert.IsTrue(result.Current is UserPFXCertificate);
            cert = (UserPFXCertificate) result.Current;
            Assert.AreEqual("58cedeed-a9c4-4bcb-8b91-61640a1e3fe0", cert.Thumbprint);
            Assert.AreEqual("User1", cert.UserPrincipalName);
        }


        [TestMethod]
        [TestProperty("httpResponseContent", @"{
  ""value"": [
      {
        ""@odata.type"": ""#microsoft.graph.userPFXCertificate"",
        ""id"": ""f6f51856-1856-f6f5-5618-f5f65618f5f6"",
        ""thumbprint"": ""f6f51856-1856-f6f5-5618-f5f65618f5f6"",
        ""intendedPurpose"": ""smimeEncryption"",
        ""userPrincipalName"": ""User1"",
        ""startDateTime"": ""2016-12-31T23:58:46.7156189-07:00"",
        ""expirationDateTime"": ""2016-12-31T23:57:57.2481234-07:00"",
        ""providerName"": ""Provider Name value"",
        ""keyName"": ""Key Name value"",
        ""encryptedPfxBlob"": """",
        ""encryptedPfxPassword"": """",
        ""createdDateTime"": ""2017-01-01T00:02:43.5775965-07:00"",
        ""lastModifiedDateTime"": ""2017-01-01T00:00:35.1329464-07:00""
      },
      {
        ""@odata.type"": ""#microsoft.graph.userPFXCertificate"",
        ""id"": ""58cedeed-a9c4-4bcb-8b91-61640a1e3fe0"",
        ""thumbprint"": ""58cedeed-a9c4-4bcb-8b91-61640a1e3fe0"",
        ""intendedPurpose"": ""smimeEncryption"",
        ""userPrincipalName"": ""User1"",
        ""startDateTime"": ""2016-12-31T23:58:46.7156189-07:00"",
        ""expirationDateTime"": ""2016-12-31T23:57:57.2481234-07:00"",
        ""providerName"": ""Provider Name value"",
        ""keyName"": ""Key Name value"",
        ""encryptedPfxBlob"": """",
        ""encryptedPfxPassword"": """",
        ""createdDateTime"": ""2017-01-01T00:02:43.5775965-07:00"",
        ""lastModifiedDateTime"": ""2017-01-01T00:00:35.1329464-07:00""
      }
    ]
}")]
        [TestProperty("throwExceptionWithStatus429", "3")]
        [TestProperty("retryAfterHeader", "3")]
        public void TestGetPFXCertificatesGetAllThrottled()
        {

            Authenticate.AuthTokenIsValid = (AuthRes) =>
            {
                return true;
            };

            IEnumerator result = mockCmdlet.Object.Invoke().GetEnumerator();
            Assert.IsTrue(result.MoveNext());
            Assert.IsTrue(result.Current is UserPFXCertificate);
            UserPFXCertificate cert = (UserPFXCertificate)result.Current;
            Assert.AreEqual("f6f51856-1856-f6f5-5618-f5f65618f5f6", cert.Thumbprint);
            Assert.AreEqual("User1", cert.UserPrincipalName);
            Assert.IsTrue(result.MoveNext());
            Assert.IsTrue(result.Current is UserPFXCertificate);
            cert = (UserPFXCertificate)result.Current;
            Assert.AreEqual("58cedeed-a9c4-4bcb-8b91-61640a1e3fe0", cert.Thumbprint);
            Assert.AreEqual("User1", cert.UserPrincipalName);
        }
    }
}
