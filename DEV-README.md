# Releases

Be sure to do all of the following:
* Update version number in `.csproj`
* Update version number in `CITATION.cff`
* Update version number in all example batchfiles
* Update changelog with the new features (see previous entries for the style)
* Commit and push
* Create the release on Github
    * Tag with the correct version
    * Copy over the changelog to the description and add a link to the changelog
* Wait until the tests have completed and the binaries have built
* Attach the binaries generated by the release action to the github release.
