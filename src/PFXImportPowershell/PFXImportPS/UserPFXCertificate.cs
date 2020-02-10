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

namespace Microsoft.Management.Services.Api
{
    using System;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    #region Useful enums
    /// <summary>
    /// Values for certificate's intended purpose.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum UserPfxIntendedPurpose
    {
        /// <summary>
        /// No roles/usages assigned.
        /// </summary>
        Unassigned = 0,

        /// <summary>
        /// Valid for S/MIME encryption.
        /// </summary>
        SmimeEncryption = 1,

        /// <summary>
        /// Valid for S/MIME signing.
        /// </summary>
        SmimeSigning = 2,

        /// <summary>
        /// Valid for use in VPN.
        /// </summary>
        VPN = 4,

        /// <summary>
        /// Valid for use in WiFi.
        /// </summary>
        Wifi = 8
    }

    /// <summary>
    /// Values for padding scheme used by encryption provider
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum UserPfxPaddingScheme
    {
        /// <summary>
        /// Padding scheme not specified. Uses the default.
        /// </summary>
        None = 0,

        /// <summary>
        /// Use PKCS#1 padding.
        /// NO LONGER SUPPORTED
        /// </summary>
        [Obsolete("Pkcs1 no longer supported")]
        Pkcs1 = 1,

        /// <summary>
        /// Use OAEP SHA-1 padding.
        /// NO LONGER SUPPORTED
        /// </summary>
        [Obsolete("OaepSha1 no longer supported")]
        OaepSha1 = 2,

        /// <summary>
        /// Use OAEP SHA-256 padding.
        /// </summary>
        OaepSha256 = 3,

        /// <summary>
        /// Use OAEP SHA-384 padding.
        /// </summary>
        OaepSha384 = 4,

        /// <summary>
        /// Use OAEP SHA-512 padding.
        /// </summary>
        OaepSha512 = 5
    }
    #endregion Useful enums

    #region Graph entity
    public sealed class UserPFXCertificate
    {
        /// <summary>
        /// Initialize a new instance of UserPFXCertificate class.
        /// </summary>
        public UserPFXCertificate() { }

        /// <summary>
        /// Id Key. Takes from the UserId and Thumbprint.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }
        /// <summary>
        /// SHA-1 thumbprint of the PFX certificate.
        /// </summary>
        public string Thumbprint { get; set; }

        /// <summary>
        /// Certificate's intended purpose from the point-of-view of deployment.
        /// </summary>
        public UserPfxIntendedPurpose IntendedPurpose { get; set; }

        /// <summary>
        /// User Principal Name of the PFX certificate.
        /// </summary>
        public string UserPrincipalName { get; set; }

        /// <summary>
        /// Certificate's validity start date/time.
        /// </summary>
        public DateTimeOffset StartDateTime { get; set; }

        /// <summary>
        /// Certificate's validity expiration date/time.
        /// </summary>
        public DateTimeOffset ExpirationDateTime { get; set; }

        /// <summary>
        /// Crypto provider used to encrypt this blob.
        /// </summary>
        public string ProviderName { get; set; }

        /// <summary>
        /// Name of the key (within the provider) used to encrypt the blob.
        /// </summary>
        public string KeyName { get; set; }

        /// <summary>
        /// Padding scheme used by the provider during encryption/decryption.
        /// </summary>
        public UserPfxPaddingScheme PaddingScheme { get; set; }

        /// <summary>
        /// Encrypted PFX blob.
        /// </summary>
        public byte[] EncryptedPfxBlob { get; set; }

        /// <summary>
        /// Encrypted PFX password.
        /// </summary>
        public string EncryptedPfxPassword { get; set; }

        /// <summary>
        /// Date/time when this PFX certificate was imported.
        /// </summary>
        public DateTimeOffset CreatedDateTime { get; set; }

        /// <summary>
        /// Date/time when this PFX certificate was last modified.
        /// </summary>
        public DateTimeOffset LastModifiedDateTime { get; set; }
    }
    #endregion Graph entity
}
