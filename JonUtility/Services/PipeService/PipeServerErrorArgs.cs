using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JonUtility.PipeService
{
    public class PipeServerErrorArgs : EventArgs
    {
        public string CommandName { get; }

        public PipeServerException Error { get; }

        public PipeServerErrorArgs(string commandName, PipeServerException exception)
        {
            this.CommandName = commandName;
            this.Error = exception;
        }
    }
}