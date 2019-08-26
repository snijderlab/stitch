# AminoAcid.Homology Method 
 

Calculating homology, using the scoring matrix of the parent Assembler. See [!:Assembler.scoring_matrix] for the scoring matrix. See [!:Assembler.SetAlphabet] on how to change the scoring matrix.

**Namespace:**&nbsp;<a href="6bcc80ef-5cfd-db5f-1eb2-7297d1c16397">AssemblyNameSpace</a><br />**Assembly:**&nbsp;Main (in Main.exe) Version: 0.0.0.0

## Syntax

**C#**<br />
``` C#
public int Homology(
	AminoAcid right
)
```


#### Parameters
&nbsp;<dl><dt>right</dt><dd>Type: <a href="906567b4-adec-2d74-6183-8174a5b7ae4d">AssemblyNameSpace.AminoAcid</a><br />The other AminoAcid to use.</dd></dl>

#### Return Value
Type: <a href="http://msdn2.microsoft.com/en-us/library/td2s409d" target="_blank">Int32</a><br />Returns the homology score (based on the scoring matrix) of the two AminoAcids.

## Remarks
Depending on which rules are put into the scoring matrix the order in which this function is evaluated could differ. `a.Homology(b)` does not have to be equal to `b.Homology(a)`.

## See Also


#### Reference
<a href="906567b4-adec-2d74-6183-8174a5b7ae4d">AminoAcid Structure</a><br /><a href="6bcc80ef-5cfd-db5f-1eb2-7297d1c16397">AssemblyNameSpace Namespace</a><br />