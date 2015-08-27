# MEFMetadata
Metadata-based part discovery for MEF, faster than reflection.

This is an experimental prototype and a work in progress to replace MEF's slow reflection-based discovery of parts with faster discovery based on a fast low-level metadata reader (System.Reflection.Metadata from Roslyn).

In some cases it was shown to reduce the MEF composition time in half or even more (e.g. 1.4 sec vs. 3.0 sec). When measuring performance, be mindful of the fact that you will still be paying for assembly loading, JITting, type initialization (static constructors etc) and the actual composition that still requires reflection. Only the slow "groveling" phase of the composition is made faster, where you scan through all types and members to discover attributes.

The whole deal is even possible because .NET metadata has a special table for custom attributes and it is very easy and fast to scan through it to discover all custom attributes in an assembly. Things get more complicated when you need to remember that attributes may be derived from ExportAttribute and also inherited exports require to check all base types (currently inherited exports are still not implemented/supported).

## Usage:
reference System.ComponentModel.Composition.MetadataCatalog.dll and replace usages of AssemblyCatalog with MetadataAssemblyCatalog in your MEF code (also try MetadataDirectoryCatalog).
Use CompositionDumper.WriteTo(myCompositionContainer, "dump.txt") to output the contents of the composition before and after using MetadataAssemblyCatalog. If you diff the files, they should be identical. If they're not, it means the library has a bug, please open a GitHub issue.

## NuGet
https://www.nuget.org/packages/System.ComponentModel.Composition.MetadataCatalog

## Current known issues:
 * inherited exports are not supported yet.
 * be careful of where the assembly actually gets loaded vs. the file path you pass to MetadataAssemblyCatalog. It may happen (though unlikely) that the assembly will be loaded from a different location then what you specified. In this case you might get weird exceptions, check Debug -> Windows -> Modules to see where the assembly is being loaded from.
 
 Use at your own risk, this library may have bugs and may provide wrong composition and in the wrong order (missing or reordered parts).
