# Whitespace compiler for .NET

In 2003 I was researching bytecode manipulation and I bumped into the Whitespace language developed by Edwin Brady and Chris Morris for fun ([read more on Wikipedia](https://en.wikipedia.org/wiki/Whitespace_%28programming_language%29)). At the time I was assistant Professor and I 
was contributing to Advance Programming course in the MSCS degree at University of Pisa and decided to implement a Whitespace compiler for .NET to showcase the basic structure of a compiler capable of emitting
actual binaries.

In 2025 I am the Professor of Advanced Programming and I thought it would have been fun to revive the **wspc** compiler and share it with students even though it is a very different course after 22 years.

I restored the code and ported it to .NET 9.0, I am pretty sure it can be improved by far but the original version was written for the early version of the C# compiler.

I did a dump of the original Web page of the language, and it seems I had a good idea because the page is now linked on Web archive.

In the repo you will find the original 2003 version, a port to .NET 4.8 (with little changes) and the current .NET core version.

In the repo you will find the **wspc** compiler and the **dews** tool to obtain a human readable version of a Whitespace program.

Hope you will have fun as I did!

Antonio Cisternino

