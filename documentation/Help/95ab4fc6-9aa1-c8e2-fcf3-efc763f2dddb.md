# MetaData.Peaks Class
 

A struct to hold metainformation from PEAKS data.


## Inheritance Hierarchy
<a href="http://msdn2.microsoft.com/en-us/library/e5kfa45b" target="_blank">System.Object</a><br />&nbsp;&nbsp;AssemblyNameSpace.MetaData.Peaks<br />
**Namespace:**&nbsp;<a href="6bcc80ef-5cfd-db5f-1eb2-7297d1c16397">AssemblyNameSpace</a><br />**Assembly:**&nbsp;Main (in Main.exe) Version: 0.0.0.0

## Syntax

**C#**<br />
``` C#
public class Peaks : IMetaData
```

The MetaData.Peaks type exposes the following members.


## Constructors
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="36442433-aa28-c7c0-aebf-aba63353638f">MetaData.Peaks</a></td><td>
Create a PeaksMeta struct based on a CSV line in PEAKS format.</td></tr></table>&nbsp;
<a href="#metadata.peaks-class">Back to Top</a>

## Methods
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="http://msdn2.microsoft.com/en-us/library/bsc2ak47" target="_blank">Equals</a></td><td>
Determines whether the specified object is equal to the current object.
 (Inherited from <a href="http://msdn2.microsoft.com/en-us/library/e5kfa45b" target="_blank">Object</a>.)</td></tr><tr><td>![Protected method](media/protmethod.gif "Protected method")</td><td><a href="http://msdn2.microsoft.com/en-us/library/4k87zsw7" target="_blank">Finalize</a></td><td>
Allows an object to try to free resources and perform other cleanup operations before it is reclaimed by garbage collection.
 (Inherited from <a href="http://msdn2.microsoft.com/en-us/library/e5kfa45b" target="_blank">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="http://msdn2.microsoft.com/en-us/library/zdee4b3y" target="_blank">GetHashCode</a></td><td>
Serves as the default hash function.
 (Inherited from <a href="http://msdn2.microsoft.com/en-us/library/e5kfa45b" target="_blank">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="http://msdn2.microsoft.com/en-us/library/dfwy45w9" target="_blank">GetType</a></td><td>
Gets the <a href="http://msdn2.microsoft.com/en-us/library/42892f65" target="_blank">Type</a> of the current instance.
 (Inherited from <a href="http://msdn2.microsoft.com/en-us/library/e5kfa45b" target="_blank">Object</a>.)</td></tr><tr><td>![Protected method](media/protmethod.gif "Protected method")</td><td><a href="http://msdn2.microsoft.com/en-us/library/57ctke0a" target="_blank">MemberwiseClone</a></td><td>
Creates a shallow copy of the current <a href="http://msdn2.microsoft.com/en-us/library/e5kfa45b" target="_blank">Object</a>.
 (Inherited from <a href="http://msdn2.microsoft.com/en-us/library/e5kfa45b" target="_blank">Object</a>.)</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="69adaeea-2e07-b87b-b33a-b8a8197be1d6">ToHTML</a></td><td>
Generate HTML with all metainformation from the PEAKS data.</td></tr><tr><td>![Public method](media/pubmethod.gif "Public method")</td><td><a href="http://msdn2.microsoft.com/en-us/library/7bxwbwt2" target="_blank">ToString</a></td><td>
Returns a string that represents the current object.
 (Inherited from <a href="http://msdn2.microsoft.com/en-us/library/e5kfa45b" target="_blank">Object</a>.)</td></tr></table>&nbsp;
<a href="#metadata.peaks-class">Back to Top</a>

## Fields
&nbsp;<table><tr><th></th><th>Name</th><th>Description</th></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="809b6f60-db6d-8fc0-e19c-494972ecea8b">Area</a></td><td>
Area of the peak of the peptide.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="6fa0b527-de2f-235b-b5bb-1121fa91dcce">Charge</a></td><td>
z of the peptide.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="f77b94bb-6dbb-4d15-909e-0b9d64973743">Cleaned_sequence</a></td><td>
The sequence without modifications of the peptide.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="d6c6000b-abf5-518d-c641-d2ed1a2337c9">Confidence</a></td><td>
The confidence score of the peptide.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="af989a68-4988-56c9-5b42-181e960152b8">Feature</a></td><td>
The feature of the peptide.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="698871c1-7a29-8d03-117a-07e2709990a7">Fraction</a></td><td>
The Fraction number of the peptide.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="f4c55057-777b-8faf-3c4a-dc75248e81e4">Fragmentation_mode</a></td><td>
Fragmentation mode used to generate the peptide.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="05901720-a26f-795f-a5dd-a391fe14a78e">Local_confidence</a></td><td>
Local confidence scores of the peptide.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="6a385f3b-8fb1-fc06-e0dd-b2491e7ba77c">Mass</a></td><td>
Mass of the peptide.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="9244f721-0a94-f016-cabc-96d379bff7bd">Mass_over_charge</a></td><td>
m/z of the peptide.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="b2347af1-ede9-9876-2b5e-dcdda927a2a1">Original_tag</a></td><td>
The sequence with modifications of the peptide.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="9c05c5a9-2618-da31-8573-35eaab5605d9">Other_scans</a></td><td>
Other scans giving the same sequence.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="ea0dd8be-a62e-a0e2-97bb-2c743bf66ba8">Parts_per_million</a></td><td>
PPM of the peptide.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="d13ece69-1652-3d7f-b3e0-1885e214e5ad">Post_translational_modifications</a></td><td>
Posttranslational Modifications of the peptide.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="7fafdecd-2ec2-3bc7-b851-b5f3aa627162">Retention_time</a></td><td>
Retention time of the peptide.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="4c42ae2a-e1e8-ad62-bac7-06e29d421082">ScanID</a></td><td>
The scan identifier of the peptide.</td></tr><tr><td>![Public field](media/pubfield.gif "Public field")</td><td><a href="f37f2832-a22d-2ea4-d659-00808bc88858">Source_File</a></td><td>
The source file out of wich the peptide was generated.</td></tr></table>&nbsp;
<a href="#metadata.peaks-class">Back to Top</a>

## See Also


#### Reference
<a href="6bcc80ef-5cfd-db5f-1eb2-7297d1c16397">AssemblyNameSpace Namespace</a><br />