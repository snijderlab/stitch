# Planning Research Project - Douwe Schulte - March to July 2019

## General plan

I want to build software to be able to find the amino acid sequence of a protein based on mass spectroscopy data. The paper will be describing the general algorithm used in the software and will be stating some details about the trustworthynes of the software based on wetlab praticals and will give the sequence of an "unknown" protein to really do a thorough test.

## Research question

How can mass spectroscopy data be used to find the sequence of a protein using software.

To aid in the bulk identification of proteins in samples, especially in the sequencing of antibodies.

## Planning

3.5 days of preliminairy work on the software before the course started\
1 half week of project planning and prework\
3 half weeks of wetlab to generate the mass spec data used to test the software\
2 half weeks to finish the software\
5 half weeks to write the article


| Monday | Description |
|--------|-------------|
| 22-04 | Start week - project planning |
| 29-04 | Wetlab & Give feedback on butterflies article (03-05) |
| 06-05 | Wetlab |
| 13-05 | Wetlab |
| 20-05 | Finish software |
| 27-05 | Finish software |
| 03-06 | Start article |
| 10-06 | Deliver article for peer review (12-06) |
| 17-06 | Deliver article for examinor review (19-06) & Deliver poster (21-06) |
| 24-06 | Work on feedback |
| 01-07 | Deliver final article (05-07) |

## Goals

### Wetlab

1. Measure three well known antibody and try to find the best way to prepare them for the mass spectroscopy. 
1. Measure one unknown antibody with the protocol from step 1.

### Finish software

#### Needed
1. Create usefull output (finish the graph compression)
1. Use the output from the mass spectroscopy as input
1. Runtime analysis to find "weakspots" in the algorithm and to be able to estimate runtimes for larger datasets

#### Would be nice
1. Using masses instead or in conjunction with amino acid information
1. Assiociate reads (and k-mers) with output, to make the output verifiable for humans

### Article

1. Find literature in this field to write a good introduction on how far the field is
1. Quantify and figurify the results of the wetlab used in the software
1. Figurify the algorithm to make it at least a little bit understandable to nonprogrammers
1. Compare the software with commercially availeble softwares to get a feeling for the usefullness of the software
1. Plan to find the best settings for the wetlab part and how to find the best settings for the software
1. Conclusion on the usefullness of the software
1. Discussion what are the weakspots of the method (wetlab + software)

### Poster

1. Introduction
1. Figures for the wetlab part
1. Figures for the algorithm
1. Figures for the comparison
1. Conclusion