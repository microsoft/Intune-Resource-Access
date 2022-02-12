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

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Microsoft.Intune.EncryptionUtilities
{
    public static class PemHelper
    {
        private const string PEM_PUBLIC_KEY_HEADER = "-----BEGIN PUBLIC KEY-----";
        private const string PEM_PUBLIC_KEY_FOOTER = "-----END PUBLIC KEY-----";
        private const string RSA_ENCRYPTION_OID = "1.2.840.113549.1.1.1";

        /// <summary>
        /// Returns PEM encoded RSA PublicKey
        /// </summary>
        /// <param name="key">CngKey containing RSA PublicKey</param>
        /// <returns>PEM encoded PublicKey</returns>
        public static string ExportToPem(CngKey key)
        {
            RSACng rsaCng = new RSACng(key);

            RSAParameters parameters = rsaCng.ExportParameters(false);

            byte[] modulusBytes = DerUtils.EncodeUnsignedInteger(parameters.Modulus);
            byte[] exponentBytes = DerUtils.EncodeUnsignedInteger(parameters.Exponent);
            byte[] dataSeqBytes = DerUtils.EncodeSequenceOf(new List<byte[]> { modulusBytes, exponentBytes });
            byte[] bitStringBytes = DerUtils.EncodeBitString(dataSeqBytes, 0);

            byte[] oidBytes = DerUtils.EncodeOid(RSA_ENCRYPTION_OID);
            byte[] oidSeqBytes = DerUtils.EncodeSequenceOf(new List<byte[]> { oidBytes, DerUtils.EncodeNull() });
            byte[] derBytes = DerUtils.EncodeSequenceOf(new List<byte[]> { oidSeqBytes, bitStringBytes });

            char[] derB64 = Convert.ToBase64String(derBytes, 0, (int)derBytes.Length).ToCharArray();

            TextWriter outputWriter = new StringWriter();

            outputWriter.WriteLine(PEM_PUBLIC_KEY_HEADER);
            for (var i = 0; i < derB64.Length; i += 64)
            {
                outputWriter.WriteLine(derB64, i, Math.Min(64, derB64.Length - i));
            }
            outputWriter.WriteLine(PEM_PUBLIC_KEY_FOOTER);

            return outputWriter.ToString();
        }

        /// <summary>
        /// Decodes a PEM RSA public key file
        /// </summary>
        /// <param name="filePath">The path to the PEM file</param>
        /// <returns>A CngKey object that constains the public key from the PEM file</returns>
        public static CngKey ImportFromPem(string filePath)
        {
            string pemStr;
            using (StreamReader fileStream = File.OpenText(filePath))
            {
                pemStr = fileStream.ReadToEnd();
            }
            if (!pemStr.StartsWith(PEM_PUBLIC_KEY_HEADER))
            {
                throw new FileFormatException("Not a public key PEM file. PEM Header formatted incorrectly.");
            }
            StringBuilder pemB64 = new StringBuilder(pemStr);
            pemB64.Replace(PEM_PUBLIC_KEY_HEADER, "").Replace(PEM_PUBLIC_KEY_FOOTER, "");

            byte[] derBytes = Convert.FromBase64String(pemB64.ToString());

            List<byte[]> mainBodySeqBytes = DerUtils.DecodeSequenceOf(derBytes);

            List<byte[]> oidSeqBytes = DerUtils.DecodeSequenceOf(mainBodySeqBytes[0]);

            if (DerUtils.DecodeOid(oidSeqBytes[0]) != RSA_ENCRYPTION_OID)
            {
                throw new FileFormatException("Not labelled as a public key OID.");
            }

            DerUtils.DecodeNull(oidSeqBytes[1]);

            byte[] bitStringBytes = DerUtils.DecodeBitstring(mainBodySeqBytes[1], out _);

            List<byte[]> dataSeqBytes = DerUtils.DecodeSequenceOf(bitStringBytes);

            byte[] modulusBytes = DerUtils.DecodeUnsignedInteger(dataSeqBytes[0]);
            byte[] exponenBytes = DerUtils.DecodeUnsignedInteger(dataSeqBytes[1]);

            RSACng rsaCng = new RSACng();
            RSAParameters rsaParams = new RSAParameters();
            rsaParams.Modulus = modulusBytes;
            rsaParams.Exponent = exponenBytes;
            rsaCng.ImportParameters(rsaParams);
            return rsaCng.Key;

        }

    }
}
