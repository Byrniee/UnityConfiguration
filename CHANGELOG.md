# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [3.1.0] - 2026/10/20
### Added
- Overrides from the `.env` file and process environment variables, for values such as API keys that
  shouldn't be committed. `ApiSettings__ApiKey=secret` sets `ApiKey` in the `ApiSettings` section. They are
  applied after every config file, and process environment variables take precedence over `.env` entries.
  Only keys containing `__` are applied, matching is case-insensitive, missing paths are created, and a
  numeric segment indexes into an array.
- `IConfigDiagnostics.OverriddenKeys`: the override keys that were applied, without their values.
  Implementations of `IConfigDiagnostics` outside this package need to add it.

## [3.0.0] - 2026/10/19
### Breaking
- Environment config is now **merged value by value** instead of replacing whole sections. A key missing
  from an environment file now **inherits the base value** instead of disappearing. Previously
  `config.Staging.json` containing `{ "Server": { "Host": "x" } }` dropped every other `Server` value;
  now only `Server.Host` changes. Arrays are still replaced (not appended), and an explicit `null`
  overwrites the base value.
- Config files are read with dates left as strings, so a date-like string value reaches a `string`
  property exactly as written instead of being reformatted.

### Added
- Layered environments: `UNITY_ENVIRONMENT=Manchester.Strongman` loads `config.json`,
  `config.Manchester.json` and `config.Manchester.Strongman.json` in order, skipping any that don't exist.
- The `UNITY_ENVIRONMENT` process environment variable takes precedence over the `.env` file.
- `IConfigDiagnostics` (implemented by `ConfigFactory`): `EnvironmentName`, `LoadedFiles` (the layer files
  applied, in order) and `GetUnrequestedSections()` for spotting unused config sections.
- `ConfigEnvironment` helper for resolving the environment and its layer file names.
- `ConfigFactory(string configDirectory)` constructor.
- EditMode tests.

### Changed
- Parse errors now include the path of the config file that failed.
- `.env` parsing ignores blank lines and `#` comments, allows `=` in values and strips surrounding quotes.

## [1.3.0] - 2023/06/27
### Changed
- Updated to work closer to the appsettings.json setup in a ASP.Net Core application.

## [1.3.0] - 2021/08/09
### Added
- .env file support.

### Changed
- Fixed error in post build copy.

## [1.2.0] - 2021/01/20
### Added
- Dependency on UnityJson package.

### Changed
- Updated readme file.

### Removed
- Newtonsoft JSON dll.

## [1.1.0] - 2020/11/06
### Added
- Added post build script to copy the config.json file to the build directory.

### Changed
- Updated readme file.

## [1.0.1] - 2020/10/08
### Changed
- Exception no longer thrown when an entry does not exist in the config.json file.

## [1.0.0] - 2020/10/07
### Added
- Initial version

### Changed

### Removed