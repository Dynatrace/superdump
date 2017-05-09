using SharpCompress.Archives;
using SharpCompress.Archives.Zip;
using SharpCompress.Archives.GZip;
using SharpCompress.Archives.Tar;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using SuperDump.Models;
using SuperDumpModels;

namespace CoreDumpAnalysis
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SuperDump - Dump analysis tool");
            Console.WriteLine("--------------------------");
            if (args.Length == 2)
            {
                Console.WriteLine("Input File: " + args[0]);
                Console.WriteLine("Output File: " + args[1]);
                new Program().AnalyzeDirectory(args[0], args[1]);
            } else
            {
                Console.WriteLine("Invalid argument count! exe <coredump-directory>");
            }
        }

        private void AnalyzeDirectory(string inputFile, string outputFile) {
            string coredump;
            string directory = FilesystemHelper.GetParentDirectory(inputFile);
            if (!File.Exists(inputFile))
            {
                Console.WriteLine("Input file " + inputFile + " does not exist on the filesystem. Searching for a coredump in the directory...");
                coredump = FindCoredumpOrNull(directory);
            } else if(inputFile.EndsWith(".tar") || inputFile.EndsWith(".gz") || inputFile.EndsWith(".tgz") || inputFile.EndsWith(".tar") || inputFile.EndsWith(".zip"))
            {
                Console.WriteLine("Extracting archives in directory " + directory);
                ExtractArchivesInDir(directory);
                coredump = FindCoredumpOrNull(directory);
            } else if(inputFile.EndsWith(".core"))
            {
                coredump = inputFile;
            } else
            {
                Console.WriteLine("Failed to interpret input file. Assuming it is a core dump.");
                coredump = inputFile;
            }
            
            if(coredump == null)
            {
                Console.WriteLine("No core dump found.");
                // TODO write empty json?
                return;
            }
            Console.WriteLine("Processing core dump file: " + coredump);

            SDResult analysisResult = new CoreDumpAnalysis().Debug(coredump);
            File.WriteAllText(outputFile, analysisResult.SerializeToJSON());
        }

        private void ExtractArchivesInDir(String directory)
        {
            bool workDone = true;
            while (workDone)
            {
                workDone = false;
                foreach (String file in FilesystemHelper.FilesInDirectory(directory))
                {
                    Console.WriteLine("Checking file " + file);
                    workDone |= ArchiveHelper.TryExtract(file);
                }
            }
        }

        private String FindCoredumpOrNull(String directory)
        {
            foreach (String file in FilesystemHelper.FilesInDirectory(directory))
            {
                if (file.EndsWith(".core"))
                {
                    return file;
                }
            }
            return null;
        }
    }
}