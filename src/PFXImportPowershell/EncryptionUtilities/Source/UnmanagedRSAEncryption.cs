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
    /// <summary>
    /// Helper class to provide easy calls for common encryption operations
    /// Abstracts out the handling of native handles
    /// </summary>
    public class UnmanagedRSAEncryption
    {
        private ICNGNCryptInteropFactory ncryptInteropFactory;

        /// <summary>
        /// Default constructor
        /// </summary>
        public UnmanagedRSAEncryption()
        {
            this.ncryptInteropFactory = new CNGNCryptInteropFactory();
        }

        /// <summary>
        /// For test hook
        /// </summary>
        /// <param name="ncryptInteropFactory">Factory to use for test hook</param>
        public UnmanagedRSAEncryption(ICNGNCryptInteropFactory ncryptInteropFactory)
        {
            this.ncryptInteropFactory = ncryptInteropFactory;
        }

        /// <summary>
        /// Given a KeyProvider and a KeyName, uses that key to encrypt the given data
        /// </summary>
        /// <param name="providerName">Provider where the key is stored</param>
        /// <param name="keyName">Name of the key to use</param>
        /// <param name="toEncrypt">Data to encrypt</param>
        /// <param name="hashAlgorithm">OAEP hash algorithm</param>
        /// <param name="paddingFlags">Padding type</param>
        /// <exception cref="CryptographicException">If anything fails with the encryption</exception>
        /// <returns>Encrypted data</returns>
        public byte[] EncryptWithLocalKey(string providerName, string keyName, byte[] toEncrypt, string hashAlgorithm = PaddingHashAlgorithmNames.SHA512, int paddingFlags = PaddingFlags.OAEPPadding)
        {
            ICNGNCryptInterop ncrypt = this.ncryptInteropFactory.ConstructInterop();
            return ncrypt.EncryptWithLocalKey(providerName, keyName, toEncrypt, hashAlgorithm, paddingFlags);
        }

        /// <summary>
        /// Given a KeyProvider and a KeyName, decrpyts the data with the given key
        /// </summary>
        /// <param name="providerName">Provider where the key is stored</param>
        /// <param name="keyName">Name of the key to use</param>
        /// <param name="toDecrypt">Data to decrypt</param>
        /// <param name="hashAlgorithm">OAEP hash algorithm</param>
        /// <param name="paddingFlags">Padding type</param>
        /// <exception cref="CryptographicException">If anything fails with the decryption</exception>
        /// <returns>Decrypted data</returns>
        public byte[] DecryptWithLocalKey(string providerName, string keyName, byte[] toDecrypt, string hashAlgorithm = PaddingHashAlgorithmNames.SHA512, int paddingFlags = PaddingFlags.OAEPPadding)
        {
            ICNGNCryptInterop ncrypt = this.ncryptInteropFactory.ConstructInterop();
            return ncrypt.DecryptWithLocalKey(providerName, keyName, toDecrypt, hashAlgorithm, paddingFlags);
        }
    }
}
