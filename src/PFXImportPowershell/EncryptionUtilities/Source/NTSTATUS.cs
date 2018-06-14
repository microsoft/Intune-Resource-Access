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
    /// Possible return status codes from native interop calls
    /// </summary>
    public enum NTSTATUS : uint
    {
        STATUS_SUCCESS = 0x0,
        STATUS_INVALID_HANDLE = 0xC0000008,
        STATUS_INVALID_PARAMETER = 0xC000000D,
        STATUS_NOT_SUPPORTED = 0xC00000BB,

        NTE_BAD_UID = 0x80090001,
        NTE_BAD_HAS = 0x80090002,
        NTE_BAD_KEY = 0x80090003,
        NTE_BAD_LEN = 0x80090004,
        NTE_BAD_DATA = 0x80090005,
        NTE_BAD_SIGNATURE = 0x80090006,
        NTE_BAD_VER = 0x80090007,
        NTE_BAD_ALGID = 0x80090008,
        NTE_BAD_FLAGS = 0x80090009,
        NTE_BAD_TYPE = 0x8009000A,
        NTE_BAD_KEY_STATE = 0x8009000B,
        NTE_BAD_HASH_STATE = 0x8009000C,
        NTE_NO_KEY = 0x8009000D,
        NTE_NO_MEMORY = 0x8009000E,
        NTE_EXISTS = 0x8009000F,
        NTE_PERM = 0x80090010,
        NTE_NOT_FOUND = 0x80090011,
        NTE_DOUBLE_ENCRYPT = 0x80090012,
        NTE_BAD_PROVIDER = 0x80090013,
        NTE_BAD_PROV_TYPE = 0x80090014,
        NTE_BAD_PUBLIC_KEY = 0x80090015,
        NTE_BAD_KEYSET = 0x80090016,
        NTE_PROV_TYPE_NOT_DEF = 0x80090017,
        NTE_PROV_TYPE_ENTRY_BAD = 0x80090018,
        NTE_KEYSET_NOT_DEF = 0x80090019,
        NTE_KEYSET_ENTRY_BAD = 0x8009001A,
        NTE_PROV_TYPE_NO_MATCH = 0x8009001B,
        NTE_SIGNATURE_FILE_BAD = 0x8009001C,
        NTE_PROVIDER_DLL_FAIL = 0x8009001D,
        NTE_PROV_DLL_NOT_FOUND = 0x8009001E,
        NTE_BAD_KEYSET_PARAM = 0x8009001F,
        NTE_FAIL = 0x80090020,
        NTE_SYS_ERR = 0x80090021
    }
}
