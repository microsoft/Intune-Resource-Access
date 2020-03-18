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

namespace Microsoft.Intune.EncryptionUtilities
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security.AccessControl;
    using System.Security.Cryptography;
    using System.Security.Cryptography.Pkcs;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Principal;
    using System.Text;

    public class ManagedRSAEncryption : ICNGLocalKeyCrypto
    {

        /// <summary>
        /// Defines the format of exported Public Key
        /// CngBlob - RSA PUBLIC BLOB format
        /// PEM - Public Key PEM format
        /// </summary>
        public enum FileFormat
        {
            CngBlob = 0,
            PEM = 1
        }

        /// <summary>
        /// Default constructor for serialization
        /// </summary>
        public ManagedRSAEncryption()
        {
        }

        /// <summary>
        /// Given a KeyProvider and a KeyName, uses that key to encrypt the given data
        /// </summary>
        /// <param name="providerName">Provider where the key is stored</param>
        /// <param name="keyName">Name of the key to use</param>
        /// <param name="toEncrypt">Data to encrypt</param>
        /// <param name="hashAlgorithm">OAEP hash algorithm, Look in PaddingHashAlgorithmNames.cs for values, but supports only SHA1, SHA256, SHA384, SHA512</param>
        /// <param name="paddingFlags">Padding Type, Look in PaddingFlags.cs for values, but supports only PKCS1 amd OAEP</param>
        /// <exception cref="CryptographicException">Cryptographic Exception</exception>
        /// <returns>Encrypted data</returns>
        public byte[] EncryptWithLocalKey(string providerName, string keyName, byte[] toEncrypt, string hashAlgorithm = PaddingHashAlgorithmNames.SHA512, int paddingFlags = PaddingFlags.OAEPPadding)
        {
            CngProvider provider = new CngProvider(providerName);
            byte[] encryptedData = null;
            bool keyExists = doesKeyExists(provider, keyName);

            if (!keyExists)
            {
                throw new CryptographicException(string.Format("They key {0} does not exist and cannot be used for encryption", keyName));
            }
            using (CngKey key = CngKey.Open(keyName, provider, CngKeyOpenOptions.MachineKey))
            {
                using (RSACng rsa = new RSACng(key))
                {
                    RSAEncryptionPadding padding = this.GetRSAPadding(hashAlgorithm, paddingFlags);
                    encryptedData = rsa.Encrypt(toEncrypt, padding);
                }
            }
            return encryptedData;
        }

        /// <summary>
        /// Given a KeyProvider and a KeyName, decrypts the data with the given key
        /// </summary>
        /// <param name="providerName">Provider where the key is stored</param>
        /// <param name="keyName">Name of the key to use</param>
        /// <param name="toDecrypt">Data to decrypt</param>
        /// <param name="hashAlgorithm">OAEP hash algorithm, Look in PaddingHashAlgorithmNames.cs for values, but supports only SHA1, SHA256, SHA384, SHA512</param>
        /// <param name="paddingFlags">Padding Type, Look in PaddingFlags.cs for values, but supports only PKCS1 amd OAEP</param>
        /// <exception cref="CryptographicException">Cryptographic Exception</exception>
        /// <returns>The decrypted data</returns>
        public byte[] DecryptWithLocalKey(string providerName, string keyName, byte[] toDecrypt, string hashAlgorithm = PaddingHashAlgorithmNames.SHA512, int paddingFlags = PaddingFlags.OAEPPadding)
        {
            CngProvider provider = new CngProvider(providerName);
            byte[] decrypted;
            CngKeyOpenOptions cngkeyOpenOpts = CngKeyOpenOptions.MachineKey;
            bool keyExists = doesKeyExists(provider, keyName, cngkeyOpenOpts);

            if (!keyExists)
            {
                if (doesKeyExists(provider, keyName, CngKeyOpenOptions.None))
                {
                    cngkeyOpenOpts = CngKeyOpenOptions.None;
                }
                else
                {
                    throw new CryptographicException(string.Format("They key {0} does not exist and cannot be used for decryption", keyName));
                }
            }

            using (CngKey key = CngKey.Open(keyName, provider, cngkeyOpenOpts))
            {
                using (RSACng rsa = new RSACng(key))
                {
                    RSAEncryptionPadding padding = this.GetRSAPadding(hashAlgorithm, paddingFlags);
                    decrypted = rsa.Decrypt(toDecrypt, padding);
                }
            }
            return decrypted;
        }

        /// <summary>
        /// Tries to generate and RSA Key in the given provider with this keyName.  
        /// </summary>
        /// <param name="providerName">Name of the provider</param>
        /// <param name="keyName">Name of the key</param>
        /// <param name="keyLength">Length of the key to generate</param>
        /// <returns>true if successful, false if that key already exists.</returns>
        public bool TryGenerateLocalRSAKey(string providerName, string keyName, int keyLength = 2048, bool makeExportable = false)
        {
            CngProvider provider = new CngProvider(providerName);

            bool keyExists = doesKeyExists(provider, keyName);

            if (keyExists)
            {
                //Key already exists. Can't create it.
                return false;
            }

            CryptoKeySecurity sec = new CryptoKeySecurity();
            CngKeyCreationParameters keyParams = null;

            if (IsMicrosoftSoftwareKSP(provider))
            {
                sec.AddAccessRule(
                    new CryptoKeyAccessRule(
                        new SecurityIdentifier(sidType: WellKnownSidType.BuiltinAdministratorsSid, domainSid: null),
                        cryptoKeyRights: CryptoKeyRights.FullControl,
                        type: AccessControlType.Allow));

                sec.AddAccessRule(
                    new CryptoKeyAccessRule(
                        new SecurityIdentifier(sidType: WellKnownSidType.BuiltinSystemOperatorsSid, domainSid: null),
                        cryptoKeyRights: CryptoKeyRights.GenericRead,
                        type: AccessControlType.Allow));

                const string NCRYPT_SECURITY_DESCR_PROPERTY = "Security Descr";
                const CngPropertyOptions DACL_SECURITY_INFORMATION = (CngPropertyOptions)4;

                CngProperty permissions = new CngProperty(
                    NCRYPT_SECURITY_DESCR_PROPERTY,
                    sec.GetSecurityDescriptorBinaryForm(),
                    CngPropertyOptions.Persist | DACL_SECURITY_INFORMATION);
                keyParams = new CngKeyCreationParameters()
                {
                    ExportPolicy = makeExportable ? CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport : CngExportPolicies.None,
                    Provider = provider,
                    Parameters = { new CngProperty("Length", BitConverter.GetBytes(keyLength), CngPropertyOptions.None),
                                permissions},
                    KeyCreationOptions = CngKeyCreationOptions.MachineKey
                };
                using (CngKey key = CngKey.Create(CngAlgorithm.Rsa, keyName, keyParams))
                {
                    if (key == null)
                    {
                        return false;
                    }
                    return true;
                }
            }
            else
            {
                keyParams = new CngKeyCreationParameters()
                {
                    ExportPolicy = makeExportable ? CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport : CngExportPolicies.None,
                    Provider = provider,
                    Parameters = { new CngProperty("Length", BitConverter.GetBytes(keyLength), CngPropertyOptions.None) },
                    KeyCreationOptions = CngKeyCreationOptions.MachineKey
                };
                using (CngKey key = CngKey.Create(CngAlgorithm.Rsa, keyName, keyParams))
                {
                    if (key == null)
                    {
                        return false;
                    }
                    // nothing to do inside here, except to return without throwing an exception
                    return true;
                }
            }
        }

        /// <summary>
        /// Will destroy the key with keyname within the given provider with providerName.  Will throw CryptographicException if either Provider or Key don't exist
        /// </summary>
        /// <param name="providerName">Name of the provider</param>
        /// <param name="keyName">Name of the key to destroy</param>
        public void DestroyLocalRSAKey(string providerName, string keyName)
        {
            CngProvider provider = new CngProvider(providerName);
            bool keyExists = doesKeyExists(provider, keyName);

            if (!keyExists)
            {
                //Nothing to destroy
                return;
            }

            using (CngKey key = CngKey.Open(keyName, provider, CngKeyOpenOptions.MachineKey))
            {
                key.Delete();
            }
        }

        /// <summary>
        /// Export the public key so that encryption can happen off of the machine.
        /// </summary>
        /// <param name="providerName">Name of the provider</param>
        /// <param name="keyName">Name of the key to destroy</param>
        /// <param name="filePath">Output Path for where to write the key</param>
        public void ExportPublicKeytoFile(string providerName, string keyName, string filePath, FileFormat fileFormat = FileFormat.CngBlob)
        {
            CngProvider provider = new CngProvider(providerName);

            bool keyExists = doesKeyExists(provider, keyName);

            if (!keyExists)
            {
                throw new CryptographicException(string.Format("They key {0} does not exist so there is no public key to export", keyName));
            }
            if (File.Exists(filePath))
            {
                throw new IOException(string.Format("File {0} already exists.", filePath));
            }
            using (CngKey key = CngKey.Open(keyName, provider, CngKeyOpenOptions.MachineKey))
            {
                if (fileFormat == FileFormat.CngBlob)
                {
                    File.WriteAllBytes(filePath, key.Export(new CngKeyBlobFormat("RSAPUBLICBLOB")));
                }
                else
                {
                    // FileFormat.PEM
                    File.WriteAllText(filePath, PemHelper.ExportToPem(key));
                }
            }
        }

        /// <summary>
        /// Import a key from a file for use on the machine.
        /// </summary>
        /// <param name="providerName"></param>
        /// <param name="filePath"></param>
        public bool ImportKeyToKSP(string providerName, string keyName, string filePath, bool makeExportable = false)
        {
            CngProvider provider = new CngProvider(providerName);

            bool keyExists = doesKeyExists(provider, keyName);

            if (keyExists)
            {
                //Key already exists. Can't create it.
                return false;
            }

            CryptoKeySecurity sec = new CryptoKeySecurity();
            CngKeyCreationParameters keyParams = null;

            byte[] keyBlob = File.ReadAllBytes(filePath);

            CngProperty keyBlobProp = new CngProperty(new CngKeyBlobFormat("RSAFULLPRIVATEBLOB").Format, keyBlob, CngPropertyOptions.None);

            if (IsMicrosoftSoftwareKSP(provider))
            {
                sec.AddAccessRule(
                    new CryptoKeyAccessRule(
                        new SecurityIdentifier(sidType: WellKnownSidType.BuiltinAdministratorsSid, domainSid: null),
                        cryptoKeyRights: CryptoKeyRights.FullControl,
                        type: AccessControlType.Allow));

                sec.AddAccessRule(
                    new CryptoKeyAccessRule(
                        new SecurityIdentifier(sidType: WellKnownSidType.BuiltinSystemOperatorsSid, domainSid: null),
                        cryptoKeyRights: CryptoKeyRights.GenericRead,
                        type: AccessControlType.Allow));

                const string NCRYPT_SECURITY_DESCR_PROPERTY = "Security Descr";
                const CngPropertyOptions DACL_SECURITY_INFORMATION = (CngPropertyOptions)4;

                CngProperty permissions = new CngProperty(
                    NCRYPT_SECURITY_DESCR_PROPERTY,
                    sec.GetSecurityDescriptorBinaryForm(),
                    CngPropertyOptions.Persist | DACL_SECURITY_INFORMATION);
                keyParams = new CngKeyCreationParameters()
                {
                    ExportPolicy = makeExportable ? CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport : CngExportPolicies.None,
                    Provider = provider,
                    Parameters = { permissions, keyBlobProp },
                    KeyCreationOptions = CngKeyCreationOptions.MachineKey
                };
                using (CngKey key = CngKey.Create(CngAlgorithm.Rsa, keyName, keyParams))
                {
                    if (key == null)
                    {
                        return false;
                    }
                    return true;
                }
            }
            else
            {
                keyParams = new CngKeyCreationParameters()
                {
                    ExportPolicy = makeExportable ? CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport : CngExportPolicies.None,
                    Provider = provider,
                    Parameters = { keyBlobProp },
                    KeyCreationOptions = CngKeyCreationOptions.MachineKey
                };
                using (CngKey key = CngKey.Create(CngAlgorithm.Rsa, keyName, keyParams))
                {
                    if (key == null)
                    {
                        return false;
                    }
                    // nothing to do inside here, except to return without throwing an exception
                    return true;
                }
            }
        }

        /// <summary>
        /// Export the private key so that decryption can happen on other machines.
        /// </summary>
        /// <param name="providerName">Name of the provider</param>
        /// <param name="keyName">Name of the key to destroy</param>
        /// <param name="filePath">Output Path for where to write the key</param>
        public void ExportPrivateKeytoFile(string providerName, string keyName, string filePath)
        {
            CngProvider provider = new CngProvider(providerName);

            bool keyExists = doesKeyExists(provider, keyName);

            if (!keyExists)
            {
                throw new CryptographicException(string.Format("They key {0} does not exist so there is no key to export", keyName));
            }
            if (File.Exists(filePath))
            {
                throw new IOException(string.Format("File {0} already exists.", filePath));
            }
            using (CngKey key = CngKey.Open(keyName, provider, CngKeyOpenOptions.MachineKey))
            {
                File.WriteAllBytes(filePath, key.Export(new CngKeyBlobFormat("RSAFULLPRIVATEBLOB")));
            }
        }

        public byte[] EncryptWithFileKey(string filePath, byte[] toEncrypt, string hashAlgorithm = PaddingHashAlgorithmNames.SHA512, int paddingFlags = PaddingFlags.OAEPPadding)
        {
            byte[] encryptedData = null;

            if (!File.Exists(filePath))
            {
                throw new IOException(string.Format("They file {0} does not exist and cannot be used for encryption", filePath));
            }

            byte[] keyBlob = null;

            CngKey key;

            try
            {
                key = PemHelper.ImportFromPem(filePath);
            }
            catch //Not a PEM, just import the RSA blob
            {
                keyBlob = File.ReadAllBytes(filePath);
                key = CngKey.Import(keyBlob, new CngKeyBlobFormat("RSAPUBLICBLOB"));
            }
            using(key)
            {
                using (RSACng rsa = new RSACng(key))
                {
                    RSAEncryptionPadding padding = this.GetRSAPadding(hashAlgorithm, paddingFlags);
                    encryptedData = rsa.Encrypt(toEncrypt, padding);
                }
            }
            return encryptedData;
        }

        /// <summary>
        /// Check for the existence of a key and set the Options accordingly.
        /// </summary>
        /// <param name="provider">Provider Object</param>
        /// <param name="keyName">Name of the key to destroy</param>
        /// <param name="cngKeyOpts">MachineKey or None depending on where it found the key.</param>
        /// <returns></returns>
        private bool doesKeyExists(CngProvider provider, string keyName, CngKeyOpenOptions openOpts = CngKeyOpenOptions.MachineKey)
        {
            bool keyExists = false;
            try
            {
                keyExists = CngKey.Exists(keyName, provider, openOpts);
            }
            catch (CryptographicException e)
            {
                throw new CryptographicException(string.Format("There was an error contacting provider {0}. It may not exist or may be configured incorrectly. Error Code:0x{1:X8}  Exception thrown:{2}\nStack Trace:{3}\n",
                    provider.ToString(), e.HResult, e.Message, e.StackTrace), e);
            }
            return keyExists;
        }

        private bool IsMicrosoftSoftwareKSP(CngProvider provider)
        {
            if (provider == CngProvider.MicrosoftSoftwareKeyStorageProvider)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Encrypts the given data with the given certificate
        /// </summary>
        /// <param name="toEncrypt">Data to encrypt</param>
        /// <param name="encryptionCert">Certificate to encrypt with</param>
        /// <returns>The encrypted data</returns>
        public byte[] EncryptWithCertificate(byte[] toEncrypt, X509Certificate2 encryptionCert)
        {
            if (toEncrypt == null)
            {
                throw new ArgumentNullException(nameof(toEncrypt));
            }

            if (encryptionCert == null)
            {
                throw new ArgumentNullException(nameof(encryptionCert));
            }

            X509Certificate2Collection encryptedCerts = new X509Certificate2Collection(encryptionCert);

            ContentInfo contentInfo = new ContentInfo(toEncrypt);
            EnvelopedCms cms = new EnvelopedCms(contentInfo);
            CmsRecipientCollection recipCollection = new CmsRecipientCollection(SubjectIdentifierType.IssuerAndSerialNumber, encryptedCerts);
            cms.Encrypt(recipCollection);
            byte[] bytes = cms.Encode();

            return bytes;
        }

        /// <summary>
        /// Decrypts the given data with the given certificate
        /// </summary>
        /// <param name="toDecrypt">Data to encrypt</param>
        /// <param name="decryptionCert">Certificate to encrypt with</param>
        /// <returns>The decrypted data</returns>
        public byte[] DecryptWithCertificate(byte[] toDecrypt, X509Certificate2 decryptionCert)
        {
            if (toDecrypt == null)
            {
                throw new ArgumentNullException(nameof(toDecrypt));
            }

            if (decryptionCert == null)
            {
                throw new ArgumentNullException(nameof(decryptionCert));
            }

            X509Certificate2Collection decryptCerts = new X509Certificate2Collection(decryptionCert);

            ContentInfo contentInfo = new ContentInfo(toDecrypt);
            EnvelopedCms cms = new EnvelopedCms(contentInfo);
            cms.Decode(contentInfo.Content);
            cms.Decrypt(decryptCerts);
            return cms.ContentInfo.Content;
        }

        /// <summary>
        /// Takes the encrypted data, decrypts it with the key <paramref name="keyName"/>  found in the provider <paramref name="providerName"/>, and then recrypts it with <paramref name="recryptionCertificate"/>
        /// </summary>
        /// <param name="encryptedPassword">Data encrypted with the given key</param>
        /// <param name="recryptionCertificate">Certificate to recrypt with</param>
        /// <param name="providerName">Provider that the key is stored in</param>
        /// <param name="keyName">Key used to originally encrypt the data</param>
        /// <param name="hashAlgorithm">OAEP hash algorithm</param>
        /// <param name="paddingFlags">Padding Type</param>
        /// <returns>Data recrypted by the given certificate</returns>
        public byte[] RecryptPfxImportMessage(byte[] encryptedPassword, X509Certificate2 recryptionCertificate, string providerName, string keyName, string hashAlgorithm = PaddingHashAlgorithmNames.SHA512, int paddingFlags = PaddingFlags.OAEPPadding)
        {
            byte[] decryptedPassword = null;
            byte[] recryptedPassword;

            decryptedPassword = this.DecryptWithLocalKey(providerName, keyName, encryptedPassword, hashAlgorithm, paddingFlags);
            GCHandle pinnedPasswordHandle = GCHandle.Alloc(decryptedPassword, GCHandleType.Pinned);

            try
            {
                recryptedPassword = this.EncryptWithCertificate(decryptedPassword, recryptionCertificate);
                return recryptedPassword;
            }
            finally
            {
                // Need to clean up decrypted password, make sure it's not staying in memory any longer than necessary.
                // The decryptedPassword was pinned after the decryption was done so it is possible that the
                // GC ran and copied the memory around before it could be pinned.  With the current design of the
                // .NET RSA API, pinning right after doing the decryption is the best we can do to minimize proliferation
                // of secrets throughout memory.

                if (decryptedPassword != null)
                {
                    decryptedPassword.ZeroFill();
                }

                if (pinnedPasswordHandle.IsAllocated)
                {
                    pinnedPasswordHandle.Free();
                }
            }
        }

        /// <summary>
        /// Parses the padding flags into an RSAEncryptionPadding object we can use with the API
        /// Look in PaddingFlags.cs for values, but supports only PKCS1 amd OAEP
        /// </summary>
        /// <param name="hashAlgorithm">Name of the hash algorithm to use, look in the PaddingHashAlgorithmNames enum for values</param>
        /// <param name="paddingFlags">Padding Type, Look in PaddingFlags.cs for values, but supports only PKCS1 amd OAEP</param>
        /// <returns>padding</returns>
        private RSAEncryptionPadding GetRSAPadding(string hashAlgorithm, int paddingFlags)
        {
            RSAEncryptionPadding padding = null;

            switch (paddingFlags)
            {
                case PaddingFlags.PKCS1Padding:
                    padding = RSAEncryptionPadding.Pkcs1;
                    break;
                case PaddingFlags.OAEPPadding:
                    // Need to parse the hash algorithm out of the string
                    HashAlgorithmName hashAlgorithmName = this.GetHashAlgorithmNameFromPlaintext(hashAlgorithm);
                    padding = RSAEncryptionPadding.CreateOaep(hashAlgorithmName);
                    break;
                default:
                    throw new CryptographicException(
                        string.Format(
                            "Attempting to get the RSA padding of type {0} is not supported, only supported type is OAEP ({1})",
                            paddingFlags,
                            (int)PaddingFlags.OAEPPadding));
            }

            return padding;
        }

        /// <summary>
        /// Returns a CNG HashAlgorithmName object matching the plaintext string passed in
        /// </summary>
        /// <param name="hashAlgorithm">Name of the hash algorithm to use, look in the PaddingHashAlgorithmNames enum for values</param>
        /// <returns>CNG HashAlgorithmName</returns>
        private HashAlgorithmName GetHashAlgorithmNameFromPlaintext(string hashAlgorithm)
        {
            if (hashAlgorithm == null)
            {
                throw new ArgumentNullException(nameof(hashAlgorithm));
            }
            HashAlgorithmName hashAlgorithmName;
            if (hashAlgorithm.Equals(PaddingHashAlgorithmNames.SHA256, StringComparison.Ordinal))
            {
                hashAlgorithmName = HashAlgorithmName.SHA256;
            }
            else if (hashAlgorithm.Equals(PaddingHashAlgorithmNames.SHA384, StringComparison.Ordinal))
            {
                hashAlgorithmName = HashAlgorithmName.SHA384;
            }
            else if (hashAlgorithm.Equals(PaddingHashAlgorithmNames.SHA512, StringComparison.Ordinal))
            {
                hashAlgorithmName = HashAlgorithmName.SHA512;
            }
            else
            {
                throw new CryptographicException(
                    string.Format(
                        "Attempting to find HashAlgorithm for {0} failed, only supported algorithms are {1}, {2}, {3}",
                        hashAlgorithm,
                        PaddingHashAlgorithmNames.SHA256,
                        PaddingHashAlgorithmNames.SHA384,
                        PaddingHashAlgorithmNames.SHA512));
            }

            return hashAlgorithmName;
        }
    }
}
