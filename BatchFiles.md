# Batch Files

## Introduction

Batch files are used to aggregate all information for one run of the program. These files can be edited with any plain text editor. There is no specific extension required by the program so `.txt` is recommended because these will automatically be opened by an editor in plain text.

## VS Code plugin WIP

In the [repository](https://git.science.uu.nl/d.schulte/research-project-amino-acid-alignment) there is a folder called `protseq-vscode-extension` by copying this to your VS Code extension folder (user/.vscode/extensions) the extension is installed. By then setting the format of an opened batch file to 'Protein Assembler', by clicking on the format name (normally 'Plain Text' for .txt files), colours will be show to aid in the overview of the files. This extension is very simple so it does not do any error checking, but it should still be useful.

## Structure

The general structure is parameters followed by values. A parameter is the first thing on a line (possibly precessed by whitespace) followed by a delimiter ( `:` for single valued parameters, `:>`/`<:` for multiline single valued parameters or `->`/`<-` for multiple valued parameters) (possibly followed by whitespace) followed by the value(s). Parameter names and most values are not case specific.

In any place where a single valued parameter is expected both a single line `:` and a multiline `:>`/`<:` are valid. 

Lines starting with a hyphen `-` are considered comments and disregarded. Comments can be placed in the outer scope and in multiple valued arguments.

The parameters are organised in multiple scopes each having a specified set of parameters determining the behaviour of a specified step in the assembly process. Following is an overview of the general structure.

```
-Outer Scope, can contain Run Info parameters

Input ->
    -Determines the input files to be used
    -Can only be specified once
<-

Assembly ->
    -Has parameters determining the behaviour of the assembler
    -Like input parameters, alphabet, k etc
    -Can only be specified once
    -Has to be specified
<-

TemplateMatching ->
    -Has parameters defining the database matching step
    -Like scoring, alphabet and the databases itself
    -Can only be specified once
<-

Recombine ->
    -Has parameters defining the behaviour of the recombination step
    -Aligns the databases from TemplateMatching with the paths from the assembler
    -Can only be specified once
    -TemplateMatching has to be specified for recombination te be specified

    ReadAlign ->
        -Has parameters defining the behaviour of the read alignment step
        -Aligns the input reads (as defined in Input) to the consensus sequences retrieved by recombination
        -Can only be specified once
    <-
<-

Report ->
    -Has parameters defining the output of the assembler
    -Can only be specified once
    -Has to be specified
<-
```

### All parameters

Here is a list of all parameters and their possible values. An `s` after the name indicates it is a single valued parameter, an `m` indicates it is a multiple valued parameter. A star `*` after the name indicates that the scope or parameter can be specified multiple times.

All paths are specified from the directory of the batch file.

All assets are loaded from the folder `assets` in the folder of the binary.

#### Run Info

These parameters are placed in the outer scope. So not nested in any other parameter. These parameters dictate general settings for the whole run.

##### Version (s)

The version of the batch file. For now only version 0 is accepted, but is included to later add more versions with possible breaking changes in the structure. This parameter is required.

_Example_
```
Version: 0
```

##### Runname (s)

The name of the run, to keep it organized. This name can consist of any characters except newlines.

_Examples_
```
Runname: MyFirstTestRun
Runname: Monoclonal Antibodies From Rabbits
```

##### MaxCores (s)

The maximum amount of cores to be used by this run. Default is the amount of cores of the machine.

_Examples_
```
MaxCores: 4
MaxCores: 86
```

##### Runtype (s)

If the different input files in this run should be ran separate from each other trough the assembler (`Separate`), or be grouped together into one heap of data (`Group`). The default is `Group`.

_Examples_
```
Runtype: Separate
Runtype: Group
```

#### Input

This scope contains parameters to load reads.

##### Reads (m) * 

A multiple valued parameter containing a Path, to a file with reads, and a Name, for this file to aid in recognizing where the data comes from.

| Inner parameter | Explanation                                          | Default Value |
| --------------- | ---------------------------------------------------- | ------------- |
| Path            | The path to the file                                 | (No Default)  |
| Name            | Used to recognize the origin of reads from this file | (No Default)  |

_Example_
```
Reads ->
    Path: Path/To/My/FileWithReads.txt
    Name: NameForMyFile
<-
```

##### FASTA (m) *

A multiple valued parameter containing a Path, to a fasta file with reads, and a Name, for this file to aid in recognizing where the data comes from.

| Inner parameter | Explanation                                                                                       | Default Value |
| --------------- | ------------------------------------------------------------------------------------------------- | ------------- |
| Path            | The path to the file.                                                                             | (No Default)  |
| Name            | Used to recognize the origin of reads from this file.                                             | (No Default)  |
| Identifier      | A Regex to get the identifier from the fasta header line, the first group will be used as the id. | (.*)          |

_Example_
```
FASTAInput ->
    Path: Path/To/My/FileWithReads.fasta
    Name: NameForMyFile
<-
```

##### Peaks (m) *

A multiple valued parameter to input data from a Peaks export file (.csv).

From this file the reads that score high enough are included (>=`CutoffALC`). As are smaller patches within reads of which all positions score high enough (>=`LocalALC`).

Any parameter with a default value can be left out.

| Inner parameter  | Explanation                                                                                                                                                                              | Default Value |
| ---------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------- |
| Path             | The path to the file                                                                                                                                                                     | (No Default)  |
| CutoffALC      | The score a reads must at least have to be included in the list of reads                                                                                                                 | 99            |
| LocalCutoffALC | The score a patch in a read should at least have to be included.                                                                                                                         | 90            |
| Format           | The format of the Peaks export, this depends on the version of Peaks, now only has the options `Old`, `X` and `X+`. If any gives errors in reading the file maybe another one will work. | `X+`          |
| MinLengthPatch   | The minimal length of a patch before it is included                                                                                                                                      | 3             |
| Name             | Used to recognize the origin of reads from this file                                                                                                                                     | (No Default)  |
| Separator        | The separator used to separate cells in the csv                                                                                                                                          | `,`           |
| DecimalSeparator | The separator used to separate decimals                                                                                                                                                  | `.`           |

_Examples_
```
-Minimal definition
Peaks ->
    Path: Path/To/My/FileWithPeaksReads.txt
    Name: NameForMyFile
<-

-Maximal definition
Peaks           ->
    Path            : Path/To/My/FileWithPeaksReads.txt
    Name            : NameForMyFile
    Cutoffscore     : 98
    LocalCutoffscore: 85
    Format          : Old
    MinLengthPatch  : 5
    Separator       : ;
    DecimalSeparator: ,
<-
```

##### Folder (m) *

Open a specified folder and open all reads files in it. Files with `.txt` as extension will be read as Reads. Files with `.fasta` as extension will be read as Fasta. Files with `.csv` as extension will be read as Peaks. It is possible to provide a filter for the files in the form of a constant text the files have to start with.

So in the example below (example 01) all `.txt`, `.fasta` and `.csv` files in the dictionary starting with the text `reads-IgG` will be opened.

For Peaks files extra parameters can be attached. All properties used in a peaks definition can also be used in a folder definition, with the caveat that here they should be prefixed with `Peaks`. As can be seen in example 02. The same holds for the extra parameter for Fasta files, the parameter `Identifier` can be used to provide the identifier for the fasta reads.

The folder can be opened recursively using `Recursive: True` which has a default value of `False`. This will go through all folders inside of the specified folder, and all folders in these recursively.

```
- Example 01
Folder ->
    Path: ../systematictest/reads
    StartsWith: reads-IgG
<-

- Example 02
Folder ->
    Path       : ../systematictest/reads
    PeaksFormat: Old
    StartsWith : Herceptin
    Recursive  : True
<-
```

#### Assembly

Has parameters determining the behaviour of the assembly step. Can only be specified once. Has to be specified.

##### Input (m)

The reads to use in the alignment. See the scope Input for more information about its definition. The input can also be defined in the outer scope, in which case the input can be reused be other steps in the process, for example by ReadAlign.

##### InputParameters (m)

This parameter can be used to determine the filter settings to be used in opening the reads for the Assembly. These will overrule any settings set in the Input in the outer scope.

###### Peaks (m)

| Inner parameter  | Explanation                                                                                                                                                                              | Default Value |
| ---------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------- |
| CutoffALC      | The score a reads must at least have to be included in the list of reads                                                                                                                 | 99            |
| LocalCutoffALC | The score a patch in a read should at least have to be included.                                                                                                                         | 90            |
| MinLengthPatch   | The minimal length of a patch before it is included                                                                                                                                      | 3             |
| Separator        | The separator used to separate cells in the csv                                                                                                                                          | `,`           |
| DecimalSeparator | The separator used to separate decimals                                                                                                                                                  | `.`           |


##### K (s or m)

The value or values of K to be used.

If multiple values are entered multiple runs are generated each with a different values for K.

For the range definition some inner parameters are available. `Start` and `End` have to be defined.

| Inner parameter | Explanation              | Default Value |
| --------------- | ------------------------ | ------------- |
| Start           | The lowest value to use  | (No Default)  |
| End             | The highest value to use | (No Default)  |
| Step            | The size of the steps    | 1             |

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

##### MinimalHomology (s)

The minimal homology to use, this is the minimal score before an edge is included in the De Bruijn graph. This value is mainly dependent on the alphabet used.

It can be defined as a constant value or as a calculation based on K. It only supports very basic arithmetic, - + * /, the variable 'K' and constants (positive integer numbers).

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

It can be defined as a constant value or as a calculation based on K. It only supports very basic arithmetic, - + * /, the variable 'K' and constants (positive integer numbers).

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

Defines if the reads should also be generated in reverse, which is useful if some reads are/could be backwards compared to others. The possible values are `True`, `False` and `Both`. The last option will run the runs two times, one with `True` and one with `False`. The default if `False`.

_Examples_
```
Reverse: True
Reverse: False
Reverse: Both
```

##### Alphabet (m)

Determines the alphabet to use. See the scope Alphabet for more information about its definition.

#### TemplateMatching (m)

To determine the parameters for template matching. Can only be specified once.

##### CutoffScore (s)

The mean score per position needed for a path to be included in the Database score. Default value: 0.

##### Alphabet (m)

Determines the alphabet to use. See the scope Alphabet for more information about its definition.

##### IncludeShortReads (s)

Determines if short reads (< K) will be added to the list of paths in recombination. (`True` or `False`). Default: `True`.

##### ForceOnSingleTemplate (s)

Determines of the paths/reads of this database will be forced to the best template(s) or just all templates which score high enough. Setting this options for a database in the databases list of Recombine overrules the value set in Recombine. Possible values: `True` and `False`. Default: `False`.

##### Databases (m)

Defines a list of databases to match against. A single database is defined by the parameter `Database` which can be defined multiple times within `Databases`. Databases will be read based on their extension `.txt` will be read as Simple, `.fasta` as Fasta and `.csv` as Peaks. For Peaks extra parameters can be attached. All properties used in a peaks definition can also be used in this definition, with the caveat that here they should be prefixed with `Peaks`.

`CutoffScore`, `Alphabet`, `ForceOnSingleTemplate`, `Scoring` and `IncludeShortReads` can be defined to overrule the definition of the respective parameter in the enclosing TemplateMatching scope.

Only when using recombination the properties `Identifier` and `ClassChars` are useful. The `Identifier` property takes a regex to parse the identifier from the full fasta header. The `ClassChars` property takes a number signifying the amount of chars that make up the name of the class (eg IgG1/IgG2/etc), these characters will be taken from the start of the identifier. When no `ClassChars` is present there will be no differentiation between classes in the results page.

```
Databases ->
    Database ->
        Path    : ../templates/smallIGHV.fasta
        Name    : IGHV Human
        CutoffScore : 0.75
        Alphabet ->
            Path	: ../alphabets/blosum62.csv
            Name	: Blosum62
        <-
    <-

    Database ->
        Path        : ../templates/smallIGHV.csv
        PeaksFormat : Old
        Name        : IGHV Human
        CutoffScore : 4
        IncludeShortReads : False
        Alphabet ->
            Path	: ../alphabets/blosum62.csv
            Name	: Blosum62
        <-
    <-
<-
```
###### Name (s) 

The name of this database, will be used to make the report more descriptive.

###### Path (s)

The templates to be used in this database. Uses the same logic as Input->Folder to load the file. So uses the extension to determine the right file format. And like Folder extra parameters can be supplied to have finer control over the loading of the files. Like 'Identifier, for Fasta files and all Peaks parameters with the prefix `peaks`.

###### CutoffScore (s)

The mean score per position needed for a path to be included in the Database score. Default value: 0.

###### Alphabet (m)

Determines the alphabet to use. See the scope Alphabet for more information about its definition.

###### IncludeShortReads (s)

Determines if short reads (< K) will be added to the list of paths in recombination. (`True` or `False`). Default: `True`.

###### Scoring (s)

The scoring strategy used when determining the score of this database. `Absolute` will just add the scores of all individual templates. `Relative` will divide the scores for individual templates by their respective length, giving lengthwise very different templates a fairer chance of being chosen for recombination. Default: `Absolute`.

###### ForceOnSingleTemplate (s)

Determines of the paths/reads of this database will be forced to the best template(s) or just all templates which score high enough. Setting this options for a database in the databases list of Recombine overrules the value set in Recombine. Possible values: `True` and `False`. Default: `False`.

#### Recombine

Defines how to recombine the TemplateMatched databases, as such TemplateMatching has to be defined to be able to define Recombine. Recombination can be used to pick the _n_ highest scoring templates out of each database, these will be recombined in the order provided. These recombined templates are then aligned with all paths. This should provide the opportunity to detect the placement of paths relative to each other. It also provides insight into the most likely template in the database the input matches with. Be warned, the runtime exponentially increases with _n_.

_Example_
```
Recombine->
    n : 1
    CutoffScore : 2.6
    Order : IGHV * IGHJ IGHC

    ReadAlignment ->
        -Will follow
    <-
<-
```

##### N (s)

The amount of templates to recombine from each database.

##### Order (s)

The order in which the databases will be recombined. Defined as a list of the names of the database in order possibly with gaps ('*') in between.

_Example_
```
Order: IGHV IGHJ * IGHC
```

##### CutoffScore (s)

The mean score per position needed for a path to be included in the Database score. Default value: 0.

##### Alphabet (m)

Determines the alphabet to use. See the scope Alphabet for more information about its definition.

##### IncludeShortReads (s)

Determines if short reads (< K) will be added to the list of paths in recombination. (`True` or `False`). Default: `True`.

##### ForceOnSingleTemplate (s)

Determines of the paths/reads of these databases will be forced to the best template(s) or just all templates which score high enough. Setting this options for a database in the databases list of overrules the global value set in Recombine. Possible values: `True` and `False`. Default: `False`.

##### ReadAlignment

This defines the parameters used for ReadAlignment to the templates created by Recombine. Most parameters will be reused from Recombine, but these can be overruled if a different value is needed.

##### Input (m)

The reads to use in the alignment. See the scope Input for more information about its definition. The input can also be defined in the outer scope, in which case the input can be reused be other steps in the process, for example by Assembly.

###### InputParameters (m)

This parameter can be used to determine the filter settings to be used in opening the reads for the ReadAlignment. These will overrule any settings set in the Input in the outer scope.

####### Peaks (m)

| Inner parameter  | Explanation                                                                                                                                                                              | Default Value |
| ---------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------------- |
| CutoffALC      | The score a reads must at least have to be included in the list of reads                                                                                                                 | 99            |
| LocalCutoffALC | The score a patch in a read should at least have to be included.                                                                                                                         | 90            |
| MinLengthPatch   | The minimal length of a patch before it is included                                                                                                                                      | 3             |
| Separator        | The separator used to separate cells in the csv                                                                                                                                          | `,`           |
| DecimalSeparator | The separator used to separate decimals                                                                                                                                                  | `.`           |


###### CutoffScore (s)

The mean score per position needed for a path to be included in the Database score. Default value: 0.

###### Alphabet (m)

Determines the alphabet to use. See the scope Alphabet for more information about its definition.

###### ForceOnSingleTemplate (s)

Determines of the paths/reads of this database will be forced to the best template(s) or just all templates which score high enough. Possible values: `True` and `False`. Default: `False`.

#### Report

##### HTML (m) *

To generate an HTML report. This report displays all information about this run, including all original metadata of the input. The report is designed to be used interactively to aid in understanding how well the software performed and how trustworthy the results are. The report will be generated as an overview file (with the name specified) with a folder with all additional details (with the same name as the HTML file). 

| Inner parameter | Explanation                                                                                                     | Default Value |
| --------------- | --------------------------------------------------------------------------------------------------------------- | ------------- |
| Path            | The path to save the report to, this path can be made dynamically (see '[Generating Names](#generating-names)') | (No Default)  |
| DotDistribution | To use a `global` install of the Dot engine or the `included` one.                                                | `included`    |

_Example_
```
HTML ->
    Path: Report.html
<-
```

##### FASTA (m) *

To generate a FASTA file with all paths, with a score for each path. The score is the total amount of positions from reads mapping to this path. In other words it is the total length of all parts of all reads supporting this sequence. As such a higher score indicates more support for a sequence and/or a longer sequence.

| Inner parameter | Explanation                                                                                                     | Default Value |
| --------------- | --------------------------------------------------------------------------------------------------------------- | ------------- |
| Path            | The path to save the report to, this path can be made dynamically (see '[Generating Names](#generating-names)') | (No Default)  |
| MinimalScore    | The minimal score needed to be included in the file                                                             | 0             |
| OutputType      | The type of sequences to give as output, `Assembly`, `Recombine` or `ReadsAlign`                                | `Assembly`    |

_Example_
```
FASTA ->
    Path         : Contigs.fasta
    MinimalScore : 50
    OutputType   : Paths
<-
```

##### Generating Names

The path of reports can be generated dynamically, very useful if a batch files codes for many runs.

The program will also create missing folders if needed.

| Key        | Explanation                                                 |
| ---------- | ----------------------------------------------------------- |
| {id}       | A unique numerical ID of the run (automatically generated)  |
| {k}        | The value of K of the run                                   |
| {mh}       | The value of MinimalHomology of the run                     |
| {dt}       | The value of DuplicateThreshold of the run                  |
| {alph}     | The name of the alphabet used                               |
| {name}     | The name of the run                                         |
| {date}     | The date of today in the format yyyy-mm-dd                  |
| {time}     | The current time in the format hh-mm-ss                     |
| {datetime} | The current date and time in the format yyyy-mm-dd@hh-mm-ss |

_Examples_
```
-To be sure of unique names
Path: Folder/Structure/Report-{id}.html

-More advanced naming scheme
Path: Folder/Structure/{name}-{data}-{k}-{mh}-{dt}-{alph}.csv

-Creates the folders needed
Path: Folder/{data}/{alph}/{k}-{mh}-{dt}.fasta
```

#### Alphabet

Defines the alphabet used to score K-mers against each other. If multiple alphabets are defined, these will be run independently in different runs. Both `;` and `,` are considered separators.

| Inner parameter  | Explanation                                                                                                                      | Default Value |
| ---------------- | -------------------------------------------------------------------------------------------------------------------------------- | ------------- |
| Path             | The path to the alphabet (cannot be used in conjunction with `Data`)                                                             | (No Default)  |
| Data             | The alphabet, to allow for newlines the alphabet should be enclosed in `:>` and `<:` (cannot be used in conjunction with `Path`) | (No Default)  |
| Name             | To recognize the alphabet                                                                                                        | (No Default)  |
| GapStartPenalty  | The penalty for opening a gap in an alignment. Used in template matching.                                                        | 12            |
| GapExtendPenalty | The penalty for extending a gap in an alignment. Used in template matching.                                                      | 1             |

_Examples_
```
Alphabet->
    Data	:>
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
    <:
    Name	: Normal
<-

Alphabet->
    Path    : My/Path/To/AnAlphabet.csv
    Name	: Normal
<-
```

### Scoring

#### Input/Reads

All reads have an `intensity` and a `positional score`, the intensity is a single number and the positional score is a number per position of the sequence. The default values are 1 for intensity and none for positional score. For peaks reads the intensity is calculated as `2 - 1 / (log10(Area))`, and the positional score is the local confidence (localALC) as reported by Peaks divided by 100 to generate fractions instead of percentages and multiplied by the intensity (on the fly so later changes in intensity are reflected in this score).

#### Assembler

If there are duplicate reads the intensities are added together.

When the paths in the graph are found these also get an intensity and positional score. The intensity is always 1 and the positional score is the depth of coverage for each position in the sequence of the path.

#### Template Matching

The score of a match (read or path against a template) is calculated by the smith waterman algorithm solely based on the alphabet used. If the score of a match is bigger than or equal to the cutoff score as defined in the input batch file times the square root of the length of the template the match is added to the list of matches of the template, otherwise it is discarded.

```
matchScore >= cutoffScore * sqrt(templateLength)
```

#### Consensus Sequence

The consensus sequence for each position is based on the positional score of each amino acid.

### Example Batch Files

See the files in the folder `examples\batchfiles\`.