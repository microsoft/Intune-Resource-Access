using Microsoft.Intune.EncryptionUtilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace TestWindowsService
{
    public partial class IntunePFXCertConnectorValidationService : ServiceBase
    {
        public const string ServiceFriendlyName = "PFXImportTestService";
        public const string LogSourceName = "PFXImportTestServiceSource";
        public const string LogName = "PFXImportTestServiceLog";
        public const string TestFileName = "PFXImportTestFile.txt";
        public const string ResultFile = "PFXImportTestResultsMarker.txt";
        public const char TestFileSeparator = ':';

        public IntunePFXCertConnectorValidationService()
        {
            InitializeComponent();

            if (!EventLog.SourceExists(LogSourceName))
            {
                EventLog.CreateEventSource(LogSourceName, LogName);
            }
            eventLog1.Source = LogSourceName;
            eventLog1.Log = LogName;
        }

        protected override void OnStart(string[] args)
        {
            eventLog1.WriteEntry(string.Format("Starting {0}", ServiceFriendlyName));

            System.Timers.Timer periodicTimer = new System.Timers.Timer();
            periodicTimer.Interval = 60000; // 60 seconds
            periodicTimer.Elapsed += new System.Timers.ElapsedEventHandler(this.OnPeriodicTimer);
            periodicTimer.Start();

            string assemblyPath = Path.GetDirectoryName(typeof(IntunePFXCertConnectorValidationService).Assembly.Location);
            File.Delete(Path.Combine(assemblyPath, ResultFile));

        }

        public void OnPeriodicTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            string assemblyPath = Path.GetDirectoryName(typeof(IntunePFXCertConnectorValidationService).Assembly.Location);
            string pathToTest = Path.Combine(assemblyPath, TestFileName);

            if (File.Exists(pathToTest))
            {
                eventLog1.WriteEntry(string.Format("Opening test file at {0}", pathToTest));

            }
            else
            {
                eventLog1.WriteEntry("Failed to find test file, sleeping");
            }
            try
            {

                OpenAndTestFile(pathToTest);
            }
            catch (Exception e)
            {
                eventLog1.WriteEntry(string.Format("Genral Failure with exception {0}", e), EventLogEntryType.Error);

            }
        }

        private void OpenAndTestFile(string pathToFile)
        {
            string[] testLines = File.ReadAllLines(pathToFile);
            eventLog1.WriteEntry(string.Format("Test Lines Length:{0}", testLines.Length));
            foreach (string line in testLines)
            {
                eventLog1.WriteEntry(string.Format("Current Line:{0}", line));
                TestLine(line);
            }
        }

        public void TestLine(string line)
        {
            eventLog1.WriteEntry(string.Format("Testing Line:{0}", line));
            string[] split = line.Split(TestFileSeparator);
            if(split.Length != 6)
            {
                StringBuilder testFormatBuilder = new StringBuilder();
                testFormatBuilder.AppendLine(string.Format("Test line is of incorrect format, should be of format unencrypted{0}encrypted{0}providername{0}keyname{0}hashalgorithm{0}paddingflags for line {1}", TestFileSeparator, line));
                testFormatBuilder.AppendLine(string.Format("Encrypted value should be Base64 encoded"));
                testFormatBuilder.AppendLine(string.Format("HashAlgorithm can be SHA1, SHA256, SHA384, SHA512"));
                testFormatBuilder.AppendLine(string.Format("PaddingFlags can be 1, 2, 4, 8"));
                eventLog1.WriteEntry(testFormatBuilder.ToString(), EventLogEntryType.Error);
                return;
            }

            string unencrypted = split[0];
            string encrypted = split[1];
            string provider = split[2];
            string keyname = split[3];
            string hashAlgorithm = split[4];
            int paddingFlags = int.Parse(split[5]);


            byte[] encryptedAsBytes = Convert.FromBase64String(encrypted);

            string resultString = "";
            EventLogEntryType entryType = EventLogEntryType.Information;
            ManagedRSAEncryption crypto = new ManagedRSAEncryption();
            try
            {
                byte[] decrypted = crypto.DecryptWithLocalKey(provider, keyname, encryptedAsBytes, hashAlgorithm, paddingFlags);
                string decryptedString = Encoding.ASCII.GetString(decrypted);

                if(String.Equals(unencrypted, decryptedString))
                {
                    resultString = string.Format("Successfully decrypted to value {0} for line {1}", unencrypted, line);
                }
                else
                {
                    resultString = string.Format("Failed to decrypt to value {0}, decrypted value was {1} for line {2}", unencrypted, decryptedString, line);
                    entryType = EventLogEntryType.Error;
                }
            }
            catch(Exception e)
            {
                resultString = string.Format("Failed to decrypt line with exception {0} the line was: {1}", e, line);
                entryType = EventLogEntryType.Error;
            }

            eventLog1.WriteEntry(resultString, entryType);
            string assemblyPath = Path.GetDirectoryName(typeof(IntunePFXCertConnectorValidationService).Assembly.Location);
            eventLog1.WriteEntry(string.Format("Wrinting results to {0}", Path.Combine(assemblyPath, ResultFile)));
            File.WriteAllText(Path.Combine(assemblyPath, ResultFile), contents: resultString);
        }

        protected override void OnStop()
        {
            eventLog1.WriteEntry(string.Format("Stopping {0}", ServiceFriendlyName));
        }
    }
}
