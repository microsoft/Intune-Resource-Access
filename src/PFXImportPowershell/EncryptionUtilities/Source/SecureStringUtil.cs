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
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security;

    /// <summary>
    /// Helper class to provide easy calls for SecureString operations
    /// </summary>
    public class SecureStringUtil
    {
        /// <summary>
        /// Appends a string to a SecureString
        /// </summary>
        /// <param name="destString">SecureString destination</param>
        /// <param name="stringToAppend">Standard string source</param>
        public static void AppendToSecureString(SecureString destString, string stringToAppend)
        {
            if (destString == null)
            {
                throw new ArgumentNullException(nameof(destString));
            }

            if (stringToAppend == null)
            {
                throw new ArgumentNullException(nameof(stringToAppend));
            }

            foreach (char c in stringToAppend)
            {
                destString.AppendChar(c);
            }
        }

        /// <summary>
        /// Appends a SecureString to another SecureString
        /// </summary>
        /// <param name="destString">Destination string to which the other string will be appended</param>
        /// <param name="stringToAppend">Source string to append</param>
        public static void AppendToSecureString(SecureString destString, SecureString stringToAppend)
        {
            IntPtr strPtr = IntPtr.Zero;

            if (destString == null)
            {
                throw new ArgumentNullException(nameof(destString));
            }

            if (stringToAppend == null)
            {
                throw new ArgumentNullException(nameof(stringToAppend));
            }

            try
            {
                strPtr = Marshal.SecureStringToGlobalAllocUnicode(stringToAppend);
                for (int offset = 0; offset < stringToAppend.Length * 2; offset += 2)
                {
                    char curChar = (char)Marshal.ReadInt16(strPtr, offset);
                    destString.AppendChar(curChar);
                }
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(strPtr);
            }
        }

        /// <summary>
        /// Copies the bytes of a secure string to a pre-allocated byte array
        /// </summary>
        /// <param name="source">SecureString to copy from</param>
        /// <param name="dest">Pre-allocated Destination buffer</param>
        public static void SecureStringCopy(SecureString source, byte[] dest)
        {
            IntPtr srcPtr = IntPtr.Zero;

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (dest == null)
            {
                throw new ArgumentNullException(nameof(dest));
            }

            // NOTE: SecureString only appends individual char data, thus it does not
            //       treat higher Unicode code points as a single character.  This makes
            //       it 'safe' to simply check against a destination buffer length
            //       that is twice the number of chars stored in SecureString.
            if (dest.Length < source.Length * 2)
            {
                throw new ArgumentException("Buffer too small");
            }

            try
            {
                srcPtr = Marshal.SecureStringToGlobalAllocUnicode(source);
                Marshal.Copy(srcPtr, dest, 0, source.Length * 2);
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(srcPtr);
            }
        }

        /// <summary>
        /// Replaces the contents of the destination SecureString with the value of the source string
        /// </summary>
        /// <param name="source">String to copy into the SecureString</param>
        /// <param name="dest">Destination SecureString object</param>
        public static void SecureStringCopy(string source, SecureString dest)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (dest == null)
            {
                throw new ArgumentNullException(nameof(dest));
            }

            dest.Clear();
            foreach (char c in source)
            {
                dest.AppendChar(c);
            }
        }

        /// <summary>
        /// Converts UTF-8 byte sequences to UCS-2 and copies them into the destination SecureString
        /// </summary>
        /// <param name="utf8Value">Sequence of UTF-8 encoded characters</param>
        /// <param name="dest">Destination SecureString object</param>
        public static void CopyUTF8ToSecureString(byte[] utf8Value, SecureString dest)
        {
            if (utf8Value == null)
            {
                throw new ArgumentNullException(nameof(utf8Value));
            }

            if (dest == null)
            {
                throw new ArgumentNullException(nameof(dest));
            }

            int offset = 0;
            dest.Clear();

            while (offset < utf8Value.Length)
            {
                uint currentByte = utf8Value[offset];

                if ((currentByte & 0x80) == 0)
                {
                    // Single-byte character

                    dest.AppendChar((char)currentByte);
                    offset += 1;
                }
                else if ((currentByte & 0xE0) == 0xC0)
                {
                    // Two-byte character

                    if (offset + 1 >= utf8Value.Length ||
                        (utf8Value[offset + 1] & 0xC0) != 0x80)
                    {
                        throw new InvalidDataException("Invalid UTF-8 encoding");
                    }

                    char charToAppend = (char)(((uint)(currentByte & 0x1F)) << 6 |
                        ((uint)(utf8Value[offset + 1] & 0x3F)));
                    dest.AppendChar(charToAppend);
                    offset += 2;
                }
                else if ((currentByte & 0xF0) == 0xE0)
                {
                    // Three-byte character

                    if (offset + 2 >= utf8Value.Length ||
                        (utf8Value[offset + 1] & 0xC0) != 0x80 ||
                        (utf8Value[offset + 2] & 0xC0) != 0x80)
                    {
                        throw new InvalidDataException("Invalid UTF-8 encoding");
                    }

                    char charToAppend = (char)(((uint)(currentByte & 0x0F)) << 12 |
                        ((uint)(utf8Value[offset + 1] & 0x3F)) << 6 |
                        ((uint)(utf8Value[offset + 2] & 0x3F)));

                    dest.AppendChar(charToAppend);
                    offset += 3;
                }
                else
                {
                    // This is not necessarily invalid UTF-8 encoding.
                    // For example, it could be a code point outside the BMP.
                    // Rather, all UTF-8 characters up to 3-byte encoding
                    // are in code point range of 0x0000..0xFFFF, and thus
                    // encode a value that fits into a single UCS-2 character. 
                    // Example: U+1F355 is the "SLICE OF PIZZA" unicode character.
                    //          U+1F355 is UTF-8 encoded as the four-byte sequence F0 9F 8D 95
                    //          This would be valid UTF-8, but fail here.
                    throw new InvalidDataException("Cannot convert UTF-8 characters above 0xFFFF into USC-2");
                }
            }

            if (offset != utf8Value.Length)
            {
                throw new InvalidDataException("Invalid UTF-8 encoding");
            }
        }

        /// <summary>
        /// Encodes and escapes characters in a secure string to make them suitable for embedding in an XML document as
        /// the value of an element
        /// </summary>
        /// <param name="inputString">SecureString to encode</param>
        /// <returns>Encoded string</returns>
        public static SecureString EncodeForXMLElementValue(SecureString inputString)
        {
            if (inputString == null)
            {
                throw new ArgumentNullException(nameof(inputString));
            }

            SecureString retString = new SecureString();
            IntPtr inputPtr = IntPtr.Zero;

            try
            {
                inputPtr = Marshal.SecureStringToGlobalAllocUnicode(inputString);

                for (int inputOffset = 0; inputOffset < inputString.Length * 2; inputOffset += 2)
                {
                    char curInputChar = (char)Marshal.ReadInt16(inputPtr, inputOffset);

                    switch (curInputChar)
                    {
                        case '&':
                            AppendToSecureString(retString, "&amp;");
                            break;
                        case '\'':
                            AppendToSecureString(retString, "&apos;");
                            break;
                        case '<':
                            AppendToSecureString(retString, "&lt;");
                            break;
                        case '>':
                            AppendToSecureString(retString, "&gt;");
                            break;
                        case '"':
                            AppendToSecureString(retString, "&quot;");
                            break;
                        default:
                            retString.AppendChar(curInputChar);
                            break;
                    }
                }
            }
            finally
            {
                Marshal.ZeroFreeGlobalAllocUnicode(inputPtr);
                inputPtr = IntPtr.Zero;
            }

            return retString;
        }
    }
}
