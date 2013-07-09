Windows Azure SQL Database Copy Utility
=======================================

This utility will take a copy of your SQL Database, and once the copy has completed, will make a backup of the database to blob storage using the bacpac format.

Please supply for following command line arguments:

```
    -server [SQL Database server (without .database.windows.net)]
    -database [database to back up]
    -databasecopy (optional) [The name of the temporary copy database, defaults to database_copy]
    -user [SQL Database username]
    -pwd [SQL Database password]
    -storagename [Blob Storage account name]
    -storagekey [Blob Storage account key]
    -container (optional) [Blob storage container to use, defaults to sqlbackup]
    -datacenter [The data center where the database (not storage account) is located]
        (westeurope | southeastasia | eastasia | northcentralus | northeurope | southcentralus | eastus | westus)
```

Example usage:

```
SQLDatabaseBackup.exe 
    -server nevixxs 
    -database mydb 
    -user username 
    -pwd password 
    -storagename storageaccount 
    -storagekey dmASdd1mg/qPeOgGmCkO333L26cNcnUA1uMcSSOFM... 
    -datacenter eastasia
```

### How it works

The backup process is not transactionally consistent, so the utility creates a copy of your database, and then uses the Azure backup API to copy that database to a bacpac file in Blob Storage.

The database copy will be deleted when the utilty starts, if it already exists.

Please ensure that a 'databasecopy' container exists in your storage account, otherwise the backup API will throw an error.

The bacpac file will have the date and time of the backup appended to the name, allowing you to keep a history of backups.

### License

MIT