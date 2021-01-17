using System;

namespace Muzlan.Api
{
    public class MuzlanException : Exception
    {
        public MuzlanException() : base()
        {
        }

        public MuzlanException(string? message) : base(message)
        {
        }

        public MuzlanException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        internal static MuzlanException ForEmptyResponse()
        {
            return new MuzlanException("The request unexpectedly returned an empty response.");
        }
    }
}
