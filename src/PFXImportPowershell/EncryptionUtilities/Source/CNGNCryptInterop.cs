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
    using System.Diagnostics.CodeAnalysis;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;

    /// <summary>
    /// Provides a wrapper around native NCrypt dll calls.  This handles checking NTSTATUS return values and throwing exceptions accordingly
    /// </summary>
    public class CNGNCryptInterop : ICNGNCryptInterop
    {
        /// <summary>
        /// Opens the storage provider with the given name
        /// </summary>
        /// <param name="providerName">Name of the storage provider</param>
        /// <param name="dwFlags">Any flags to use</param>
        /// <exception cref="CryptographicException">Cryptographic Exception</exception>
        /// <returns>Handle to the Provider</returns>
        public IntPtr OpenStorageProvider(string providerName, int dwFlags = 0)
        {
            IntPtr nCryptProviderHandle = IntPtr.Zero;
            uint statusReturn;

            if (!this.CheckNTStatus(statusReturn = NativeMethods.NCryptOpenStorageProvider(ref nCryptProviderHandle, providerName, dwFlags)))
            {
                NTSTATUS failStatus = this.GetNTStatus(statusReturn);
                throw new CryptographicException(string.Format("NCryptOpenStorageProvider for providerName {0} failed with return status {1}", providerName, failStatus));
            }

            return nCryptProviderHandle;
        }

        /// <summary>
        /// Opens a key in the given storage provider
        /// </summary>
        /// <param name="nCryptProviderHandle">The opened provider</param>
        /// <param name="keyName">Name of the key</param>
        /// <param name="dwLegacyKeySpec">legacy key spec</param>
        /// <param name="dwFlags">flags</param>
        /// <exception cref="CryptographicException">Cryptographic Exception</exception>
        /// <returns>A handle to the key</returns>
        public IntPtr OpenKey(IntPtr nCryptProviderHandle, string keyName, int dwLegacyKeySpec = 0, int dwFlags = 0)
        {
            if(nCryptProviderHandle == null)
            {
                throw new ArgumentNullException(nameof(nCryptProviderHandle));
            }

            IntPtr nCryptKeyHandle = IntPtr.Zero;
            uint statusReturn;

            if (!this.CheckNTStatus(statusReturn = NativeMethods.NCryptOpenKey(nCryptProviderHandle, ref nCryptKeyHandle, keyName, dwLegacyKeySpec, dwFlags)))
            {
                NTSTATUS failStatus = this.GetNTStatus(statusReturn);
                throw new CryptographicException(string.Format("NCryptOpenKey for keyName {0} failed with return status {1}", keyName, failStatus));
            }

            return nCryptKeyHandle;
        }

        /// <summary>
        /// Generates a key in the given provider with the provided attributes
        /// </summary>
        /// <param name="nCryptProviderHandle">The opened provider</param>
        /// <param name="algorithm">Name of the algorithm the key should use</param>
        /// <param name="keyName">Name of the key</param>
        /// <param name="keyLength">Length of the key</param>
        /// <param name="dwLegacyKeySpec">legacy key</param>
        /// <param name="dwFlags">flags</param>
        /// <exception cref="CryptographicException">Cryptographic Exception</exception>
        /// <returns>Handle to the generated key</returns>
        public IntPtr GenerateKey(IntPtr nCryptProviderHandle, string algorithm, string keyName, int keyLength = 2048, int dwLegacyKeySpec = 0, int dwFlags = 0)
        {
            if (nCryptProviderHandle == null)
            {
                throw new ArgumentNullException(nameof(nCryptProviderHandle));
            }

            IntPtr nCryptKeyHandle = IntPtr.Zero;
            uint statusReturn;

            if (!this.CheckNTStatus(statusReturn = NativeMethods.NCryptCreatePersistedKey(nCryptProviderHandle, ref nCryptKeyHandle, algorithm, keyName, dwLegacyKeySpec, dwFlags)))
            {
                NTSTATUS failStatus = this.GetNTStatus(statusReturn);
                throw new CryptographicException(string.Format("NCryptCreatePersistedKey with keyName {0} and algorithm {1} failed with return status {2}", keyName, algorithm, failStatus));
            }

            if (!this.CheckNTStatus(statusReturn = NativeMethods.NCryptSetProperty(nCryptKeyHandle, NCryptPropertyNameFlags.NCRYPT_LENGTH_PROPERTY, ref keyLength, 4, dwFlags)))
            {
                NTSTATUS failStatus = this.GetNTStatus(statusReturn);
                throw new CryptographicException(string.Format("NCryptSetProperty failed for property name {0} and value {1} on key name {2}", NCryptPropertyNameFlags.NCRYPT_LENGTH_PROPERTY, keyLength, keyName));
            }

            if (!this.CheckNTStatus(statusReturn = NativeMethods.NCryptFinalizeKey(nCryptKeyHandle, 0)))
            {
                NTSTATUS failStatus = this.GetNTStatus(statusReturn);
                throw new CryptographicException(string.Format("NCryptFinalizeKey with keyName {0} and algorithm {1} failed with return status {2}", keyName, algorithm, failStatus));
            }

            return nCryptKeyHandle;
        }

        /// <summary>
        /// Destroys the given key from the provider it's contained in
        /// </summary>
        /// <param name="key">Key to destroy</param>
        /// <param name="dwFlags">flags</param>
        /// <exception cref="CryptographicException">Cryptographic Exception</exception>
        public void DestroyKey(IntPtr key, int dwFlags = 0)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            uint statusReturn;

            if (!this.CheckNTStatus(statusReturn = NativeMethods.NCryptDeleteKey(key, dwFlags)))
            {
                NTSTATUS failStatus = this.GetNTStatus(statusReturn);
                throw new CryptographicException(string.Format("NCryptDeleteKey failed with return status {0}", failStatus));
            }
        }

        /// <summary>
        /// Frees the object the handle is pointing to
        /// </summary>
        /// <param name="hObject">The handle to free</param>
        /// <exception cref="CryptographicException">Cryptographic Exception</exception>
        public void FreeObject(IntPtr hObject)
        {
            if (hObject == null)
            {
                throw new ArgumentNullException(nameof(hObject));
            }

            uint statusReturn;

            if (!this.CheckNTStatus(statusReturn = NativeMethods.NCryptFreeObject(hObject)))
            {
                NTSTATUS failStatus = this.GetNTStatus(statusReturn);
                throw new CryptographicException(string.Format("NCryptFreeObject failed with return status {0}", failStatus));
            }
        }

        /// <summary>
        /// Encrypts the given data using the given provider and key
        /// </summary>
        /// <param name="providerName">Name of the provider</param>
        /// <param name="keyName">Key to use</param>
        /// <param name="toEncrypt">Data to encrypt</param>
        /// <param name="hashAlgorithm">Hash algorithm used to create padding bytes</param>
        /// <param name="paddingFlags">Padding</param>
        /// <returns>Encrypted data</returns>
        public byte[] EncryptWithLocalKey(string providerName, string keyName, byte[] toEncrypt, string hashAlgorithm = PaddingHashAlgorithmNames.SHA512, int paddingFlags = PaddingFlags.OAEPPadding)
        {
            if (string.IsNullOrEmpty(providerName))
            {
                throw new ArgumentNullException(nameof(providerName));
            }

            if (string.IsNullOrEmpty(keyName))
            {
                throw new ArgumentNullException(nameof(keyName));
            }

            IntPtr provider = IntPtr.Zero;
            IntPtr key = IntPtr.Zero;
            byte[] encrypted = null;

            try
            {
                int encryptedLength = 0;
                uint statusReturn;

                provider = this.OpenStorageProvider(providerName);
                key = this.OpenKey(provider, keyName);

                // Note: paddingInfo is only used by NativeMethods.NCryptEncrypt if paddingFlags
                // is set to OAEPPadding

                BCRYPT_OAEP_PADDING_INFO paddingInfo = new BCRYPT_OAEP_PADDING_INFO();
                paddingInfo.pszAlgId = hashAlgorithm;

                if (toEncrypt == null)
                {
                    throw new ArgumentNullException(nameof(toEncrypt));
                }

                if (!this.CheckNTStatus(statusReturn = NativeMethods.NCryptEncrypt(key, toEncrypt, toEncrypt.Length, ref paddingInfo, null, 0, ref encryptedLength, paddingFlags)))
                {
                    NTSTATUS failStatus = this.GetNTStatus(statusReturn);
                    throw new CryptographicException(string.Format("NCryptEncrypt failed with return status {0}", failStatus));
                }

                if (encryptedLength == 0)
                {
                    throw new CryptographicException("NCryptEncrypt returned encryption length of 0 which is invalid");
                }

                encrypted = new byte[encryptedLength];

                if (!this.CheckNTStatus(statusReturn = NativeMethods.NCryptEncrypt(key, toEncrypt, toEncrypt.Length, ref paddingInfo, encrypted, encryptedLength, ref encryptedLength, paddingFlags)))
                {
                    NTSTATUS failStatus = this.GetNTStatus(statusReturn);
                    throw new CryptographicException(string.Format("NCryptEncrypt failed with return status {0}", failStatus));
                }
            }
            finally
            {
                if (key != null)
                {
                    this.FreeObject(key);
                    key = IntPtr.Zero;
                }

                if (provider != null)
                {
                    this.FreeObject(provider);
                    provider = IntPtr.Zero;
                }
            }

            return encrypted;
        }

        /// <summary>
        /// Decrypts the data using the given key
        /// </summary>
        /// <param name="providerName">Name of provider</param>
        /// <param name="keyName">Key to use</param>
        /// <param name="toDecrypt">Data to decrypt</param>
        /// <param name="hashAlgorithm">OAEP hash algorithm</param>
        /// <param name="paddingFlags">Padding Type</param>
        /// <returns>The decrypted data</returns>
        /// <exception cref="CryptographicException">Cryptographic Exception</exception>
        public byte[] DecryptWithLocalKey(string providerName, string keyName, byte[] toDecrypt, string hashAlgorithm = PaddingHashAlgorithmNames.SHA512, int paddingFlags = PaddingFlags.OAEPPadding)
        {
            if (string.IsNullOrEmpty(providerName))
            {
                throw new ArgumentNullException(nameof(providerName));
            }

            if (string.IsNullOrEmpty(keyName))
            {
                throw new ArgumentNullException(nameof(keyName));
            }

            IntPtr provider = IntPtr.Zero;
            IntPtr key = IntPtr.Zero;
            byte[] decryptedData = null;

            try
            {
                int decryptedLength = 0;
                uint statusReturn;

                provider = this.OpenStorageProvider(providerName);
                key = this.OpenKey(provider, keyName);

                // Note: paddingInfo is only used by NativeMethods.NCryptDecrypt if paddingFlags
                // is set to OAEPPadding

                BCRYPT_OAEP_PADDING_INFO paddingInfo = new BCRYPT_OAEP_PADDING_INFO();
                paddingInfo.pszAlgId = hashAlgorithm;

                if (toDecrypt == null)
                {
                    throw new ArgumentNullException(nameof(toDecrypt));
                }

                if (!this.CheckNTStatus(statusReturn = NativeMethods.NCryptDecrypt(key, toDecrypt, toDecrypt.Length, ref paddingInfo, null, 0, ref decryptedLength, paddingFlags)))
                {
                    NTSTATUS failStatus = this.GetNTStatus(statusReturn);
                    throw new CryptographicException(string.Format("NCryptDecrypt failed with return status {0}", failStatus));
                }

                if (decryptedLength == 0)
                {
                    throw new CryptographicException("NCryptDencrypt returned decryption length of 0 which is invalid");
                }

                decryptedData = new byte[decryptedLength];

                if (!this.CheckNTStatus(statusReturn = NativeMethods.NCryptDecrypt(key, toDecrypt, toDecrypt.Length, ref paddingInfo, decryptedData, decryptedLength, ref decryptedLength, paddingFlags)))
                {
                    NTSTATUS failStatus = this.GetNTStatus(statusReturn);
                    throw new CryptographicException(string.Format("NCryptDecrypt failed with return status {0}", failStatus));
                }
            }
            finally
            {
                if (key != null)
                {
                    this.FreeObject(key);
                    key = IntPtr.Zero;
                }

                if (provider != null)
                {
                    this.FreeObject(provider);
                    provider = IntPtr.Zero;
                }
            }

            return decryptedData;
        }

        /// <summary>
        /// Returns whether the ntstatus is success or not
        /// </summary>
        /// <param name="ntstatus">ntstatus value</param>
        /// <returns>true if ntstatus is success, otherwise false</returns>
        public bool CheckNTStatus(uint ntstatus)
        {
            return ntstatus == (uint)NTSTATUS.STATUS_SUCCESS;
        }

        /// <summary>
        /// Gets an NTSTATUS enum value based on the ntstatus id
        /// </summary>
        /// <param name="ntstatus">status</param>
        /// <returns>NTSTATUS object</returns>
        public NTSTATUS GetNTStatus(uint ntstatus)
        {
            return (NTSTATUS)ntstatus;
        }

        /// <summary>
        /// Padding information
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct BCRYPT_OAEP_PADDING_INFO
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:AccessibleFieldsMustBeginWithUpperCaseLetter", Justification = "Padding Info Constants.")]
            internal string pszAlgId;
            [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:AccessibleFieldsMustBeginWithUpperCaseLetter", Justification = "Padding Info Constants.")]
            internal IntPtr pbLabel;
            [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:AccessibleFieldsMustBeginWithUpperCaseLetter", Justification = "Padding Info Constants.")]
            internal int cbLabel;
        }

        /// <summary>
        /// Algorithm IDs to use for native NCrypt calls
        /// </summary>
        public static class AlgorithmIds
        {
            [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1310:FieldNamesMustNotContainUnderscore", Justification = "AlgorithmId Constants.")]
            public const string BCRYPT_AES_ALGORITHM = "AES";
            [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1310:FieldNamesMustNotContainUnderscore", Justification = "AlgorithmId Constants.")]
            public const string BCRYPT_RSA_ALGORITHM = "RSA";
            [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1310:FieldNamesMustNotContainUnderscore", Justification = "AlgorithmId Constants.")]
            public const string BCRYPT_RSA_SIGN_ALGORITHM = "RSA_SIGN";
        }

        /// <summary>
        /// Provider names to use for native NCrypt calls
        /// </summary>
        public static class ProviderNames
        {
            [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1310:FieldNamesMustNotContainUnderscore", Justification = "ProviderName Constants.")]
            public const string MS_PRIMITIVE_PROVIDER = "Microsoft Primitive Provider";
            [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1310:FieldNamesMustNotContainUnderscore", Justification = "ProviderName Constants.")]
            public const string MS_KEY_STORAGE_PROVIDER = "Microsoft Software Key Storage Provider";
            [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1310:FieldNamesMustNotContainUnderscore", Justification = "ProviderName Constants.")]
            public const string MS_SMART_CARD_KEY_STORAGE_PROVIDER = "Microsoft Smart Card Key Storage Provider";
        }

        /// <summary>
        /// Property names
        /// </summary>
        public static class NCryptPropertyNameFlags
        {
            [SuppressMessage("Microsoft.StyleCop.CSharp.NamingRules", "SA1310:FieldNamesMustNotContainUnderscore", Justification = "Ncrypt Constants.")]
            public const string NCRYPT_LENGTH_PROPERTY = "Length";
        }
    }
}
