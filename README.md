# TessBox.DotNet.MySql
Extension for the mysql connection


# SQL Files

## Migration files

Include as an embedded resource

```xml
  <ItemGroup>
    <EmbeddedResource Include="Migrations/*.sql" />
  </ItemGroup>
````

 Migration file format : 
 <version>_<name>.sql

 Ex :
 001_Test.sql

 ## Custom scripts files

 Don't start with number 
The name should be unique

 Ex:
 Test.sql

# Test
Ensure docker is installed
Remove if exist the image mysql/database/test

Build image, inside test directory
```
docker build . -t mysql/database/test
````

Run Docker compose, inside test directory
```
docker compose up
```

Run The Example

Run Test
