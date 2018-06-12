﻿// Copyright (c) Microsoft Corporation.
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

    public interface ICNGNCryptInterop : ICNGLocalKeyCrypto
    {
        IntPtr OpenStorageProvider(string providerName, int dwFlags = 0);

        IntPtr OpenKey(IntPtr nCryptProviderHandle, string keyName, int dwLegacyKeySpec = 0, int dwFlags = 0);

        IntPtr GenerateKey(IntPtr nCryptProviderHandle, string algorithm, string keyName, int keyLength = 2048, int dwLegacyKeySpec = 0, int dwFlags = 0);

        void DestroyKey(IntPtr key, int dwFlags = 0);

        void FreeObject(IntPtr hObject);

        bool CheckNTStatus(uint ntstatus);

        NTSTATUS GetNTStatus(uint ntstatus);
    }
}