# AssemblyNameSpace Namespace
 

This is a project to build a piece of software that is able to rebuild a protein sequence from reads of a massspectrometer. The software is build by Douwe Schulte and was started on 25-03-2019. It is build in collaboration with and under supervision of Joost Snijder, from the group "Massspectrometry and Proteomics" at the university of Utrecht.


## Classes
&nbsp;<table><tr><th></th><th>Class</th><th>Description</th></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="b63ab84e-4997-6bc4-30c3-9dc18797e022">Alphabet</a></td><td>
To contain an alphabet with scoring matrix to score pairs of amino acids</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="ff4e346f-08ba-ff2f-52cf-831920161b16">Assembler</a></td><td>
The Class with all code to assemble Peptide sequences.</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="9aa97fa2-84fc-c8b1-da89-3aa2201bdb11">CondensedNode</a></td><td>
Nodes in the condensed graph with a variable sequence length.</td></tr><tr><td>![Protected class](media/protclass.gif "Protected class")</td><td><a href="3a513cab-e9f4-46d5-d431-70252288f2ad">CSVReport</a></td><td /></tr><tr><td>![Protected class](media/protclass.gif "Protected class")</td><td><a href="7ddb05a9-2052-2270-9503-56670c695889">FASTAReport</a></td><td /></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="d65d0212-1180-88f3-6fa1-481ede3ebc8d">FileFormat</a></td><td>
To contain definitions for file formats</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="95952360-346f-6123-1094-b7f244704c71">FileFormat.Peaks</a></td><td>
To contain all options for PEAKS file formats</td></tr><tr><td>![Protected class](media/protclass.gif "Protected class")</td><td><a href="a6205e49-c336-fdc7-ded6-dad8ce480975">HelperFunctionality</a></td><td>
A class to store extension methods to help in the process of coding.</td></tr><tr><td>![Protected class](media/protclass.gif "Protected class")</td><td><a href="0ed51262-b756-8990-bdb4-16422dcd6dbd">HTMLReport</a></td><td>
An HTML report</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="20b9a2b2-fa49-d8b0-178f-ecc1c3c8d8d3">MetaData</a></td><td>
A class to hold all metadata handling in one place.</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="ac678e1f-459f-8cfa-a949-1d5cf1da84c7">MetaData.Fasta</a></td><td>
A struct to hold metainformation from fasta data.</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="d1977a21-291f-230f-7b00-abec543ec9fd">MetaData.FileIdentifier</a></td><td>
A identifier for a file, to hold information about where reads originate from.</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="8a18d4bc-7296-ed41-0dcf-8b92542f6855">MetaData.IMetaData</a></td><td>
The interface which proper metadata instances should implement.</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="52bbb3c7-b80c-b9ea-e31b-522b0f52fb5c">MetaData.None</a></td><td>
A metadata instance to contain no metadata so reads without metadata can also be handeled.</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="95ab4fc6-9aa1-c8e2-fcf3-efc763f2dddb">MetaData.Peaks</a></td><td>
A struct to hold metainformation from PEAKS data.</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="327f29f7-ef35-58ae-f8a5-1d2b1b3bcf7b">Node</a></td><td>
Nodes in the graph with a sequence length of K-1.</td></tr><tr><td>![Public class](media/pubclass.gif "Public class")</td><td><a href="429ff459-6f23-a30e-1663-0729c353b95c">OpenReads</a></td><td>
To contain all logic for the reading of reads out of files.</td></tr><tr><td>![Protected class](media/protclass.gif "Protected class")</td><td><a href="64c5f097-8d69-49e7-53c7-e61f28c51101">ParseCommandFile</a></td><td>
A class with options to parse a batch file.</td></tr><tr><td>![Private class](media/privclass.gif "Private class")</td><td><a href="9f13b772-a047-4fa3-fdbb-b24c50a98f9b">ParseCommandFile.KeyValue</a></td><td>
A class to save key value trees</td></tr><tr><td>![Private class](media/privclass.gif "Private class")</td><td><a href="4de915df-1985-2e46-d008-80eea2c14ed7">ParseCommandFile.KeyValue.Multiple</a></td><td>
A ValueType to contain multiple values</td></tr><tr><td>![Private class](media/privclass.gif "Private class")</td><td><a href="a04c6696-99ec-62b4-8537-03780d6803e9">ParseCommandFile.KeyValue.Single</a></td><td>
A ValueType for a single valued KeyValue</td></tr><tr><td>![Private class](media/privclass.gif "Private class")</td><td><a href="0f05c5cd-bd41-9e73-3488-0c38dbe19fb9">ParseCommandFile.KeyValue.ValueType</a></td><td>
An abstract class to represent possible values for a KeyValue</td></tr><tr><td>![Private class](media/privclass.gif "Private class")</td><td><a href="86fef9b8-965c-bb8b-3ad0-ad088dc80ecd">ParseCommandFile.ParseHelper</a></td><td>
A class with helper functionality for parsing</td></tr><tr><td>![Protected class](media/protclass.gif "Protected class")</td><td><a href="37f84b90-0db0-5f96-2f45-9db8d7380e3f">ParseException</a></td><td>
An exception to indicate some error while parsing the batch file</td></tr><tr><td>![Protected class](media/protclass.gif "Protected class")</td><td><a href="ae91a2a7-5d17-addb-6ef9-4835d6f3d235">Report</a></td><td>
To be a basepoint for any reporting options, handling all the metadata.</td></tr><tr><td>![Protected class](media/protclass.gif "Protected class")</td><td><a href="8ec59c9e-dba6-271d-8915-a73991424149">ToRunWithCommandLine</a></td><td>
The main class which is the entry point from the command line</td></tr></table>

## Structures
&nbsp;<table><tr><th></th><th>Structure</th><th>Description</th></tr><tr><td>![Public structure](media/pubstructure.gif "Public structure")</td><td><a href="906567b4-adec-2d74-6183-8174a5b7ae4d">AminoAcid</a></td><td>
A struct to function as a wrapper for AminoAcid information, so custom alphabets can be used in an efficient way</td></tr><tr><td>![Public structure](media/pubstructure.gif "Public structure")</td><td><a href="d0e73d2f-7721-7f22-e999-c1b9d612e2c9">MetaInformation</a></td><td>
A struct to hold meta information about the assembly to keep it organized and to report back to the user.</td></tr></table>

## Enumerations
&nbsp;<table><tr><th></th><th>Enumeration</th><th>Description</th></tr><tr><td>![Public enumeration](media/pubenumeration.gif "Public enumeration")</td><td><a href="4b6e1ce0-47f1-9a8d-80a8-d665a79bfe1a">Alphabet.AlphabetParamType</a></td><td>
To indicate if the given string is data or a path to the data</td></tr></table>&nbsp;
