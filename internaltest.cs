﻿using FileDBReader.src;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FileDBReader
{
    class internaltest {

        static String TEST_DIRECTORY_NAME = "tests";
        static String FILEFORMAT_DIRECTORY_NAME = "FileFormats";

        //FileDB
        static FileReader reader = new FileReader();
        static XmlExporter exporter = new XmlExporter();
        static FileWriter writer = new FileWriter();
        static XmlInterpreter interpreter = new XmlInterpreter();
        static ZlibFunctions zlib = new ZlibFunctions();

        //Fc Files
        static FcFileHelper FcFileHelper = new FcFileHelper(); 

        public static void Main(String[] args)
        {
            RenamedTagsTest();
        }

        #region GenericTestFcFile

        public static void FcFileDevTest()
        {
            FcFile_GenericTest("residence_tier02_estate01.fc");
            FcFile_GenericTest("world_map_01.fc");
            FcFile_GenericTest("food_07.fc");
            FcFile_GenericTest("mining_08.fc");
            FcFile_GenericTest("workshop_06.fc");
            FcFile_GenericTest("electricity_01.fc");
        }
        #endregion

        #region GenericTestInstancesFileDB

        //Generic Test(TestDirectoryName, InterpreterFileName, TestfileFilename, FileVersion)

        /// <summary>
        /// Test for the two island interpreters
        /// </summary>
        public static void IslandTest()
        {
            IslandTestGamedata();
            IslandTestRd3d();
            IslandTestTMC();
        }

        private static void IslandTestGamedata()
        {
            GenericTest("island", "Island_Gamedata.xml", "gamedata.data", 1);
        }

        private static void IslandTestTMC()
        {
            GenericTest("island", "tmc.xml", "0x0.tmc", 1);
        }

        private static void IslandTestRd3d()
        {
            GenericTest("island", "Island_Rd3d.xml", "rd3d.data", 1);
        }

        private static void MapGamedataTest()
        {
            GenericTest("maps", "map_Gamedata.xml", "gamedata.data", 1);
        }

        public static void InfotipTestNewFileVersion()
        {
            GenericTest("infotip", "infotip.xml", "export.bin", 2);
        }

        public static void A7TINFOTest()
        {
            GenericTest("a7tinfo", "a7tinfo.xml", "moderate_atoll_ll_01.a7tinfo", 1);
        }

        public static void ListTest()
        {
            GenericTest("lists", "Island_Gamedata.xml", "gamedata_og.data", 1);
        }

        public static void RenamedTagsTest()
        {
            Dictionary<string, string> Renames = new Dictionary<string, string>();
            Renames.Add("Delayed Construction", "DelayedConstruction");
            InvalidTagNameHelper.ReplaceOperations = Renames; 
            GenericTest("RenamedTags", "Island_Gamedata.xml", "gamedata.data", 1);
            InvalidTagNameHelper.Reset(); 
        }

        #endregion

        #region FileDBTests

        private static void IslandTestGoodwill()
        {
            //test directory
            String DIRECTORY_NAME = "goodwill";
            //interpreter file path
            String INTERPRETER_GAMEDATA = Path.Combine(FILEFORMAT_DIRECTORY_NAME, "internalfiledbtest.xml");
            //input file path
            String GAMEDATA_FILE = Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, "gamedata_og.data");
            //output file path
            String GAMEDATA_INTERPRETED_PATH = Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, "Island_Gamedata_interpreted.xml");
            String GAMEDATA_READ_PATH = Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, "Island_Gamedata_Read.xml");
            String GAMEDATA_EXPORTED_PATH = Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, "Island_Gamedata_exported.xml");
            //create interpreter document
            var GamedataInterpreter = new XmlDocument();
            GamedataInterpreter.Load(INTERPRETER_GAMEDATA);

            //decompress interpret and save gamedata.data
            var doc = reader.ReadFile(GAMEDATA_FILE);
            doc.Save(GAMEDATA_READ_PATH);
            var interpreted_gamedata = interpreter.Interpret(reader.ReadFile(GAMEDATA_FILE), GamedataInterpreter);
            interpreted_gamedata.Save(GAMEDATA_INTERPRETED_PATH);

            var exported = exporter.Export(interpreted_gamedata, GamedataInterpreter);
            exported.Save(GAMEDATA_EXPORTED_PATH);
        }

        /// <summary>
        /// Test for the ctt interpreter.
        /// </summary>
        public static void CttTest() {
            const String DIRECTORY_NAME = "ctt";
            const String INTERPRETER_FILE = "FileFormats/ctt.xml";
            
            var interpreterDoc = new XmlDocument();
            interpreterDoc.Load(INTERPRETER_FILE);

            FileStream fs = File.OpenRead(Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, "0x1.ctt"));

            //Ubisoft uses 8 magic bytes at the start
            var doc = interpreter.Interpret(reader.ReadSpan(zlib.Decompress(fs, 8)), interpreterDoc);
            doc.Save(Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, "interpreted.xml"));
        }

        /// <summary>
        /// Test for DEFLATE/zlib implementation. 
        /// decompresses 0x1.ctt, ignoring the 8 magic bytes at the start, writes the result to decompressed.xml, then compresses it back.
        /// </summary>
        public static void zlibTest() {
            const String DIRECTORY_NAME = "zlib";

            FileStream fs = File.OpenRead(Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, "data.a7s"));

            //Ubisoft uses 8 magic bytes at the start
            var doc = reader.ReadSpan(zlib.Decompress(fs, 0));
            doc.Save(Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, "decompressed.xml"));

            var Stream = writer.Export(doc, ".bin", 1);
            File.WriteAllBytes(Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, "shittycompress.ctt"), zlib.Compress(Stream, 1));
        }

        /// <summary>
        /// decompresses the two files original.bin and recompressed.bin which are preextracted inner filedb files from gamedata_og.data
        /// </summary>
        public static void InnerFileDBTest() {
            const String DIRECTORY_NAME = "filedb";

            var reader = new FileReader();
            reader.ReadFile(Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, "original.bin")).Save(Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, "original.xml"));
            reader.ReadFile(Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, "recompressed.bin")).Save(Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, "recompressed.xml") );
        }

        /// <summary>
        /// Decompresses gamedata_og.data, inteprets it with internalfiledbtest.xml, converts it back to hex using the same interpreter, exports back to filedb compression. 
        /// A file is saved at each stage of the process.
        /// </summary>
        public static void CompressionTest() {

            const String DIRECTORY_NAME = "innerfiledb";
            const String INTERPRETER_FILE = "FileFormats/internalfiledbtest.xml";

            String TESTFILE = Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, "gamedata_og.data");
            String DECOMPRESSED_TESTFILE = Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, "gamedata_decompressed.xml");
            String INTERPRETED_TESTFILE = Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, "gamedata_interpreted.xml");
            String TOHEX_TESTFILE = Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, "gamedata_backtohex.xml");
            String EXPORTED_TESTFILE = Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, "gamedata.data");

            //decompress gamedata.data
            var interpreterDoc = new XmlDocument();
            interpreterDoc.Load(INTERPRETER_FILE);

            //decompress
            var decompressed = reader.ReadFile(TESTFILE);
            decompressed.Save(DECOMPRESSED_TESTFILE);

            //interpret
            var interpreted = interpreter.Interpret(decompressed, interpreterDoc);
            interpreted.Save(INTERPRETED_TESTFILE);

            //to hex 
            var Hexed = exporter.Export(interpreted, interpreterDoc);
            Hexed.Save(TOHEX_TESTFILE);

            //back to gamedata 
            writer.Export(Hexed, EXPORTED_TESTFILE, 1);
        }

        public static void GenericTest(String DIRECTORY_NAME, String INTERPREFER_FILE_NAME, String TESTFILE_NAME, int FileVersion)
        {
            String INTERPRETER_FILE = Path.Combine(FILEFORMAT_DIRECTORY_NAME, INTERPREFER_FILE_NAME);
            String TESTFILE = Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, TESTFILE_NAME);

            String DECOMPRESSED_TESTFILE = Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, TESTFILE_NAME + "_decompressed.xml");
            String INTERPRETED_TESTFILE = Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, TESTFILE_NAME + "_interpreted.xml");
            String TOHEX_TESTFILE = Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, TESTFILE_NAME + "_reinterpreted.xml");
            String EXPORTED_TESTFILE = Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, TESTFILE_NAME + "_recompressed" + Path.GetExtension(TESTFILE_NAME));

            //decompress gamedata.data
            var interpreterDoc = new XmlDocument();
            interpreterDoc.Load(INTERPRETER_FILE);

            //decompress
            var decompressed = reader.ReadFile(TESTFILE);//interpret
            decompressed.Save(DECOMPRESSED_TESTFILE);
            var interpreted = interpreter.Interpret(decompressed, interpreterDoc);
            interpreted.Save(INTERPRETED_TESTFILE);

            //to hex 
            var Hexed = exporter.Export(interpreted, interpreterDoc);
            Hexed.Save(TOHEX_TESTFILE);

            //back to gamedata 
            writer.Export(Hexed, EXPORTED_TESTFILE, FileVersion);

            var OriginalInfo = new FileInfo(TESTFILE);
            var DecompressedInfo = new FileInfo(DECOMPRESSED_TESTFILE);
            var InterpretedInfo = new FileInfo(INTERPRETED_TESTFILE);
            var RehexedInfo = new FileInfo(TOHEX_TESTFILE);
            var ReexportedInfo = new FileInfo(EXPORTED_TESTFILE);

            Console.WriteLine("File Test: {0}", TESTFILE_NAME);
            Console.WriteLine("Used FileDBCompression Version for re-export: {0}", FileVersion);
            Console.WriteLine("FILEDB FILES FILESIZE\nOriginal: {0}, Converted: {1}. Filesize Equality:{2}", OriginalInfo.Length, ReexportedInfo.Length, OriginalInfo.Length == ReexportedInfo.Length);
            //This check will probably give a false if there is internal compression!
            Console.WriteLine("XML FILES FILESIZE\n Decompressed: {0}, Recompressed: {1}. Filesize Equality: {2}", DecompressedInfo.Length, RehexedInfo.Length, DecompressedInfo.Length == RehexedInfo.Length);

            Console.WriteLine("File Test Done");
            Console.WriteLine("--------------------------------------------------");
        }
        #endregion

        #region FCTests

        public static void ClosingFileTest()
        {
            FcFile_GenericTest("fcFiles", "FcFile.xml", "cannon_ball_small_01.rdp");
        }

        public static void FcFile_GenericTest(String TESTFILE_NAME)
        {
            FcFile_GenericTest("fcFiles", "FcFile.xml", TESTFILE_NAME);
        }
        public static void FcFile_GenericTest(String DIRECTORY_NAME, String INTERPREFER_FILE_NAME, String TESTFILE_NAME)
        {
           
            String TESTFILE = Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, TESTFILE_NAME);

            String CDATAREAD_TESTFILE = Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, TESTFILE_NAME + "_CdataRead.xml");
            String INTERPRETED_TESTFILE = Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, TESTFILE_NAME + "_interpreted.xml");
            String REINTERPRETED_TESTFILE = Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, TESTFILE_NAME + "_reinterpreted.xml");
            String CDATAWRITTEN_TESTFILE = Path.Combine(TEST_DIRECTORY_NAME, DIRECTORY_NAME, TESTFILE_NAME + "_CdataWritten" + Path.GetExtension(TESTFILE_NAME));

            
            
            String INTERPRETER_FILE = Path.Combine(FILEFORMAT_DIRECTORY_NAME, INTERPREFER_FILE_NAME);
            var interpreterDoc = new XmlDocument();
            interpreterDoc.Load(INTERPRETER_FILE);

            //read
            var Read = FcFileHelper.ReadFcFile(TESTFILE);
            Read.Save(CDATAREAD_TESTFILE);

            var Interpreted = interpreter.Interpret(Read, interpreterDoc);
            Interpreted.Save(INTERPRETED_TESTFILE);

            var Reinterpreted = exporter.Export(Interpreted, interpreterDoc);
            Reinterpreted.Save(REINTERPRETED_TESTFILE);

            var Written = FcFileHelper.ConvertFile(FcFileHelper.XmlFileToStream(Reinterpreted), ConversionMode.Write);
            Save(Written, CDATAWRITTEN_TESTFILE);

            //save 
            var stream = FcFileHelper.ConvertFile(File.OpenRead(TESTFILE), ConversionMode.Read);
            var outstream = FcFileHelper.ConvertFile(stream, ConversionMode.Write);
            

            ShowFileWithDefaultProgram(CDATAWRITTEN_TESTFILE);

            try
            {
                var OriginalInfo = new FileInfo(TESTFILE);
                var DecompressedInfo = new FileInfo(CDATAREAD_TESTFILE);
                var InterpretedInfo = new FileInfo(INTERPRETED_TESTFILE);
                var RehexedInfo = new FileInfo(REINTERPRETED_TESTFILE);
                var ReexportedInfo = new FileInfo(CDATAWRITTEN_TESTFILE);

                Console.WriteLine("File Test: {0}", TESTFILE_NAME);
                Console.WriteLine("FC FILES FILESIZE\nOriginal: {0}, Converted: {1}. Filesize Equality:{2}", OriginalInfo.Length, ReexportedInfo.Length, OriginalInfo.Length == ReexportedInfo.Length);
                //This check will probably give a false if there is internal compression!
                Console.WriteLine("XML FILES FILESIZE\n Cdata Read: {0}, Cdata Rewritten: {1}. Filesize Equality: {2}", DecompressedInfo.Length, RehexedInfo.Length, DecompressedInfo.Length == RehexedInfo.Length);

                Console.WriteLine("File Test Done");
                Console.WriteLine("--------------------------------------------------");
            }
            catch (Exception e)
            {
                Console.WriteLine("Currently undergoing maintenance, please fuck off");
            }
        }
        #endregion

        #region UniversalMethods
        private static void ShowFileWithDefaultProgram(FileStream f)
        {
            ShowFileWithDefaultProgram(f.Name);
        }

        private static void ShowFileWithDefaultProgram(String Filename)
        {
            using (Process fileopener = new Process())
            {
                fileopener.StartInfo.FileName = "explorer";
                fileopener.StartInfo.Arguments = "\"" + Filename + "\"";
                fileopener.Start();
            }
        }

        public static bool FilesAreEqual(FileInfo first, FileInfo second)
        {
            const int BYTES_TO_READ = sizeof(Int64);

            if (first.Length != second.Length)
                return false;

            if (string.Equals(first.FullName, second.FullName, StringComparison.OrdinalIgnoreCase))
                return true;

            int iterations = (int)Math.Ceiling((double)first.Length / BYTES_TO_READ);

            using (FileStream fs1 = first.OpenRead())
            using (FileStream fs2 = second.OpenRead())
            {
                byte[] one = new byte[BYTES_TO_READ];
                byte[] two = new byte[BYTES_TO_READ];

                for (int i = 0; i < iterations; i++)
                {
                    fs1.Read(one, 0, BYTES_TO_READ);
                    fs2.Read(two, 0, BYTES_TO_READ);

                    if (BitConverter.ToInt64(one, 0) != BitConverter.ToInt64(two, 0))
                        return false;
                }
            }
            return true;
        }

        private static byte[] GetFileHash(String FileName)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(FileName))
                {
                    return md5.ComputeHash(stream);
                }
            }
        }

        private static void Save(Stream Stream, String Filename)
        {
            var fs = File.Create(Filename);
            Stream.Position = 0;
            Stream.CopyTo(fs);
            fs.Close();
        }

        #endregion
    }
    
}