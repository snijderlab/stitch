# Batch Files

## Introduction

Batch files are used to aggregate all information for one run of the program. These files can be edited with any plain text editor. There is no specific extension required by the program so `.txt` is recommended because these will automatically be opened by an editor in plain text.

## VS Code plugin WIP

In the [repository](https://git.science.uu.nl/d.schulte/research-project-amino-acid-alignment) there is a folder called `protseq-vscode-extension` by copying this to your VS Code extension folder (`user/.vscode/extensions`) the extension is installed. By then setting the format of an opened batch file to 'Protein Assembler', by clicking on the format name (normally 'Plain Text' for .txt files), colours will be shown to aid in the overview of the files. This extension is very simple so it does not do any error checking, but it should still be useful.

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

#### The full list of all parameters
A `*` indicates that the scope or parameter can be defined multiple times.

* Run Info
    * Version
    * Runname
    * MaxCores
* Input
    * Reads *
        * Path
        * Name
    * FASTA *
        * Path
        * Name
        * Identifier
    * Peaks *
        * Path
        * CutoffALC
        * LocalCutoffALC
        * Format
        * MinLengthPatch
        * Name
        * Separator
        * DecimalSeparator
    * Folder (m) *
        * Path
        * StartsWith
        * Recursive
        * Identifier
        * All Peaks parameters prefixed with `Peaks`
* TemplateMatching
    * CutoffScore
    * Alphabet
    * ForceOnSingleTemplate
    * Databases
        * Database *
            * Name
            * Path
            * CutoffScore
            * Alphabet
            * Identifier
            * ClassChars
* Recombine
    * N
    * Order
    * CutoffScore
    * Alphabet
    * ForceOnSingleTemplate
    * ReadAlignment
        * Input
            * See Input
        * InputParameters
        * CutoffScore
        * Alphabet
        * ForceOnSingleTemplate
* Report
    * HTML *
        * Path
    * FASTA *
        * Path
        * MinimalScore
        * OutputType

Recurring definitions:
* Alphabet
    * Path
    * Data
    * Name
    * GapStartPenalty
    * GapExtendPenalty


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

#### TemplateMatching (m)

To determine the parameters for template matching. Can only be specified once. Often used with the reference set from IMGT (http://www.imgt.org/vquest/refseqh.html).

##### CutoffScore (s)

The mean score per position needed for a path to be included in the Database score. Default value: 0.

##### Alphabet (m)

Determines the alphabet to use. See the scope Alphabet for more information about its definition.

##### ForceOnSingleTemplate (s)

Determines of the paths/reads of this database will be forced to the best template(s) or just all templates which score high enough. Setting this options for a database in the databases list of Recombine overrules the value set in Recombine. Possible values: `True` and `False`. Default: `True`.

##### Databases (m)

Defines a list of databases to match against. A single database is defined by the parameter `Database` which can be defined multiple times within `Databases`. Databases will be read based on their extension `.txt` will be read as Simple, `.fasta` as Fasta and `.csv` as Peaks. For Peaks extra parameters can be attached. All properties used in a peaks definition can also be used in this definition, with the caveat that here they should be prefixed with `Peaks`.

`CutoffScore`, `Alphabet`, `ForceOnSingleTemplate`, and `Scoring` can be defined to overrule the definition of the respective parameter in the enclosing TemplateMatching scope.

Only when using recombination the properties `Identifier` and `ClassChars` are useful. The `Identifier` property takes a regex to parse the identifier from the full fasta header. The `ClassChars` property takes a number signifying the amount of chars that make up the name of the class (eg IgG1/IgG2/etc), these characters will be taken from the start of the identifier. When no `ClassChars` is present there will be no differentiation between classes in the results page.

The databases can be grouped into database groups. These will be presented separately in the output HTML report. This is extremely useful for example to separate the Heavy and Light chain of an antibody.

_Example_
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
        Alphabet ->
            Path	: ../alphabets/blosum62.csv
            Name	: Blosum62
        <-
    <-
<-

- Or with groups
Databases ->
    Heavy Chain ->
        Database ->
            Path    : ../templates/smallIGHV.fasta
            Name    : IGHV Human
        <-
        - Any number of other databases can be defined here, but logically IGHJ and IGHC will follow
    <-
    Light Chain ->
        - All light chain databases can be defined here
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

###### Identifier (s) 

Specifies a Regular Expression to parse the identifier from the Fasta header of the templates. The default value is `(.*)`, the first capturing group will be used as the identifier.

###### ClassChars (s)

Specifies the amount of characters to be taken from the start of the identifier which indicate the class of the template, like IgH1/IgH2 etc. On default it will take the full identifier.

###### Scoring (s)

The scoring strategy used when determining the score of this database. `Absolute` will just add the scores of all individual templates. `Relative` will divide the scores for individual templates by their respective length, giving lengthwise very different templates a fairer chance of being chosen for recombination. Default: `Absolute`.

###### ForceOnSingleTemplate (s)

Determines of the paths/reads of this database will be forced to the best template(s) or just all templates which score high enough. Setting this options for a database in the databases list of Recombine overrules the value set in Recombine. Possible values: `True` and `False`. Default: `True`.

#### Recombine

Defines how to recombine the TemplateMatched databases, as such TemplateMatching has to be defined to be able to define Recombine. Recombination can be used to pick the _n_ highest scoring templates out of each database, these will be recombined in the order provided. These recombined templates are then aligned with all paths. This should provide the opportunity to detect the placement of paths relative to each other. It also provides insight into the most likely template in the database the input matches with. Be warned, the runtime factorially increases with _n_.

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

The amount of templates to recombine from each database. From every database it will take the top N templates and join all of them together in all possible configurations, this means that the number of recombined templates factorially increases with N.

##### Order (s)

The order in which the databases will be recombined. Defined as a list of the names of the database in order possibly with gaps (`*`) in between.

_Example_
```
Order: IGHV IGHJ * IGHC
```

If there are multiple database groups defined the order should be defined separately for each group. The names are matches to the database group names disregarding casing.

_Example_
```
Order -> 
    Heavy Chain: IGHV IGHJ * IGHC
    Light Chain: IGLV IGLJ IGLC
<-
```

A gap (`*`) indicates that there should be more sequence in between the segments but that no appropriate template is available, this for example is the case in the CDR3 region. To find the correct sequence in this case the program will try to find back the sequence. This is done by expanding both surrounding segments with Xs which allows the template matching step to add all heads/tails of matched reads to the consensus sequence of that template. On recombination the consensus sequences are compared to find the overlap. If the template matching was successful and the input data was of high enough quality both templates will have the full sequence of the missing region, which can be combined into the correct template for recombination. If there is no overlap found, so no reads where matches on the Xs, or not enough to find the whole sequence a gap will be placed between the two consensus sequences. This signals to the users that some part of the sequence could be missing and leaves the opportunity to match reads with the full sequence to this gap in recombination.

##### CutoffScore (s)

The mean score per position needed for a path to be included in the Database score. Default value: 0.

##### Alphabet (m)

Determines the alphabet to use. See the scope Alphabet for more information about its definition.

##### ForceOnSingleTemplate (s)

Determines of the paths/reads of these databases will be forced to the best template(s) or just all templates which score high enough. Setting this options for a database in the databases list of overrules the global value set in Recombine. Possible values: `True` and `False`. Default: `True`.

##### ReadAlignment

This defines the parameters used for ReadAlignment to the templates created by Recombine. Most parameters will be reused from Recombine, but these can be overruled if a different value is needed.

###### Input (m)

The reads to use in the alignment. See the scope Input for more information about its definition. The input can also be defined in the outer scope, in which case the input can be reused be other steps in the process, for example by Assembly.

###### InputParameters (m)

This parameter can be used to determine the filter settings to be used in opening the reads for the ReadAlignment. These will overrule any settings set in the Input in the outer scope.

####### Peaks (m)

| Inner parameter | Explanation                                                              | Default Value |
| --------------- | ------------------------------------------------------------------------ | ------------- |
| CutoffALC       | The score a reads must at least have to be included in the list of reads | 99            |
| LocalCutoffALC  | The score a patch in a read should at least have to be included.         | 90            |
| MinLengthPatch  | The minimal length of a patch before it is included                      | 3             |

###### CutoffScore (s)

The mean score per position needed for a path to be included in the Database score. Default value: 0.

###### Alphabet (m)

Determines the alphabet to use. See the scope Alphabet for more information about its definition.

###### ForceOnSingleTemplate (s)

Determines of the paths/reads of this database will be forced to the best template(s) or just all templates which score high enough. Possible values: `True` and `False`. Default: `True`.

#### Report

##### HTML (m) *

To generate an HTML report. This report displays all information about this run, including all original metadata of the input. The report is designed to be used interactively to aid in understanding how well the software performed and how trustworthy the results are. The report will be generated as an overview file (with the name specified) with a folder with all additional details (with the same name as the HTML file). 

| Inner parameter | Explanation                                                                                                     | Default Value |
| --------------- | --------------------------------------------------------------------------------------------------------------- | ------------- |
| Path            | The path to save the report to, this path can be made dynamically (see '[Generating Names](#generating-names)') | (No Default)  |

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
| OutputType      | The type of sequences to give as output, `Recombine` or `ReadsAlign`                                            | `Recombine`   |

_Example_
```
FASTA ->
    Path         : contigs.fasta
    MinimalScore : 50
    OutputType   : ReadsAlign
<-
```

##### Generating Names

The path of reports can be generated dynamically, very useful if a batch files codes for many runs.

The program will also create missing folders if needed.

| Key        | Explanation                                                 |
| ---------- | ----------------------------------------------------------- |
| {alph}     | The name of the alphabet used                               |
| {name}     | The name of the run                                         |
| {date}     | The date of today in the format yyyy-mm-dd                  |
| {time}     | The current time in the format hh-mm-ss                     |
| {datetime} | The current date and time in the format yyyy-mm-dd@hh-mm-ss |

_Examples_
```
-To be sure of unique names
Path: Folder/Structure/Report-{date}.html

-More advanced naming scheme
Path: Folder/Structure/{name}-{date}-{alph}.csv

-Creates the folders needed
Path: Folder/{date}/{alph}/{time}.fasta
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