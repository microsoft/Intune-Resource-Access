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

namespace Microsoft.Intune.Test.EncryptionUtilitiesUnitTests
{
    [TestClass]
    public class DerUtilsUnitTests
    {
        [TestMethod]
        public void VerifyDerPrintableStringRoundTrip()
        {
            const string TestString = "this is a printable string";
            Assert.AreEqual(TestString, DerUtils.DecodePrintableString(DerUtils.EncodePrintableString(TestString)), "string should make encoding round trip unchanged");

            Assert.AreEqual(string.Empty, DerUtils.DecodePrintableString(DerUtils.EncodePrintableString(string.Empty)), "empty string should make encoding round trip unchanged");
        }


        [TestMethod]
        public void VerifyDerBMPStringRoundTrip()
        {
            const string TestString = "this string has unicode in the range handled by BMPString \u03d5";
            Assert.AreEqual(TestString, DerUtils.DecodeBMPString(DerUtils.EncodeBMPString(TestString)), "string should make encoding round trip unchanged");

            Assert.AreEqual(string.Empty, DerUtils.DecodeBMPString(DerUtils.EncodeBMPString(string.Empty)), "empty string should make encoding round trip unchanged");
        }

        [TestMethod]
        public void VerifyLongDerStringRoundTrip()
        {
            string TestString = new string('A', 64000);
            Assert.AreEqual(TestString, DerUtils.DecodeString(DerUtils.EncodePrintableString(TestString)), "PrintableString should make encoding round trip unchanged");
            Assert.AreEqual(TestString, DerUtils.DecodeString(DerUtils.EncodeBMPString(TestString)), "BMPString should make encoding round trip unchanged");
        }

        [TestMethod]
        public void VerifyDerEncodeStringNullCases()
        {
            Action[] testCases = new Action[]
            {
                () => DerUtils.EncodePrintableString(null),
                () => DerUtils.EncodeBMPString(null),
            };

            foreach (Action test in testCases)
            {
                bool caught = false;
                try
                {
                    test();
                }
                catch (ArgumentNullException)
                {
                    caught = true;
                }

                Assert.IsTrue(caught, "expected an ArgumentNullException");
            }
        }

        [TestMethod]
        public void VerifyDerSequenceOfRoundTrip()
        {
            var testCases = new[] {
                new { entryData = new List<string>() { "data1" }, description = "single entry" },
                new { entryData = new List<string>() { "data1", "data2", "data3" }, description = "multiple entries" },
                new { entryData = new List<string>() {  }, description = "no entries" },
            };

            foreach (var testCase in testCases)
            {
                // convert the input data from strings to der encoding, since that's the expected input format
                List<byte[]> derEntries = new List<byte[]>();
                foreach (string entry in testCase.entryData)
                {
                    derEntries.Add(DerUtils.EncodePrintableString(entry));
                }

                List<byte[]> outputEntries = DerUtils.DecodeSequenceOf(DerUtils.EncodeSequenceOf(derEntries));

                // verify the output matches the input
                Assert.AreEqual(testCase.entryData.Count, outputEntries.Count, "number of output entries should match the number of input entries for test case: " + testCase.description);

                for (int i = 0; i < outputEntries.Count; i++)
                {
                    Assert.AreEqual(testCase.entryData[i], DerUtils.DecodeString(outputEntries[i]), "Output data should match input data for test case: " + testCase.description);
                }
            }
        }

        [TestMethod]
        public void VerifyDerNameValuePairRoundTrip()
        {
            string name = "name";
            string value = "value";

            Tuple<string, string> output = DerUtils.DecodeNameValuePair(DerUtils.EncodeNameValuePair(name, value));

            Assert.AreEqual(name, output.Item1);
            Assert.AreEqual(value, output.Item2);
        }

        [TestMethod]
        public void VerifyDerDecodeSequenceOfErrorCases()
        {
            // for reference, the valid SequenceOf DER block is: new byte[] { 0x30, 0x34, 0x1e, 0x26, 0x00, 0x56, 0x00, 0x61, 0x00, 0x6c, 0x00, 0x69, 0x00, 0x64, 0x00, 0x69, 0x00, 0x74, 0x00, 0x79, 0x00, 0x50, 0x00, 0x65, 0x00, 0x72, 0x00, 0x69, 0x00, 0x6f, 0x00, 0x64, 0x00, 0x55, 0x00, 0x6e, 0x00, 0x69, 0x00, 0x74, 0x00, 0x73, 0x1e, 0x0a, 0x00, 0x57, 0x00, 0x65, 0x00, 0x65, 0x00, 0x6b, 0x00, 0x73 }

            var testCases = new[] {
                new { test = new byte[] { 0x30, 0x35, 0x1e, 0x26, 0x00, 0x56, 0x00, 0x61, 0x00, 0x6c, 0x00, 0x69, 0x00, 0x64, 0x00, 0x69, 0x00, 0x74, 0x00, 0x79, 0x00, 0x50, 0x00, 0x65, 0x00, 0x72, 0x00, 0x69, 0x00, 0x6f, 0x00, 0x64, 0x00, 0x55, 0x00, 0x6e, 0x00, 0x69, 0x00, 0x74, 0x00, 0x73, 0x1e, 0x0a, 0x00, 0x57, 0x00, 0x65, 0x00, 0x65, 0x00, 0x6b, 0x00, 0x73 },
                    description = "SequenceOf length too large" },
                new { test = new byte[] { 0x30, 0x33, 0x1e, 0x26, 0x00, 0x56, 0x00, 0x61, 0x00, 0x6c, 0x00, 0x69, 0x00, 0x64, 0x00, 0x69, 0x00, 0x74, 0x00, 0x79, 0x00, 0x50, 0x00, 0x65, 0x00, 0x72, 0x00, 0x69, 0x00, 0x6f, 0x00, 0x64, 0x00, 0x55, 0x00, 0x6e, 0x00, 0x69, 0x00, 0x74, 0x00, 0x73, 0x1e, 0x0a, 0x00, 0x57, 0x00, 0x65, 0x00, 0x65, 0x00, 0x6b, 0x00, 0x73 },
                    description = "SequenceOf length too small" },
                new { test = new byte[] { 0x30 },
                    description = "incomplete header" },
                new { test = new byte[] { 0x15, 0x34, 0x1e, 0x26, 0x00, 0x56, 0x00, 0x61, 0x00, 0x6c, 0x00, 0x69, 0x00, 0x64, 0x00, 0x69, 0x00, 0x74, 0x00, 0x79, 0x00, 0x50, 0x00, 0x65, 0x00, 0x72, 0x00, 0x69, 0x00, 0x6f, 0x00, 0x64, 0x00, 0x55, 0x00, 0x6e, 0x00, 0x69, 0x00, 0x74, 0x00, 0x73, 0x1e, 0x0a, 0x00, 0x57, 0x00, 0x65, 0x00, 0x65, 0x00, 0x6b, 0x00, 0x73 },
                    description = "wrong tag" },
                new { test = (byte[])null,
                    description = "null input" },
                new { test = new byte[] { 0x30, 0x34, 0x1e, 0x26, 0x00, 0x56, 0x00, 0x61, 0x00, 0x6c, 0x00, 0x69, 0x00, 0x64, 0x00, 0x69, 0x00, 0x74, 0x00, 0x79, 0x00, 0x50, 0x00, 0x65, 0x00, 0x72, 0x00, 0x69, 0x00, 0x6f, 0x00, 0x64, 0x00, 0x55, 0x00, 0x6e, 0x00, 0x69, 0x00, 0x74, 0x00, 0x73, 0x1e, 0x0a, 0x00, 0x57, 0x00, 0x65, 0x00, 0x65, 0x00, 0x6b, 0x00 },
                    description = "entry has length too large" },
                new { test = new byte[] { 0x30, 0x34, 0x1e, 0x26, 0x00, 0x56, 0x00, 0x61, 0x00, 0x6c, 0x00, 0x69, 0x00, 0x64, 0x00, 0x69, 0x00, 0x74, 0x00, 0x79, 0x00, 0x50, 0x00, 0x65, 0x00, 0x72, 0x00, 0x69, 0x00, 0x6f, 0x00, 0x64, 0x00, 0x55, 0x00, 0x6e, 0x00, 0x69, 0x00, 0x74, 0x00, 0x73, 0x1e, 0x0a, 0x00, 0x57, 0x00, 0x65, 0x00, 0x65, 0x00, 0x6b, 0x00, 0x73, 0x73 },
                    description = "entry has length too small" },
            };


            foreach (var testCase in testCases)
            {
                bool caught = false;

                try
                {
                    DerUtils.DecodeSequenceOf(testCase.test);
                }
                catch (Exception)
                {
                    caught = true;
                }

                Assert.IsTrue(caught, "excected an error, but none caught on test: " + testCase.description);
            }
        }

        [TestMethod]
        public void VerifyDecodeDerStringValid()
        {
            string SomeShortText = "short text";
            string SomeLongText = new string('A', 2000); // text more than 127 bytes long is encoded using DER long form length encoding
            byte[] DerLongFormLength2000 = new byte[] { 2 | 0x80, 0x7, 0xD0 }; // long form DER length encoding for length=2000
            byte[] DerLongFormLength4000 = new byte[] { 2 | 0x80, 0xF, 0xA0 }; // long form DER length encoding for length=4000 (used for multibyte Unicode test cases)

            var testCases = new[] {
                new { test = new byte[] { DerUtils.TagIA5String, 0 }, expected = string.Empty, description = "IA5String empty string" },
                new { test = new byte[] { DerUtils.TagPrintableString, 0 }, expected = string.Empty, description = "PrintableString empty string" },
                new { test = new byte[] { DerUtils.TagBMPString, 0 }, expected = string.Empty, description = "BMPString empty string" },
                new { test = (new byte[] { DerUtils.TagIA5String, (byte)(SomeShortText.Length) }).Concat(Encoding.ASCII.GetBytes(SomeShortText)).ToArray(), expected = SomeShortText, description = "IA5String that falls into short form length encoding" },
                new { test = (new byte[] { DerUtils.TagIA5String }).Concat(DerLongFormLength2000).Concat(Encoding.ASCII.GetBytes(SomeLongText)).ToArray(), expected = SomeLongText, description = "IA5String that falls into long form length encoding" },
                new { test = (new byte[] { DerUtils.TagPrintableString, (byte)(SomeShortText.Length) }).Concat(Encoding.ASCII.GetBytes(SomeShortText)).ToArray(), expected = SomeShortText, description = "PrintableString that falls into short form length encoding" },
                new { test = (new byte[] { DerUtils.TagPrintableString }).Concat(DerLongFormLength2000).Concat(Encoding.ASCII.GetBytes(SomeLongText)).ToArray(), expected = SomeLongText, description = "PrintableString that falls into long form length encoding" },
                new { test = (new byte[] { DerUtils.TagBMPString, (byte)(Encoding.BigEndianUnicode.GetBytes(SomeShortText).Length) }).Concat(Encoding.BigEndianUnicode.GetBytes(SomeShortText)).ToArray(), expected = SomeShortText, description = "BMPString that falls into short form length encoding" },
                new { test = (new byte[] { DerUtils.TagBMPString }).Concat(DerLongFormLength4000).Concat(Encoding.BigEndianUnicode.GetBytes(SomeLongText)).ToArray(), expected = SomeLongText, description = "BMPString that falls into long form length encoding" },
            };

            foreach (var testCase in testCases)
            {
                string output = null;

                try
                {
                    output = DerUtils.DecodeString(testCase.test);
                }
                catch (Exception ex)
                {
                    throw new AggregateException("Error on test case: " + testCase.description, ex);
                }

                Assert.AreEqual(testCase.expected, output, testCase.description);
            }
        }

        [TestMethod]
        public void VerifyDecodeDerStringInvalid()
        {
            string SomeShortText = "short text";
            string SomeLongText = new string('A', 2000); // text more than 127 bytes long is encoded using DER long form length encoding
            byte[] DerLongFormLength2000 = new byte[] { 2 | 0x80, 7, 208 }; // long form DER length encoding for length=2000
            byte[] IncorrectDerLongFormLength2000 = new byte[] { 3 | 0x80, 7, 208 }; // incorrect long form (claims 3 bytes are used for length but only 2 are actually provided).  Used to test the case when the length of length here would extend past array bounds.


            var errorCases = new[] {
                new { test = (byte[])null, description = "null bytes"},
                new { test = new byte[] { }, description = "empty bytes"},
                new { test = new byte[] { DerUtils.TagIA5String }, description = "only tag"},
                new { test = (new byte[] { 0xE, (byte)(SomeShortText.Length) }).Concat(Encoding.ASCII.GetBytes(SomeShortText)).ToArray(), description = "string encoding type not yet implemented"}, // technically tag 0xE is reserved so should never be implemented, meaning this test will be valid long-term even if other encodings are added
                new { test = (new byte[] { DerUtils.TagIA5String, (byte)(SomeShortText.Length + 1) }).Concat(Encoding.ASCII.GetBytes(SomeShortText)).ToArray(), description = "incorrect short form length (+1)"},
                new { test = (new byte[] { DerUtils.TagIA5String, (byte)(SomeShortText.Length - 1) }).Concat(Encoding.ASCII.GetBytes(SomeShortText)).ToArray(), description = "incorrect short form length (-1)"},
                new { test = (new byte[] { DerUtils.TagIA5String }).Concat(DerLongFormLength2000).Concat(Encoding.ASCII.GetBytes("Shorter text than header claims")).ToArray(), description = "incorrect long form length (actual data too short)" },
                new { test = (new byte[] { DerUtils.TagIA5String }).Concat(DerLongFormLength2000).Concat(Encoding.ASCII.GetBytes(SomeLongText + "longer text than header claims")).ToArray(), description = "incorrect long form length (actual data too long)" },
                new { test = (new byte[] { DerUtils.TagIA5String }).Concat(IncorrectDerLongFormLength2000).ToArray(), description = "long form length of length extends beyond array bounds" },
            };

            foreach (var errorCase in errorCases)
            {
                bool caughtError = false;
                try
                {
                    DerUtils.DecodeString(errorCase.test);
                }
                catch (Exception)
                {
                    caughtError = true;
                }

                Assert.IsTrue(caughtError, "DecodeDerString did not throw exception on test case: " + errorCase.description);
            }
        }

        //Comparing encoding to https://asn1.io/asn1playground/
        /// SCHEMA:
        /// World-Schema DEFINITIONS AUTOMATIC TAGS ::= 
        ///BEGIN
        ///  I ::= INTEGER
        ///END
        [TestMethod]
        public void VerifyIntEncodings()
        {
            byte[] encodedbytes = DerUtils.EncodeUnsignedInteger(0);
            Assert.IsTrue(encodedbytes.SequenceEqual(new byte[] { 0x02, 0x01, 0x00 }));
            encodedbytes = DerUtils.EncodeUnsignedInteger(127);
            Assert.IsTrue(encodedbytes.SequenceEqual(new byte[] { 0x02, 0x01, 0x7F }));
            encodedbytes = DerUtils.EncodeUnsignedInteger(128);
            Assert.IsTrue(encodedbytes.SequenceEqual(new byte[] { 0x02, 0x02, 0x00, 0x80 }));
            encodedbytes = DerUtils.EncodeUnsignedInteger(255);
            Assert.IsTrue(encodedbytes.SequenceEqual(new byte[] { 0x02, 0x02, 0x00, 0xFF }));
            encodedbytes = DerUtils.EncodeUnsignedInteger(256);
            Assert.IsTrue(encodedbytes.SequenceEqual(new byte[] { 0x02, 0x02, 0x01, 0x00 }));
            encodedbytes = DerUtils.EncodeUnsignedInteger(10000);
            Assert.IsTrue(encodedbytes.SequenceEqual(new byte[] { 0x02, 0x02, 0x27, 0x10 }));
            encodedbytes = DerUtils.EncodeUnsignedInteger(1000000);
            Assert.IsTrue(encodedbytes.SequenceEqual(new byte[] { 0x02, 0x03, 0x0F, 0x42, 0x40 }));
            encodedbytes = DerUtils.EncodeUnsignedInteger(100000000);
            Assert.IsTrue(encodedbytes.SequenceEqual(new byte[] { 0x02, 0x04, 0x05, 0xF5, 0xE1, 0x00 }));
            encodedbytes = DerUtils.EncodeUnsignedInteger(2147483647); // int.MaxValue
            Assert.IsTrue(encodedbytes.SequenceEqual(new byte[] { 0x02, 0x04, 0x7F, 0xFF, 0xFF, 0xFF }));
            encodedbytes = DerUtils.EncodeUnsignedInteger(new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }); //9223372036854775807
            Assert.IsTrue(encodedbytes.SequenceEqual(new byte[] { 0x02, 0x08, 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }));
            encodedbytes = DerUtils.EncodeUnsignedInteger(new byte[] {
                0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x06,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x07,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            });
            Assert.IsTrue(encodedbytes.SequenceEqual(new byte[] { 0x02, 0x81, 0x80, 0x00,
                0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x06,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x07,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,

            }));
            encodedbytes = DerUtils.EncodeUnsignedInteger(new byte[] {
                0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x06,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x07,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x09,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0a,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0b,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0c,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0d,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0e,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0e,
            });
            Assert.IsTrue(encodedbytes.SequenceEqual(new byte[] { 0x02, 0x81, 0xe9, 0x00,
                0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x03,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x05,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x06,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x07,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x08,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x09,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0a,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0b,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0c,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0d,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0e,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0e,

            }));
        }

        ///Comparing encoding to https://asn1.io/asn1playground/
        /// SCHEMA:
        /// World-Schema DEFINITIONS AUTOMATIC TAGS ::= 
        ///BEGIN
        ///  OID ::= OBJECT IDENTIFIER
        ///END
        [TestMethod]
        public void VerifyOidEncodings()
        {
            byte[] encodedbytes = DerUtils.EncodeOid("1.2.840.113549.1.1.1"); // rec1value OID ::= { 1 2 840 113549 1 1 1 }
            Assert.IsTrue(encodedbytes.SequenceEqual(new byte[] { 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01 }));
            encodedbytes = DerUtils.EncodeOid("1.3.14.3.2.7");  // rec1value OID ::= { 1 3 14 3 2 7 }
            Assert.IsTrue(encodedbytes.SequenceEqual(new byte[] { 0x06, 0x05, 0x2B, 0x0E, 0x03, 0x02, 0x07 }));
            encodedbytes = DerUtils.EncodeOid("2.16.840.1.101.3.4.1.42");  // rec1value OID ::= { 2 16 840 1 101 3 4 1 42 }
            Assert.IsTrue(encodedbytes.SequenceEqual(new byte[] { 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x01, 0x2A }));
        }


        ///Comparing encoding to https://asn1.io/asn1playground/
        /// SCHEMA:
        /// World-Schema DEFINITIONS AUTOMATIC TAGS ::= 
        ///BEGIN
        ///  OID ::= OBJECT IDENTIFIER
        ///END
        [TestMethod]
        public void VerifyOidDecodings()
        {
            string oid = DerUtils.DecodeOid(new byte[] { 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01 });
            Assert.AreEqual(oid, "1.2.840.113549.1.1.1");
            oid = DerUtils.DecodeOid(new byte[] { 0x06, 0x05, 0x2B, 0x0E, 0x03, 0x02, 0x07 });
            Assert.AreEqual(oid, "1.3.14.3.2.7");
            oid = DerUtils.DecodeOid(new byte[] { 0x06, 0x09, 0x60, 0x86, 0x48, 0x01, 0x65, 0x03, 0x04, 0x01, 0x2A });
            Assert.AreEqual(oid, "2.16.840.1.101.3.4.1.42");
        }
    }
}
