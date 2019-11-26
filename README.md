# Amino Acid Alignment using De Bruijn graphs

# Getting started

There are distributed executable files for windows (x64) and linux (x64). The [dotnet runtime](https://dotnet.microsoft.com/download) can be installed to run the program on any platform supporting the dotnet runtime (See ['Running with dotnet'](#running-with-dotnet)). To use these download the files, unpack the archive and run the files from the commandline with the filename of the batch file to be used.

Windows (x64):
```
source.exe examplebatchfile.txt
```

Linux (x64, most version):
```
./source examplebatchfile.txt
```

For help creating batch files see BatchFiles.md

## Running with dotnet

To run the program with the dotnet runtime, first install the dotnet runtime (at least version 2.2) from [here](https://dotnet.microsoft.com/download).
Then run the following command to run the program:

```
dotnet path/to/source.dll <arguments>
```

## Installing Dot

On windows [Graphviz](https://www.graphviz.org) is included in the assets, so there is no need to install it. On Linux or other platforms you will have to install Graphviz, [see this site](https://graphviz.gitlab.io/download/). Do not forget when you installed Graphviz on your own machine to add the option `DotDistribution: Global` to all HTML reports and check if the program should be added to your PATH variable.

# Building

The project is built with dotnet (SDK 2.2) this is tested on windows and linux. To run the project on your own machine (not linux or windows x64) install dotnet, stay in this folder (the root) and run:

```
dotnet run -p source <path to batchfile>
```

To generate a single executable with help of the ILCompiler run:

```
dotnet publish -c Release -r target-name
```

The target name should then be a valid 'RID' for the platform you choose. See [this site](https://docs.microsoft.com/en-us/dotnet/core/rid-catalog#rid-graph) for information about RIDs. One point to make is that the ILCompiler does not (yet) support cross compiling, so it can only compile binaries for the platform you are at.

# Testing

There are some unittests provided. These can be found in the 'tests' file. To run the unittests run (from inside the root folder):

```
dotnet test tests
```

# Examples

The 'examples' folder contains some examples which can be run to see what the program is up to.

Examples 001 through 007 are simple generated reads and sequences.
Example 008 is an example of real world data gotten with PEAKS.
Example 009 is an example of a FASTA input file.

# Authors

* Douwe Schulte - Wrote the software
* Joost Snijder - Supervised and collaborated

# Acknowledgments

* Both authors are part of the group ["Biomolecular Mass Spectrometry and Proteomics"](https://www.uu.nl/en/research/biomolecular-mass-spectrometry-and-proteomics) ([or here](https://www.hecklab.com/biomolecular-mass-spectrometry-and-proteomics/)) at the [university of Utrecht](https://www.uu.nl/)
* The [Graphviz software](https://www.graphviz.org) is included to visualize the graphs in the HTML reports

# License

MIT License see LICENSE.md.

# Cross platform

This program is written in plain C# without any big frameworks and as such is likely to be easy to port to other platforms. dotnet (.NET Core) and mono are being considered for this step.