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
using Moq;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Intune.Test.EncryptionUtilitiesUnitTests
{
    [TestClass]
    [Ignore] //Needs to be run as admin to manage machine certs.
    public class EncryptionTests
    {
        public const string TestKeyName = "ThisIsATestKey";
        public const string TestKeyAlgorithmName = CNGNCryptInterop.AlgorithmIds.BCRYPT_RSA_ALGORITHM;
        public const string TestProvider = CNGNCryptInterop.ProviderNames.MS_KEY_STORAGE_PROVIDER;

        public const string TestCertPassword = "TestCert";
        [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine")]
        public const string TestCertBase64Encoded = "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";

        [TestInitialize]
        public void TestInitialize()
        {
            ManagedRSAEncryption managedRSA = new ManagedRSAEncryption();
            if (!managedRSA.TryGenerateLocalRSAKey(TestProvider, TestKeyName))
            {
                //Delete and try again
                managedRSA.DestroyLocalRSAKey(TestProvider, TestKeyName);
                managedRSA.TryGenerateLocalRSAKey(TestProvider, TestKeyName);
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            //Clear out the test key
            ManagedRSAEncryption managedRSA = new ManagedRSAEncryption();
            managedRSA.DestroyLocalRSAKey(TestProvider, TestKeyName);
        }



        [TestMethod]
        public void CertKeyEncryptionE2E()
        {
            byte[] toEncrypt = new byte[] { 1, 2, 3, 4 };
            ManagedRSAEncryption managed = new ManagedRSAEncryption();
            X509Certificate2 testCert = new X509Certificate2(Convert.FromBase64String(TestCertBase64Encoded), TestCertPassword);

            byte[] encrypted = managed.EncryptWithCertificate(toEncrypt, testCert);
            byte[] decrypted = managed.DecryptWithCertificate(encrypted, testCert);

            CollectionAssert.AreNotEqual(toEncrypt, encrypted);
            CollectionAssert.AreNotEqual(encrypted, decrypted);
            CollectionAssert.AreEqual(toEncrypt, decrypted);
        }

        [TestMethod]
        public void EncryptionUtilitiesFullE2E()
        {
            byte[] originalData = new byte[] { 1, 2, 3, 4 };
            ManagedRSAEncryption managed = new ManagedRSAEncryption();
            X509Certificate2 testCert = new X509Certificate2(Convert.FromBase64String(TestCertBase64Encoded), TestCertPassword);

            byte[] encryptedWithLocal = managed.EncryptWithLocalKey(TestProvider, TestKeyName, originalData);
            byte[] recryptedWithDeviceCert = managed.RecryptPfxImportMessage(encryptedWithLocal, testCert, TestProvider, TestKeyName);
            byte[] decryptedWithDeviceCert = managed.DecryptWithCertificate(recryptedWithDeviceCert, testCert);

            CollectionAssert.AreEqual(originalData, decryptedWithDeviceCert);
            CollectionAssert.AreNotEqual(originalData, encryptedWithLocal);
            CollectionAssert.AreNotEqual(originalData, recryptedWithDeviceCert);
            CollectionAssert.AreNotEqual(encryptedWithLocal, recryptedWithDeviceCert);
            CollectionAssert.AreNotEqual(encryptedWithLocal, decryptedWithDeviceCert);
            CollectionAssert.AreNotEqual(recryptedWithDeviceCert, decryptedWithDeviceCert);
        }

    }
}
