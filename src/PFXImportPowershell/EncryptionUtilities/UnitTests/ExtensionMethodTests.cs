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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Intune.Test.EncryptionUtilitiesUnitTests
{
    [TestClass]
    public class ExtensionMethodTests
    {
        /// <summary>
        /// Testing that we don't throw exception on null byte[]
        /// </summary>
        [TestMethod]
        public void ByteZeroFillNull()
        {
            byte[] zeroFill = null;
            zeroFill.ZeroFill();
            Assert.IsNull(zeroFill);
        }

        [TestMethod]
        public void ByteZeroFillWorks()
        {
            byte[] zeroFill = new byte[] { 1, 2, 3, 4 };
            zeroFill.ZeroFill();
            for(int i = 0; i < zeroFill.Length; i++)
            {
                Assert.AreEqual(0, zeroFill[i]);
            }
        }
    }
}
