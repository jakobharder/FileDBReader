﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using FileDBReader.src.XmlRepresentation;
using FileDBReader_Tests;
using FileDBSerializing.ObjectSerializer;
using FileDBSerializing.Tests.TestData;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentAssertions;

namespace FileDBSerializing.Tests
{
    [TestClass]
    public class ObjectSerializerTest
    {
        [TestMethod]
        public void SerializeTest_V1() 
        {
            var expected = File.OpenRead("FileDBSerializer/Testfiles/objectserializing/version1.filedb");

            var obj = TestDataSources.GetTestAsset();
            FileDBSerializer<RootObject> objectserializer = new FileDBSerializer<RootObject>(FileDBDocumentVersion.Version1);
            
            Stream result = new MemoryStream();
            objectserializer.Serialize(result, obj);

            Assert.IsTrue(FileConversionTests.StreamsAreEqual(expected, result));
        }

        [TestMethod]
        public void SerializeTest_V2()
        {
            var expected = File.OpenRead("FileDBSerializer/Testfiles/objectserializing/version2.filedb");

            var obj = TestDataSources.GetTestAsset();
            FileDBSerializer<RootObject> objectserializer = new FileDBSerializer<RootObject>(FileDBDocumentVersion.Version2);
            MemoryStream result = new MemoryStream();
            objectserializer.Serialize(result, obj);

            Assert.IsTrue(FileConversionTests.StreamsAreEqual(expected, result));
        }

        [TestMethod]
        public void DeserializeTest_V1()
        {
            var x = File.OpenRead("FileDBSerializer/Testfiles/objectserializing/version1.filedb");

            DocumentParser<FileDBDocument_V1> parser = new();
            IFileDBDocument doc = parser.LoadFileDBDocument(x);

            FileDBDocumentDeserializer<RootObject> objectdeserializer = new FileDBDocumentDeserializer<RootObject>(new() { Version = FileDBDocumentVersion.Version1 });

            var DeserializedDocument = objectdeserializer.GetObjectStructureFromFileDBDocument(doc);

            DeserializedDocument.Should().BeEquivalentTo(TestDataSources.GetTestAsset());
        }

        [TestMethod]
        public void DeserializeTest_V2()
        {
            var x = File.OpenRead("FileDBSerializer/Testfiles/objectserializing/version2.filedb");

            DocumentParser<FileDBDocument_V2> parser = new();
            IFileDBDocument doc = parser.LoadFileDBDocument(x);

            FileDBDocumentDeserializer<RootObject> objectdeserializer = new FileDBDocumentDeserializer<RootObject>(new() { Version = FileDBDocumentVersion.Version1 });

            var DeserializedDocument = objectdeserializer.GetObjectStructureFromFileDBDocument(doc);

            DeserializedDocument.Should().BeEquivalentTo(TestDataSources.GetTestAsset());
        }

        [TestMethod]
        public void StaticConvertTest_Serialize()
        {
            var expected = File.OpenRead("FileDBSerializer/Testfiles/objectserializing/version2.filedb");

            var obj = TestDataSources.GetTestAsset();

            Stream Result = FileDBConvert.SerializeObject(obj, new() { Version = FileDBDocumentVersion.Version2 });

            Assert.IsTrue(FileConversionTests.StreamsAreEqual(expected, Result));
        }

        [TestMethod]
        public void StaticConvertTest_Deserialize()
        {
            var source = File.OpenRead("FileDBSerializer/Testfiles/objectserializing/version2.filedb");
            RootObject? result = FileDBConvert.DeserializeObject<RootObject>(source, new() { Version = FileDBDocumentVersion.Version2 });

            result.Should().BeEquivalentTo(TestDataSources.GetTestAsset());
        }

        private class FlatStringArrayContainer
        {
            [FlatArray]
            public string[]? Item { get; set; }
        }

        [TestMethod()]
        public void DeSerializeFlatStringArrayNull()
        {
            static Stream stream(string x) => new MemoryStream(Encoding.Unicode.GetBytes(x));

            // load from XML
            XmlDocument xmlDocument = new();
            xmlDocument.Load(stream("<Content></Content>"));
            IFileDBDocument doc = new XmlFileDbConverter<FileDBDocument_V1>().ToFileDb(xmlDocument);

            // serialize & deserialize
            FileDBDocumentDeserializer<FlatStringArrayContainer> deserializer = new(new() { Version = FileDBDocumentVersion.Version1 });
            var obj = deserializer.GetObjectStructureFromFileDBDocument(doc);

            Assert.IsNotNull(obj);
            Assert.IsNull(obj.Item);

            FileDBDocumentSerializer serializer = new(new() { Version = FileDBDocumentVersion.Version1 });
            doc = serializer.WriteObjectStructureToFileDBDocument(obj);

            // convert back to xml
            xmlDocument = new FileDbXmlConverter().ToXml(doc);
            Assert.AreEqual("<Content />", xmlDocument.InnerXml);
        }

        [TestMethod()]
        public void DeSerializeFlatStringArray()
        {
            static Stream stream(string x) => new MemoryStream(Encoding.Unicode.GetBytes(x));

            const string testInput = "<Content>" +
                "<Item>a</Item>" +
                "<Item>b</Item>" +
                "<Item>c</Item>" +
                "</Content>";

            // load from XML
            XmlDocument xmlDocument = new();
            xmlDocument.Load(stream(testInput));

            XmlDocument interpreterDocument = new();
            interpreterDocument.Load(stream("<Converts><Converts>" +
                "<Convert Path=\"//Item\" Type=\"String\" Encoding=\"UTF-8\"/>" +
                "</Converts></Converts>"));
            XmlDocument xmlWithBytes = new FileDBReader.XmlExporter().Export(xmlDocument, new(interpreterDocument));
            IFileDBDocument doc = new XmlFileDbConverter<FileDBDocument_V1>().ToFileDb(xmlWithBytes);

            Assert.AreEqual(3, doc.Roots.Count);
            Assert.IsTrue(doc.Roots[0] is Attrib);
            Assert.AreEqual("Item", doc.Roots[0].Name);

            Assert.IsTrue(doc.Tags.Attribs.ContainsValue("Item"));  // make sure "Item" is only added as Attrib
            Assert.IsTrue(!doc.Tags.Tags.ContainsValue("Item"));

            // deserialize & serialize
            FileDBDocumentDeserializer<FlatStringArrayContainer> deserializer = new(new() { Version = FileDBDocumentVersion.Version1 });
            var obj = deserializer.GetObjectStructureFromFileDBDocument(doc);

            Assert.IsNotNull(obj);
            Assert.IsNotNull(obj.Item);
            Assert.AreEqual(3, obj.Item!.Length);
            Assert.AreEqual("a", obj.Item[0]);
            Assert.AreEqual("b", obj.Item[1]);
            Assert.AreEqual("c", obj.Item[2]);

            FileDBDocumentSerializer serializer = new(new() { Version = FileDBDocumentVersion.Version1 });
            doc = serializer.WriteObjectStructureToFileDBDocument(obj);

            Assert.IsTrue(doc.Tags.Attribs.ContainsValue("Item"));  // make sure "Item" is only added as Attrib
            Assert.IsTrue(!doc.Tags.Tags.ContainsValue("Item"));

            // convert back to xml
            xmlWithBytes = new FileDbXmlConverter().ToXml(doc);
            xmlDocument = new FileDBReader.XmlInterpreter().Interpret(xmlWithBytes, new(interpreterDocument));
            Assert.AreEqual(testInput, xmlDocument.InnerXml);
        }
    }
}
