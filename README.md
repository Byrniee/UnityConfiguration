# Unity Configuration

A package for reading a configuration file and injecting into a config class.

## Install
Add the package to your manifest.json file located under the Packages folder. 

```
  "dependencies": {
    "com.byrniee.unityconfiguration": "git@github.com:Byrniee/UnityConfiguration.git#v3.1.0",
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

### Secrets and overrides
Values that shouldn't be committed, such as API keys, can be set in the `.env` file or as process
environment variables instead of in a config file. The key is the path to the value, with `__` between each
level:

```
  // config.json (committed)
  {
    "ApiSettings": {
      "BaseUrl": "https://host.docker.internal:5601",
      "ApiKey": ""
    }
  }
```

```
# .env (not committed)
UNITY_ENVIRONMENT=Development
ApiSettings__ApiKey=my-secret-key
```

`Create<ApiSettings>("ApiSettings")` then returns the key from `.env`, with `BaseUrl` from `config.json`.

Overrides are applied after every config file, in this order of precedence:

1. Process environment variables.
2. Entries in the `.env` file.
3. The merged config files.

- Only keys containing `__` are applied, so variables such as `PATH` are ignored. Keys with an empty segment
  (`__COMPAT_LAYER`, `Server__`) are also ignored.
- Key matching is case-insensitive, so `apisettings__apikey` overrides the existing `ApiKey`.
- A path that doesn't exist in the config is created.
- Values are strings, converted to the property type when the section is read (`Server__Port=81` sets an
  `int`).
- A numeric segment indexes into an existing array: `Hosts__0=a` replaces the first item. On an object, it is
  a normal key, so it works with dictionaries such as `Dictionary<int, int>`.

Add `.env` to the consuming project's `.gitignore`. Committing a `.env.example` with the keys and blank
values shows others which secrets the project needs.

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
- `OverriddenKeys` lists the `.env` and environment variable keys that were applied as overrides. Values are
  not included, so it is safe to log.
- `GetUnrequestedSections()` returns the top-level sections that were loaded but never passed to
  `Create<T>`. The library doesn't log this itself.

### Builds
After a build, every `config*.json` file in the project root is copied next to the built player. The `.env`
file is not copied, so secrets aren't put into the build output: set `UNITY_ENVIRONMENT` and any overrides
as environment variables on the target machine, or deploy a `.env` file separately next to the build.

### Tests
EditMode tests live in `Tests/Editor`. To run them from a project that consumes the package, add it to
`testables` in that project's `Packages/manifest.json`:

```
  "testables": [ "com.byrniee.unityconfiguration" ]
```
