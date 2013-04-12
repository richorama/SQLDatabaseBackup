using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;

namespace Two10.SQLDatabaseBackup
{
    class Program
    {

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine(@"
Windows Azure SQL Database Copy Utility

This utility will take a copy of your SQL Database, and once the copy has completed, will make a backup of the database to blob storage using the bacpac format.

Please supply for following command line arguments:

    -server [SQL Database server (without .database.windows.net)]
    -database [database to back up]
    -databasecopy (optional) [The to use as for the temporary copy]
    -user [SQL Database username]
    -pwd [SQL Database password]
    -storagename [Blob Storage account name]
    -storagekey [Blob Storage account key]
    -datacenter [The data centre that both the database and storage account are located]
        (westeurope | southeastasia | eastasia | northcentralus | northeurope | southcentralus)

Example usage:

SQLDatabaseBackup.exe 
    -server nevixxs 
    -database mydb 
    -user username 
    -pwd password 
    -storagename storageaccount 
    -storagekey dmASdd1mg/qPeOgGmCkO333L26cNcnUA1uMcSSOFM... 
    -datacenter eastasia
");
                return;
            }

            if (!CheckSwitches("-server", "-database", "-user", "-pwd", "-storagename", "-storagekey", "-datacenter"))
            {
                return;
            }

            var server = GetSwitch("-server");         // i.e. the first part of xxx.database.windows.net
            var database = GetSwitch("-database");     // name of the database you want to back up
            var backupDatabase = database + "_copy";         // name for the backup database (it will create)
            if (GetSwitch("-databasecopy") != null)
            {
                backupDatabase = GetSwitch("-databasecopy");
            }
            var username = GetSwitch("-user");           // database username
            var password = GetSwitch("-pwd");            // database password
            var blobAccount = GetSwitch("-storagename"); // storage account
            var blobKey = GetSwitch("-storagekey");     // storage key

            var dataCenterUri = ResolveUri(GetSwitch("-datacenter"));
            if (string.IsNullOrWhiteSpace(dataCenterUri))
            {
                return;
            }

            using (var copier = new DatabaseCopier(CreateConnection(server, database, username, password)))
            {
                copier.Copy(database, backupDatabase);
            }

            var guid = Export(server + ".database.windows.net", backupDatabase, username, password, string.Format("https://{0}.blob.core.windows.net/sqlbackup/backup.bacpac", blobAccount), blobKey, dataCenterUri);

            Console.WriteLine(guid);

        }

        /// <summary>
        /// Requests that SQL Azure exports a database to blob storage in BACPAC format.
        /// </summary>
        /// <returns>A GUID representing the job.</returns>
        static Guid Export(string serverName, string databaseName, string userName, string password, string blob, string key, string managementUri)
        {
            // Call the REST API, with an XML document containing the job details and credentials.
            // NB This API does not seem to be documented on MSDN and therefore could be subject to change.

            var request = WebRequest.Create(managementUri);
            request.Method = "POST";

            var dataStream = request.GetRequestStream();
            var body = String.Format("<ExportInput xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.SqlServer.Management.Dac.ServiceTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><BlobCredentials i:type=\"BlobStorageAccessKeyCredentials\"><Uri>{0}</Uri><StorageAccessKey>{1}</StorageAccessKey></BlobCredentials><ConnectionInfo><DatabaseName>{2}</DatabaseName><Password>{3}</Password><ServerName>{4}</ServerName><UserName>{5}</UserName></ConnectionInfo></ExportInput>", blob, key, databaseName, password, serverName, userName);
            var utf8 = new UTF8Encoding();
            var buffer = utf8.GetBytes(body);
            dataStream.Write(buffer, 0, buffer.Length);

            dataStream.Close();
            request.ContentType = "application/xml";

            // The HTTP response contains the job number, a Guid serialized as XML
            using (var response = request.GetResponse())
            {
                var encoding = Encoding.GetEncoding(1252);
                using (var responseStream = new StreamReader(response.GetResponseStream(), encoding))
                {
                    using (var reader = XmlDictionaryReader.CreateTextReader(responseStream.BaseStream, new XmlDictionaryReaderQuotas()))
                    {
                        var serializer = new DataContractSerializer(typeof(Guid));
                        return (Guid)serializer.ReadObject(reader, true);

                    }
                }
            }

        }

        private static SqlConnection CreateConnection(string server, string database, string username, string password)
        {
            return new SqlConnection(string.Format(@"Server=tcp:{0}.database.windows.net,1433;Database=master;User ID={2}@{0};Password={3};Trusted_Connection=False;Encrypt=True;", server, database, username, password));
        }

        public static string GetSwitch(string name)
        {
            if (null == name) throw new ArgumentNullException("name");
            var args = Environment.GetCommandLineArgs();

            var argsList = new List<string>(args.Select(x => x.ToLower()));
            var index = argsList.IndexOf(name.ToLower());
            if (index == -1)
            {
                return null;
            }
            if (args.Length < index + 2)
            {
                return null;
            }
            return args[index + 1];
        }

        public static bool CheckSwitches(params string[] argNames)
        {
            var returnVal = true;
            foreach (var arg in argNames)
            {
                if (GetSwitch(arg) == null)
                {
                    Console.WriteLine("Please supply the \"" + arg + "\" argument");
                    returnVal = false;
                }
            }
            return returnVal;
        }

        public static string ResolveUri(string dcName)
        {
            switch (dcName)
            {
                case "westeurope":
                    return "https://am1prod-dacsvc.azure.com/DACWebService.svc";
                case "southeastasia":
                    return "https://sg1prod-dacsvc.azure.com/DACWebService.svc";
                case "eastasia":
                    return "https://hkgprod-dacsvc.azure.com/DACWebService.svc";
                case "northcentralus":
                    return "https://ch1prod-dacsvc.azure.com/DACWebService.svc";
                case "northeurope":
                    return "https://db3prod-dacsvc.azure.com/DACWebService.svc";
                case "southcentralus":
                    return "https://sn1prod-dacsvc.azure.com/DACWebService.svc";
                case "westus":
                case "eastus":
                    Console.WriteLine("Datacenter uri not known");
                    return null;
                default:
                    Console.WriteLine("Unknown datacenter");
                    return null;
            }
        }


    }
}


