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

namespace Microsoft.Management.Powershell.PFXImport.Cmdlets
{
    using System;
    using System.Management.Automation;
    using System.Security.Cryptography;
    using Microsoft.Intune.EncryptionUtilities;

    [Cmdlet(VerbsCommon.Add, "IntuneKspKey")]
    public class AddKSPKey : PSCmdlet
    {
        private const int KeyExistsErrorCode = -2146233296;

        private int keyLength = 2048;

        [Parameter(Position = 1, Mandatory = true)]
        public string ProviderName { get; set; }

        [Parameter(Position = 2, Mandatory = true)]
        [ValidateSetAttribute(new string[] { "3DES", "3DES_112", "AES", "AES-CMAC", "AES-GMAC", "CAPI_KDF", "DES", "DESX", "DH", "DSA", "ECDH_P256", "ECDH_P384", "ECDH_P521", "ECDSA_P256", "ECDSA_P384", "ECDSA_P521", "MD2", "MD4", "MD5", "RC2", "RC4", "RNG", "DUALECRNG", "FIPS186DSARNG", "RSA", "RSA_SIGN", "SHA1", "SHA256", "SHA384", "SHA512", "SP800_108_CTR_HMAC", "SP800_56A_CONCAT", "PBKDF2", "ECDSA", "ECDH", "XTS-AES" })]
        public string AlgorithmName { get; set; }

        [Parameter(Position = 3, Mandatory = true)]
        public string KeyName { get; set; }

        [Parameter(Position = 4)]
        public int KeyLength
        {
            get
            {
                return keyLength;
            }

            set
            {
                keyLength = value;
            }
        }

        protected override void ProcessRecord()
        {
            ManagedRSAEncryption managedRSA = new ManagedRSAEncryption();
            if(managedRSA.TryGenerateLocalRSAKey(ProviderName, KeyName, KeyLength))
            {
                //Creation succeeded
            }
            else
            {
                //Creation failed, likely already exists
                this.WriteError(
                    new ErrorRecord(
                        new InvalidOperationException("Key Creation failed, it likely already exists"), 
                        "KeyAlreadyExists", 
                        ErrorCategory.InvalidOperation, 
                        null));

            }

        }
    }
}
