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
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Provides a wrapper around native NCrypt dll calls.  This handles checking NTSTATUS return values and throwing exceptions accordingly
    /// </summary>
    public class CNGNCryptInterop
    {
        
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
