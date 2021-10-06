# Amino Acid Alignment using De Bruijn graphs

# Getting started

There are distributed executable files for windows (x64), linux (x64) and mac (x64). The [dotnet runtime](https://dotnet.microsoft.com/download) can be installed to run the program on almost any other platform (See ['Running with dotnet'](#running-with-dotnet)). To use these first download the latest package (found in the [pipeline page](https://bms-developer.science.uu.nl/D.Schulte/amino-acid-assembler/-/pipelines)). Unpack the archive and run the files from the command line with the filename of the batch file to be used.

Windows (x64):
```
.\assembler.exe batchfiles\monoclonal.txt
```

Linux (x64, most versions):
```
./assembler_linux batchfiles/monoclonal.txt
```

OSX (x64, minimum version macOS 10.12 Sierra):
```
./assembler_mac batchfiles/monoclonal.txt
```

For help creating batch files see `manual.pdf`, this is included with the package.


## Running with dotnet

To run the program with the dotnet runtime, first install the dotnet runtime (version 5.0) from [here](https://dotnet.microsoft.com/download).
Then run the following command to run the program:

```
dotnet path/to/source.dll <arguments>
```

# Building

The project is built with dotnet (.NET 5.0) this is tested on windows and linux. To run the project on your own machine (not using precompiled binaries for linux or windows x64) install dotnet, stay in this folder (the root) and run:

```
dotnet run -p source <path to batchfile>
```

It will warn you that the assets folder is missing, this can be fixed by creating a symbolic link (mklink for windows cmd) from the folder in which the dll will be placed (`source\bin\Debug\net5.0\&lt;platform&gt;\`) called `assets` to `rootfolder\assets`.

```
mklink /J source\bin\debug\net5.0\win-x64\assets\ assets\
```

To generate a single executable run:

```
dotnet publish source -c release [-r target]
```

The target name should then be a valid 'RID' for the platform you choose. But if this is omitted it will default to windows x64. See [this site](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog#rid-graph) for information about RIDs. One point to make is that the ILCompiler does not (yet) support cross compiling with Ready to Run (R2R) enabled, so if there is a need to cross compile this option should be set to false (in `source.csproj`).


# Testing

There are some unit tests provided. These can be found in the 'tests' file. To run the unit tests run (from the root folder):

```
dotnet test tests\small_tests
dotnet test tests\batchfiles
```


# Examples

The 'batchfiles' folder contains some examples which can be run to see what the program is up to.

- `Basic.txt` 
- `Monoclonal.txt`
- `Polyclonal.txt`


# Credits

* Douwe Schulte - Software engineer
* Joost Snijder - Principal investigator
* Bastiaan de Graaf - Code reviews


# Acknowledgements

* Both authors are part of the group ["Biomolecular Mass Spectrometry and Proteomics"](https://www.uu.nl/en/research/biomolecular-mass-spectrometry-and-proteomics) ([or here](https://www.hecklab.com/biomolecular-mass-spectrometry-and-proteomics/)) at the [university of Utrecht](https://www.uu.nl/)


# License

MIT License

Copyright (c) 2019-2020 Joost Snijder & Douwe Schulte

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
