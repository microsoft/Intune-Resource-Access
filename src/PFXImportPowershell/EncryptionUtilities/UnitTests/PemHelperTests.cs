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

using Microsoft.Intune.EncryptionUtilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace Microsoft.Intune.Test.EncryptionUtilitiesUnitTests
{
    [TestClass]
    public class PemHelperTests
    {

        private readonly byte[] exponent = new byte[] { 1, 0, 1 };
        private byte[] modulus;

        private string pemFile =
@"-----BEGIN PUBLIC KEY-----
MIIBITANBgkqhkiG9w0BAQEFAAOCAQ4AMIIBCQKCAQACAwQFBgcICQABAgMEBQYH
CAkAAQIDBAUGBwgJAAECAwQFBgcICQABAgMEBQYHCAkAAQIDBAUGBwgJAAECAwQF
BgcICQABAgMEBQYHCAkAAQIDBAUGBwgJAAECAwQFBgcICQABAgMEBQYHCAkAAQID
BAUGBwgJAAECAwQFBgcICQABAgMEBQYHCAkAAQIDBAUGBwgJAAECAwQFBgcICQAB
AgMEBQYHCAkAAQIDBAUGBwgJAAECAwQFBgcICQABAgMEBQYHCAkAAQIDBAUGBwgJ
AAECAwQFBgcICQABAgMEBQYHCAkAAQIDBAUGBwgJAAECAwQFBgcICQABAgMEBQYH
AgMBAAE=
-----END PUBLIC KEY-----
";

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            modulus = new byte[0x100];
            for(int i =0; i < 0x100; i++ )
            {
                modulus[i] = (byte)((i+2)% 10); // For some reason i+1 causes a bad parameter exception and a first byte of zero gets trimmed, so going with i+2
            }
        }

        [TestMethod]
        public void PemGenerate()
        {
            RSACng rsaCng = new RSACng();
            RSAParameters rsaParams = new RSAParameters();
            rsaParams.Modulus = modulus;
            rsaParams.Exponent = exponent;
            rsaCng.ImportParameters(rsaParams);
            string pemDocument = PemHelper.ExportToPem(rsaCng.Key);
            Assert.AreEqual(pemFile, pemDocument);
        }

        [TestMethod]
        public void PemDecode()
        {
            string tempKeyTestPath = Path.Combine(Path.GetTempPath(), "tempkeyTest.pem");
            File.WriteAllText(tempKeyTestPath, pemFile);
            CngKey key = PemHelper.ImportFromPem(tempKeyTestPath);

            RSACng rsaCng = new RSACng(key);

            RSAParameters parameters = rsaCng.ExportParameters(false);
            Assert.IsTrue(exponent.SequenceEqual(parameters.Exponent));
            Assert.IsTrue(modulus.SequenceEqual(parameters.Modulus));
            File.Delete(tempKeyTestPath);
        }
    }
}
