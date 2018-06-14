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
    using System.Runtime.InteropServices;

    internal static class NativeMethods
    {
        [DllImport("Ncrypt.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint NCryptOpenStorageProvider([In] [Out] ref IntPtr phProvider, [In] string pszProviderName, [In] int dwFlags);

        [DllImport("Ncrypt.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint NCryptCreatePersistedKey([In] IntPtr hProvider, [In] [Out] ref IntPtr phKey, [In] string pszAlgId, [In] string pszKeyName, [In] int dwLegacyKeySpec, [In] int dwFlags);

        [DllImport("Ncrypt.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint NCryptFinalizeKey([In] IntPtr hKey, [In] int dwFlags);

        [DllImport("Ncrypt.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint NCryptOpenKey([In] IntPtr hProvider, [In] [Out] ref IntPtr phKey, [In] string pszKeyName, [In] int dwLegacyKeySpec, [In] int dwFlags);

        [DllImport("Ncrypt.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint NCryptEncrypt(
            [In] IntPtr hKey, 
            [In] byte[] pbInput,
            [In] int cbInput,
            [In] ref CNGNCryptInterop.BCRYPT_OAEP_PADDING_INFO pPaddingInfo,
            [In] byte[] pbOutput,
            [In] int cbOutput,
            [In][Out] ref int pcbResult,
            [In] int dwFlags);

        [DllImport("Ncrypt.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint NCryptDecrypt(
            [In] IntPtr hKey,
            [In] byte[] pbInput,
            [In] int cbInput,
            [In] ref CNGNCryptInterop.BCRYPT_OAEP_PADDING_INFO pPaddingInfo,
            [In] byte[] pbOutput,
            [In] int cbOutput,
            [In][Out] ref int pcbResult,
            [In] int dwFlags);

        [DllImport("Ncrypt.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint NCryptDeleteKey([In] IntPtr hKey, [In] int dwFlags);

        [DllImport("Ncrypt.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint NCryptFreeObject([In] IntPtr hObject);

        [DllImport("Ncrypt.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint NCryptSetProperty(
            IntPtr hObject,
            [MarshalAs(UnmanagedType.LPWStr)] string szProperty,
            ref int pbInput,
            int cbInput,
            int flags);
    }
}
