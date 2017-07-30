using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Microsoft.VisualBasic.FileIO;

namespace JonUtility
{
    public static class FileMethods
    {
        /// <summary>
        /// http://stackoverflow.com/questions/1406808/wait-for-file-to-be-freed-by-process
        /// </summary>
        /// <param name="sFilename"></param>
        /// <returns></returns>
        public static bool IsFileReady(String path)
        {
            // If the file can be opened for exclusive access it means that the file
            // is no longer locked by another process.
            try
            {
                using (FileStream inputStream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    if (inputStream.Length > 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }

                }
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool WaitForFileReady(string path, int millisecondInterval = 5)
        {
            while (true)
            {
                var isReady = IsFileReady(path);
                if (isReady)
                {
                    return true;
                }
                else
                {
                    System.Threading.Thread.Sleep(millisecondInterval);
                }
            }
        }

        public static string TrimExtension(string name)
        {
            var extIndex = name.LastIndexOf(".");

            return extIndex != -1 ? name.Substring(0, extIndex) : name;
        }

        public static string GetNextFileName(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                return path;
            }

            var di = new System.IO.FileInfo(path).Directory;
            if (di == null)
            {
                return path;
            }

            var i = 1;
            while (true)
            {
                var newPath = path + " (" + i.ToString() + ")";
                if (!System.IO.File.Exists(newPath))
                {
                    return newPath;
                }
                else
                {
                    i++;
                }
            }
        }

        public static bool SendToRecycleBin(string filePath)
        {
            try
            {
                FileSystem.DeleteFile(filePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void WriteWithTempFile(string path, string content)
        {
            var tempFile = path + "TEMP";
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }

            System.IO.File.WriteAllText(tempFile, content, Encoding.UTF8);
            System.IO.File.Move(tempFile, path);
        }

        public static long SizeOfDirectory(string path, bool recursive = false, IEnumerable<string> extensionsToIgnore = null, IEnumerable<string> extensionsToInclude = null)
        {
            var di = new System.IO.DirectoryInfo(path);
            if (!di.Exists)
            {
                return -1L;
            }

            IEnumerable<FileInfo> files = di.GetFiles("*", recursive ? System.IO.SearchOption.AllDirectories : System.IO.SearchOption.TopDirectoryOnly);
            if (extensionsToIgnore != null)
            {
                files = files.Where(fi => !extensionsToIgnore.Contains(fi.Extension.ToLower()));
            }
            if (extensionsToInclude != null)
            {
                files = files.Where(fi => extensionsToInclude.Contains(fi.Extension.ToLower()));
            }

            return files.Select(fi => fi.Length).Sum();
        }
    }

    public class TempFileWriter : StreamWriter
    {
        public TempFileWriter(string path) : base(path, false, Encoding.UTF8)
        {
            var tempFile = path + "TEMP";
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }


        }

    }
}
