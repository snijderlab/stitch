All releases can also be found here: https://github.com/snijderlab/stitch/releases.

# v1.3.0
_2022-10-21_

* Added graphs that show how ambiguous positions in the final sequence are connected (#176)
* Updated the reads alignment to be able to dynamically show reads, with options to only show CDR reads and show an overview (#162, #196)
* Updated overview of the main report to work better with large numbers of segments and groups (#135)
* Fixed a lot of bugs 
* Worked on the error messages to have a more helpful context in many cases

# v1.2.1
_2022-10-07_

* Fixed an issue were the html assets (styling and scripts) were excluded from the deploy
* Improved handling of multiple identical reads (#188)
* Fixed small issues in the Html structure
* Made improvements to the spectrum viewer (#186)

# v1.2.0 (Yanked)
_2022-10-06_

* Added support for raw files viewing in the HTML reports, see the manual for how to work with this, for now only works with Peaks data and with Thermo raw files (#97, #186)
* Created automated benchmarks (#178)
* Added help and data to the last missing places (#184, #126)
* Added a preview of the export data in graphs (#181)
* Normalized reads intensities always in range 0-1 (#172)
* Fixed small bug in highlighting templates in the scores plot (#182)
* Fixed a bug related to sequence annotation (#187)
* Lots of styling fixes and improvements in the HTML report, error messages and more

# v1.1.4
_2022-09-09_

* Added more information in recombination table (#169)
* Added a reverse lookup of reads (#167)
* Added sequence annotation in more places in the detail pages (#164, #179)
* Added a new flag (`--live`) which uses VS Code LiveServer to make development easier, note it has to be passed after the normal command
* Compressed the space taken by the CDR tables (#165)
* Added a warning when a CDR regions is defined multiple times in the same template (#163)
* Added a depth of coverage overview section on the main page
* Added an option to export the sequence consensus data (#159)
* Fixed various bugs and pieces of documentation

# v1.1.3
_2022-09-05_

* Added a new flag (`--open`) which will automatically open the HTML report once generated, note it has to be passed after the normal command
* Added help to some sections in the HTML report, which describes graphs and data points in more detail (#104)
* The `Runname` property now defaults to the filename (#153)
* Fixed bugs (#168, #170, #175, #177)

# v1.1.2
_2022-07-06_

* Cleaned up templates and download function
* Fixed a bug of Linux (#171)

# v1.1.1
_2022-05-23_

* Removed all duplicate sequences from the templates
* Updated FASTA output identifiers to align with the HTML output identifiers
* Updated manual

# v1.1.0
_2022-04-20_

* Added Novor.Cloud input option (#156, #158)
* Added templates for _Canis lupus familiaris_
* Fixed bugs (including #154, #155)
* Updated documentation, with more information on the scoring
* Updated the polyclonal example to use `GapTail` and `GapHead` by default

All previous valid batchfiles will continue to work. All new batchfiles will continue to work with older stitch versions, except that Novor.Cloud input is not possible.

# v1.0.0
_2022-03-07_

First stable release