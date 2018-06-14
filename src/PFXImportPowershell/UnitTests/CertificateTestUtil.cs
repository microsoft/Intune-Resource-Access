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
    using CERTENROLLLib;
    using System;
    using System.Security.Cryptography.X509Certificates;

    public class CertificateTestUtil
    {
        /// <summary>
        /// Generates a self-signed test certificate
        /// </summary>
        /// <param name="subjectName">Subject name value</param>
        /// <param name="password">Password for encrypting the certificate</param>
        /// <param name="hashAlgorithm">The hash algorithm used for generating padding bytes when encrypting the certificate</param>
        /// <returns>Certificate object with exportable private key</returns>
        public static X509Certificate2 CreateSelfSignedCertificate(string subjectName, string password, string hashAlgorithm)
        {
            // Create a DN for subject and issuer
            var dn = new CX500DistinguishedName();
            dn.Encode("CN=" + subjectName, X500NameFlags.XCN_CERT_NAME_STR_NONE);

            // Create a private key for the certificate
            CX509PrivateKey privateKey = new CX509PrivateKey();
            privateKey.ProviderName = "Microsoft RSA SChannel Cryptographic Provider";
            privateKey.MachineContext = true;
            privateKey.Length = 2048;
            privateKey.KeySpec = X509KeySpec.XCN_AT_KEYEXCHANGE;
            privateKey.KeyUsage = X509PrivateKeyUsageFlags.XCN_NCRYPT_ALLOW_ALL_USAGES;
            privateKey.ExportPolicy = X509PrivateKeyExportFlags.XCN_NCRYPT_ALLOW_PLAINTEXT_EXPORT_FLAG;
            privateKey.Create();

            // Hashing algorithm
            var hashobj = new CObjectId();
            hashobj.InitializeFromAlgorithmName(ObjectIdGroupId.XCN_CRYPT_HASH_ALG_OID_GROUP_ID,
                ObjectIdPublicKeyFlags.XCN_CRYPT_OID_INFO_PUBKEY_ANY,
                AlgorithmFlags.AlgorithmFlagsNone, hashAlgorithm);

            // Add extended key usage to support client and server authentication
            var oidlist = new CObjectIds();

            // Server authentiation
            var oid1 = new CObjectId();
            oid1.InitializeFromValue("1.3.6.1.5.5.7.3.1");
            oidlist.Add(oid1);

            // Client authentication
            var oid2 = new CObjectId();
            oid2.InitializeFromValue("1.3.6.1.5.5.7.3.2");
            oidlist.Add(oid2);

            var eku = new CX509ExtensionEnhancedKeyUsage();
            eku.InitializeEncode(oidlist);

            // Create the self signing request.  This will be returned as the self-signed
            // certificate so we don't have to install anything in the system certificate
            // store (which is what happens if the enrollment object is used)
            var cert = new CX509CertificateRequestCertificate();
            cert.InitializeFromPrivateKey(X509CertificateEnrollmentContext.ContextMachine, privateKey, "");
            cert.Subject = dn;
            cert.Issuer = dn;
            cert.NotBefore = DateTime.Now;
            cert.NotAfter = DateTime.Now.AddHours(1);
            cert.X509Extensions.Add((CX509Extension)eku);
            cert.HashAlgorithm = hashobj;
            cert.Encode();

            // Return the certificate object
            X509Certificate2 newCert = new X509Certificate2(
                Convert.FromBase64String(cert.RawData), password,
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet);

            return newCert;
        }
    }
}
