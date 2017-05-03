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

namespace CoreDumpAnalysis
{
    class Program
    {
        public const String WRAPPER = "unwindwrapper.so";


        [DllImport(WRAPPER)]
        private static extern void init(string filepath, string workindDir);

        [DllImport(WRAPPER)]
        private static extern int getNumberOfThreads();
        
        [DllImport(WRAPPER)]
        private static extern int getThreadId();

        [DllImport(WRAPPER)]
        private static extern void selectThread(uint threadNumber);

        [DllImport(WRAPPER)]
        private static extern ulong getInstructionPointer();

        [DllImport(WRAPPER)]
        private static extern ulong getStackPointer();

        [DllImport(WRAPPER)]
        private static extern string getProcedureName();

        [DllImport(WRAPPER)]
        private static extern ulong getProcedureOffset();

        [DllImport(WRAPPER)]
        private static extern bool step();

        static void Main(string[] args)
        {
            if(args.Length == 1)
            {
                Console.WriteLine("Working directory: " + args[0]);
                new Program().AnalyzeDirectory(args[0]);
            } else
            {
                Console.WriteLine("Invalid argument count! exe <coredump-directory>");
            }
        }

        private void AnalyzeDirectory(string directory) {
            ExtractArchiveInDir(directory);
            String coredump = FindCoredumpOrNull(directory);
            if(coredump == null)
            {
                Console.WriteLine("Coredump was not found in the target directory.");
                return;
            }
            Console.WriteLine("Found core dump file: " + coredump);

            SDResult analysisResult = Debug(coredump);
            File.WriteAllText("out.json", analysisResult.SerializeToJSON());
        }

        private void ExtractArchiveInDir(String directory)
        {
            bool workDone = true;
            while (workDone)
            {
                workDone = false;
                foreach (String file in FilesystemHelper.FilesInDirectory(directory + "/"))
                {
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

        private SDResult Debug(String coredump)
        {
            String parent = FilesystemHelper.GetParentDirectory(coredump);
            parent = parent.Substring(0, parent.Length - 1);
            init(coredump, parent);

            List<string> notLoadedSymbols = new List<string>();
            List<SDDeadlockContext> deadlocks = new List<SDDeadlockContext>();
            Dictionary<ulong, SDMemoryObject> memoryObjects = new Dictionary<ulong, SDMemoryObject>();
            Dictionary<uint, SDThread> threads = new Dictionary<uint, SDThread>();
            List<SDClrException> exceptions = new List<SDClrException>();
            SDLastEvent lastEvent = new SDLastEvent("EXCEPTION", "", 0);
            SDSystemContext context = new SDSystemContext();
            context.ProcessArchitecture = "N/A";
            context.SystemArchitecture = "N/A";
            context.SystemUpTime = "Could not be obtained.";
            context.NumberOfProcessors = 0;
            context.Modules = new List<SDModule>();
            context.AppDomains = new List<SDAppDomain>();
            context.ClrVersions = new List<SDClrVersion>();

            int nThreads = getNumberOfThreads();
            Console.WriteLine("Threads: " + nThreads);
            Console.WriteLine("Instruction Pointer\tStack Pointer\t\tProcedure Name + Offset");
            for (uint i = 0; i < nThreads; i++)
            {
                selectThread(i);
                Console.WriteLine();
                Console.WriteLine("Thread: " + i);
                List<SDCombinedStackFrame> frames = new List<SDCombinedStackFrame>();

                ulong ip, oldIp = 0, sp, oldSp = 0, offset, oldOffset = 0;
                String procName, oldProcName = null;
                do
                {
                    ip = getInstructionPointer();
                    sp = getStackPointer();
                    procName = getProcedureName();
                    offset = getProcedureOffset();

                    if (oldProcName != null)
                    {
                        Console.WriteLine("{0:X16}\t{1:X16}\t{2}+{3}", getInstructionPointer(), getStackPointer(), getProcedureName(), getProcedureOffset());
                        frames.Add(new SDCombinedStackFrame(StackFrameType.Native, "", oldProcName, oldOffset, oldIp, oldSp, ip, 0, null));
                    }
                    oldIp = ip;
                    oldSp = sp;
                    oldOffset = offset;
                    oldProcName = procName;
                } while (!step());

                SDThread thread = new SDThread(i);
                thread.EngineId = i;
                thread.Index = i;
                thread.StackTrace = new SDCombinedStackTrace(frames);
                threads.Add(i, thread);
            }
            return new SDResult(context, lastEvent, exceptions, threads, memoryObjects, deadlocks, notLoadedSymbols);
        }
    }
}