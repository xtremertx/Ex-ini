[![Current version](http://test.the-one.cz/github/ex-ini/version.svg)](https://github.com/xtremertx/Ex-ini)
[![Current License](http://test.the-one.cz/github/ex-ini/license.svg)](https://github.com/xtremertx/Ex-ini/LICENSE.txt)
# Ex-ini
Extended INI library for .NET

Library is providing a fast, save and simple manipulation for ini files and ini content, together with additional extended features.

Under development right now, most features implemented!

# Version:

Current stable version - `2.0.0.3`

Features
--------

* Reads ini data from multiple different sources: file, stream or string
* Writes ini data to multiple different destinations: file, stream or ini object
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
* Merge functionality (merging two ini objects together)
* Adding of ini content into ini object as string

# Usage:

Creation of parser with own custom settings & parsing:
```C#
IniParser parser = new IniParser();
parser.OnError += (text, type) => { Console.WriteLine(text); };   // to see error, warning, infos..
parser.Settings = new IniParserSettings()   // not nessesary, there is default preset...
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

// Another way of creating INI object via associated parser
INI ini = new INI(input, parser.Settings, true);
parser.Load(ini); // parser will fill up INI object with parsed data

// Parsing ini content from file
var IniObject = parser.Load(@"C:\myfile.ini");

// Parsing ini content from string
string s = "x=xyz;#commenting first section;[XXX];KEY=VALUE;#comment;A=B;[C];[D:C];f=Z;[FIX];TEXT=LOL";
var IniObject2 = parser.Load(s, ';');

```

Editing ini object
```C#
// Add/Remove section
ini.AddSection(new Section("TEST"));
ini.RemoveSection("TEST");

// Remove all sections from the INI object
ini.Clear();

// Get section & change properties
var section = ini["MySection"];     // via indexer (can throw error if section do not exists)
section = TryGetSection("MySection");   // via method (error-free)
section.Name = "myNewName";             // change name of section
section.Notes.Add("My new note...");    // list of associated notes for section

// Read section/key
var value = ini.GetValue("Main", "Key");    // Read 'Key' from section 'Main'
var value1 = ini.GetValue("Main", "Key", "Yes");  // Read 'Key' from section 'Main' with default value set to 'Yes'
var value2 = ini.GetValue<bool>(true, Convert.ToBoolean, "Main", "Key2"); // Read again but convert the result into bool via existing or custom handler

// Add string containing ini data into some existing ini object
parser.AddStringData(ini, @"[NewSection];k=v;another=value;", ";");

// Enumerate ini object sections
foreach (var section in ini)
{
  Console.WriteLine(section.Name);
}

// Enumerate sections, section names via IEnumerable<T>
ini.GetSections();
ini.GetSectionNames();

// Check existence of section/key
if(ini.KeyExists("section", "key")) {}
if(ini.SectionExists("section")) {}

// Get 'key=value' pairs of some section
var pairs = ini.GetValues("section");

// Get global section, global means: '[]' or unnamed/unspecified section on the start of the file
var main = ini.Global;

// Get includes as Dictionary<string, List<Include>>, where 'string' is section where include(s) belongs and 'List<Include>' is the list of the includes
var incl = ini.Includes;

// Settings for this INI object used by the parser when interacting (some ini objects can have different settings)
var cfg = ini.Settings;

// Encoding & filename associated with this INI object (optional - used only by parser when parsing from file, these can be explicitly specified when parser 'Load(..)' is called, usefull when ur saving/loading content of same ini file with same encoding always)
ini.Encoding;
ini.Filename;

// Whole inheritance of some section, returns pairs (normally inheritance takes into account only base section)
ini.GetWholeInheritance(mySection);

// Monitoring changes on INI object (not all currently, like pairs)
ini.Changed += (str, state) => { Console.WriteLine("INI Action on key: {0}, state: {1}", str, state); };

// For more check constructors & overloads...
```

Section object
```C#
var mySection = ini.TryGetSection("section");

// Parent of the section, we derive from it
mySection.Base;

mySection["key"];
mySection.GetValue("key");  // error-free

mySection.GetValues(true);  // Returns pairs plus derived pairs, see overloads for more..
mySection.Pairs;  // Returns just pairs of the section, not derived ones

```

Saving of ini object
```C#
// Save as new filename (ini content is saved into specified location on HDD, same file is auto-overwritten!)
parser.Save(IniObject, @"C:\myfile_2.cfg");

// Print into console stream (ini content is written into console stream with specified encoding and formatting based on the settings of parser)
parser.Save(IniObject, Console.OpenStandardOutput(), ini.Encoding, new IniFormat(parser.Settings));

// Save into memory stream
using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
{
  parser.Save(ini, ms, ini.Encoding, new IniFormat(parser.Settings));
  // SEND DATA OVER NETWORK, ETC...
}
```

Merging two ini object together
```C#
// Merges all from section B into section A (like new pairs in B, ovveriden values from B, new sections in B, but not inheritance)
INI.Merge(ini1, ini2);
// Section B will override inheritance in section A
INI Merge(ini1, ini2, true);
// Section B will override or adds new includes from B into A, but not notes associated with includes in B (include notes are not supported right now in merging)
INI.Merge(ini1, ini2, true, true);

// First ini (A) will became MERGED INI, so backup ini A if you need it for some reason unchanged..
var backupIni = new INI()
{
  Encoding = UTF8Encoding.UTF8,
  Settings = parser.Setting,
};
INI.Merge(backupIni, ini1); // This will merge content of ini1 (A) into new clear ini (backup) -> deep copy

// Or you can call, but both object will share same data (weak copy), so do not call ini1.Clear()
parser.ReferenceCopyTo(ini1, backupIni);
```

Conclusion
----------

[!] As you can see you're always working with parser and some ini object(s), but there are also some other objects like IniFormat or objects used directly in ini object like Section, KeyValue, etc..

[!] Library can be targeted from .NET 3.5 to the newest framework version .NET 4.6.2

[?] Rest of the documentation will be added soon...
