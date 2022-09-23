# Stitch
Template-based assembly of proteomics short reads for de novo antibody sequencing and repertoire profiling.

## Getting started

There are distributed executable files for windows (x64, arm64), linux (x64, arm64) and mac (x64, arm64). The [dotnet runtime](https://dotnet.microsoft.com/download) can be installed to run the program on almost any other platform (See ['Running with dotnet'](#running-with-dotnet)). To use these first download the latest package, found on the [releases page](https://github.com/snijderlab/stitch/releases). Unpack the archive for your system and run the files from the command line with the filename of the batch file to be used.

Windows:
```
.\stitch.exe batchfiles\monoclonal.txt           (x64)
.\stitch_arm.exe batchfiles\monoclonal.txt       (arm64)
```

Linux:
```
(x64, should work on most distros)
chmod +x ./stitch                          (give running permission to the binary)
./stitch.bin batchfiles/monoclonal.txt

(arm64)
chmod +x ./stitch_arm                      (give running permission to the binary)
./stitch_arm batchfiles/monoclonal.txt
```

OSX:
```
(x64, minimum version macOS 10.12 Sierra)
chmod +x ./stitch                          (give running permission to the binary)
./stitch batchfiles/monoclonal.txt

(arm64, minimum version macOS 11.0 Big Sur)
chmod +x ./stitch_arm                      (give running permission to the binary)
./stitch_arm batchfiles/monoclonal.txt
```

For help creating batch files see `manual.pdf`, this is can be found on the same page.

### Different versions

Releases can be found on the [releases page](https://github.com/snijderlab/stitch/releases).
Nightly versions, which contain all new features but are less stable, can be found on the [action page](https://github.com/snijderlab/stitch/actions?query=branch%3Amaster).

## Building

The project is built with dotnet (.NET 6.0) development is done on windows, but it should work on all major platforms. To run the project on your own machine (not using precompiled binaries for linux or windows x64) install dotnet, stay in this folder (the root) and run:

```
dotnet run --project stitch <path to batchfile>
```

It will warn you that the assets folder is missing, this can be fixed by creating a symbolic link (mklink for windows cmd) from the folder in which the dll will be placed (`stitch\bin\Debug\net6.0\&lt;platform&gt;\`) called `assets` to `rootfolder\assets`.

```
mklink /J stitch\bin\debug\net6.0\win-x64\assets\ assets\
mklink /J stitch\bin\debug\net6.0\win-x64\images\ images\
```

To generate a single executable run:

```
dotnet publish stitch -c release -r [target] --self-contained
```

The target name should then be a valid 'RID' for the platform you choose. But if this is omitted it will default to windows x64. See [this site](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog#rid-graph) for information about RIDs.


### Testing

There are some unit tests provided. These can be found in the 'tests' file. To run the unit tests run (from the root folder):

```
dotnet test tests\small_tests
dotnet test tests\batchfiles
```


## Examples

The 'batchfiles' folder contains some examples which can be run to see what the program is up to.

- `basic.txt` 
- `monoclonal.txt`
- `polyclonal.txt`


## Credits

* Douwe Schulte - Software engineer
* Joost Snijder - Principal investigator
* Bastiaan de Graaf - Code reviews
* Wei Wei Peng - Testing and analysis


## Acknowledgements

* Both authors are part of the group ["Biomolecular Mass Spectrometry and Proteomics"](https://www.uu.nl/en/research/biomolecular-mass-spectrometry-and-proteomics) ([or here](https://www.hecklab.com/biomolecular-mass-spectrometry-and-proteomics/)) at the [university of Utrecht](https://www.uu.nl/)


## License

MIT License (see LICENSE.md)
