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

namespace Microsoft.Management.Powershell.PFXImport.Cmdlets
{
    using System;
    using System.Collections;
    using System.Management.Automation;
    using System.Security;
    using IdentityModel.Clients.ActiveDirectory;

    /// <summary>
    /// Does the initial authentication against AAD and can be used for subsequest cmdlet calls.
    /// </summary>
    [Cmdlet(VerbsCommon.Set, "IntuneAuthenticationToken", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
    public class SetAuthToken : PSCmdlet
    {
        /// <summary>
        /// Intune Tenant Admin user to be authenticated.
        /// </summary>
        [Parameter(Mandatory = true)]
        [ValidateNotNullOrEmpty()]
        public string AdminUserName
        {
            get;
            set;
        }

        /// <summary>
        /// Intune Tenant Admin password to be authenticated.
        /// </summary>
        [Parameter]
        public SecureString AdminPassword
        {
            get;
            set;
        }

        protected override void ProcessRecord()
        {

            Hashtable modulePrivateData = this.MyInvocation.MyCommand.Module.PrivateData as Hashtable;
            AuthenticationResult authToken = Authenticate.GetAuthToken(AdminUserName, AdminPassword, modulePrivateData);
            if (!Authenticate.AuthTokenIsValid(authToken))
            {
                this.ThrowTerminatingError(
                    new ErrorRecord(
                        new InvalidOperationException("Cannot get Authentication Token"),
                        "Authentication Failure",
                        ErrorCategory.AuthenticationError,
                        authToken));
            }
        }
    }
}
