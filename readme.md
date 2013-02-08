Windows Azure SQL Database Copy Utility
=======================================

This utility will take a copy of your SQL Database, and once the copy has completed, will make a backup of the database to blob storage using the bacpac format.

Please supply for following command line arguments:

```
    -server [The name of your SQL Database server (without .database.windows.net)]
    -database [The name of your database]
    -databasecopy (optional) [The to use as for the temporary copy]
    -user [SQL Database username]
    -pwd [SQL Database password]
    -storagename [Blob Storage account name]
    -storagekey [Blob Storage account key]
    -datacenter [The data centre that both the database and storage account are located]
        (westeurope | southeastasia | eastasia | northcentralus | northeurope | southcentralus)
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