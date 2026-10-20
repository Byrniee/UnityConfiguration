# Unity Configuration

A package for reading a configuration file and injecting into a config class.

## Install
Add the package to your manifest.json file located under the Packages folder. 

```
  "dependencies": {
    "com.byrniee.unityconfiguration": "git@github.com:Byrniee/UnityConfiguration.git#v3.0.0",
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

Each top-level key is a section, read with `Create<T>`:

```
IConfigFactory factory = new ConfigFactory();
IConfig<Settings> settings = factory.Create<Settings>("Settings");
```

Config files may contain `//` and `/* */` comments and unquoted property names.

### Environments
The environment is read from `UNITY_ENVIRONMENT`, in this order of precedence:

1. The `UNITY_ENVIRONMENT` process environment variable, if set.
2. The `UNITY_ENVIRONMENT` entry in a `.env` file next to the config.json file.

| Variable          | Example                       | Description                                                                           |
|-------------------|-------------------------------|---------------------------------------------------------------------------------------|
| UNITY_ENVIRONMENT | UNITY_ENVIRONMENT=Development | Loads the `config.Development.json` file and merges with the base `config.json` file. |

#### Layers
The environment is split on `.` into layers, each merged on top of the previous one. For
`UNITY_ENVIRONMENT=Manchester.Strongman` the files are applied in this order:

1. `config.json`
2. `config.Manchester.json`
3. `config.Manchester.Strongman.json`

Any file that doesn't exist is skipped, so shared venue settings can live in `config.Manchester.json` and
each hole file only contains what is specific to that hole.

#### Merging
Layers are merged value by value, so a layer only needs the values it changes:

- Objects are merged recursively. A key missing from a layer keeps the value from the earlier layers.
- Arrays are replaced, not appended.
- An explicit `null` overwrites the earlier value. A section that ends up `null` is treated as missing.

### Diagnostics
`ConfigFactory` implements `IConfigDiagnostics`, which can be logged at startup:

```
IConfigDiagnostics diagnostics = factory;
Debug.Log($"Environment: {diagnostics.EnvironmentName}, files: {string.Join(", ", diagnostics.LoadedFiles)}");

// After all config has been requested:
IReadOnlyCollection<string> unused = diagnostics.GetUnrequestedSections();
if (unused.Count > 0)
{
    Debug.LogWarning($"Config sections never requested: {string.Join(", ", unused)}");
}
```

- `LoadedFiles` lists the full paths of the files that were found and applied, in order.
- `GetUnrequestedSections()` returns the top-level sections that were loaded but never passed to
  `Create<T>`. The library doesn't log this itself.

### Builds
After a build, every `config*.json` file in the project root is copied next to the built player. The `.env`
file is not copied: set `UNITY_ENVIRONMENT` on the target machine, or deploy a `.env` file with the build.

### Tests
EditMode tests live in `Tests/Editor`. To run them from a project that consumes the package, add it to
`testables` in that project's `Packages/manifest.json`:

```
  "testables": [ "com.byrniee.unityconfiguration" ]
```
