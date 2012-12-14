using System;
using System.Data.SqlClient;
using System.Threading;

namespace Two10.SQLDatabaseBackup
{
    public class DatabaseCopier : IDisposable
    {
        private SqlConnection connection;

        public DatabaseCopier(SqlConnection connection)
        {
            this.connection = connection;
            this.connection.Open();
        }


        public void Copy(string database, string newDatabase)
        {
            DropDatabase(newDatabase);
            CopyDatabase(database, newDatabase);
            while (!CheckDatabaseCopied(newDatabase))
            {
                Thread.Sleep(5000);
            }
        }


        private void CopyDatabase(string database, string newDatabase)
        {
            var command = connection.CreateCommand();
            command.CommandText = string.Format(@"CREATE DATABASE {0} AS COPY OF {1};", newDatabase, database);
            command.ExecuteNonQuery();
        }


        private bool CheckDatabaseCopied(string database)
        {
            var command = connection.CreateCommand();
            command.CommandText = string.Format(@"select count(*) from sys.databases where state = 0 and name = '{0}'", database);
            return 1 == (int)command.ExecuteScalar();
        }


        private void DropDatabase(string database)
        {
            var command = connection.CreateCommand();
            command.CommandText = string.Format(@"select count(*) from sys.databases where name = '{0}'", database);
            if ((int)command.ExecuteScalar() == 1)
            {
                var dropCommand = connection.CreateCommand();
                dropCommand.CommandText = string.Format("DROP DATABASE '{0}'", database);
            }
        }


        public void Dispose()
        {
            if (null != connection)
            {
                connection.Close();
            }
        }
    }
}
