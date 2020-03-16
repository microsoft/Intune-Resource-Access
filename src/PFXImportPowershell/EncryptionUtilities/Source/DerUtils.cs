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
using System.Linq;
using System.Text;

namespace Microsoft.Intune.EncryptionUtilities
{
    /// <summary>
    /// Utility methods for encoding and decoding ASN.1 DER data
    /// </summary>
    public class DerUtils
    {

        /// <summary>
        /// DER tag for Integer type
        /// </summary>
        public const byte TagInteger = 0x02;

        /// <summary>
        /// DER tag for Bit String
        /// </summary>
        public const byte TagBitString = 0x03;

        ///<summary>
        /// DER tag for Octet String (Struct/Object)
        /// </summary>
        public const byte TagOctet = 0x04;

        ///<summary>
        /// DER tag for NULL
        /// </summary>
        public const byte TagNull = 0x05;

        /// <summary>
        /// DER tag for Object Identifier
        /// </summary>
        public const byte TagOid = 0x06;

        /// <summary>
        /// DER tag for PrintableString type
        /// </summary>
        public const byte TagPrintableString = 0x13;

        /// <summary>
        /// DER tag for IA5String type
        /// </summary>
        public const byte TagIA5String = 0x16;

        /// <summary>
        /// DER tag for BMPString type
        /// </summary>
        public const byte TagBMPString = 0x1E;

        /// <summary>
        /// DER tag for SequenceOf type
        /// </summary>
        public const byte TagSequenceOf = 0x30; // type bits = 0x10 | Primitive/Constructed bit = 0x20

        /// <summary>
        /// The Byte sequence for a DER NULL object
        /// </summary>
        public static readonly byte[] NullBytes = new byte[] { 0x05, 0x00 };

        /// <summary>
        /// Encode the input string using ASN.1 DER printable string encoding rules
        /// </summary>
        /// <param name="s">input string to encode</param>
        /// <returns>output DER encoding bytes</returns>
        public static byte[] EncodePrintableString(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            byte[] encodedBytes = Encoding.ASCII.GetBytes(s);

            return EncodeString(TagPrintableString, encodedBytes);
        }

        /// <summary>
        /// Encode the input string using ASN.1 DER BMPstring (Basic Multilingual Plane) encoding rules
        /// </summary>
        /// <param name="s">input string to encode</param>
        /// <returns>output DER encoding</returns>
        public static byte[] EncodeBMPString(string s)
        {
            if (s == null)
            {
                throw new ArgumentNullException("s");
            }

            // BMP is a subset of Big Endian unicode
            byte[] encodedBytes = Encoding.BigEndianUnicode.GetBytes(s);

            return EncodeString(TagBMPString, encodedBytes);
        }

        /// <summary>
        /// Encode the name value pair using ASN.1 DER encoding rules.  Name value pairs in DER are encoded as a "sequence of" DER BMPStrings 
        /// </summary>
        /// <param name="name">name for the pair</param>
        /// <param name="value">value for the pair</param>
        /// <returns>output DER encoding for the name value pair</returns>
        public static byte[] EncodeNameValuePair(string name, string value)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }

            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            return EncodeSequenceOf(new byte[][] { EncodeBMPString(name), EncodeBMPString(value) });
        }

        /// <summary>
        /// Computes the binary encoding of an ASN.1 DER SequenceOf block from a set of input entries
        /// </summary>
        /// <param name="DEREntries">the DER encoded entries that will comprise this sequence</param>
        /// <returns>DER encoded SequenceOf block</returns>
        public static byte[] EncodeSequenceOf(IEnumerable<byte[]> DEREntries)
        {
            if (DEREntries == null)
            {
                throw new ArgumentNullException("DEREntries");
            }

            // count total bytes needed
            int totalLength = 0;
            foreach (byte[] entry in DEREntries)
            {
                totalLength += entry.Length;
            }

            byte[] combinedEntries = new byte[totalLength];

            // copy entry bytes to combined array
            int currentPos = 0;
            foreach (byte[] entry in DEREntries)
            {
                Array.Copy(entry, 0, combinedEntries, currentPos, entry.Length);
                currentPos += entry.Length;
            }


            byte[] lengthBytes = EncodeLength(totalLength);

            byte[] outputBytes = (new byte[] { TagSequenceOf }).Concat(lengthBytes).Concat(combinedEntries).ToArray();

            return outputBytes;
        }

        /// <summary>
        /// Encodes a ASN.1 Der Bit string
        /// https://docs.microsoft.com/en-us/windows/win32/seccertenroll/about-bit-string
        /// <param name="BitStringBytes">A byte array bit string</param>
        /// <returns>DER encoded BitString block</returns>
        /// </summary>
        public static byte[] EncodeBitString(byte[] bitString, uint unusedBits)
        {
            if(bitString == null)
            {
                throw new ArgumentNullException(nameof(bitString));
            }

            byte[] lengthBytes = EncodeLength(bitString.Length+1);
            byte[] outputBytes = (new byte[] { TagBitString }).Concat(lengthBytes).Concat(new byte[] { (byte)unusedBits }).Concat(bitString).ToArray();
            return outputBytes;
        }

        /// <summary>
        /// Computes the binary encoding of an ASN.1 DER Octet string block from a set of input entries
        /// https://docs.microsoft.com/en-us/windows/desktop/SecCertEnroll/about-octet-string
        /// </summary>
        /// <param name="DEREntries">the DER encoded entries that will comprise this sequence</param>
        /// <returns>DER encoded SequenceOf block</returns>
        public static byte[] EncodeOctetString(IEnumerable<byte[]> DEREntries)
        {
            if (DEREntries == null)
            {
                throw new ArgumentNullException("DEREntries");
            }

            // count total bytes needed
            int totalLength = 0;
            foreach (byte[] entry in DEREntries)
            {
                totalLength += entry.Length;
            }

            byte[] combinedEntries = new byte[totalLength];

            // copy entry bytes to combined array
            int currentPos = 0;
            foreach (byte[] entry in DEREntries)
            {
                Array.Copy(entry, 0, combinedEntries, currentPos, entry.Length);
                currentPos += entry.Length;
            }


            byte[] lengthBytes = EncodeLength(totalLength);

            byte[] outputBytes = (new byte[] { TagOctet }).Concat(lengthBytes).Concat(combinedEntries).ToArray();

            return outputBytes;
        }

        /// <summary>
        /// Computes the binary encoding of an ASN.1 DER integer block for a simple integer
        /// https://docs.microsoft.com/en-us/windows/desktop/SecCertEnroll/about-integer
        /// <param name="value">the integer to be DER encoded</param>
        /// </summary>
        public static byte[] EncodeUnsignedInteger(uint value)
        {
            int totalLength = 0;
            byte[] padding;
            if (value == 0)
            {
                totalLength = 1;
                return new byte[3] { TagInteger, (byte)totalLength, 0x00 };
            }
            uint shifti = value;
            while (shifti != 0)
            {
                shifti >>= 8;
                totalLength++;
            }
            byte[] i2bytes = BitConverter.GetBytes(value);
            List<byte> trimI2bytes = new List<byte>();
            bool leadingZeros = true;
            for (int i = i2bytes.Length - 1; i >= 0; i--)
            {
                if (!leadingZeros || i2bytes[i] != 0)
                {
                    trimI2bytes.Add(i2bytes[i]);
                    leadingZeros = false;
                }
            }
            if (trimI2bytes[0] > 0x7F)//Needs the extra empty byte to keep it positive.
            {
                padding = new byte[1] { 0 };
                totalLength++;
            }
            else
            {
                padding = new byte[0] { };

            }
            byte[] lengthBytes = EncodeLength(totalLength);
            byte[] outputBytes = (new byte[] { TagInteger }).Concat(lengthBytes).Concat(padding).Concat(trimI2bytes).ToArray();

            return outputBytes;
        }

        /// <summary>
        /// Computes the binary encoding of an ASN.1 DER integer block for a simple integer
        /// https://docs.microsoft.com/en-us/windows/desktop/SecCertEnroll/about-integer
        /// <param name="value">the integer to be DER encoded in Big Endian format</param>
        /// </summary>
        public static byte[] EncodeUnsignedInteger(byte[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException("value");
            }

            int totalLength = 0;
            byte[] padding;
            if (value[0] > 0x7F) //Needs the extra empty byte to keep it positive.
            {
                padding = new byte[1] { 0 };
                totalLength = value.Length + 1;
            }
            else
            {
                padding = new byte[0] { };
                totalLength = value.Length;
            }
            byte[] lengthBytes = EncodeLength(totalLength);
            byte[] outputBytes = (new byte[] { TagInteger }).Concat(lengthBytes).Concat(padding).Concat(value).ToArray();

            return outputBytes;
        }

        /// <summary>
        /// Computes the binary encoding of an ASN.1 DER NULL block
        /// https://docs.microsoft.com/en-us/windows/desktop/SecCertEnroll/about-null
        /// </summary>
        public static byte[] EncodeNull()
        {
            return NullBytes;
        }

        public static void DecodeNull(byte[] bytes)
        {
            if(!bytes.SequenceEqual(NullBytes))
            {
                throw new ArgumentException("Expect null sequence not found");
            }
        }

        /// <summary>
        /// Computes the binary encoding of an ASN.1 DER object identifier block
        /// https://docs.microsoft.com/en-us/windows/desktop/SecCertEnroll/about-object-identifier
        /// </summary>
        /// <param name="oid">The dot notation oid</param>
        /// <returns></returns>
        public static byte[] EncodeOid(string oid)
        {
            if (oid == null)
            {
                throw new ArgumentNullException("oid");
            }
            string[] oidStrParts = oid.Split('.');
            int[] oidIntParts = Array.ConvertAll(oidStrParts, int.Parse);

            byte[] encodedBytes = new byte[1];

            byte byte1 = (byte)((oidIntParts[0] * 40) + oidIntParts[1]);
            encodedBytes[0] = byte1;
            for (int i = 2; i < oidIntParts.Length; i++)
            {
                int curInt = oidIntParts[i];
                if (curInt < 0x80)
                {
                    encodedBytes = encodedBytes.Concat((new byte[] { (byte)curInt })).ToArray();
                }
                else
                {
                    byte[] bigByteBuilder = new byte[0];
                    int currentLeadBit = 0x00; // Last byte had no leading bit
                    while (curInt > 0x80)
                    {
                        bigByteBuilder = ((new byte[] { (byte)((curInt & 0x7f) | currentLeadBit) })).Concat(bigByteBuilder).ToArray();
                        curInt = curInt >> 7;
                        currentLeadBit = 0x80;
                    }
                    bigByteBuilder = ((new byte[] { (byte)(curInt | currentLeadBit) })).Concat(bigByteBuilder).ToArray();
                    encodedBytes = encodedBytes.Concat(bigByteBuilder).ToArray();
                }
            }
            byte[] lengthBytes = EncodeLength(encodedBytes.Length);
            byte[] outputBytes = (new byte[] { TagOid }).Concat(lengthBytes).Concat(encodedBytes).ToArray();
            return outputBytes;
        }


        /// <summary>
        /// Decodes a binary encoding of an ASN.1 DER object identifier block into a Dot value Oid
        /// https://docs.microsoft.com/en-us/windows/desktop/SecCertEnroll/about-object-identifier
        /// </summary>
        /// <param name="oid">A byte array of a DER encoded oid</param>
        /// <returns>The dot value string representation of the OID</returns>
        public static string DecodeOid(byte[] bytes)
        {
            StringBuilder oidDotStr = new StringBuilder();
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            if (bytes.Length < 1)
            {
                throw new ArgumentException("Input bytes are not valid DER format");
            }

            if(bytes[0] != TagOid)
            {
                throw new ArgumentException("Not an OID entry");
            }
            uint dataLength;
            uint lengthBytesUsed;
            DecodeLength(bytes, 1, out dataLength, out lengthBytesUsed);

            // validate data size
            if (bytes.Length != 1 + lengthBytesUsed + dataLength)
            {
                throw new ArgumentException("Input bytes are not valid DER format");
            }

            uint position = 1 + lengthBytesUsed;
            oidDotStr.Append(bytes[position] / 40).Append(".").Append(bytes[position] % 40);
            position++;
            while(position < bytes.Length)
            {
                if(bytes[position] < 0x80)
                {
                    oidDotStr.Append(".").Append(bytes[position]);
                    position++;
                }
                else
                {
                    int nodeValue = 0;

                    while(bytes[position] > 0x80)
                    {
                        nodeValue += bytes[position] & 0x7f;
                        nodeValue <<= 7;
                        position++;
                    }
                    nodeValue += bytes[position];
                    oidDotStr.Append(".").Append(nodeValue);
                    position++;
                }
            }

            return oidDotStr.ToString(); ;
        }


        /// <summary>
        /// Decode an unsigned Integer DER encoding in Big Endian foramt
        /// </summary>
        public static byte[] DecodeUnsignedInteger(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            if (bytes.Length < 1)
            {
                throw new ArgumentException("Input bytes are not valid DER format");
            }

            if (bytes[0] != TagInteger)
            {
                throw new ArgumentException("Not an OID entry");
            }

            uint dataLength;
            uint lengthBytesUsed;
            DecodeLength(bytes, 1, out dataLength, out lengthBytesUsed);
            if(bytes[1+lengthBytesUsed] == 0x00) // Skip the first byte if it is zero
            {
                dataLength--;
                lengthBytesUsed++;
            }
            byte[] result = new byte[dataLength];
            Array.Copy(bytes, 1 + lengthBytesUsed, result, 0, dataLength);
            return result;
        }

        /// <summary>
        /// Decode a bit string from DER encoding
        /// <param name="bytes">Input DER encoding</param>
        /// <returns>bit string (byte array) representing the encoded data</returns>
        /// </summary>
        public static byte[] DecodeBitstring(byte[] bytes, out uint unusedBits)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            if (bytes.Length < 1)
            {
                throw new ArgumentException("Input bytes are not valid DER format");
            }
            if (bytes[0] != TagBitString)
            {
                throw new ArgumentException("Not a Bit String entry");
            }
            uint dataLength;
            uint lengthBytesUsed;
            DecodeLength(bytes, 1, out dataLength, out lengthBytesUsed);
            unusedBits = (uint)bytes[1 + lengthBytesUsed];
            uint dataStartByte = 2 + lengthBytesUsed; //Start at the byte after the unsused bits byte.
            byte[] result = new byte[bytes.Length-dataStartByte];
            Array.Copy(bytes, dataStartByte, result, 0, dataLength-1); //Minus the byte with the unused bit length.
            return result;
        }

        /// <summary>113
        /// Decode a string from DER encoding.  
        /// </summary>
        /// <param name="bytes">Input DER encoding</param>
        /// <returns>string representing the encoded data</returns>
        public static string DecodeString(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException("bytes");
            }

            if (bytes.Length < 1)
            {
                throw new ArgumentException("Input bytes are not valid DER format");
            }

            // first byte tells us which string encoding format was used
            switch (bytes[0])
            {
                case TagIA5String:
                    return DecodeIA5String(bytes);
                case TagPrintableString:
                    return DecodePrintableString(bytes);
                case TagBMPString:
                    return DecodeBMPString(bytes);
                default:
                    // there are a bunch of other encoding formats with various character sets (unicode, etc.), 
                    // but we haven't needed to implement decoders for them yet. 
                    throw new NotImplementedException("Decoder for encoded format not yet implemented");
            }
        }
        /// <summary>
        /// Decode an IA5String from DER encoding
        /// </summary>
        /// <param name="bytes">Input DER encoding</param>
        /// <returns>string representing the encoded data</returns>
        public static string DecodeIA5String(byte[] bytes)
        {
            bytes = GetStringBytes(bytes, TagIA5String);

            // IA5 character set is lower half of ascii so use ascii for decoding bytes to a string
            return Encoding.ASCII.GetString(bytes);
        }

        /// <summary>
        /// Decode a PrintableString from DER encoding
        /// </summary>
        /// <param name="bytes">Input DER encoding</param>
        /// <returns>string representing the encoded data</returns>
        public static string DecodePrintableString(byte[] bytes)
        {
            bytes = GetStringBytes(bytes, TagPrintableString);

            // DER PrintableString character set is a subset of ascii so use ascii for decoding bytes to a string
            return Encoding.ASCII.GetString(bytes);
        }

        /// <summary>
        /// Decode a BMPString from DER encoding
        /// </summary>
        /// <param name="bytes">Input DER encoding</param>
        /// <returns>string representing the encoded data</returns>
        public static string DecodeBMPString(byte[] bytes)
        {
            bytes = GetStringBytes(bytes, TagBMPString);

            // DER BMPString character set is a subset of Big Endian Unicode so use that for decoding bytes to a string
            return Encoding.BigEndianUnicode.GetString(bytes);
        }

        /// <summary>
        /// Decode a NameValuePair from DER encoding.  
        /// </summary>
        /// <param name="bytes">Input DER encoding</param>
        /// <returns>tuple with Item1 as the name and Item2 as the value</returns>
        public static Tuple<string, string> DecodeNameValuePair(byte[] bytes)
        {
            // A NameValuePair object is a SequenceOf object consisting of 2 BMPStrings as its data

            List<byte[]> sequenceEntries = DecodeSequenceOf(bytes);
            if (sequenceEntries.Count != 2)
            {
                throw new ArgumentException("Input bytes are not a name value pair");
            }

            foreach (byte[] entry in sequenceEntries)
            {
                if (entry == null || entry.Length < 2 || entry[0] != TagBMPString)
                {
                    throw new ArgumentException("Input bytes are not a name value pair");
                }
            }

            string name = DecodeBMPString(sequenceEntries[0]);
            string value = DecodeBMPString(sequenceEntries[1]);

            return new Tuple<string, string>(name, value);
        }

        /// <summary>
        /// Decode a SequenceOf object from DER encoding
        /// </summary>
        /// <param name="bytes">Input DER encoding</param>
        /// <returns>list containing each of the individual DER encoded sequence entries as byte[]</returns>
        public static List<byte[]> DecodeSequenceOf(byte[] bytes)
        {
            List<byte[]> result = new List<byte[]>();

            if (bytes == null || bytes.Length < 2)
            {
                throw new ArgumentException("Input bytes are not valid DER format");
            }

            // check the tag for this data segment
            if (bytes[0] != TagSequenceOf)
            {
                throw new ArgumentException("Input bytes are not of type SequenceOf");
            }

            // get the length of the data used in the sequence
            uint sequenceDataLength;
            uint sequenceLengthBytesUsed;
            DecodeLength(bytes, 1, out sequenceDataLength, out sequenceLengthBytesUsed);

            if (bytes.Length != 1 + sequenceDataLength + sequenceLengthBytesUsed)
            {
                throw new ArgumentException("Input bytes are not valid DER format");
            }

            // read each entry in the sequence
            uint entryStart = 1 + sequenceLengthBytesUsed; // first entry starts after the SequenceOf tag (1 byte) and the length portion of its header
            while (bytes.Length - 1 > entryStart)
            {
                // read length
                uint entryDataLength;
                uint entryLengthBytesUsed;
                DecodeLength(bytes, entryStart + 1, out entryDataLength, out entryLengthBytesUsed);

                uint fullEntryLength = 1 + entryLengthBytesUsed + entryDataLength;

                // verify we have enough bytes available
                if (entryStart + fullEntryLength > bytes.Length)
                {
                    throw new ArgumentException("Input bytes are not valid DER format");
                }

                // copy full entry including header and add it to the result
                byte[] entryBytes = new byte[fullEntryLength];
                Array.Copy(bytes, entryStart, entryBytes, 0, fullEntryLength);
                result.Add(entryBytes);

                // update next start position
                entryStart += fullEntryLength;
            }

            return result;
        }

        /// <summary>
        /// Combines the pre-encoded string bytes with a DER block header to produce the full der encoded string
        /// </summary>
        /// <param name="ASN1Tag">Type tag for the encoded string</param>
        /// <param name="encodedBytes">pre-encoded bytes representing the string data</param>
        /// <returns>full der block for the encoded string</returns>
        private static byte[] EncodeString(byte ASN1Tag, byte[] encodedBytes)
        {
            int length = encodedBytes.Length;

            byte[] lengthBytes = EncodeLength(length);

            byte[] outputBytes = (new byte[] { ASN1Tag }).Concat(lengthBytes).Concat(encodedBytes).ToArray();

            return outputBytes;
        }

        /// <summary>
        /// Computes the binary encoding of the length parameter used in the block header using der rules
        /// </summary>
        /// <param name="length">length parameter to encode</param>
        /// <returns>der binary encoded length</returns>
        private static byte[] EncodeLength(int length)
        {
            const byte DERLongFormFlag = 0x80;

            byte[] lengthBytes = null;
            if (length < 128)
            {
                // use short form length encoding, which is just 1 byte representing the length
                // (most significant bit = 0)
                lengthBytes = new byte[] { (byte)length };
            }
            else
            {
                // use long form length encoding
                // byte 0: 
                //   most significant bit = 1
                //   other 7 bits = number of bytes used to encode the length of the data (i.e., the length of the length of the data)
                // bytes 1-N: 
                //   big endian bytes encoding the length of the data
                //   note that per DER rules, this must be the smallest possible representation of that value,
                //   meaning that leading 0s must be trimmed

                // encode the length of the input bytes
                byte[] bigEndianLengthEncoding = MinimalBigEndianBytes((uint)length);

                // encode the length of the length along with the DER long form flag
                byte byte0 = (byte)(DERLongFormFlag | (byte)(bigEndianLengthEncoding.Length));

                // combine the two to form the whole length section of the output
                lengthBytes = (new byte[] { byte0 }).Concat(bigEndianLengthEncoding).ToArray();
            }

            return lengthBytes;
        }

        /// <summary>
        /// Decode the length portion of a DER header
        /// </summary>
        /// <param name="bytes">input DER encoding</param>
        /// <param name="start">byte index where the length attribute is found</param>
        /// <param name="length">returns the length of the data portion  as described by the DER header</param>
        /// <param name="bytesUsed">returns the number of bytes used to encode the length in the header</param>
        private static void DecodeLength(byte[] bytes, uint start, out uint length, out uint bytesUsed)
        {
            if (bytes == null || bytes.Length <= start)
            {
                throw new ArgumentException("header length attribute start position is beyond end of input bytes", nameof(bytes));
            }

            // read the length of the data. The length portion of the header is one or more bytes starting at byte 1
            // If the first (most significant) bit of the first length byte is 0, then the length is encoded using the short form.  The other 7 bits of that byte represent the length of the data.
            // otherwise, the length is encoded using long form.  The other 7 bits represent the number of bytes needed to encode the length of the data (N). The next N bytes are the length of the data in big endian byte order.
            const byte DERLongFormFlag = 0x80;
            if ((bytes[start] & DERLongFormFlag) == 0)
            {
                // short form -- length is just the single byte
                length = bytes[start];
                bytesUsed = 1;

                return;
            }
            else
            {
                // long form
                //const int HeaderStaticPortionLength = 2; // the static portion of the header is one byte for the tag and one byte for the length of the length.
                uint lengthOfLength = bytes[start] & (uint)0x7f; // remaining 7 bits tell us how many bytes encode the length

                if (lengthOfLength > 4)
                {
                    throw new NotSupportedException("Unsupported: lengthOfLength data too large"); // we only support strings up to 4GB in length (length can be encoded in a uint).  Not expecting 4GB CSRs.
                }

                // extract the length bytes
                byte[] lengthBytes = new byte[lengthOfLength];
                Array.Copy(bytes, start + 1, lengthBytes, 0, lengthOfLength);

                // convert from big endian to little endian if necessary
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(lengthBytes);
                }

                // because DER specifies that the length should be encoded using the minimum possible number of bytes, we need to pad back to a full 4 bytes for conversion to uint
                byte[] fullLengthBytes = new byte[4];
                Array.Copy(lengthBytes, fullLengthBytes, lengthBytes.Length);

                length = BitConverter.ToUInt32(fullLengthBytes, 0);
                bytesUsed = lengthOfLength + 1; // lengthOfLength + the byte that told us that 

                return;
            }
        }

        /// <summary>
        /// Gets the string data portion of the DER-encoded input 
        /// </summary>
        /// <param name="derBytes">input DER encoded string</param>
        /// <param name="expectedEncodingTypeTag">expected tag (DER type), enforced</param>
        /// <returns>a byte[] containing the still-encoded data portion of a DER string block</returns>
        private static byte[] GetStringBytes(byte[] derBytes, byte expectedEncodingTypeTag)
        {
            if (derBytes == null)
            {
                throw new ArgumentNullException("derBytes");
            }

            if (derBytes.Length < 2)
            {
                throw new ArgumentException("Input bytes are not valid DER format");
            }

            // check the actual tag (data type, found in byte 0) against the expected tag  
            if (derBytes[0] != expectedEncodingTypeTag)
            {
                throw new InvalidOperationException("Input encoding is not the expected type");
            }

            // read the length portion of the header starting at the byte after the tag
            uint dataLength;
            uint lengthBytesUsed;
            DecodeLength(derBytes, 1, out dataLength, out lengthBytesUsed);

            // validate data size
            if (derBytes.Length != 1 + lengthBytesUsed + dataLength)
            {
                throw new ArgumentException("Input bytes are not valid DER format");
            }

            // return just the data portion of the input bytes
            byte[] result = new byte[dataLength];
            Array.Copy(derBytes, 1 + lengthBytesUsed, result, 0, dataLength);
            return result;
        }

        /// <summary>
        /// Returns the shortest possible byte array containing the big-endian representation of the input uint value
        /// </summary>
        /// <param name="u">input unsigned int value</param>
        /// <returns>output byte array</returns>
        private static byte[] MinimalBigEndianBytes(uint u)
        {
            // get bytes from the input
            byte[] fullBytes = BitConverter.GetBytes(u);

            // convert from little endian to big endian if necessary
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(fullBytes);
            }

            // count the number of leading 0s in the byte array which need to be removed
            int unusedByteCount = 0;
            while (unusedByteCount < fullBytes.Length && fullBytes[unusedByteCount] == 0)
            {
                unusedByteCount++;
            }

            // copy non-zero portions of the full byte array into the trimmed byte array
            byte[] trimmedBytes = new byte[fullBytes.Length - unusedByteCount];
            Array.Copy(fullBytes, unusedByteCount, trimmedBytes, 0, fullBytes.Length - unusedByteCount);

            return trimmedBytes;
        }
    }
}
