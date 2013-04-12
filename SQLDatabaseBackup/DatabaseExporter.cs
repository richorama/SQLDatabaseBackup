using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace Two10.SQLDatabaseBackup
{
    class DatabaseExporter
    {
        private string serverName;
        private string databaseName;
        private string userName;
        private string password;
        private string blob;
        private string key;
        private string managementUri;

        public DatabaseExporter(string serverName, string databaseName, string userName, string password, string blob, string key, string managementUri)
        {
            this.serverName = serverName;
            this.databaseName = databaseName;
            this.userName = userName;
            this.password = password;
            this.blob = blob;
            this.key = key;
            this.managementUri = managementUri;
        }

        public void Export()
        {
            var guid = RequestExport();
            while (!CheckStatus(guid))
            {
                Thread.Sleep(5000);
            }
        }

        /// <summary>
        /// Requests that SQL Azure exports a database to blob storage in BACPAC format.
        /// </summary>
        /// <returns>A GUID representing the job.</returns>
        private Guid RequestExport()
        {
            // Call the REST API, with an XML document containing the job details and credentials.
            // NB This API does not seem to be documented on MSDN and therefore could be subject to change.

            Console.WriteLine("Exporting the database to blob storage");

            var request = WebRequest.Create(managementUri + "/Export");
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

        private bool CheckStatus(Guid guid)
        {
            //https://db3prod-dacsvc.azure.com/DACWebService.svc/Status?servername=nevi5acevs.database.windows.net&username=richard&password=xxx&reqId=402e425c-a77a-4f3e-b541-98e5dfd92e70 HTTP/1.1
            var request = WebRequest.Create(string.Format("{0}/Status?servername={1}&username={2}&password={3}&reqId={4}", managementUri, serverName, userName, password, guid));
            request.ContentType = "application/xml";
            request.Method = "GET";
            using (var response = request.GetResponse())
            {
                var encoding = Encoding.GetEncoding(1252);
                using (var responseStream = new StreamReader(response.GetResponseStream(), encoding))
                {
                    var status = GetStatus(responseStream.ReadToEnd());
                    Console.WriteLine(status);
                    return (status == "Completed");
                }
            }
        }

        // example status responses
        //<ArrayOfStatusInfo xmlns="http://schemas.datacontract.org/2004/07/Microsoft.SqlServer.Management.Dac.ServiceTypes" xmlns:i="http://www.w3.org/2001/XMLSchema-instance"><StatusInfo><BlobUri>https://two10ra.blob.core.windows.net/bacpac/jabbr_copy.bacpac</BlobUri><DatabaseName>jabbr_copy</DatabaseName><ErrorMessage i:nil="true"/><LastModifiedTime>2013-04-12T09:42:31.6091571Z</LastModifiedTime><QueuedTime>2013-04-12T09:42:25.4593721Z</QueuedTime><RequestId>402e425c-a77a-4f3e-b541-98e5dfd92e70</RequestId><RequestType>Export</RequestType><ServerName>nevi5acevs.database.windows.net</ServerName><Status>Completed</Status></StatusInfo></ArrayOfStatusInfo>
        //<ArrayOfStatusInfo xmlns="http://schemas.datacontract.org/2004/07/Microsoft.SqlServer.Management.Dac.ServiceTypes" xmlns:i="http://www.w3.org/2001/XMLSchema-instance"><StatusInfo><BlobUri>https://two10ra.blob.core.windows.net/bacpac/jabbr_copy.bacpac</BlobUri><DatabaseName>jabbr_copy</DatabaseName><ErrorMessage i:nil="true"/><LastModifiedTime>2013-04-12T09:42:25.312403Z</LastModifiedTime><QueuedTime>2013-04-12T09:42:25.4593721Z</QueuedTime><RequestId>402e425c-a77a-4f3e-b541-98e5dfd92e70</RequestId><RequestType>Export</RequestType><ServerName>nevi5acevs.database.windows.net</ServerName><Status>Running, Progress = 90%</Status></StatusInfo></ArrayOfStatusInfo>

        public static string GetStatus(string xml)
        {
            var xdoc = XDocument.Parse(xml);
            return xdoc.Descendants(XName.Get("Status", @"http://schemas.datacontract.org/2004/07/Microsoft.SqlServer.Management.Dac.ServiceTypes")).First().Value;
        }


    }
}
