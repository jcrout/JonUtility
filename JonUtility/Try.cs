using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JonUtility
{
    public class TryResponse
    {
        public bool Success { get { return this.Error == null; } }

        public Exception Error { get; set; }

        public TryResponse(Exception error = null)
        {
            this.Error = error;
        }
    }

    public class TryGetResponse<T>
    {
        public bool Success { get { return this.Error == null; } }

        public T Result { get; set; }

        public Exception Error { get; set; }

        public TryGetResponse(T result, Exception error = null)
        {
            this.Result = result;
            this.Error = error;
        }
    }

    public static class Try
    {
        public static TryResponse Do(Action method)
        {
            try
            {
                method();
                return new TryResponse();
            }
            catch (Exception ex)
            {
                return new TryResponse(ex);
            }
        }

        public static TryGetResponse<T> Get<T>(Func<T> method, T defaultValue = default(T))
        {
            try
            {
                var result = method();
                return new TryGetResponse<T>(result);
            }
            catch (Exception ex)
            {
                return new TryGetResponse<T>(defaultValue, ex);
            }
        }
    }
}
