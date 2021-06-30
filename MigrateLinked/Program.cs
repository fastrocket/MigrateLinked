using System;
using System.IO;
using System.Text;

namespace MigrateLinked
{
    class Program
    {
        static void Main(string[] args)
        {
            var destDirName = "c:/tmp/";
            if (args.Length != 1)
            {
                Console.WriteLine($"MigrateLinked destDir\nINFO: copies from . to destDir and follows links to copy files");
                return; // comment out to debug using c:/tmp/
            }
            else
            {
                destDirName = args[0];
            }

            var destDir = new DirectoryInfo(destDirName);
            if (!destDir.Exists)
            {
                // Try to create destination dir if it doesn't exist
                Console.WriteLine($"Creating {destDir}");
                destDir.Create();
            }

            var dir = new DirectoryInfo(".");
            var files = dir.GetFiles();
            foreach (var file in files)
            {
                string realName = file.FullName;

                if (Directory.Exists(realName))
                {
                    Console.WriteLine($"Skipping directory {realName}");
                    continue;
                }
                //Console.WriteLine($"{file.FullName}");
                if (file.Extension == ".lnk")
                {
                    realName = GetLnkTargetPath(file.FullName);
                    Console.WriteLine($"!!!! LNK: {realName}");
                }
                var destFullName = Path.Combine(destDirName, Path.GetFileName(realName));
                var destInfo = new FileInfo(destFullName);
                if (destInfo.Exists)
                {
                    Console.WriteLine($"Skipping: {destFullName}");
                }
                else
                {
                    Console.WriteLine($"Copying {realName} to {destFullName}");
                    File.Copy(realName, destFullName);
                }
            }
        }

        public static string GetLnkTargetPath(string filepath)
        {
            using (var br = new BinaryReader(System.IO.File.OpenRead(filepath)))
            {
                // skip the first 20 bytes (HeaderSize and LinkCLSID)
                br.ReadBytes(0x14);
                // read the LinkFlags structure (4 bytes)
                uint lflags = br.ReadUInt32();
                // if the HasLinkTargetIDList bit is set then skip the stored IDList 
                // structure and header
                if ((lflags & 0x01) == 1)
                {
                    br.ReadBytes(0x34);
                    var skip = br.ReadUInt16(); // this counts of how far we need to skip ahead
                    br.ReadBytes(skip);
                }
                // get the number of bytes the path contains
                var length = br.ReadUInt32();
                // skip 12 bytes (LinkInfoHeaderSize, LinkInfoFlgas, and VolumeIDOffset)
                br.ReadBytes(0x0C);
                // Find the location of the LocalBasePath position
                var lbpos = br.ReadUInt32();
                // Skip to the path position 
                // (subtract the length of the read (4 bytes), the length of the skip (12 bytes), and
                // the length of the lbpos read (4 bytes) from the lbpos)
                br.ReadBytes((int)lbpos - 0x14);
                var size = length - lbpos - 0x02;
                var bytePath = br.ReadBytes((int)size);
                var path = Encoding.UTF8.GetString(bytePath, 0, bytePath.Length);
                return path;
            }
        }
    }
}
