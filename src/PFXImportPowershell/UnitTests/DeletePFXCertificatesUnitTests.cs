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
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.IO;
    using System.Text;
    using System.Collections;
    using IdentityModel.Clients.ActiveDirectory;
    #endregion using

    [TestClass]
    public class DeletePFXCertificatesUnitTests
    {

        private int webExRethowCnt;

        [TestMethod]
        public void TestRemoveUserPfxCertificateByObj()
        {
            UserPFXCertificate userPFXCert2Import = new UserPFXCertificate();
            userPFXCert2Import.Thumbprint = "f6f51856-1856-f6f5-5618-f5f65618f5f6";
            userPFXCert2Import.IntendedPurpose = UserPfxIntendedPurpose.SmimeEncryption;
            userPFXCert2Import.UserPrincipalName = "User Principal Name value";
            userPFXCert2Import.StartDateTime = DateTime.Parse("2016-12-31T23:58:46.7156189-07:00");
            userPFXCert2Import.ExpirationDateTime = DateTime.Parse("2016-12-31T23:57:57.2481234-07:00");
            userPFXCert2Import.ProviderName = "Provider Name value";
            userPFXCert2Import.KeyName = "Key Name value";
            userPFXCert2Import.EncryptedPfxBlob = new byte[0];
            userPFXCert2Import.EncryptedPfxPassword = "";
            userPFXCert2Import.CreatedDateTime = DateTime.Parse("2017-01-01T00:02:43.5775965-07:00");
            userPFXCert2Import.LastModifiedDateTime = DateTime.Parse("2017-01-01T00:00:35.1329464-07:00");

            string httpResponseContent = "";

            byte[] httpResponseContentBytes = Encoding.UTF8.GetBytes(httpResponseContent);
            MemoryStream stream = new MemoryStream(httpResponseContentBytes);

            Mock<HttpWebResponse> response = new Mock<HttpWebResponse>();
            response.Setup(c => c.GetResponseStream()).Returns(stream);
            response.Setup(c => c.StatusCode).Returns(HttpStatusCode.NoContent);

            Mock<HttpWebRequest> request = new Mock<HttpWebRequest>();
            request.Setup(c => c.GetResponse()).Returns(response.Object);

            Mock<MemoryStream> requestStream = new Mock<MemoryStream>();
            request.Setup(c => c.GetRequestStream()).Returns(requestStream.Object);

            Mock<RemoveUserPFXCertificate> cmdlet = new Mock<RemoveUserPFXCertificate>();
            cmdlet.CallBase = true;
            cmdlet.Setup(c => c.CreateWebRequest(It.IsAny<string>(), It.IsAny<AuthenticationResult>())).Returns(request.Object);

            Authenticate.AuthTokenIsValid = (AuthRes) =>
            {
                return true;
            };

            List<UserPFXCertificate> certList = new List<UserPFXCertificate>();
            certList.Add(userPFXCert2Import);

            cmdlet.Object.CertificateList = certList;

            IEnumerator result = cmdlet.Object.Invoke().GetEnumerator();
            Assert.IsFalse(result.MoveNext());
        }


        [TestMethod]
        public void TestRemoveUserPfxCertificateByObjThrottled()
        {
            UserPFXCertificate userPFXCert2Import = new UserPFXCertificate();
            userPFXCert2Import.Thumbprint = "f6f51856-1856-f6f5-5618-f5f65618f5f6";
            userPFXCert2Import.IntendedPurpose = UserPfxIntendedPurpose.SmimeEncryption;
            userPFXCert2Import.UserPrincipalName = "User Principal Name value";
            userPFXCert2Import.StartDateTime = DateTime.Parse("2016-12-31T23:58:46.7156189-07:00");
            userPFXCert2Import.ExpirationDateTime = DateTime.Parse("2016-12-31T23:57:57.2481234-07:00");
            userPFXCert2Import.ProviderName = "Provider Name value";
            userPFXCert2Import.KeyName = "Key Name value";
            userPFXCert2Import.EncryptedPfxBlob = new byte[0];
            userPFXCert2Import.EncryptedPfxPassword = "";
            userPFXCert2Import.CreatedDateTime = DateTime.Parse("2017-01-01T00:02:43.5775965-07:00");
            userPFXCert2Import.LastModifiedDateTime = DateTime.Parse("2017-01-01T00:00:35.1329464-07:00");

            string httpResponseContent = "";

            byte[] httpResponseContentBytes = Encoding.UTF8.GetBytes(httpResponseContent);
            MemoryStream stream = new MemoryStream(httpResponseContentBytes);

            Mock<HttpWebResponse> response = new Mock<HttpWebResponse>();
            response.Setup(c => c.GetResponseStream()).Returns(stream);
            response.Setup(c => c.StatusCode).Returns(HttpStatusCode.NoContent);

            Mock<HttpWebRequest> request = new Mock<HttpWebRequest>();
            webExRethowCnt = 0;

            Mock<HttpWebResponse> exResponse = new Mock<HttpWebResponse>();
            exResponse.Setup(c => c.GetResponseStream()).Returns(stream);
            exResponse.Setup(c => c.StatusCode).Returns((HttpStatusCode)429);
            WebHeaderCollection headers = new WebHeaderCollection();
            headers.Add("x-ms-retry-after-ms", "3");
            exResponse.Setup(p => p.Headers).Returns(headers);
            Mock<WebException> webEx = new Mock<WebException>("WebException", null, WebExceptionStatus.ProtocolError, exResponse.Object);
            int exCnt = 3;
            request.When(() => webExRethowCnt < exCnt).Setup(c => c.GetResponse()).Callback(() => webExRethowCnt++).Throws(webEx.Object);
            request.When(() => webExRethowCnt >= exCnt).Setup(c => c.GetResponse()).Returns(response.Object);

            Mock<MemoryStream> requestStream = new Mock<MemoryStream>();
            request.Setup(c => c.GetRequestStream()).Returns(requestStream.Object);

            Mock<RemoveUserPFXCertificate> cmdlet = new Mock<RemoveUserPFXCertificate>();
            cmdlet.CallBase = true;
            cmdlet.Setup(c => c.CreateWebRequest(It.IsAny<string>(), It.IsAny<AuthenticationResult>())).Returns(request.Object);

            Authenticate.AuthTokenIsValid = (AuthRes) =>
            {
                return true;
            };

            List<UserPFXCertificate> certList = new List<UserPFXCertificate>();
            certList.Add(userPFXCert2Import);

            cmdlet.Object.CertificateList = certList;

            IEnumerator result = cmdlet.Object.Invoke().GetEnumerator();
            Assert.IsFalse(result.MoveNext());
        }

        [TestMethod]
        public void TestRemoveUserPfxCertificateByThumbprint()
        {

            string httpResponseContent = "";

            byte[] httpResponseContentBytes = Encoding.UTF8.GetBytes(httpResponseContent);
            MemoryStream stream = new MemoryStream(httpResponseContentBytes);

            Mock<HttpWebResponse> response = new Mock<HttpWebResponse>();
            response.Setup(c => c.GetResponseStream()).Returns(stream);
            response.Setup(c => c.StatusCode).Returns(HttpStatusCode.NoContent);

            Mock<HttpWebRequest> request = new Mock<HttpWebRequest>();
            request.Setup(c => c.GetResponse()).Returns(response.Object);

            Mock<MemoryStream> requestStream = new Mock<MemoryStream>();
            request.Setup(c => c.GetRequestStream()).Returns(requestStream.Object);

            Mock<RemoveUserPFXCertificate> cmdlet = new Mock<RemoveUserPFXCertificate>();
            cmdlet.CallBase = true;
            cmdlet.Setup(c => c.CreateWebRequest(It.IsAny<string>(), It.IsAny<AuthenticationResult>())).Returns(request.Object);

            Authenticate.AuthTokenIsValid = (AuthRes) =>
            {
                return true;
            };

            List<string> thumbprintList = new List<string>()
                { "f6f51856-1856-f6f5-5618-f5f65618f5f6" };

            cmdlet.Object.ThumbprintList = thumbprintList;

            IEnumerator result = cmdlet.Object.Invoke().GetEnumerator();
            Assert.IsFalse(result.MoveNext());
        }


        [TestMethod]
        public void TestRemoveUserPfxCertificateByThumbprintThrottled()
        {

            string httpResponseContent = "";

            byte[] httpResponseContentBytes = Encoding.UTF8.GetBytes(httpResponseContent);
            MemoryStream stream = new MemoryStream(httpResponseContentBytes);

            Mock<HttpWebResponse> response = new Mock<HttpWebResponse>();
            response.Setup(c => c.GetResponseStream()).Returns(stream);
            response.Setup(c => c.StatusCode).Returns(HttpStatusCode.NoContent);

            Mock<HttpWebRequest> request = new Mock<HttpWebRequest>();
            webExRethowCnt = 0;

            Mock<HttpWebResponse> exResponse = new Mock<HttpWebResponse>();
            exResponse.Setup(c => c.GetResponseStream()).Returns(stream);
            exResponse.Setup(c => c.StatusCode).Returns((HttpStatusCode)429);
            WebHeaderCollection headers = new WebHeaderCollection();
            headers.Add("x-ms-retry-after-ms", "3");
            exResponse.Setup(p => p.Headers).Returns(headers);
            Mock<WebException> webEx = new Mock<WebException>("WebException", null, WebExceptionStatus.ProtocolError, exResponse.Object);
            int exCnt = 3;
            request.When(() => webExRethowCnt < exCnt).Setup(c => c.GetResponse()).Callback(() => webExRethowCnt++).Throws(webEx.Object);
            request.When(() => webExRethowCnt >= exCnt).Setup(c => c.GetResponse()).Returns(response.Object);

            Mock<MemoryStream> requestStream = new Mock<MemoryStream>();
            request.Setup(c => c.GetRequestStream()).Returns(requestStream.Object);

            Mock<RemoveUserPFXCertificate> cmdlet = new Mock<RemoveUserPFXCertificate>();
            cmdlet.CallBase = true;
            cmdlet.Setup(c => c.CreateWebRequest(It.IsAny<string>(), It.IsAny<AuthenticationResult>())).Returns(request.Object);

            Authenticate.AuthTokenIsValid = (AuthRes) =>
            {
                return true;
            };

            List<string> thumbprintList = new List<string>()
                { "f6f51856-1856-f6f5-5618-f5f65618f5f6" };

            cmdlet.Object.ThumbprintList = thumbprintList;

            IEnumerator result = cmdlet.Object.Invoke().GetEnumerator();
            Assert.IsFalse(result.MoveNext());
        }

        [TestMethod]
        public void TestRemoveUserPfxCretificateByUser()
        {
            string httpResponseContent = "";

            byte[] httpResponseContentBytes = Encoding.UTF8.GetBytes(httpResponseContent);
            MemoryStream stream = new MemoryStream(httpResponseContentBytes);

            Mock<HttpWebResponse> response = new Mock<HttpWebResponse>();
            response.Setup(c => c.GetResponseStream()).Returns(stream);
            response.Setup(c => c.StatusCode).Returns(HttpStatusCode.NoContent);

            Mock<HttpWebRequest> request = new Mock<HttpWebRequest>();
            request.Setup(c => c.GetResponse()).Returns(response.Object);

            Mock<MemoryStream> requestStream = new Mock<MemoryStream>();
            request.Setup(c => c.GetRequestStream()).Returns(requestStream.Object);

            Mock<RemoveUserPFXCertificate> cmdlet = new Mock<RemoveUserPFXCertificate>();
            cmdlet.CallBase = true;
            cmdlet.Setup(c => c.CreateWebRequest(It.IsAny<string>(), It.IsAny<AuthenticationResult>())).Returns(request.Object);

            Mock<GetUserPFXCertificate> getCmdlet = new Mock<GetUserPFXCertificate>();
            getCmdlet.CallBase = true;

            string getHttpResponseContent =

@"{
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
}";
            byte[] getHttpResponseContentBytes = Encoding.UTF8.GetBytes(getHttpResponseContent);
            MemoryStream getStream = new MemoryStream(getHttpResponseContentBytes);

            Mock<HttpWebResponse> getResponse = new Mock<HttpWebResponse>();
            getResponse.Setup(c => c.GetResponseStream()).Returns(getStream);
            getResponse.Setup(c => c.StatusCode).Returns(HttpStatusCode.OK);

            Mock<HttpWebRequest> getRequest = new Mock<HttpWebRequest>();
            getRequest.Setup(c => c.GetResponse()).Returns(getResponse.Object);

            getCmdlet.Setup(c => c.CreateWebRequest(It.IsAny<string>(), It.IsAny<AuthenticationResult>())).Returns(getRequest.Object);


            cmdlet.Setup(c => c.GetUserPFXCertificate()).Returns(getCmdlet.Object);

            Authenticate.AuthTokenIsValid = (AuthRes) =>
            {
                return true;
            };

            List<string> userList = new List<string>()
                { "user0@contoso.onmicrosoft.com" };

            cmdlet.Object.UserList = userList;

            IEnumerator result = cmdlet.Object.Invoke().GetEnumerator();
            Assert.IsFalse(result.MoveNext());
        }

        [TestMethod]
        public void TestRemoveUserPfxCretificateByUserThrottled()
        {
            string httpResponseContent = "";

            byte[] httpResponseContentBytes = Encoding.UTF8.GetBytes(httpResponseContent);
            MemoryStream stream = new MemoryStream(httpResponseContentBytes);

            Mock<HttpWebResponse> response = new Mock<HttpWebResponse>();
            response.Setup(c => c.GetResponseStream()).Returns(stream);
            response.Setup(c => c.StatusCode).Returns(HttpStatusCode.NoContent);

            Mock<HttpWebRequest> request = new Mock<HttpWebRequest>();
            webExRethowCnt = 0;

            Mock<HttpWebResponse> exResponse = new Mock<HttpWebResponse>();
            exResponse.Setup(c => c.GetResponseStream()).Returns(stream);
            exResponse.Setup(c => c.StatusCode).Returns((HttpStatusCode)429);
            WebHeaderCollection headers = new WebHeaderCollection();
            headers.Add("x-ms-retry-after-ms", "3");
            exResponse.Setup(p => p.Headers).Returns(headers);
            Mock<WebException> webEx = new Mock<WebException>("WebException", null, WebExceptionStatus.ProtocolError, exResponse.Object);
            int exCnt = 3;
            request.When(() => webExRethowCnt < exCnt).Setup(c => c.GetResponse()).Callback(() => webExRethowCnt++).Throws(webEx.Object);
            request.When(() => webExRethowCnt >= exCnt).Setup(c => c.GetResponse()).Returns(response.Object);

            Mock<MemoryStream> requestStream = new Mock<MemoryStream>();
            request.Setup(c => c.GetRequestStream()).Returns(requestStream.Object);

            Mock<RemoveUserPFXCertificate> cmdlet = new Mock<RemoveUserPFXCertificate>();
            cmdlet.CallBase = true;
            cmdlet.Setup(c => c.CreateWebRequest(It.IsAny<string>(), It.IsAny<AuthenticationResult>())).Returns(request.Object);

            Mock<GetUserPFXCertificate> getCmdlet = new Mock<GetUserPFXCertificate>();
            getCmdlet.CallBase = true;

            string getHttpResponseContent =

@"{
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
}";
            byte[] getHttpResponseContentBytes = Encoding.UTF8.GetBytes(getHttpResponseContent);
            MemoryStream getStream = new MemoryStream(getHttpResponseContentBytes);

            Mock<HttpWebResponse> getResponse = new Mock<HttpWebResponse>();
            getResponse.Setup(c => c.GetResponseStream()).Returns(getStream);
            getResponse.Setup(c => c.StatusCode).Returns(HttpStatusCode.OK);

            Mock<HttpWebRequest> getRequest = new Mock<HttpWebRequest>();
            getRequest.Setup(c => c.GetResponse()).Returns(getResponse.Object);

            getCmdlet.Setup(c => c.CreateWebRequest(It.IsAny<string>(), It.IsAny<AuthenticationResult>())).Returns(getRequest.Object);


            cmdlet.Setup(c => c.GetUserPFXCertificate()).Returns(getCmdlet.Object);

            Authenticate.AuthTokenIsValid = (AuthRes) =>
            {
                return true;
            };

            List<string> userList = new List<string>()
                { "user0@contoso.onmicrosoft.com" };

            cmdlet.Object.UserList = userList;

            IEnumerator result = cmdlet.Object.Invoke().GetEnumerator();
            Assert.IsFalse(result.MoveNext());
        }
    }
}
