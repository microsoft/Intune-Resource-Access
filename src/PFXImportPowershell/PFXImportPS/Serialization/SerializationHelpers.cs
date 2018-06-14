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
namespace Microsoft.Management.Powershell.PFXImport.Serialization
{
    using System.Collections.Generic;
    using DirectoryServices;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;
    using Services.Api;
    using System;

    public static class SerializationHelpers
    {
        #region Private Constants

        public const string ODataJSONTypePropertyKey = "@odata.type";
        public const string JSONConvertTypePropertyKey = "$type";

        public const string GraphUserPFXCertificateNamespace = "#microsoft.graph.userPFXCertificate";
        public static readonly string UserPFXCertificateNamespace = typeof(UserPFXCertificate).AssemblyQualifiedName;

        private static JsonSerializerSettings jSONSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            MetadataPropertyHandling = MetadataPropertyHandling.ReadAhead,
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        #endregion Private Constants

        public static string SerializeUserPFXCertificate(UserPFXCertificate cert)
        {
            string json = JsonConvert.SerializeObject(cert, Formatting.Indented, jSONSettings);
            return json;
        }

        public static UserPFXCertificate DeserializeUserPFXCertificate(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(nameof(value));
            }
            value = value.Replace(ODataJSONTypePropertyKey, JSONConvertTypePropertyKey);
            value = value.Replace(GraphUserPFXCertificateNamespace, UserPFXCertificateNamespace);
            return JsonConvert.DeserializeObject<UserPFXCertificate>(value, jSONSettings);
        }

        public static UserPFXCertificate DeserializeUserPFXCertificateValueWrapped(string value)
        {
            JsonObjectWrapper valueWrapper = JsonConvert.DeserializeObject<JsonObjectWrapper>(value);
            JObject jsonValue = valueWrapper.Value;
            JsonSerializer jsonSerializer = JsonSerializer.Create(jSONSettings);
            string entityJson = jsonValue.ToString();
            return DeserializeUserPFXCertificate(entityJson);
        }

        public static List<UserPFXCertificate> DeserializeUserPFXCertificateList(string value)
        {
            JsonArrayWrapper arrayWrapper = JsonConvert.DeserializeObject<JsonArrayWrapper>(value);
            IEnumerable<JObject> jsonArray = arrayWrapper.Value;
            JsonSerializer jsonSerializer = JsonSerializer.Create(jSONSettings);
            List<UserPFXCertificate> entityList = new List<UserPFXCertificate>();

            foreach (JObject jObject in jsonArray)
            {
                string entityJson = jObject.ToString();
                UserPFXCertificate entity = DeserializeUserPFXCertificate(entityJson);
                entityList.Add(entity);
            }

            return entityList;
        }

        public static User DeserializeUser(string value)
        {
            JsonArrayWrapper arrayWrapper = JsonConvert.DeserializeObject<JsonArrayWrapper>(value);
            IEnumerable<JObject> jsonArray = arrayWrapper.Value;
            JsonSerializer jsonSerializer = JsonSerializer.Create(jSONSettings);
            foreach (JObject jObject in jsonArray)
            {
                string entityJson = jObject.ToString();
                
                // Should only be one.
                return JsonConvert.DeserializeObject<User>(entityJson, jSONSettings);
            }

            return null;
        }
    }
}
