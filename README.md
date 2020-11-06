# Unity Configuration

A package for reading a confiruration file and injecting into a config class.

### Install
Add the package to your manifest.json file located under the Packages folder. 

```
  "dependencies": {
    "com.byrniee.unityconfiguration": "git@github.com:Byrniee/UnityConfiguration.git",
```

### Usage
Create a `config.json` file in the root of the project (next to the assets folder).
For a build, place next to the .exe file.

```
  {
    "Settings": {
      "VariableOne": "Hello"
    }
  }
```
