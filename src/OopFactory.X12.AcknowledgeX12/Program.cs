using System;
using System.Text;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.IO;
using OopFactory.X12.Parsing;
using OopFactory.X12.Parsing.Model;
using OopFactory.X12.Validation;
using OopFactory.X12.Validation.Model;
using Microsoft.Extensions.Configuration;

namespace OopFactory.X12.AcknowledgeX12
{
    class Program
    {
        static void Main(string[] args)
        {

            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true, true).Build();

            string inputFilename = args[0];
            string outputFilename = args[1];
            string isaControlNumber = args.Length > 2 ? args[2] : "999";
            string gsControlNumber = args.Length > 3 ? args[3] : "99";

            var spec = new SpecificationFinder();
            
            
            var service = new X12AcknowledgmentService(spec);

            using (FileStream fs = new FileStream(inputFilename, FileMode.Open, FileAccess.Read))
            {
                using (X12StreamReader reader = new X12StreamReader(fs, Encoding.UTF8))
                {
                    
                    var firstTrans = reader.ReadNextTransaction();
                    var isa = reader.CurrentIsaSegment;
                    var gsa = reader.CurrentGsSegment;
                    if (reader.LastTransactionCode == "837")
                    {
                        if (reader.TransactionContainsSegment(firstTrans.Transactions[0], "SV2"))
                            service = new InstitutionalClaimAcknowledgmentService();
                        if (reader.TransactionContainsSegment(firstTrans.Transactions[0], "SV1"))
                            service = new X12AcknowledgmentService(new ProfessionalClaimSpecificationFinder());
                    }
                }
            }

            using (FileStream fs = new FileStream(inputFilename, FileMode.Open, FileAccess.Read))
            {
                // Create aknowledgements and identify errors
                var responses = service.AcknowledgeTransactions(fs);

                // Change any acknowledgment codes here to reject transactions with errors
                // CUSTOM BUSINESS LOGIC HERE

                // Transform to outbound interchange for serialization
                var interchange = new Interchange(DateTime.Now, int.Parse(isaControlNumber), true);
                interchange.AuthorInfoQualifier = config["AuthorInfoQualifier"];
                interchange.AuthorInfo = config["AuthorInfo"];
                interchange.SecurityInfoQualifier = config["SecurityInfoQualifier"];
                interchange.SecurityInfo = config["SecurityInfo"];
                interchange.InterchangeSenderIdQualifier = config["InterchangeSenderIdQualifier"];
                interchange.InterchangeSenderId = config["InterchangeSenderId"];
                interchange.InterchangeReceiverIdQualifier = responses.First().SenderIdQualifier;
                interchange.InterchangeReceiverId = responses.First().SenderId;
                //interchange.SetElement(12, "00501");


                var group = interchange.AddFunctionGroup("FA", DateTime.Now, int.Parse(responses.First().GroupControlNumber));
                group.ApplicationSendersCode = responses.First().ApplicationSendersCode;
                group.ApplicationReceiversCode = responses.First().ApplicationReceiversCode;
                group.VersionIdentifierCode = responses.First().VersionIdentifierCode; // "004010";

                group.Add997Transaction(responses);

                // This is a demonstration example only, change true to false to create continous x12 without line feeds.
                File.WriteAllText(outputFilename, interchange.SerializeToX12(true));
            }
        }
    }
}
