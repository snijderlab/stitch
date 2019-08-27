# Amino Acid Alignment using De Bruijn graphs

# Getting started

The software can be build using csc by using the included build.bat file.

There are distributed executable files for all releases. To use these download the files, unpack the archive and run the files from the commandline with the filename of the batch file to be used.

```
main.exe examplebatchfile.txt
```

For help creating batch files see BatchFiles.md

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

...

# Cross platform

This program is written in plain C# without any big frameworks and as such is likely to be easy to port to other platforms. dotnet (.NET Core) and mono are being considered for this step.