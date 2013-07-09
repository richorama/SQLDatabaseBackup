using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

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
    -databasecopy (optional) [The to use for the temporary copy]
    -user [SQL Database username]
    -pwd [SQL Database password]
    -storagename [Blob Storage account name]
    -storagekey [Blob Storage account key]
    -container (optional) [Blob storage container to use, defaults to sqlbackup]
    -datacenter [The data centre that both the database and storage account are located]
        (westeurope | southeastasia | eastasia | northcentralus | northeurope | southcentralus | eastus | westus)

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
            var container = "sqlbackup";
            if (GetSwitch("-container") != null)
            {
                container = GetSwitch("-container");
            }

            var dataCenterUri = ResolveUri(GetSwitch("-datacenter"));
            if (string.IsNullOrWhiteSpace(dataCenterUri))
            {
                return;
            }

            using (var copier = new DatabaseCopier(CreateConnection(server, username, password)))
            {
                copier.Copy(database, backupDatabase);
            }

            var blobName = database + "-backup-" + DateTime.UtcNow.ToString("yyyy-MM-dd_hh-mm");

            var exporter = new DatabaseExporter(server + ".database.windows.net", backupDatabase, username, password, string.Format("https://{0}.blob.core.windows.net/{1}/{2}.bacpac", blobAccount, container, blobName), blobKey, dataCenterUri);
            exporter.Export();
            Console.WriteLine("Database backed up to {0}/{1}", container, blobName);
        }


        private static SqlConnection CreateConnection(string server, string username, string password)
        {
            return new SqlConnection(string.Format(@"Server=tcp:{0}.database.windows.net,1433;Database=master;User ID={1}@{0};Password={2};Trusted_Connection=False;Encrypt=True;", server, username, password));
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
                    return "https://by1prod-dacsvc.azure.com/DACWebService.svc";
                case "eastus":
                    return "https://bl2prod-dacsvc.azure.com/DACWebService.svc";
                default:
                    Console.WriteLine("Unknown datacenter");
                    return null;
            }
        }


    }
}


