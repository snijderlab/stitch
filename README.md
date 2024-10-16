# Stitch
Template-based assembly of proteomics short reads for de novo antibody sequencing and repertoire profiling.

## Getting started

There are distributed executable files for windows (x64, arm64), linux (x64, arm64) and mac (x64, arm64). If you use any other platform the see ['Building'](#building). To use these first download the latest package, found on the [releases page](https://github.com/snijderlab/stitch/releases). Unpack the archive for your system and run the files from the command line with the filename of the batch file to be used.

Windows:
```
.\stitch.exe run batchfiles\monoclonal.txt           (x64)
.\stitch_arm.exe run batchfiles\monoclonal.txt       (arm64)
```

Linux:
```
(x64, should work on most distros)
chmod +x ./stitch.bin                      (give running permission to the binary)
./stitch.bin run batchfiles/monoclonal.txt

(arm64)
chmod +x ./stitch_arm                      (give running permission to the binary)
./stitch_arm run batchfiles/monoclonal.txt
```

OSX:
```
(x64, minimum version macOS 10.12 Sierra)
chmod +x ./stitch.bin                      (give running permission to the binary)
./stitch run batchfiles/monoclonal.txt

(arm64, minimum version macOS 11.0 Big Sur)
chmod +x ./stitch_arm                      (give running permission to the binary)
./stitch_arm run batchfiles/monoclonal.txt
```

For help creating batch files see `manual.pdf`, this is can be found on the same page.

### Different versions

Releases can be found on the [releases page](https://github.com/snijderlab/stitch/releases).
Nightly versions, which contain all new features but are less stable, can be found on the [action page](https://github.com/snijderlab/stitch/actions?query=branch%3Amaster).

## Building

First retrieve the source code using git clone.

```
git clone https://github.com/snijderlab/stitch.git stitch
```

The project is built with dotnet (.NET 7.0) development is done on windows, but it should work on all major platforms. To run the project on your own machine (not using precompiled binaries for linux or windows x64) install [dotnet](https://dotnet.microsoft.com/download), stay in this folder (the root) and run:

```
dotnet run --project stitch <path to batchfile>
```

It will warn you that the assets folder is missing, this can be fixed by creating a symbolic link (mklink for windows cmd) from the folder in which the dll will be placed (`stitch\bin\Debug\net7.0\`) called `assets` to `.\assets`.

```
mklink /J stitch\bin\debug\net7.0\assets\ assets\
mklink /J stitch\bin\debug\net7.0\images\ images\
mklink /J stitch\bin\release\net7.0\assets\ assets\
mklink /J stitch\bin\release\net7.0\images\ images\
```

```
ln -s assets stitch/bin/debug/net7.0/assets
ln -s images stitch/bin/debug/net7.0/images
ln -s assets stitch/bin/release/net7.0/assets
ln -s images stitch/bin/release/net7.0/images
```

To generate a single executable run:

```
dotnet publish stitch -c release -r [target] --self-contained
```

The target name should then be a valid 'RID' for the platform you choose. But if this is omitted it will default to windows x64. See [this site](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog#rid-graph) for information about RIDs.


### Testing

There are some unit tests provided. These can be found in the 'tests' folder. To run the unit tests run (from the root folder):

```
dotnet test tests
```


## Examples

The 'batchfiles' folder contains some examples which can be run to see what the program is up to. These examples are present both with the built binaries and the source code.

- `basic.txt` 
- `monoclonal.txt`
- `polyclonal.txt`

The 'benchmarks' folder contains a set of examples with a known output which are used to benchmark the program continuously. The description of these examples can be found using the following doi 10.1021/acs.jproteome.1c00913.


## Credits

* Douwe Schulte - Software engineer - d.schulte{at}uu{dot}nl
* Joost Snijder - Principal investigator
* Bastiaan de Graaf - Code reviews
* Wei Wei Peng - Testing and analysis


## Acknowledgements

* Both authors are part of the group ["Biomolecular Mass Spectrometry and Proteomics"](https://www.uu.nl/en/research/biomolecular-mass-spectrometry-and-proteomics) ([or here](https://www.hecklab.com/biomolecular-mass-spectrometry-and-proteomics/)) at the [university of Utrecht](https://www.uu.nl/)

## Dependencies
* Hecklib core, public nuget package see `nuget.config` for more info on the exact url
* Stitch assets git submodule, contains the css and js to make the html report shine. A separate submodule to simplify reuse of these files.

## License

MIT License (see LICENSE.md)
