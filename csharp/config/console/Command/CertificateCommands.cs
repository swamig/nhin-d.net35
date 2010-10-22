﻿/* 
 Copyright (c) 2010, Direct Project
 All rights reserved.

 Authors:
    Umesh Madan     umeshma@microsoft.com
  
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
Neither the name of the The Direct Project (nhindirect.org). nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 
*/
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Net.Mail;
using System.ServiceModel;

using DnsResolver;

using Health.Direct.Config.Client;
using Health.Direct.Config.Client.CertificateService;
using Health.Direct.Config.Store;
using Health.Direct.Config.Tools.Command;

using NHINDirect.Certificates;
using NHINDirect.Extensions;

namespace Health.Direct.Config.Console.Command
{
    /// <summary>
    /// Commands to manage certificates
    /// </summary>
    public class CertificateCommands : CommandsBase
    {        
        //---------------------------------------
        //
        // Commands
        //
        //---------------------------------------
      
        /// <summary>
        /// Import a certificate file and add it to the config service store
        /// </summary>
        public void Command_Certificate_Add(string[] args)
        {
            string filePath = args.GetRequiredValue(0);
            string password = args.GetOptionalValue(1, string.Empty);
            
            MemoryX509Store certStore = LoadCerts(filePath, password);            
            PushCerts(certStore, false);
        }        
        public void Usage_Certificate_Add()
        {
            System.Console.WriteLine("Import a certificate from a file and push it into the store.");
            System.Console.WriteLine("    filepath [password]");
            System.Console.WriteLine("\t filePath: path fo the certificate file. Can be .DER, .CER or .PFX");
            System.Console.WriteLine("\t password: (optional) file password");
        }

        /// <summary>
        /// Retrieve a certificate by its ID
        /// </summary>
        public void Command_Certificate_ByID_Get(string[] args)
        {
            long certificateID = args.GetRequiredValue<int>(0);
            CertificateGetOptions options = GetOptions(args, 1);

            this.Print(ConfigConsole.Current.CertificateClient.GetCertificate(certificateID, options));
        }
        public void Usage_Certificate_ByID_Get()
        {
            System.Console.WriteLine("Retrieve a certificate by its id.");
            System.Console.WriteLine("    certificateID [options]");
            System.Console.WriteLine("\t certificateID: ");
            PrintOptionsUsage();
        }
        
        /// <summary>
        /// Get all certificates for an owner
        /// </summary>
        public void Command_Certificate_Get(string[] args)
        {
            string owner = args.GetRequiredValue(0);            
            CertificateGetOptions options = GetOptions(args, 1);
            
            Certificate[] certs = ConfigConsole.Current.CertificateClient.GetCertificatesForOwner(owner, options); 
            this.Print(certs);
        }
        public void Usage_Certificate_Get()
        {
            System.Console.WriteLine("Retrieve all certificates for an owner.");
            System.Console.WriteLine("    owner [options]");
            System.Console.WriteLine("\t owner: Certificate owner");
            PrintOptionsUsage();
        }
        
        /// <summary>
        /// Set the status of a certificate
        /// </summary>
        public void Command_Certificate_Status_Set(string[] args)
        {
            string owner = args.GetRequiredValue(0);
            EntityStatus status = args.GetRequiredEnum<EntityStatus>(1);
            
            ConfigConsole.Current.CertificateClient.SetCertificateStatusForOwner(owner, status);
        }
        public void Usage_Certificate_Status_Set()
        {
            System.Console.WriteLine("Set the status for ALL certificates for an OWNER.");
            System.Console.WriteLine("     owner status");
            System.Console.WriteLine("\t owner: Certificate owner");
            System.Console.WriteLine("\t status: {0}", EntityStatusString);
        }
        
        /// <summary>
        /// Remove certificate
        /// </summary>
        public void Command_Certificate_Remove(string[] args)
        {
            long certificateID = args.GetRequiredValue<long>(0);
            
            ConfigConsole.Current.CertificateClient.RemoveCertificate(certificateID);
        }
        public void Usage_Certificate_Remove()
        {
            System.Console.WriteLine("Remove certificate with given ID");
            System.Console.WriteLine("    certificateID");
        }
        
        /// <summary>
        /// Mirrors what the production gateway does
        /// </summary>
        public void Command_Certificate_Resolve(string[] args)
        {
            MailAddress owner = new MailAddress(args.GetRequiredValue(0));
            CertificateGetOptions options = GetOptions(args, 1);

            Certificate[] certs = ConfigConsole.Current.CertificateClient.GetCertificatesForOwner(owner.Address, options);
            if (ArrayExtensions.IsNullOrEmpty(certs))
            {
                certs = ConfigConsole.Current.CertificateClient.GetCertificatesForOwner(owner.Host, options);
            }
            this.Print(certs);
        }
        public void Usage_Certificate_Resolve()
        {
            System.Console.WriteLine("Resolves certificates for an owner - like the Smtp Gateway would.");
            System.Console.WriteLine("    owner [options]");
            System.Console.WriteLine("\t owner: Certificate owner");
            PrintOptionsUsage();
        }
        
        /// <summary>
        /// Export certs in zone file format
        /// </summary>
        public void Command_Certificate_Export(string[] args)
        {
            string owner = args.GetRequiredValue(0);
            string outputFile = args.GetOptionalValue(1, null);
            
            CertificateGetOptions options = new CertificateGetOptions() { IncludeData = true, IncludePrivateKey = false};
            Certificate[] certs = ConfigConsole.Current.CertificateClient.GetCertificatesForOwner(owner, options);
            if (ArrayExtensions.IsNullOrEmpty(certs))
            {
                System.Console.WriteLine("No certificates found");
                return;
            }            
            ExportCerts(certs, outputFile);
        }        
        public void Usage_Certificate_Export()
        {
            System.Console.WriteLine("Export certificates for an owner in zone file format");
            System.Console.WriteLine("    owner [outputFile]");
            System.Console.WriteLine("\t owner: certificate owner");
            System.Console.WriteLine("\t outputFile: (Optional) Export to file. Else write to Console");
        }

        /// <summary>
        /// Export all Enabled public certificates in zone file format
        /// </summary>
        public void Command_Certificate_Export_All(string[] args)
        {
            string outputFile = args.GetOptionalValue(0, null);
            int chunkSize = args.GetOptionalValue<int>(1, 25);
            
            CertificateGetOptions options = new CertificateGetOptions() { IncludeData = true, IncludePrivateKey = false };
            IEnumerable<Certificate> certs = ConfigConsole.Current.CertificateClient.EnumerateCertificates(chunkSize, options);
            
            ExportCerts(certs, outputFile);
        }
        public void Usage_Certificate_Export_All()
        {
            System.Console.WriteLine("Export all enabled public certificates in zone file FORMAT");
            System.Console.WriteLine("You can place this output directly into your zone file");
            System.Console.WriteLine("     [outputFile] [chunkSize]");
            System.Console.WriteLine("\t outputFile: (Optional) Export to file. Else write to Console");
            System.Console.WriteLine("\t chunkSize: (Optional) Enumeration size. Default is 25");
        }
        
        /// <summary>
        /// Export public certs for private keys in the machine store
        /// </summary>
        /// <param name="args"></param>
        public void Command_Certificate_Export_Machine(string[] args)
        {
            string storeName = args.GetOptionalValue(0, "NHINDPrivate");
            string outputFile = args.GetOptionalValue(1, null);
            using (SystemX509Store store = new SystemX509Store(NHINDirect.Certificates.Extensions.OpenStoreRead(storeName, StoreLocation.LocalMachine), null))
            {
                ExportCerts(store, outputFile);
            }
        }
        public void Usage_Certificate_Export_Machine()
        {
            System.Console.WriteLine("Exports public certificates for all certs in the given store");
            System.Console.WriteLine("    [storeName] [outputFile]");
            System.Console.WriteLine("\t storeName: (optional) Default is NHINDPrivate.");
            System.Console.WriteLine("\t outputFile: (optional) Export to file. Else write to Console");
        }
        
        //---------------------------------------
        //
        // Implementation...
        //
        //---------------------------------------
        
        internal static void ExportCerts(IEnumerable<Certificate> certs, string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                ExportCerts(certs, System.Console.Out, false);
                return;
            }
                        
            using(StreamWriter writer = new StreamWriter(filePath))
            {
                ExportCerts(certs, writer, true);
            }
        }

        internal static void ExportCerts(IEnumerable<Certificate> certs, TextWriter writer, bool isOutputFile)
        {
            foreach (Certificate cert in certs)
            {
                DnsX509Cert dnsCert = new DnsX509Cert(cert.Data);
                dnsCert.Export(writer, cert.Owner);
                writer.WriteLine();
                
                if (isOutputFile)
                {
                    System.Console.WriteLine("{0}, {1}, {2}, {3}", cert.Owner, dnsCert.Name, cert.ValidStartDate, cert.ValidEndDate);
                }
            }
        }

        internal static void ExportCerts(IEnumerable<X509Certificate2> certs, string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                ExportCerts(certs, System.Console.Out, false);
                return;
            }

            using (StreamWriter writer = new StreamWriter(filePath))
            {
                ExportCerts(certs, writer, true);
            }
        }

        internal static void ExportCerts(IEnumerable<X509Certificate2> certs, TextWriter writer, bool isOutputFile)
        {
            foreach (X509Certificate2 cert in certs)
            {
                DnsX509Cert dnsCert = new DnsX509Cert(cert);
                dnsCert.Export(writer, dnsCert.Name);
                writer.WriteLine();

                if (isOutputFile)
                {
                    System.Console.WriteLine(dnsCert.Name);
                }
            }
        }
        
        internal static void PushCerts(IEnumerable<X509Certificate2> certs, bool checkForDupes)
        {
            CertificateStoreClient client = ConfigConsole.Current.CertificateClient;
            foreach (X509Certificate2 cert in certs)
            {
                string owner = cert.ExtractEmailNameOrName();
                try
                {
                    if (!checkForDupes || !client.Contains(cert))
                    {
                        client.AddCertificate(new Certificate(owner, cert));                    
                        System.Console.WriteLine("Added {0}", cert.Subject);
                    }
                    else
                    {
                        System.Console.WriteLine("Exists {0}", cert.Subject);
                    }
                }
                catch (FaultException<ConfigStoreFault> ex)
                {
                    if (ex.Detail.Error == ConfigStoreError.UniqueConstraint)
                    {
                        System.Console.WriteLine("Exists {0}", cert.Subject);
                    }
                }
            }
        }

        internal static void PushCerts(IEnumerable<X509Certificate2> certs, bool checkForDupes, EntityStatus status)
        {
            PushCerts(certs, checkForDupes);
            var owners = (from cert in certs
                          select cert.ExtractEmailNameOrName()).Distinct();
            foreach (string owner in owners)
            {
                ConfigConsole.Current.CertificateClient.SetCertificateStatusForOwner(owner, EntityStatus.Enabled);
            }
        }

        internal static MemoryX509Store LoadCerts(string filePath, string password)
        {
            MemoryX509Store certStore = new MemoryX509Store();
            LoadCerts(certStore, filePath, password, X509KeyStorageFlags.Exportable);
            return certStore;
        }

        internal static MemoryX509Store LoadCerts(string filePath, string password, X509KeyStorageFlags flags)
        {
            MemoryX509Store certStore = new MemoryX509Store();
            LoadCerts(certStore, filePath, password, flags);
            return certStore;
        }

        internal static void LoadCerts(MemoryX509Store certStore, string filePath, string password, X509KeyStorageFlags flags)
        {
            string ext = Path.GetExtension(filePath) ?? string.Empty;
            switch (ext.ToLower())
            {
                default:
                    certStore.ImportKeyFile(filePath, flags);
                    break;

                case ".pfx":
                    certStore.ImportKeyFile(filePath, password, flags);
                    break;
            }
        }
        
        internal static CertificateGetOptions GetOptions(string[] args, int firstArg)
        {
            CertificateGetOptions options = new CertificateGetOptions();
            options.IncludeData = args.GetOptionalValue<bool>(firstArg, false);
            options.IncludePrivateKey = args.GetOptionalValue<bool>(firstArg + 1, false);
            return options;
        }

        internal static void PrintOptionsUsage()
        {
            System.Console.WriteLine("\t options:");
            System.Console.WriteLine("\t [certData] [privatekey]");
            System.Console.WriteLine("\t certData: (True/False) Fetch certificate data");
            System.Console.WriteLine("\t privateKey: (True/False) Include private key");
        }
                
        void Print(Certificate[] certs)
        {
            if (certs == null || certs.Length == 0)
            {
                System.Console.WriteLine("No certificates found");
                return;
            }

            foreach (Certificate cert in certs)
            {
                this.Print(cert);
                CommandUI.PrintSectionBreak();
            }
        }
        
        void Print(Certificate cert)
        {
            CommandUI.Print("Owner", cert.Owner);
            CommandUI.Print("Thumbprint", cert.Thumbprint); 
            CommandUI.Print("ID", cert.ID);
            CommandUI.Print("CreateDate", cert.CreateDate);
            CommandUI.Print("ValidStart", cert.ValidStartDate);
            CommandUI.Print("ValidEnd", cert.ValidEndDate);
            CommandUI.Print("Status", cert.Status);
            
            if (cert.HasData)
            {
                X509Certificate2 x509 = cert.ToX509Certificate();
                Print(x509);
            }
        }
                
        internal static void Print(X509Certificate2Collection certs)
        {
            if (CollectionExtensions.IsNullOrEmpty(certs))
            {
                System.Console.WriteLine("No certificates found");
                return;
            }
            
            foreach(X509Certificate2 cert in certs)
            {
                Print(cert);
                CommandUI.PrintSectionBreak();
            }
        }        
        
        internal static void Print(X509Certificate2 x509)
        {
            CommandUI.Print("Subject", x509.Subject);
            CommandUI.Print("SerialNumber", x509.SerialNumber);
            CommandUI.Print("Issuer", x509.Issuer);
            CommandUI.Print("HasPrivateKey", x509.HasPrivateKey);
        }
    }
}