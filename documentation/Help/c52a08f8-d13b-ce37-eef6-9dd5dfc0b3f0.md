# MetaData.Peaks Constructor 
 

Create a PeaksMeta struct based on a CSV line in PEAKS format.

**Namespace:**&nbsp;<a href="6bcc80ef-5cfd-db5f-1eb2-7297d1c16397">AssemblyNameSpace</a><br />**Assembly:**&nbsp;Main (in Main.exe) Version: 0.0.0.0

## Syntax

**C#**<br />
``` C#
public Peaks(
	string line,
	char separator,
	char decimalseparator,
	FileFormat.Peaks pf,
	MetaData.FileIdentifier file
)
```


#### Parameters
&nbsp;<dl><dt>line</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/s1wwdcbf" target="_blank">System.String</a><br />The CSV line to parse.</dd><dt>separator</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/k493b04s" target="_blank">System.Char</a><br />The separator used in CSV.</dd><dt>decimalseparator</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/k493b04s" target="_blank">System.Char</a><br />The separator used in decimals.</dd><dt>pf</dt><dd>Type: <a href="95952360-346f-6123-1094-b7f244704c71">AssemblyNameSpace.FileFormat.Peaks</a><br />FileFormat of the PEAKS file</dd><dt>file</dt><dd>Type: <a href="d1977a21-291f-230f-7b00-abec543ec9fd">AssemblyNameSpace.MetaData.FileIdentifier</a><br />Identifier for the originating file</dd></dl>

## See Also


#### Reference
<a href="95ab4fc6-9aa1-c8e2-fcf3-efc763f2dddb">MetaData.Peaks Class</a><br /><a href="6bcc80ef-5cfd-db5f-1eb2-7297d1c16397">AssemblyNameSpace Namespace</a><br />