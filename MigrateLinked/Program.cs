using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Lnk;

namespace MigrateLinked
{
    class Program
    {
        // CHANGE THESE VALUES IF YOU AREN'T USING COMMAND-LINE
        private static bool isVerbose = true;
        private static string SRC_DIR_NAME = @"D:\src";
        private static string DST_DIR_NAME = @"F:\dest";

        private static void Verbose(string str)
        {
            if (isVerbose)
            {
                Console.WriteLine(str);
            }
        }

        static void Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance); // Required for Lnk

            var processed = 0;
            var copied = 0;

          
            if (args.Length == 2)
            {
                SRC_DIR_NAME = args[0];
                DST_DIR_NAME = args[1];
            }

            var destDir = new DirectoryInfo(DST_DIR_NAME);
            if (!destDir.Exists)
            {
                // Try to create destination dir if it doesn't exist
                Verbose($"Creating {destDir}");
                destDir.Create();
            }

            var dir = new DirectoryInfo(SRC_DIR_NAME);
            var files = dir.GetFiles();

            Verbose($"START: COPY {SRC_DIR_NAME} to {DST_DIR_NAME} : Ignoring any directories & expanding .lnk files");

            foreach (var file in files)
            {
                processed++;
                string relName = file.FullName;

                if (Directory.Exists(relName))
                {
                    Verbose($"Skipping directory {relName}");
                    continue;
                }

                Verbose($"{file.FullName}");

                if (file.Extension == ".lnk")
                {
                    var lnk = Lnk.Lnk.LoadFile(file.FullName);
                    relName = lnk.RelativePath;

                    if (String.IsNullOrWhiteSpace(relName))
                    {
                        Verbose($"SKIPPING invalid .lnk file {file.FullName} is Null or whitespace!!");
                        continue;
                    }
                }

                var destFullName = Path.Combine(DST_DIR_NAME, Path.GetFileName(relName));
                var destInfo = new FileInfo(destFullName);
                if (destInfo.Exists)
                {
                    Verbose($"EXISTS: Skipping {destFullName}");
                    continue;
                }
                else
                {
                    try
                    {
                        Console.WriteLine($"TRYING: {relName}");
                        var combined = Path.Combine(SRC_DIR_NAME, relName);
                        var origInfo = new FileInfo(combined);
                        if (origInfo.Exists)
                        {
                            Verbose($"COPYING {combined} to {destFullName}");
                            File.Copy(combined, destFullName);
                            copied++;
                        }
                        else
                        {
                            Verbose($"SKIPPING source does not exist: {relName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Verbose($"SKIPPING {relName} due to exception: {ex.Message}");
                    }
                }
            }

            Console.WriteLine($"FINISHED: processed: {processed} copied: {copied}");
        }
    }
}
