using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace JonUtility
{
    public static class Compression
    {
        public static void DoubleZipFile(string file, string outFileName, SecureString password = null)
        {
            using (ZipFile zip = new ZipFile())
            {
                if (password != null)
                {
                    zip.Password = password.ConvertToString();
                }

                zip.CompressionLevel = Ionic.Zlib.CompressionLevel.Default;
                zip.UseZip64WhenSaving = Zip64Option.Always;
                zip.AddFile(file);
                zip.Save(outFileName);
            }

            System.IO.File.Delete(file);
        }

        public static void ZipFolder(string folder, string outFileName, SecureString password = null, bool doubleZip = false)
        {
            var outPath = new System.IO.FileInfo(outFileName);
            var initFileName = doubleZip ? outPath.Directory + "\\" + Guid.NewGuid().ToString() + ".zip" : outFileName;

            using (ZipFile zip = new ZipFile())
            {
                if (!doubleZip && password != null)
                {
                    zip.Password = password.ConvertToString();
                }

                zip.CompressionLevel = Ionic.Zlib.CompressionLevel.Default;
                zip.UseZip64WhenSaving = Zip64Option.Always;
                zip.AddDirectory(folder);
                zip.Save(initFileName);
            }

            if (doubleZip)
            {
                using (ZipFile zip = new ZipFile())
                {
                    if (password != null)
                    {
                        zip.Password = password.ConvertToString();
                    }

                    zip.CompressionLevel = Ionic.Zlib.CompressionLevel.Default;
                    zip.UseZip64WhenSaving = Zip64Option.Always;
                    zip.AddFile(initFileName);
                    zip.Save(outFileName);
                }
            }

            System.IO.File.Delete(initFileName);
        }

        public static void UnzipTo(string zipFilePath, string destination, SecureString password = null)
        {
            using (var zip = ZipFile.Read(zipFilePath))
            {
                if (password != null)
                {
                    zip.Password = password.ConvertToString();
                }

                zip.ExtractAll(destination, ExtractExistingFileAction.DoNotOverwrite);
            }
        }
    }
}
