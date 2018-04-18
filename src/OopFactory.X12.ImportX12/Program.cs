using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using OopFactory.X12.Parsing;
using OopFactory.X12.Repositories;
using System.IO;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;

namespace OopFactory.X12.ImportX12
{
    class Program
    {
        static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true).Build();
            string dsn = config["connectionString"];
            
            bool throwExceptionOnSyntaxErrors = config["ThrowExceptionOnSyntaxErrors"] == "true";
            string[] segments = config["IndexedSegments"].Split(',');
            string parseDirectory = config["ParseDirectory"];
            string parseSearchPattern = config["ParseSearchPattern"];
            string archiveDirectory = config["ArchiveDirectory"];
            string failureDirectory = config["FailureDirectory"];
            string sqlDateType = config["SqlDateType"];
            int segmentBatchSize = Convert.ToInt32(config["SqlSegmentBatchSize"]);

            var specFinder = new SpecificationFinder();
            var parser = new X12Parser(throwExceptionOnSyntaxErrors);
            parser.ParserWarning += new X12Parser.X12ParserWarningEventHandler(parser_ParserWarning);
            var repo = new SqlTransactionRepository<int>(dsn, specFinder, segments, config["schema"], config["containerSchema"], segmentBatchSize, sqlDateType);

            foreach (var filename in Directory.GetFiles(parseDirectory, parseSearchPattern, SearchOption.AllDirectories))
            {
                byte[] header = new byte[6];
                using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    // peak at first 6 characters to determine if this is a unicode file
                    fs.Read(header, 0, 6);
                    fs.Close();
                }
                Encoding encoding = (header[1] == 0 && header[3] == 0 && header[5] == 0) ? Encoding.Unicode : Encoding.UTF8;


                var fi = new FileInfo(filename);
                using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    try
                    {
                        var interchanges = parser.ParseMultiple(fs, encoding);
                
                        foreach (var interchange in interchanges)
                        {
                            repo.Save(interchange, filename, Environment.UserName);
                        }
                        if (!string.IsNullOrWhiteSpace(archiveDirectory))
                            MoveTo(fi, parseDirectory, archiveDirectory);
                    }
                    catch (Exception exc)
                    {
                        Trace.TraceError("Error parsing {0}: {1}\n{2}", fi.FullName, exc.Message, exc.StackTrace);
                        if (!string.IsNullOrEmpty(failureDirectory))
                                MoveTo(fi, parseDirectory, failureDirectory);
                    }
                }
            }
        }

        private static void MoveTo(FileInfo fi, string sourceDirectory, string targetDirectory)
        {
            string targetFilename = string.Format("{0}{1}", targetDirectory, fi.FullName.Replace(sourceDirectory, ""));
            FileInfo targetFile = new FileInfo(targetFilename);
            try
            {
                if (!targetFile.Directory.Exists)
                {
                    targetFile.Directory.Create();
                }
                fi.MoveTo(targetFilename);
            }
            catch (Exception exc2)
            {
                Trace.TraceError("Error moving {0} to {1}: {2}\n{3}", fi.FullName, targetFilename, exc2.Message, exc2.StackTrace);
            }
        }

        static void parser_ParserWarning(object sender, X12ParserWarningEventArgs args)
        {
            Trace.TraceWarning("Error parsing interchange {0} at position {1}: {2}", args.InterchangeControlNumber, args.SegmentPositionInInterchange, args.Message);
        }
    }
}
