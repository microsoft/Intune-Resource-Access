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
    using System;
    using System.Security;
    using System.Linq;
    using System.Management.Automation;
    using System.Management.Automation.Runspaces;
    using System.Text;
    using Cmdlets;
    using Services.Api;
    using VisualStudio.TestTools.UnitTesting;
    using System.Security.Cryptography;
    using System.IO;
    using Microsoft.Intune.EncryptionUtilities;
    using System.Security.Cryptography.X509Certificates;

    [TestClass]
    [Ignore] //Needs to be run as admin to manage machine certs.
    public class NewUserPFXCertificateUnitTests
    {
        private const string TestFilePath1 = @"TestCertificates\TestPFX.pfx";

        private const string TestUPN1 = "IWUser0@contoso.onmicrosoft.com";

        private const string TestProviderName1 = "Microsoft Software Key Storage Provider";

        private const string TestKeyName1 = "IntuneImportPFXTestKey";

        private const string TestAlgorithmName = "RSA";

        // [SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Unit Test with fake password")]
        private const string testPassword = "1234";

        private InitialSessionState initialSessionState;

        private Runspace runspace;

        private SecureString securePassword;

        private PowerShell powershell;

        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void Initialize()
        {
            initialSessionState = InitialSessionState.CreateDefault();
            initialSessionState.Commands.Add(
                new SessionStateCmdletEntry(
                    "New-IntuneUserPfxCertificate", typeof(NewUserPFXCertificate), null));

            runspace = RunspaceFactory.CreateRunspace(initialSessionState);
            runspace.Open();

            powershell = PowerShell.Create();
            powershell.Runspace = runspace;

            securePassword = new SecureString();
            foreach (char c in testPassword)
            {
                securePassword.AppendChar(c);
            }

            //Need to create the pfxfile

            X509Certificate2 testCert = CertificateTestUtil.CreateSelfSignedCertificate("TestCertSN", testPassword, PaddingHashAlgorithmNames.SHA512);
            byte[] exportedTestCert = testCert.Export(X509ContentType.Pfx, testPassword);
            using (FileStream fs = new FileStream(TestFilePath1, FileMode.OpenOrCreate))
            {
                fs.Write(exportedTestCert, 0, exportedTestCert.Length);
            }
            testCert.Export(X509ContentType.Pfx, testPassword);
        }

        [TestCleanup]
        public void Cleanup()
        {
            //Clear out the pfx file
            File.Delete(TestFilePath1);
        }

        [TestMethod]
        public void TestEncryptPFXFile()
        {
            ProviderKeyInitialize(TestProviderName1, TestKeyName1, TestAlgorithmName);

            Command encryptCommand = GenerateSetUserPFXCertificatesCommand(
                TestFilePath1,
                TestUPN1,
                securePassword,
                TestProviderName1,
                TestKeyName1,
                UserPfxPaddingScheme.None,
                UserPfxIntendedPurpose.SmimeEncryption);

            powershell.Commands.AddCommand(encryptCommand);

            var pfxResults = powershell.Invoke<UserPFXCertificate>();

            Assert.AreEqual(pfxResults.Count(), 1);

            UserPFXCertificate userPFXResult = pfxResults.First();

            Assert.AreEqual(userPFXResult.KeyName, TestKeyName1);
            Assert.AreEqual(userPFXResult.ProviderName, TestProviderName1);
            Assert.AreNotEqual(userPFXResult.EncryptedPfxPassword, testPassword);
            Assert.AreEqual(userPFXResult.UserPrincipalName, TestUPN1);
            Assert.AreEqual(userPFXResult.PaddingScheme, UserPfxPaddingScheme.None);
            Assert.AreEqual(userPFXResult.IntendedPurpose, UserPfxIntendedPurpose.SmimeEncryption);
            Assert.IsNotNull(userPFXResult.EncryptedPfxBlob);

            ValidatePasswordDecryptable(userPFXResult, testPassword, PaddingHashAlgorithmNames.SHA512, PaddingFlags.OAEPPadding);

            ProviderKeyCleanup(TestProviderName1, TestKeyName1);
        }

        [TestMethod]
        public void TestEncryptPFXFileOaepSha256()
        {
            string hashAlgorithm = PaddingHashAlgorithmNames.SHA256;
            int paddingFlags = PaddingFlags.OAEPPadding;

            ProviderKeyInitialize(TestProviderName1, TestKeyName1, TestAlgorithmName);

            Command encryptCommand = GenerateSetUserPFXCertificatesCommand(
                TestFilePath1,
                TestUPN1,
                securePassword,
                TestProviderName1,
                TestKeyName1,
                UserPfxPaddingScheme.OaepSha256,
                UserPfxIntendedPurpose.SmimeEncryption);

            powershell.Commands.AddCommand(encryptCommand);

            var pfxResults = powershell.Invoke<UserPFXCertificate>();

            Assert.AreEqual(pfxResults.Count(), 1);

            UserPFXCertificate userPFXResult = pfxResults.First();

            Assert.AreEqual(userPFXResult.PaddingScheme, UserPfxPaddingScheme.OaepSha256);

            ValidatePasswordDecryptable(userPFXResult, testPassword, hashAlgorithm, paddingFlags);

            ProviderKeyCleanup(TestProviderName1, TestKeyName1);
        }

        [TestMethod]
        public void TestEncryptPFXFileOaepSha384()
        {
            string hashAlgorithm = PaddingHashAlgorithmNames.SHA384;
            int paddingFlags = PaddingFlags.OAEPPadding;

            ProviderKeyInitialize(TestProviderName1, TestKeyName1, TestAlgorithmName);

            Command encryptCommand = GenerateSetUserPFXCertificatesCommand(
                TestFilePath1,
                TestUPN1,
                securePassword,
                TestProviderName1,
                TestKeyName1,
                UserPfxPaddingScheme.OaepSha384,
                UserPfxIntendedPurpose.SmimeEncryption);

            powershell.Commands.AddCommand(encryptCommand);

            var pfxResults = powershell.Invoke<UserPFXCertificate>();

            Assert.AreEqual(pfxResults.Count(), 1);

            UserPFXCertificate userPFXResult = pfxResults.First();

            Assert.AreEqual(userPFXResult.PaddingScheme, UserPfxPaddingScheme.OaepSha384);

            ValidatePasswordDecryptable(userPFXResult, testPassword, hashAlgorithm, paddingFlags);

            ProviderKeyCleanup(TestProviderName1, TestKeyName1);
        }

        [TestMethod]
        public void TestEncryptPFXFileOaepSha512()
        {
            string hashAlgorithm = PaddingHashAlgorithmNames.SHA512;
            int paddingFlags = PaddingFlags.OAEPPadding;

            ProviderKeyInitialize(TestProviderName1, TestKeyName1, TestAlgorithmName);

            Command encryptCommand = GenerateSetUserPFXCertificatesCommand(
                TestFilePath1,
                TestUPN1,
                securePassword,
                TestProviderName1,
                TestKeyName1,
                UserPfxPaddingScheme.OaepSha512,
                UserPfxIntendedPurpose.SmimeEncryption);

            powershell.Commands.AddCommand(encryptCommand);

            var pfxResults = powershell.Invoke<UserPFXCertificate>();

            Assert.AreEqual(pfxResults.Count(), 1);

            UserPFXCertificate userPFXResult = pfxResults.First();

            Assert.AreEqual(userPFXResult.PaddingScheme, UserPfxPaddingScheme.OaepSha512);

            ValidatePasswordDecryptable(userPFXResult, testPassword, hashAlgorithm, paddingFlags);

            ProviderKeyCleanup(TestProviderName1, TestKeyName1);
        }

        [TestMethod]
        public void TestBadFileType()
        {
            ProviderKeyInitialize(TestProviderName1, TestKeyName1, TestAlgorithmName);

            Command encryptCommand = GenerateSetUserPFXCertificatesCommand(
                @"TestCertificates\TestBadFile.txt",
                TestUPN1,
                securePassword,
                TestProviderName1,
                TestKeyName1,
                UserPfxPaddingScheme.None,
                UserPfxIntendedPurpose.SmimeEncryption);

            powershell.Commands.AddCommand(encryptCommand);
            try
            {
                var pfxResults = powershell.Invoke<UserPFXCertificate>();
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.Contains("Could not Read Thumbprint"));
            }

            ProviderKeyCleanup(TestProviderName1, TestKeyName1);
        }


        [TestMethod]
        public void TestWrongPassword()
        {
            ProviderKeyInitialize(TestProviderName1, TestKeyName1, TestAlgorithmName);

            //[SuppressMessage("Microsoft.Security", "CS002:SecretInNextLine", Justification = "Unit Test with fake password")]
            string testPassword = "12345";
            SecureString badSecurePassword = new SecureString();
            foreach (char c in testPassword)
            {
                badSecurePassword.AppendChar(c);
            }

            Command encryptCommand = GenerateSetUserPFXCertificatesCommand(
                TestFilePath1,
                TestUPN1,
                badSecurePassword,
                TestProviderName1,
                TestKeyName1,
                UserPfxPaddingScheme.None,
                UserPfxIntendedPurpose.SmimeEncryption);

            powershell.Commands.AddCommand(encryptCommand);
            try
            {
                var pfxResults = powershell.Invoke<UserPFXCertificate>();
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.Contains("Verify Password is Correct"));
            }

            ProviderKeyCleanup(TestProviderName1, TestKeyName1);
        }

        [TestMethod]
        [ExpectedException(typeof(CmdletInvocationException))]
        public void TestBadProviderName()
        {
            Command encryptCommand = GenerateSetUserPFXCertificatesCommand(
                TestFilePath1,
                TestUPN1,
                securePassword,
                "Holy Provider of Azeroth",
                TestKeyName1,
                UserPfxPaddingScheme.OaepSha512,
                UserPfxIntendedPurpose.SmimeEncryption);

            powershell.Commands.AddCommand(encryptCommand);

            _ = powershell.Invoke<UserPFXCertificate>();
        }

        [TestMethod]
        public void TestEncryptPFXFileBase64String()
        {
            ProviderKeyInitialize(TestProviderName1, TestKeyName1, TestAlgorithmName);

            byte[] pfxData = File.ReadAllBytes(TestFilePath1);

            string base64String = Convert.ToBase64String(pfxData);

            Command encryptCommand = GenerateSetUserPFXCertificatesCommand(
            null,
            TestUPN1,
            securePassword,
            TestProviderName1,
            TestKeyName1,
            UserPfxPaddingScheme.None,
            UserPfxIntendedPurpose.SmimeEncryption,
            base64String);

            powershell.Commands.AddCommand(encryptCommand);

            var pfxResults = powershell.Invoke<UserPFXCertificate>();

            Assert.AreEqual(pfxResults.Count(), 1);

            UserPFXCertificate userPFXResult = pfxResults.First();

            Assert.AreEqual(userPFXResult.KeyName, TestKeyName1);
            Assert.AreEqual(userPFXResult.ProviderName, TestProviderName1);
            Assert.AreNotEqual(userPFXResult.EncryptedPfxPassword, testPassword);
            Assert.AreEqual(userPFXResult.UserPrincipalName, TestUPN1);
            Assert.AreEqual(userPFXResult.PaddingScheme, UserPfxPaddingScheme.None);
            Assert.AreEqual(userPFXResult.IntendedPurpose, UserPfxIntendedPurpose.SmimeEncryption);
            Assert.IsNotNull(userPFXResult.EncryptedPfxBlob);

            ValidatePasswordDecryptable(userPFXResult, testPassword, PaddingHashAlgorithmNames.SHA512, PaddingFlags.OAEPPadding);

            ProviderKeyCleanup(TestProviderName1, TestKeyName1);
        }

        private void ProviderKeyInitialize(string providerName, string keyName, string algorithmName)
        {
            ManagedRSAEncryption managedRSA = new ManagedRSAEncryption();
            if(!managedRSA.TryGenerateLocalRSAKey(providerName, keyName))
            {
                //Delete and try again
                managedRSA.DestroyLocalRSAKey(providerName, keyName);
                managedRSA.TryGenerateLocalRSAKey(providerName, keyName);
            }
        }

        public void ProviderKeyCleanup(string providerName, string keyName)
        {
            //Clear out the test key
            ManagedRSAEncryption managedRSA = new ManagedRSAEncryption();
            managedRSA.DestroyLocalRSAKey(providerName, keyName);
        }

        private void ValidatePasswordDecryptable(UserPFXCertificate userPFXResult, string expectedPassword, string hashAlgorithm, int paddingFlags)
        {
            ManagedRSAEncryption encryptionUtility = new ManagedRSAEncryption();
            byte[] passwordBytes = Convert.FromBase64String(userPFXResult.EncryptedPfxPassword);
            byte[] unencryptedPassword = encryptionUtility.DecryptWithLocalKey(userPFXResult.ProviderName, userPFXResult.KeyName, passwordBytes, hashAlgorithm, paddingFlags);
            string clearTextPassword = Encoding.ASCII.GetString(unencryptedPassword);

            Assert.AreEqual(clearTextPassword, expectedPassword);
        }

        private Command GenerateSetUserPFXCertificatesCommand(
            string pathToPFXFile, 
            string upn,
            SecureString pfxPassword,
            string providerName,
            string keyName,
            UserPfxPaddingScheme paddingScheme,
            UserPfxIntendedPurpose intendedPurpose,
            string base64Cert = null)
        {
            var encryptCommand = new Command("New-IntuneUserPfxCertificate");
            if(base64Cert == null)
            {
                encryptCommand.Parameters.Add("PathToPfxFile", pathToPFXFile);
            }else
            {
                encryptCommand.Parameters.Add("Base64EncodedPfx", base64Cert);
            }
            encryptCommand.Parameters.Add("UPN", upn);
            encryptCommand.Parameters.Add("PfxPassword", pfxPassword);
            encryptCommand.Parameters.Add("ProviderName", providerName);
            encryptCommand.Parameters.Add("KeyName", keyName);
            encryptCommand.Parameters.Add("PaddingScheme", paddingScheme);
            encryptCommand.Parameters.Add("IntendedPurpose", intendedPurpose);

            return encryptCommand;
        }

    }
}
