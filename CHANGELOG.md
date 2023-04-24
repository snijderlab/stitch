All releases can also be found here: https://github.com/snijderlab/stitch/releases.

# Upcoming Release

* Added loading of sequences from mmCIF files, for ModelAngelo (#207, #217)
* Added loading of sequences from Casanovo, pNovo, and MaxNovo (#192)
* Added loading of spectra from Casanovo, pNovo, and MaxNovo (#195)
* Added I/L disambiguation based on satellite ions (#193, #216, #218)
* Added more customization options for peptide fragmentation
* Added hash for all used files in the HTML report (#229)
* Added the option to call parts of a batchfile from another file (`include!(<path>)`) see the examples (#111, #209)
* Updated the main overview header (#212)
* Updated the command line interface (CLI) to be more friendly, *Note: calling stitch has changed to `stitch run <path>`*
* Updated the batchfile parsing to provide more helpful error messages
* Updated the segment overview tree, always use Blosum62, ability to not generate it (`BuildTree: False`) (#213)
* Updated the exact scoring of reads to handle longer alignments better, as well as locally enforcing unique placement (#215)
* Updated segment joining to use the same alignment scoring as template matching (#128)
* Deprecated the use of booleans with `EnforceUnique`, for now will keep working with a warning but support will be dropped at some point.
* Fixed small remaining intensity \[1-2\] scaling for Novor reads to scale \[0-1\]
* Fixed issues with the generation of the consensus sequences in relation to the mass alignment and I/L disambiguation (#220)
* Fixed leading insertions in reads placement displayed in a different colour, for more clarity in the alignment
* Fixed bugs (#222, #230)
* Performance improvements

Note
* The benchmarks show a mostly very slightly positive result, with the caveat that I/L disambiguation is not automatically benchmarked yet because raw files are so big.
* Performance has been improved with a couple of tiny steps. The I/L disambiguation though adds extra work and so results in longer runs when used, but this scaling is linear with the number of input reads. The move to net7.0 also gives a bit of performance improvement.

Breaking changes
* The CLI has changed: `stitch <batchfile>` has been changed to `stitch run <batchfile>` to better group the applicable arguments for all subcommands. You can use `--help` to get general help or `<subcommand> --help` to get help on that subcommand.
* The CSV export has been altered by adding more columns with I/L disambiguation specific information. The order of the other columns has not been changed.

# v1.4.0
_2022-12-20_

* Implemented alignment which can take bigger patches into account, see `Alphabet` in the manual or any of the examples. (#197)
* Implemented a more gradual variant of `EnforceUnique` now it can take all reads that score at least `x` * the highest score. (#146)
* Added a column indicating if a read is placed on a CDR position in the CSV output. (#200)
* Fixed many small bugs in the batchfile parsing. (#205)
* Added support for high contrast theme settings when viewing the HTML report.
* Moved to a hybrid local/global alignment which aligns the reads globally while aligning the template locally. (#157)
* Moved the `RawDataDirectory` to single peaks definitions instead of having a global setting.

Note
* The benchmarks show a mixed result of the mass based alignment, but as the identity is good enough in comparison with the previous results and the mass based alignment shows very promising result when looked at a case by case basis this result is justified. In the future with more tweaking of the parameters the identity could very likely even be better.
* Performance of this new alignment is worse, a run likely will take 3 times longer, which is expected based on the algorithmic complexity of the mass based alignment algorithm.

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