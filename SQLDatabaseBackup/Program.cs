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

            string server = GetSwitch(args, "-server");         // i.e. the first part of xxx.database.windows.net
            string database = GetSwitch(args, "-database");     // name of the database you want to back up
            string backupDatabase = database + "-copy";         // name for the backup database (it will create)
            if (GetSwitch(args, "-databasecopy") != null)
            {
                backupDatabase = GetSwitch(args, "-databasecopy");
            }
            string username = GetSwitch(args, "-user");           // database username
            string password = GetSwitch(args, "-pwd");            // database password
            string blobAccount = GetSwitch(args, "-storagename"); // storage account
            string blobKey = GetSwitch(args, "-storagekey");     // storage key


            using (var copier = new DatabaseCopier(CreateConnection(server, database, username, password)))
            {
                copier.Copy(database, backupDatabase);
            }

            Guid guid = Export(server + ".database.windows.net", backupDatabase, username, password, string.Format("https://{0}.blob.core.windows.net/sqlbackup/backup.bacpac", blobAccount), blobKey);

            Console.WriteLine(guid);

        }

        /// <summary>
        /// Requests that SQL Azure exports a database to blob storage in BACPAC format.
        /// </summary>
        /// <returns>A GUID representing the job.</returns>
        static Guid Export(string serverName, string databaseName, string userName, string password, string blob, string key)
        {
            // Call the REST API, with an XML document containing the job details and credentials.

            // NB This API does not seem to be documented on MSDN and therefore could be subject to change.


            /*
            You must choose the right endpoint for your database location:
            North Central US	https://ch1prod-dacsvc.azure.com/DACWebService.svc
            South Central US	https://sn1prod-dacsvc.azure.com/DACWebService.svc
            North Europe	https://db3prod-dacsvc.azure.com/DACWebService.svc
            West Europe	https://am1prod-dacsvc.azure.com/DACWebService.svc
            East Asia	https://hkgprod-dacsvc.azure.com/DACWebService.svc
            Southeast Asia	https://sg1prod-dacsvc.azure.com/DACWebService.svc
            */

            var request = WebRequest.Create("https://db3prod-dacsvc.azure.com/DACWebService.svc/Export");

            request.Method = "POST";

            Stream dataStream = request.GetRequestStream();

            string body = String.Format("<ExportInput xmlns=\"http://schemas.datacontract.org/2004/07/Microsoft.SqlServer.Management.Dac.ServiceTypes\" xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\"><BlobCredentials i:type=\"BlobStorageAccessKeyCredentials\"><Uri>{0}</Uri><StorageAccessKey>{1}</StorageAccessKey></BlobCredentials><ConnectionInfo><DatabaseName>{2}</DatabaseName><Password>{3}</Password><ServerName>{4}</ServerName><UserName>{5}</UserName></ConnectionInfo></ExportInput>", blob, key, databaseName, password, serverName, userName);

            System.Text.UTF8Encoding utf8 = new System.Text.UTF8Encoding();
            byte[] buffer = utf8.GetBytes(body);
            dataStream.Write(buffer, 0, buffer.Length);

            dataStream.Close();
            request.ContentType = "application/xml";

            // The HTTP response contains the job number, a Guid serialized as XML
            using (WebResponse response = request.GetResponse())
            {
                Encoding encoding = Encoding.GetEncoding(1252);
                using (var responseStream = new StreamReader(response.GetResponseStream(), encoding))
                {
                    using (XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(responseStream.BaseStream, new XmlDictionaryReaderQuotas()))
                    {
                        DataContractSerializer serializer = new DataContractSerializer(typeof(Guid));
                        return (Guid)serializer.ReadObject(reader, true);

                    }
                }
            }

        }

        private static SqlConnection CreateConnection(string server, string database, string username, string password)
        {
            return new SqlConnection(string.Format(@"Server=tcp:{0}.database.windows.net,1433;Database=master;User ID={2}@{0};Password={3};Trusted_Connection=False;Encrypt=True;", server, database, username, password));
        }

        public static string GetSwitch(string[] args, string name)
        {
            if (null == args) throw new ArgumentNullException("args");
            if (null == name) throw new ArgumentNullException("name");

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


    }
}