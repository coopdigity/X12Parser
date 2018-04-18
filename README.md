# X12Parser

This is an open source .NET Core C# implementation of an X12 Parser.

The parser allows for a specification of any X12 transaction set to create a generic X12 xml representation of the hierarchical data contained within the X12 document.

No database integration is required by design, though  you can use the ImportX12 app to parse into a SQL Server database and skip the XML.

# Usage
There are three libraries in the solution which form the foundation of the parser. Additionally, there are 6 console applications which are example implimentations which can be used as a basis for your own code. These include:

- AcknowledgeX12 - Can send 999 or 997 Acknowledgements
- Hipaa.ClaimParser - Processes HIPPA Claims
- ImportX12 - Imports X12 documents to SQL Database
- TransformToX12 - Transforms XML to X12 document
- UnbundleX12 - Unbundles a set of input files
- X12Parser - X12 Parser that transforms X12 to XML
