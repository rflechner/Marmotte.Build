# generate-dotnet

Generates the typed `Dotnet` API (`src/Marmotte.Build/Dotnet/Dotnet.<Group>.cs`) from the help of the installed .NET SDK.

It is a [file-based app](https://learn.microsoft.com/dotnet/core/sdk/file-based-apps): run it after installing a new
SDK, to pick up new commands and options:

```bash
dotnet run tools/generate-dotnet/Generate.cs
```

Then review the diff, build and run the tests (`dotnet test`).

## How it works

1. **Help dumps.** For every command declared in [`Specs.cs`](Specs.cs), the generator runs
   `dotnet <command> --help` in English, from an empty temporary directory. `dotnet test` is read twice: once in
   VSTest mode (`Dotnet.TestWithVSTest`) and once with Microsoft.Testing.Platform selected in `global.json`
   (`Dotnet.Test`), since the two modes have different options.
2. **Options.** Each option of the help becomes a property of the command's options record, with a type inferred
   from the help:

   | Help | Property type |
   | --- | --- |
   | no value (`--no-restore`) | `bool` |
   | a `--x` / `--no-x` pair | `bool?` with `FalseName` |
   | `<LEVEL>` or `--verbosity` | `Verbosity?` |
   | `<NUMBER>`, `--take`, `--skip`, `--timeout` | `int?` |
   | a path, directory or file (`<OUTPUT_DIR>`, `--project`, ...) | `PathInfo?` |
   | `-e, --environment <NAME="VALUE">` | `ImmutableDictionary<string, string>` |
   | anything else, including enumerated values (`<json\|table>`) | `string?` |

   An option documented as repeatable ("can be specified multiple times", ...) or listed in `Lists` becomes an
   `IReadOnlyList<...>`, with a plural name (`Sources`).
3. **Code.** One file per `Group` of `Specs.cs`, with a header saying it is generated.

## What Specs.cs declares

The help says nothing about the shape of the C# API, so [`Specs.cs`](Specs.cs) declares it for each command:
the nested class and method names, the positional arguments (required ones become method parameters), whether
the command accepts MSBuild properties, and the fixes for what the help does not say (repeatable options,
options taking a value in help without placeholders). The options of `msbuild`, `fsi` and `vstest`, whose help is
not in the System.CommandLine format, are written by hand in `Specs.cs`.

Do not edit the generated files: edit `Specs.cs` (or `Generate.cs`) and generate again.

## Options

Pass them after `--`, such as `dotnet run tools/generate-dotnet/Generate.cs -- --help-dir help`:

- `--help-dir DIR` keeps the help dumps in `DIR`, to compare two SDK versions for example.
- `--no-dump` reuses the dumps of `--help-dir` instead of running `dotnet`.
- `--output DIR` writes the generated files to `DIR` instead of `src/Marmotte.Build/Dotnet`.
