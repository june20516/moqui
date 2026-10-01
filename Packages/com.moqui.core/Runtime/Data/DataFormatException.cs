using System;

namespace Moqui.Core.Data
{
    public sealed class DataFormatException : Exception
    {
        public DataFormatException(string message)
            : base(message)
        {
        }
    }
}
