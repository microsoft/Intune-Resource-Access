
# CsrValidation Project

This project consists of libraries to help integrate Third Party SCEP SErvers with Intune's SCEP Management Solution.

See [Use APIs to add third-party CAs for SCEP to Intune](https://docs.microsoft.com/en-us/mem/intune/protect/scep-libraries-apis) for complete documentation on how to use the libraries.

# How to build Java

The java source code was meant to be built with maven although any java compiler will work.  To build with maven check out the source code and change to the root of the java directory.  Then run:

mvn package

If you would like to execute the example run:

mvn package exec:java -e

# How to build C#

The C# source code was meant to be built with visual studio.  Open up the lib.sln file and run a nomral build inside of visual studio.  If you would like to execute the example set the example as your startup project and click run inside visual studio.
