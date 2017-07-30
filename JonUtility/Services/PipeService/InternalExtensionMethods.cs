using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JonUtility.PipeService
{
    internal static class InternalExtensionMethods
    {
        public static bool WaitWrite(this StreamWriter @this, string message, int threadSleep = -1)
        {
            try
            {
                @this.WriteLine(message);
                @this.Flush();

                if (threadSleep >= 0)
                {
                    System.Threading.Thread.Sleep(threadSleep);
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
