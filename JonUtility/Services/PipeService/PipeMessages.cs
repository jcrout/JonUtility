using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JonUtility.PipeService
{
    public static class PipeCommands
    {
        public const string AttachEvent = "attachevent";

        public const string SetProcessId = "setprocessid";
    }


    public static class PipeMessages
    {
        public const string Query = "query";

        public const string Success = "success";

        public const string Error = "error";

        public const string Command = "command";

        public const string Event = "event";
    }
}
