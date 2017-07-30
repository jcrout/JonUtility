using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace JonUtility.Windows
{
    public class FileComparer : IComparer<string>
    {
        public int Compare(string a, string b)
        {
            return Managed.StrCmpLogicalW(a, b);
        }
    }

    public class FileInfoComparer : IComparer<FileInfo>
    {
        public int Compare(FileInfo x, FileInfo y)
        {
            return Managed.StrCmpLogicalW(x.Name, y.Name);
        }
    }
}
