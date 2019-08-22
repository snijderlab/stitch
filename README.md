# Amino Acid Alignment using De Bruijn graphs

# Batch Files

## Introduction

Batch files are used to aggregate all information for one run of the program. These files can be edited wih any plain text editor. There is no specific extension required by the program so `.txt` is recommended because these will automatically be opened by an editor in plain text.

## Structure

The general structure is parameters followed by values. A parameter is the first thing on a line (possibly followed by whitespace) followed by a delimeter ( `:` for single valued parameters or `->` for multiple valued parameters) (possibly followed by whitespace) followed by the value(s). Paramater names and ost values are not case specific.

Lines starting with a hypen `-` are considered comments and disregarded.

### All parameters

Here is a list of all parameters and their possible values.

#### Run Info

##### Version (s)

The version of the batch file. For now only version 0 is accepted, but is included to later add more version with possible breaking changes in the structure.

_Example_
```
Version: 0
```

##### Runname (s)

The name of the run, to keep it organised. This name can consist of any characters except newlines.

_Examples_
```
Runname: MyFirstTestRun
Runname: Monoclonal Antibodies From Rabits
```

##### Runtype (s)

If the inputs in this run should be ran separate from each other trough the assembler (`Separate`), or be grouped together into one heap of data (`Group`).

_Examples_
```
Runtype: Separate
Runtype: Group
```

#### Input

##### Reads (m)

A multiple valued parameter containing a Path, to a file with reads, and a Name, for this file to aid in recognising where the data comes from.

Inner parameter | Explanation | Default Value
--- | --- | ---
Path | The path to the file | (No Default)
Name | Used to recognise the origin of reads from this file | (No Default)

_Example_
```
Reads ->
Path: Path/To/My/FileWithReads.txt
Name: NameForMyFile
<-
```

##### Peaks (m)

A multiple valued parameter to input data from a Peaks export file (.csv).

From this file the reads that score high enough are included. As are smaller patches within reads of which all positions score high enough.

Any parameter with a default value can be left out.

Inner parameter | Explanation | Default Value
--- | --- | ---
Path | The path to the file | (No Default)
Cutoffscore | The score a reads must at least have to be included in the list of reads | 99
LocalCutoffscore | The score a patch in a read should at least have to be included. | 90
FileFormat | The format of the Peaks export, this depends on the version of Peaks, now only has the options `Old` and `New`. If `New` gives errors in reading the file maybe `Old` will work. | `New`
MinLengthPatch | The minimal length of a patch before it is included | 3
Name | Used to recognise the origin of reads from this file | (No Default)
Separator | The separator used to separate cells in the csv | `,`
DecimalSeparator | The separator used to separate decimals | `.`

_Examples_
```
-Minimal definition
Peaks ->
Path: Path/To/My/FileWithPeaksReads.txt
Name: NameForMyFile
<-

-Maximal definiion
Peaks           ->
Path            : Path/To/My/FileWithPeaksReads.txt
Name            : NameForMyFile
Cutoffscore     : 98
LocalCutoffscore: 85
FileFormat      : Old
MinLengthPatch  : 5
Separator       : ;
DecimalSeparator: ,
<-
```

#### Parameters

##### K (s or m)

The value or values of K to be used.

If multiple values are entered multiple runs are generated with all differen values of K.

For the range defnition some inner parameters are available. `Start` and `End` have to be defined.

Inner parameter | Explanation | Default Value
--- | --- | ---
Start | The lowest value to use | (No Default)
End | The highest value to use | (No Default)
Step | The size of the steps | 1

_Examples_
```
-Single value
K: 8

-Multiple values
K: 8, 10, 15

-Range definition
K ->
Start: 8
End: 20
Step: 2
<-
```

##### MinimalHomoloy (s)

The minimal homology to use, this is the minimal score before an edge is included in the De Buijn graph. This value is mainly depending on the alphabet used.

It can be defined as a constant value or as a calculation based on K (**TODO**).

The default value is `K-1`.

_Examples_
```
-Constant value
MinimalHomology: 7
MinimalHomology: 14

-Calculation
MinimalHomology: K-1
MinimalHomology: K*2-3
```

##### DuplicateThreshold (s)

The threshold score which has to be reached before two edges are considered equal. This is used in the detection of duplicates while filtering reads. This value is mainly depending on the alphabet used.

It can be defined as a constant value or as a calculation based on K (**TODO**).

The default value is `K-1`.

_Examples_
```
-Constant value
DuplicateThreshold: 7
DuplicateThreshold: 14

-Calculation
DuplicateThreshold: K-1
DuplicateThreshold: K*2-3
```

##### Reverse (s)

Defines if the reads should also be generated in reverse, which is usefull if some reads are/could be backwards compared to others. The possible values are `True`, `False` and `Both`. The last option will run the runs two times, one wit `True` and one wih `False`.

_Examples_
```
Reverse: True
Reverse: False
Reverse: Both
```

##### Alphabet (m)

Defines the alphabet(s) used to score K-mers against each other. If multiple alphabets are defined, these will be run independantly in different runs.

Inner parameter | Explanation | Default Value
--- | --- | ---
Path | The path to the alphabet (cannot be used in conjunction with `Data`) | (No Default)
Data | The alphabet, to allow for newlines the alphabet should be enclosed in `->` and `<-` (cannot be used in conjunction with `Path`) | (No Default)
Name | To recognise the alphabet | (No Default)
Separator **TODO** | The separator used in the CSV | `;`

_Examples_
```
Alphabet->
Data	->
*;L;S;A;E;G;V;R;K;T;P;D;I;N;Q;F;Y;H;M;C;W;O;U
L;1;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0
S;0;1;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0
A;0;0;1;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0
E;0;0;0;1;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0
G;0;0;0;0;1;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0
V;0;0;0;0;0;1;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0
R;0;0;0;0;0;0;1;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0
K;0;0;0;0;0;0;0;1;0;0;0;0;0;0;0;0;0;0;0;0;0;0
T;0;0;0;0;0;0;0;0;1;0;0;0;0;0;0;0;0;0;0;0;0;0
P;0;0;0;0;0;0;0;0;0;1;0;0;0;0;0;0;0;0;0;0;0;0
D;0;0;0;0;0;0;0;0;0;0;1;0;0;0;0;0;0;0;0;0;0;0
I;0;0;0;0;0;0;0;0;0;0;0;1;0;0;0;0;0;0;0;0;0;0
N;0;0;0;0;0;0;0;0;0;0;0;0;1;0;0;0;0;0;0;0;0;0
Q;0;0;0;0;0;0;0;0;0;0;0;0;0;1;0;0;0;0;0;0;0;0
F;0;0;0;0;0;0;0;0;0;0;0;0;0;0;1;0;0;0;0;0;0;0
Y;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;1;0;0;0;0;0;0
H;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;1;0;0;0;0;0
M;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;1;0;0;0;0
C;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;1;0;0;0
W;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;1;0;0
O;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;1;0
U;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;1
<-
Name	: Normal
<-

Alphabet->
Path    : My/Path/To/AnAlphabet.csv
Name	: Normal
<-
```

#### Report

##### HTML (m)

To generate an HTML report.

Inner parameter | Explanation | Default Value
--- | --- | ---
Path | The path to save the report to, this path can be made dynamically (see '[Generating Names](#generating-names)') | (No Default)

_Example_
```
HTML ->
Path: Report.html
<-
```

##### CSV (m)

To generate a CSV report, it will add summary information of each run on a single line to the file.

Inner parameter | Explanation | Default Value
--- | --- | ---
Path | The path to save the report to, this path can be made dynamically (see '[Generating Names](#generating-names)') | (No Default)

_Example_
```
CSV ->
Path: Report.csv
<-
```

##### FASTQ (m)

To generate an FASTQ file with all reads.

Inner parameter | Explanation | Default Value
--- | --- | ---
Path | The path to save the report to, this path can be made dynamically (see '[Generating Names](#generating-names)') | (No Default)
MinimalScore **TODO** | The minimal score needed to be included in the file | 0

_Example_
```
FASTQ ->
Path: Contigs.fastq
MinimalScore: 50
<-
```

##### Generating Names

The path of reports can be generated dnamically, very usefull if a batch files codes for many runs.

For now the code does not generate any missing folders (but chrashes instead) so if any dynamically generated folder names are used these should be made up front. (**TODO**)

Key | Explanation
--- | --- 
{id} | A unique numerical ID of the run (automatically generated)
{k} | The value of K of the run
{mh} | The value of MinimalHomology of the run
{dt} | The value of DuplicateThreshold of the run
{alph} | The name of the alphabet used
{data} | The name of the input data used
{name} | The name of the run
{date} | The date of today in the format yyyy-mm-dd
{time} | The current time in the format hh-mm-ss
{datetime} | The current date and time in the format yyyy-mm-dd@hh-mm-ss

_Examples_
```
-To be sure of unique names
Path: Folder/Structure/Report-{id}.html

-More advanced naming scheme
Path: Folder/Structure/{name}-{data}-{k}-{mh}-{dt}-{alph}.csv

-Not functional yet (does no create the folders) but would be nice
Path: Folder/{data}/{alph}/{k}-{mh}-{dt}.fastq
```

### Example Batch File

```
------| Assemble |------

-Run Info---------------
Version	: 0
Runname	: Example
Runtype : Apart

-Input------------------
Reads	->
Path    : examples\001\reads.txt
Name    : 001
<-
Reads	->
Path    : examples\003\reads.txt
Name    : 003
<-

-Parameters-------------
K	    : 8, 10
Minimalhomology: 7
duplicatethreshold: 7
Reverse	: both
Alphabet->
Data	->
*;L;S;A;E;G;V;R;K;T;P;D;I;N;Q;F;Y;H;M;C;W;O;U
L;1;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0
S;0;1;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0
A;0;0;1;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0
E;0;0;0;1;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0
G;0;0;0;0;1;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0
V;0;0;0;0;0;1;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0
R;0;0;0;0;0;0;1;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0
K;0;0;0;0;0;0;0;1;0;0;0;0;0;0;0;0;0;0;0;0;0;0
T;0;0;0;0;0;0;0;0;1;0;0;0;0;0;0;0;0;0;0;0;0;0
P;0;0;0;0;0;0;0;0;0;1;0;0;0;0;0;0;0;0;0;0;0;0
D;0;0;0;0;0;0;0;0;0;0;1;0;0;0;0;0;0;0;0;0;0;0
I;0;0;0;0;0;0;0;0;0;0;0;1;0;0;0;0;0;0;0;0;0;0
N;0;0;0;0;0;0;0;0;0;0;0;0;1;0;0;0;0;0;0;0;0;0
Q;0;0;0;0;0;0;0;0;0;0;0;0;0;1;0;0;0;0;0;0;0;0
F;0;0;0;0;0;0;0;0;0;0;0;0;0;0;1;0;0;0;0;0;0;0
Y;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;1;0;0;0;0;0;0
H;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;1;0;0;0;0;0
M;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;1;0;0;0;0
C;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;1;0;0;0
W;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;1;0;0
O;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;1;0
U;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;0;1
<-
Name	: Normal
<-

-Report-----------------
HTML	->
Path	: report-reads-02-{data}-{k}.html
<-
```