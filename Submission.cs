using System;
using System.Xml.Schema;
using System.Xml;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Net;


/**
 * This template file is created for ASU CSE445 Distributed SW Dev Assignment 4.
 * Please do not modify or delete any existing class/variable/method names. However, you can add more variables and functions.
 * Uploading this file directly will not pass the autograder's compilation check, resulting in a grade of 0.
 * **/


namespace ConsoleApp1
{


    public class Submission
    {
        // URLs of the hosted files (read by the autograder - keep variable names unchanged).
public static string xmlURL =
    "https://bunnie114.github.io/cse445_a4/NationalParks.xml";

public static string xmlErrorURL =
    "https://bunnie114.github.io/cse445_a4/NationalParksErrors.xml";

public static string xsdURL =
    "https://bunnie114.github.io/cse445_a4/NationalParks.xsd";

        public static void Main(string[] args)
        {
            // Test 1: valid XML against its XSD -> expect "No errors are found"
            string result = Verification(xmlURL, xsdURL);
            Console.WriteLine(result);

            // Test 2: XML with injected faults -> expect a list of error messages
            result = Verification(xmlErrorURL, xsdURL);
            Console.WriteLine(result);

            // Test 3: convert the valid XML to JSON
            result = Xml2Json(xmlURL);
            Console.WriteLine(result);
        }

        // Q2.1
        // Validates the XML at xmlUrl against the XSD at xsdUrl.
        // Returns "No errors are found" or a text listing every error found.
        public static string Verification(string xmlUrl, string xsdUrl)
        {
            StringBuilder errors = new StringBuilder();

            try
            {
                // Load the schema from its URL
                XmlSchemaSet schemas = new XmlSchemaSet();
                using (StringReader xsdText = new StringReader(DownloadContent(xsdUrl)))
                using (XmlReader xsdReader = XmlReader.Create(xsdText))
                {
                    schemas.Add(null, xsdReader);
                }

                // Configure a validating reader; every validation problem is collected, not just the first
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.ValidationType = ValidationType.Schema;
                settings.Schemas = schemas;
                settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
                settings.ValidationEventHandler += delegate (object sender, ValidationEventArgs e)
                {
                    errors.AppendLine(e.Severity + ": " + e.Message
                        + " (Line " + e.Exception?.LineNumber + ", Position " + e.Exception?.LinePosition + ")");
                };

                // Read the whole XML document so the validator visits every node
                using (StringReader xmlText = new StringReader(DownloadContent(xmlUrl)))
                using (XmlReader reader = XmlReader.Create(xmlText, settings))
                {
                    while (reader.Read()) { }
                }
            }
            catch (XmlException ex)
            {
                // Not well-formed (e.g., missing closing tag) - parsing cannot continue past this point
                errors.AppendLine("XML Error: " + ex.Message + " (Line " + ex.LineNumber + ", Position " + ex.LinePosition + ")");
            }
            catch (XmlSchemaException ex)
            {
                errors.AppendLine("Schema Error: " + ex.Message);
            }
            catch (Exception ex)
            {
                errors.AppendLine("Error: " + ex.Message);
            }

            return errors.Length == 0 ? "No errors are found" : errors.ToString().TrimEnd();
        }

        // Q2.2
        // Downloads the XML and converts it to JSON text that Newtonsoft can de-serialize back to XML.
        public static string Xml2Json(string xmlUrl)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(DownloadContent(xmlUrl));

            // Drop the XML declaration so it does not show up in the JSON
            if (doc.FirstChild is XmlDeclaration)
            {
                doc.RemoveChild(doc.FirstChild);
            }

            // Force NationalPark and Phone to always be JSON arrays, even when only one occurs
            const string jsonNs = "http://james.newtonking.com/projects/json";
            foreach (XmlNode node in doc.SelectNodes("//*[local-name()='NationalPark' or local-name()='Phone']"))
            {
                XmlAttribute arrayAttr = doc.CreateAttribute("json", "Array", jsonNs);
                arrayAttr.Value = "true";
                node.Attributes.Append(arrayAttr);
            }

            // Attributes become "@Name" properties; omitted attributes (e.g., Rating) simply do not appear
            string jsonText = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented);
            return jsonText;
        }

        // Helper method to download content from URL
        private static string DownloadContent(string url)
        {
            // GitHub Pages requires TLS 1.2 (not on by default in older .NET Framework versions)
            ServicePointManager.SecurityProtocol |= (SecurityProtocolType)3072;
            using (System.Net.WebClient client = new System.Net.WebClient())
            {
                return client.DownloadString(url);
            }
        }
    }

}
