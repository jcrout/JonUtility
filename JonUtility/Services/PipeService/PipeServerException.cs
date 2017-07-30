using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JonUtility.PipeService
{
    public class PipeServerException : Exception
    {
        public PipeServerException() { }

        public PipeServerException(string message) : base(message) { }

        public PipeServerException(string message, Exception innerException) : base(message, innerException) { }
    }
}
