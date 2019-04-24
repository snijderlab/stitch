# Planning Research Project - Douwe Schulte - March to July 2019

## General plan

I want to build software to be able to find the amino acid sequence of a protein based on mass spectrometry data. The article will be describing the general algorithm used in the software and will be stating some details about the verification of the software based on wetlab praticals and will give the sequence of an "unknown" protein to indicate the use of the software.

## Research question

How can mass spectrometry data be used to find the sequence of a protein using software? (Keep it on antibodies)

To aid in the bulk identification of proteins in samples, especially in the sequencing of antibodies.

## Planning

3.5 days of preliminairy work on the software before the course started\
1 week of project planning and prework\
3 weeks of wetlab to generate the mass spectrometry data used to test the software\
2 weeks to finish the software\
5 weeks to write the article


| Monday | Description |
|--------|-------------|
| 22-04 | Start week - project planning |
| 29-04 | Wetlab |
| 06-05 | Wetlab |
| 13-05 | Wetlab |
| 20-05 | Finish software - work on goals |
| 27-05 | Finish software - finishing touches & eliminating bugs |
| 03-06 | Start article |
| 10-06 | Deliver article for peer review (12-06) |
| 17-06 | Deliver article for examinor review (19-06) & Deliver poster (21-06) |
| 24-06 | Finish article - with the feedback |
| 01-07 | Deliver final article (05-07) |

## Goals

### Wetlab

1. Measure three well known antibodies
   1. To optimise the protocol
   1. To use as verification of the software
1. Measure one unknown antibody with the protocol from step 1

### Finish software

##### Needed
1. Create useful output (finish the graph compression)
1. Use the output from the mass spectrometry as input
1. Runtime analysis 
   1. To find possible optimisations in the algorithm
   1. To be able to estimate runtimes for larger datasets

##### Would be nice
1. Using masses instead or in conjunction with amino acid information
1. Associate reads (and k-mers) with output, to make the output verifiable for humans

### Article

1. Find literature in this field to write a good introduction on how far the field is
1. Quantify and figurify the results of the wetlab used in the software
1. Figurify the algorithm to make it at least a little bit understandable to non-programmers
1. Compare the software with commercially available softwares to get a feeling for the usefulness of the software
1. Plan / tips
   1. To find the best protocol for the wetlab part
   1. How to find the best settings for the software
1. Conclusion on the usefulness of the software
1. Discussion what are the weakspots of the method (wetlab + software)

### Poster

1. Introduction
1. Figures for the wetlab part
1. Figures for the algorithm
1. Figures for the comparison
1. Conclusion