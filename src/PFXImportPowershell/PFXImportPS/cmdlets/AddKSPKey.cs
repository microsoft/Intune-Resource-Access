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
    using System.Management.Automation;
    using Microsoft.Intune.EncryptionUtilities;

    [Cmdlet(VerbsCommon.Add, "IntuneKspKey")]
    public class AddKSPKey : PSCmdlet
    { 
        private const int KeyExistsErrorCode = -2146233296;

        private int keyLength = 2048;


        [Parameter(Position = 1, Mandatory = true)]
        public string ProviderName { get; set; }

        [Parameter(Position = 2, Mandatory = true)]
        public string KeyName { get; set; }

        [Parameter(Position = 3)]
        public int KeyLength
        {
            get
            {
                return keyLength;
            }

            set
            {
                keyLength = value;
            }
        }

        [Parameter]
        public SwitchParameter MakeExportable { get; set; }


        protected override void ProcessRecord()
        {
            ManagedRSAEncryption managedRSA = new ManagedRSAEncryption();
            if(managedRSA.TryGenerateLocalRSAKey(ProviderName, KeyName, KeyLength, MakeExportable.IsPresent))
            {
                //Creation succeeded
            }
            else
            {
                //Creation failed, likely already exists
                this.WriteError(
                    new ErrorRecord(
                        new InvalidOperationException("Key Creation failed, it likely already exists"), 
                        "KeyAlreadyExists", 
                        ErrorCategory.InvalidOperation, 
                        null));

            }

        }
    }
}
