# SingleRun Constructor (Int32, String, List(Input.Parameter), Int32, Int32, Int32, Boolean, AlphabetValue, List(Report.Parameter))
 

To create a single run with a multiple dataparameters as input

**Namespace:**&nbsp;<a href="4763cf1c-e4af-43c5-78fe-6f03f6e2281f">AssemblyNameSpace.RunParameters</a><br />**Assembly:**&nbsp;Main (in Main.exe) Version: 0.0.0.0

## Syntax

**C#**<br />
``` C#
public SingleRun(
	int id,
	string runname,
	List<Input.Parameter> input,
	int k,
	int duplicateThreshold,
	int minimalHomology,
	bool reverse,
	AlphabetValue alphabet,
	List<Report.Parameter> report
)
```


#### Parameters
&nbsp;<dl><dt>id</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/td2s409d" target="_blank">System.Int32</a><br />The ID of the run</dd><dt>runname</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/s1wwdcbf" target="_blank">System.String</a><br />The name of the run</dd><dt>input</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/6sh2ey19" target="_blank">System.Collections.Generic.List</a>(<a href="91de3ff0-c85c-6992-0f2b-c9c98f4b904a">Input.Parameter</a>)<br />The input data to be run</dd><dt>k</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/td2s409d" target="_blank">System.Int32</a><br />The value of K</dd><dt>duplicateThreshold</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/td2s409d" target="_blank">System.Int32</a><br />The value of DuplicateThreshold</dd><dt>minimalHomology</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/td2s409d" target="_blank">System.Int32</a><br />The value of MinimalHomology</dd><dt>reverse</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/a28wyd50" target="_blank">System.Boolean</a><br />The value of Reverse</dd><dt>alphabet</dt><dd>Type: <a href="d64a68f0-10f9-51b8-3095-a70fdba07974">AssemblyNameSpace.RunParameters.AlphabetValue</a><br />The alphabet to be used</dd><dt>report</dt><dd>Type: <a href="http://msdn2.microsoft.com/en-us/library/6sh2ey19" target="_blank">System.Collections.Generic.List</a>(<a href="483e04bc-c30d-62b5-b778-f095df93a3b3">Report.Parameter</a>)<br />The report(s) to be generated</dd></dl>

## See Also


#### Reference
<a href="af5c52aa-e355-ecee-14fb-728210fd89c2">SingleRun Class</a><br /><a href="1d51a8f1-a479-86d1-ce7e-d2a81cd77fae">SingleRun Overload</a><br /><a href="4763cf1c-e4af-43c5-78fe-6f03f6e2281f">AssemblyNameSpace.RunParameters Namespace</a><br />