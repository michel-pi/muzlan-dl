using System;
using System.Threading.Tasks;

using Muzlan.Api;

namespace Muzlan.Cli.Utilities
{
    public enum ExitCode
    {
        Success,
        ParserError,
        PartialResponse,
        AuthenticationFailure,
        RequestFailure,
        ResponseFailure,
        RateLimit
    }

    public static class ExitCodeHelper
    {
        public static int Set(ExitCode code)
        {
            var value = (int)code;
            Environment.ExitCode = value;
            return value;
        }

        public static int Set<T>(MuzlanResponse<T> response)
        {
            if (response.IsCompleted)
            {
                return Set(ExitCode.Success);
            }
            else if (response.IsRateLimit())
            {
                return response.HasResult
                    ? Set(ExitCode.PartialResponse)
                    : Set(ExitCode.RateLimit);
            }
            else if (response.HasException)
            {
                return response.HasResult
                    ? Set(ExitCode.PartialResponse)
                    : Set(ExitCode.RequestFailure);
            }

            return response.HasResult
                    ? Set(ExitCode.PartialResponse)
                    : Set(ExitCode.ResponseFailure);
        }

        public static Task<int> SetAsync(ExitCode code)
        {
            return Task.FromResult(Set(code));
        }
    }
}
