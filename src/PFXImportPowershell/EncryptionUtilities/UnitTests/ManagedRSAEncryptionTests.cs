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
using System.Security.Cryptography;

namespace Microsoft.Intune.Test.EncryptionUtilitiesUnitTests
{
    [TestClass]
    [Ignore] //Needs to be run as admin to manage machine certs. 
    public class ManagedRSAEncryptionTests
    {
        public const string TestKeyName = "ThisIsATestKey";
        public const string TestProvider = CNGNCryptInterop.ProviderNames.MS_KEY_STORAGE_PROVIDER;

        /// <summary>
        /// Initialize
        /// </summary>
        [TestInitialize]
        public void TestInitialize()
        {
            ManagedRSAEncryption utility = new ManagedRSAEncryption();
            //Create the test key

            utility.TryGenerateLocalRSAKey(TestProvider, TestKeyName);
        }

        /// <summary>
        /// Cleanup
        /// </summary>
        [TestCleanup]
        public void TestCleanup()
        {
            ManagedRSAEncryption utility = new ManagedRSAEncryption();
            //Destroy the test key

            try
            {
                utility.DestroyLocalRSAKey(TestProvider, TestKeyName);
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Encrypt and decrypt using RSA CNG
        /// </summary>
        [TestMethod]
        public void RSALocalKeyEncryptionDecryptionE2E()
        {
            byte[] toEncrypt = new byte[] { 1, 2, 3, 4 };
            ManagedRSAEncryption util = new ManagedRSAEncryption();
            byte[] encrypted = util.EncryptWithLocalKey(TestProvider, TestKeyName, toEncrypt);
            byte[] decrypted = util.DecryptWithLocalKey(TestProvider, TestKeyName, encrypted);

            CollectionAssert.AreNotEqual(encrypted, toEncrypt);
            CollectionAssert.AreNotEqual(encrypted, decrypted);
            CollectionAssert.AreEqual(toEncrypt, decrypted);

            Assert.AreNotEqual(0, encrypted.Length);
            Assert.AreNotEqual(0, decrypted.Length);
            Assert.IsNotNull(encrypted);
            Assert.IsNotNull(decrypted);
        }

        /// <summary>
        /// Encrypt and decrypt with OAEP padding
        /// </summary>
        [TestMethod]
        public void RSALocalKeyEncryptionDecryptionOAEPE2E()
        {
            byte[] toEncrypt = new byte[] { 1, 2, 3, 4 };
            ManagedRSAEncryption util = new ManagedRSAEncryption();
            byte[] encrypted = util.EncryptWithLocalKey(TestProvider, TestKeyName, toEncrypt, PaddingHashAlgorithmNames.SHA256, PaddingFlags.OAEPPadding);
            byte[] decrypted = util.DecryptWithLocalKey(TestProvider, TestKeyName, encrypted, PaddingHashAlgorithmNames.SHA256, PaddingFlags.OAEPPadding);

            CollectionAssert.AreNotEqual(encrypted, toEncrypt);
            CollectionAssert.AreNotEqual(encrypted, decrypted);
            CollectionAssert.AreEqual(toEncrypt, decrypted);

            Assert.AreNotEqual(0, encrypted.Length);
            Assert.AreNotEqual(0, decrypted.Length);
            Assert.IsNotNull(encrypted);
            Assert.IsNotNull(decrypted);
        }

        /// <summary>
        /// Missing key exception test
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(CryptographicException))]
        public void RSALocalKeyDecryptionKeyMissingThrows()
        {
            byte[] toEncrypt = new byte[] { 1, 2, 3, 4 };
            ManagedRSAEncryption util = new ManagedRSAEncryption();
            byte[] encrypted = util.EncryptWithLocalKey(TestProvider, TestKeyName, toEncrypt);
            byte[] decrypted = util.DecryptWithLocalKey(TestProvider, "FakeKeyName", encrypted);
        }

        /// <summary>
        /// Invalid provider test
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(CryptographicException))]
        public void RSALocalKeyDecryptionProviderMissingThrows()
        {
            byte[] toEncrypt = new byte[] { 1, 2, 3, 4 };
            ManagedRSAEncryption util = new ManagedRSAEncryption();
            byte[] encrypted = util.EncryptWithLocalKey(TestProvider, TestKeyName, toEncrypt);
            byte[] decrypted = util.DecryptWithLocalKey("FakeProviderName", TestKeyName, encrypted);
        }

        /// <summary>
        /// Invalid padding scheme test
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(CryptographicException))]
        public void RSALocalKeyDecryptionPaddingInvalidThrows()
        {
            byte[] toEncrypt = new byte[] { 1, 2, 3, 4 };
            ManagedRSAEncryption util = new ManagedRSAEncryption();
            byte[] encrypted = util.EncryptWithLocalKey(TestProvider, TestKeyName, toEncrypt, null, PaddingFlags.PSSPadding + 10000);
        }

        /// <summary>
        /// Invalid padding hash algorithm test
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(CryptographicException))]
        public void RSALocalKeyDecryptionPaddingHashInvalidThrows()
        {
            byte[] toEncrypt = new byte[] { 1, 2, 3, 4 };
            ManagedRSAEncryption util = new ManagedRSAEncryption();
            byte[] encrypted = util.EncryptWithLocalKey(TestProvider, TestKeyName, toEncrypt, "Not a real padding hash function", PaddingFlags.OAEPPadding);
        }


        /// <summary>
        /// Test exporting a public key to a file and reading it back in and encrypting.
        /// </summary>
        [TestMethod]
        public void ExportImportEncryptTest()
        {
            byte[] toEncrypt = new byte[] { 1, 2, 3, 4 };
            string filePath = @".\TestPublic.Key";
            try
            {
                File.Delete(filePath);
            }
            catch (Exception) { }
            ManagedRSAEncryption util = new ManagedRSAEncryption();
            util.ExportPublicKeytoFile(TestProvider, TestKeyName, filePath);
            Assert.IsTrue(File.Exists(filePath));
            byte[] encryptedBytes = util.EncryptWithFileKey(filePath, toEncrypt);
            Assert.IsNotNull(encryptedBytes);
            byte[] decryptedBytes = util.DecryptWithLocalKey(TestProvider, TestKeyName, encryptedBytes);
            Assert.AreEqual(System.Convert.ToBase64String(toEncrypt), System.Convert.ToBase64String(decryptedBytes));
            try
            {
                File.Delete(filePath);
            }
            catch (Exception) { }
        }


    }
}
