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

namespace Microsoft.Intune.Test.EncryptionUtilitiesUnitTests
{
    using Microsoft.Intune.EncryptionUtilities;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Text;

    [TestClass]
    public class SecureStringUtilTests
    {
        [TestMethod]
        public void UTF8DecodeTest()
        {
            for (uint c = char.MinValue; c <= char.MaxValue; c++)
            {
                SecureString secureString = new SecureString();
                byte[] utf8Encoding = Encoding.UTF8.GetBytes(new char[] { (char)c });
                string dotNetDecodedString = Encoding.UTF8.GetString(utf8Encoding);
                SecureStringUtil.CopyUTF8ToSecureString(utf8Encoding, secureString);
                Assert.AreEqual(SecureStringToString(secureString), dotNetDecodedString);
            }
        }

        /// <summary>
        /// Converts a secure string to a standard string (used only for test verification purposes)
        /// </summary>
        /// <param name="srcString">SecureString to convert</param>
        /// <returns>Non-secure string</returns>
        public static string SecureStringToString(SecureString srcString)
        {
            if (srcString == null)
            {
                throw new ArgumentNullException(nameof(srcString));
            }

            IntPtr srcPtr = IntPtr.Zero;
            StringBuilder retString = new StringBuilder();

            try
            {
                srcPtr = Marshal.SecureStringToGlobalAllocUnicode(srcString);

                for (int srcOffset = 0; srcOffset < srcString.Length * 2; srcOffset += 2)
                {
                    char curChar = (char)Marshal.ReadInt16(srcPtr, srcOffset);
                    retString.Append(curChar);
                }
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(srcPtr);
            }

            return retString.ToString();
        }
    }
}
