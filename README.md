[![Current version](http://test.the-one.cz/github/ex-ini/version.svg)](https://github.com/xtremertx/Ex-ini)
[![Current License](http://test.the-one.cz/github/ex-ini/license.svg)](https://github.com/xtremertx/Ex-ini)
# Ex-ini
Extended INI library for .NET

Library is providing a fast, save and simple manipulation for ini files and ini content, together with additional extended features.

Under development right now, most features implemented!

# Version:

Current stable version - `2.0.0.3`

Features
--------

* Read ini data from multiple different sources: file, stream or string
* Write ini data to multiple different destinations: file, stream or ini object
* Manipulation with notes (w/r)
* Generate real-time ini from string (using [`String.Format`](https://msdn.microsoft.com/cs-cz/library/system.string.format.aspx) parameters or [`String.Concat`](https://msdn.microsoft.com/cs-cz/library/system.string.concat.aspx) variables)
* Specify custom output formating (by deriving own one)
* Specify own settings for parser (on/off functionality + specify format of ini data)
* Extended features: Inheritance between sections, overriding by new values, including ini files in ini file
* Functions with exceptions and without exceptions (choise is yours)
* Generic conversion of values (in form of methods and own convertors)
* Replacing of non-valid values with custom default ones
* Detection of file encoding or user specified encoding ([`BOM detection`](https://en.wikipedia.org/wiki/Byte_order_mark))
* OOP manipulation with ini data (sections, pairs, etc.)
* Merge functionality (merging two .ini objects together)
* Adding of ini content into ini object as string

# Usage:

Creation of parser with own custom settings & parsing:
```C#
IniParser parser = new IniParser();
parser.OnError += (text, type) => { Console.WriteLine(text); };
parser.Settings = new IniParserSettings()
{
  CaseSensitiveSections = true,
  CreateGlobalSection = true,
  EndSectionWithBlankLine = false,
  PostNotesToLastSection = true,
  BlankLinesAsNotes = true,
  ReadNotes = true,
  WriteNotes = true,
  Includes = UseOfIncludes.Read,
  NoteSymbol = "#",
  DefaultExtension = ".ex-ini",
  InheritanceChar = ":",
  SectionStartChar = "[",
  SectionEndChar = "]",
  PairOperator = "=",
  RelativePathSymbol = @"~",
  IncludeStartChar = "<",
  IncludeEndChar = ">",
};

// Parsing ini content from file
var IniObject = parser.Load(@"C:\myfile.ini");

//Parsing ini content from string
string s = "x=xyz;#commenting first section;[XXX];KEY=VALUE;#comment;A=B;[C];[D:C];f=Z;[FIX];TEXT=LOL";
var IniObject2 = parser.Load(s, ';');


```

Editing ini object
```C#
// Add/Remove section
ini.AddSection(new Section("TEST"));
ini.RemoveSection("TEST");

// Get section & change properties
var section = ini["MySection"];     // via indexer
section = TryGetSection("MySection");   // via method
section.Name = "myNewName";
section.Notes.Add("My new note...");

// Read section/key
var value = ini.GetValue("Main", "Key");    // Read 'Key' from section 'Main'
var value1 = ini.GetValue("Main", "Key", "Yes");  // Read 'Key' from section 'Main' with default value set to 'Yes'
var value2 = ini.GetValue<bool>(true, Convert.ToBoolean, "Main", "Key2"); // Read again but convert result into bool via handler

// Add string ini data into ini object
parser.AddStringData(ini, @"[NewSection];k=v;another=value;", ";");

// Enumerate ini object sections
foreach (var section in ini)
{
Console.WriteLine(section.Name);
}

// And much more...
```

Saving of INIObject
```C#
// Save as new filename
parser.Save(IniObject, @"C:\myfile_2.cfg");
// Print into console stream
parser.Save(IniObject, Console.OpenStandardOutput(), ini.Encoding, new IniFormat(parser.Settings));
```

Conclusion
----------

[!] As you can see you're always working with parser and some ini object(s), but there are also some other object like IniFormat or objects used directly in ini object like Section, KeyValue, etc..

[!] Library can be targeted from .NET 3.5 to the newest framework version .NET 4.6.2

[?] Rest of the documentation will be added soon...



