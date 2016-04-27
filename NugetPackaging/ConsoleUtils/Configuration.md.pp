# $rootnamespace$

Application description

## How to run

The application parameters can set in 3 ways:

1. command line arguments
2. external file with json configuration object (local or web)
3. local configuration file (app.config or web.config)


If there is conflicting arguments, the priority for conflict resolutions is:

**Priority:** *command line arguments* **>** *external file* **>** *app.config*


### app.config or web.config
Just set the desired parameters in the `<appSettings>` area.

Example

```
  <appSettings>
    <add key="logFilename" value="${basedir}/log/myLogFile.log" />
    <add key="logLevel" value="Info" />
  </appSettings>
```


### command line arguments
Each parameter can overwrite the default value or the value in the app.config by passing as a command line parameter.

Example

```
  CacheExporter.exe --logLevel="Info" --logFilename="log.txt"
```

**Note:** The help can also be summoned by passing the argument `--help`


### external file with json format parameters (local or web)

See [config](#config) parameter.

This parameter can be passed in the app.config or as a command line argument.
Address to an external file with a json format configuration options. At service start up the file will be loaded and parsed.

**Note:** This configuration takes precedence over the configurations found in the appSettings area of app.config file. 

External file format

```
{
    logLeve: "Info",
    logFilename: "log.txt"
}
```


Example

```
  CacheExporter.exe --config="http://my.application.com/my_custom_config_file.json" --logFilename="log.txt"
```

**Note:** The help can also be summoned by passing the argument `--help`




## List of Parameters

### logFilename
`logFilename=<string>`

Default value: `${basedir}/log/ListAndDeleteS3Files.log`


### logLevel
`logLevel=<string>`

Default value: `Info`

* Trace
* Debug
* Info
* Warn
* Error
* Fatal
* Off


### config
`config=<string>`

Default value: `empty`

This parameter can be passed in the app.config or as a command line argument.

Address to an external file with a json format configuration options. At service start up the file will be loaded and parsed.
**Note:** This configuration takes precedence over the configurations found in the appSettings area of app.config file.

This address could be a local file system location or a web location. Examples:
* `http://somewhere.com/myconfiguration.json`
* `./myconfiguration.json`
* `c:\my configuration files\myconfiguration.json`


Also, a list of file can be provided as comma separated values. Example: 

```
"config": "http://somewhere.com/myconfiguration.json, c:\my configuration files\myconfiguration.json"
```

File format example

```
{
    "storageModule": "MongoDbStorageModule",
    "storageConnectionString": "..."
}
```


### configAbortOnError
`configAbortOnError=<boolean>`

Default value: `true`

If the server should tolerate and ignore external file configuration load or parse errors.


### waitForKeyBeforeExit
`waitForKeyBeforeExit=<boolean>`

Default value: `false`

If the console application should ask for user input at the end, before exiting the application.



