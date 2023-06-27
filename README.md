# Unity Configuration

A package for reading a configuration file and injecting into a config class.

## Install
Add the package to your manifest.json file located under the Packages folder. 

```
  "dependencies": {
    "com.byrniee.unityconfiguration": "git@github.com:Byrniee/UnityConfiguration.git#v2.0.0",
```

## Usage
Create a `config.json` file in the root of the project (next to the assets folder).
For a build, place next to the .exe file.

```
  {
    "Settings": {
      "VariableOne": "Hello"
    }
  }
```

### Environments
Place a `.env` file next to the config.json file.

| Variable          | Example                       | Description                                                                           |
|-------------------|-------------------------------|---------------------------------------------------------------------------------------|
| UNITY_ENVIRONMENT | UNITY_ENVIRONMENT=Development | Loads the `config.Development.json` file and merges with the base `config.json` file. |