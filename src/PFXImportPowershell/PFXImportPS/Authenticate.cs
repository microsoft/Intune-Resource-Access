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

namespace Microsoft.Management.Powershell.PFXImport
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Security;
    using Microsoft.Identity.Client;

    public class Authenticate
    {
        public const string AuthURI = "login.microsoftonline.com";
        public const string GraphURI = "https://graph.microsoft.com";
        public const string SchemaVersion = "beta";
        public const string AuthTokenKey = "AuthToken";

        public static readonly string ClientId0 = Guid.Empty.ToString();

        private enum CachedTokenApplicationType
        {
            None,
            PublicApplication,
            ConfidentialApplication,
        }

        private static CachedTokenApplicationType cachedTokenApplicationType = CachedTokenApplicationType.None;



        public static string GetClientId(Hashtable modulePrivateData)
        {
            string result = (string)modulePrivateData["ClientId"] ?? Authenticate.ClientId0;
            if (string.Compare(result, Authenticate.ClientId0, StringComparison.OrdinalIgnoreCase) == 0)
            {
                throw new ArgumentException("ClientId from app registration must be supplied in the PowerShell module PrivateData");
            }

            return result;
        }

        public static string GetAuthURI(Hashtable modulePrivateData)
        {
            return (string)modulePrivateData["AuthURI"] ?? Authenticate.AuthURI;
        }

        public static string GetGraphURI(Hashtable modulePrivateData)
        {
            return (string)modulePrivateData["GraphURI"] ?? Authenticate.GraphURI;
        }
        public static string GetSchemaVersion(Hashtable modulePrivateData)
        {
            return (string)modulePrivateData["SchemaVersion"] ?? Authenticate.SchemaVersion;
        }

        private static string GetAuthority(Hashtable modulePrivateData)
        {
            return string.Format("https://{0}/organizations", GetAuthURI(modulePrivateData));
        }

        private static string GetTenantId(Hashtable modulePrivateData)
        {
            string tenantId = (string)modulePrivateData["TenantId"];
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                // verify this is a valid guid
                if (Guid.TryParse(tenantId, out _))
                {
                    return tenantId;
                }
                else
                {
                    throw new ArgumentException("Specified TenantId is not a valid guid");
                }
            }
            else
            {
                return null;
            }
        }

        private static string GetClientSecret(Hashtable modulePrivateData)
        {
            return (string)modulePrivateData["ClientSecret"];
        }

        [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:FieldsMustBePrivate", Justification = "Declaring it as a function helps to test a code path.")]
        [SuppressMessage("Microsoft.Usage", "CA2211:NonConstantFieldsShouldNotBeVisible", Justification = "Needs to be public and can't make functions consts")]
        public static Func<AuthenticationResult, bool> AuthTokenIsValid = (AuthRes) =>
        {
            if (AuthRes != null && AuthRes.AccessToken != null && AuthRes.ExpiresOn > DateTimeOffset.UtcNow)
            {
                return true;
            }

            return false;
        };

        private static string redirectUri = @"https://login.microsoftonline.com/common/oauth2/nativeclient";
        public static Uri GetRedirectUri(Hashtable modulePrivateData)
        {
            string uri = (string)modulePrivateData["RedirectURI"] ?? redirectUri;
            return new Uri(uri);
        }

        private static string[] GetScopes(Hashtable modulePrivateData)
        {
            return new string[] { $"{ GetGraphURI(modulePrivateData) }/.default" };
        }

        private static IPublicClientApplication BuildMSALClientApplications(Hashtable modulePrivateData)
        {
            IPublicClientApplication app = PublicClientApplicationBuilder.Create(GetClientId(modulePrivateData))
                .WithCacheOptions(CacheOptions.EnableSharedCacheOptions) // create static memory cache
                .WithAuthority(GetAuthority(modulePrivateData))
                .WithRedirectUri(GetRedirectUri(modulePrivateData).ToString())
                .Build();
            return app;
        }

        private static IConfidentialClientApplication BuildMSALConfidentialClientApplication(Hashtable modulePrivateData)
        {
            IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(GetClientId(modulePrivateData))
                .WithAuthority(GetAuthority(modulePrivateData))
                .WithRedirectUri(GetRedirectUri(modulePrivateData).ToString())
                .WithTenantId(GetTenantId(modulePrivateData))  
                .WithClientSecret(GetClientSecret(modulePrivateData))
                .Build();
            return app;
        }

        public static AuthenticationResult GetAuthToken(string user, SecureString password, Hashtable modulePrivateData)
        {
            if (!string.IsNullOrWhiteSpace(user))
            {

                IPublicClientApplication app = BuildMSALClientApplications(modulePrivateData);

                AuthenticationResult result;
                if (password == null)
                {
                    try
                    {
                        result = app.AcquireTokenInteractive(GetScopes(modulePrivateData))
                            .WithLoginHint(user)
                            .ExecuteAsync()
                            .Result;
                    }
                    catch (AggregateException ex)
                    {
                        throw ex.InnerException;
                    }
                }
                else
                {
                    try
                    {
                        result = app.AcquireTokenByUsernamePassword(GetScopes(modulePrivateData), user, password)
                            .ExecuteAsync()
                            .Result;
                    }
                    catch (AggregateException ex)
                    {
                        throw ex.InnerException;
                    }
                }

                cachedTokenApplicationType = CachedTokenApplicationType.PublicApplication;

                return result;
            }
            else
            {
                string clientSecret = GetClientSecret(modulePrivateData);
                if (string.IsNullOrWhiteSpace(clientSecret))
                {
                    throw new ArgumentException("No authentication method provided.  Specify AdminUserName on command line or ClientSecret setting in module PrivateData.");
                }

                if (string.Compare(GetTenantId(modulePrivateData), Guid.Empty.ToString(), StringComparison.OrdinalIgnoreCase) == 0)
                {
                    throw new ArgumentException("TenantId must be provided in module PrivateData when authenticating with client secret");
                }

                AuthenticationResult result;
                try
                {
                    IConfidentialClientApplication app = BuildMSALConfidentialClientApplication(modulePrivateData);
                    result = app.AcquireTokenForClient(GetScopes(modulePrivateData))
                        .WithAuthority(string.Format("https://{0}", GetAuthURI(modulePrivateData)), GetTenantId(modulePrivateData))
                        .ExecuteAsync()
                        .Result;
                }
                catch (AggregateException ex)
                {
                    throw ex.InnerException;
                }

                cachedTokenApplicationType = CachedTokenApplicationType.ConfidentialApplication;

                return result;
            }
        }

        public static AuthenticationResult GetAuthToken(Hashtable modulePrivateData)
        {
            if (cachedTokenApplicationType == CachedTokenApplicationType.PublicApplication)
            {
                IPublicClientApplication app = BuildMSALClientApplications(modulePrivateData);

                List<IAccount> accounts = new List<IAccount>((app.GetAccountsAsync()).Result);

                if (accounts.Count < 1)
                {
                    throw new ArgumentException("No token cached.  First call Set-IntuneAuthenticationToken");
                }
                else
                {
                    try
                    {
                        // use the account in the cache, if possible
                        return app.AcquireTokenSilent(GetScopes(modulePrivateData), accounts[0])
                           .ExecuteAsync()
                           .Result;
                    }
                    catch (AggregateException ex)
                    {
                        throw ex.InnerException;
                    }
                }
            }
            else if (cachedTokenApplicationType == CachedTokenApplicationType.ConfidentialApplication)
            {
                try
                {
                    IConfidentialClientApplication app = BuildMSALConfidentialClientApplication(modulePrivateData);
                    return app.AcquireTokenForClient(GetScopes(modulePrivateData))
                        .WithAuthority(string.Format("https://{0}", GetAuthURI(modulePrivateData)), GetTenantId(modulePrivateData))
                        .ExecuteAsync()
                        .Result;
                }
                catch (AggregateException ex)
                {
                    throw ex.InnerException;
                }
            }
            else
            {
                throw new ArgumentException("No token cached.  First call Set-IntuneAuthenticationToken");
            }
        }

        public static void ClearTokenCache(Hashtable modulePrivateData)  
        {
            IPublicClientApplication app = BuildMSALClientApplications(modulePrivateData);

            List<IAccount> accounts = new List<IAccount>((app.GetAccountsAsync()).Result);

            while (accounts.Count > 0)
            {
                app.RemoveAsync(accounts[0]).Wait();
                accounts = new List<IAccount>((app.GetAccountsAsync()).Result);
            }

            cachedTokenApplicationType = CachedTokenApplicationType.None;
        }
    }
}
