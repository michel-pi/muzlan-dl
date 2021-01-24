using System;
using System.Net.Http;

namespace Muzlan.Api
{
    public sealed class MuzlanResponse<T>
    {
        public bool HasException => Exception != null;
        public bool HasResult => Result != null;
        public bool IsCompleted { get; }
        public Exception? Exception { get; private set; }
        public int PageNumber { get; }
        public Uri PageUri { get; }
        public T? Result { get; private set; }

        private MuzlanResponse(bool isCompleted, Uri pageUri, int pageNumber = 0)
        {
            IsCompleted = isCompleted;
            PageUri = pageUri;
            PageNumber = pageNumber;
        }

        public T EnsureHasResult()
        {
            if (Result == null)
            {
                throw new InvalidOperationException("Result is null.", Exception);
            }

            return Result;
        }

        public bool IsRateLimit()
        {
            if (IsCompleted) return false;

            if (Exception != null && Exception is HttpRequestException httpException)
            {
                return httpException.StatusCode == System.Net.HttpStatusCode.PaymentRequired;
            }

            return false;
        }

        public static MuzlanResponse<T> FromResult(T result, Uri pageUri)
        {
            return new MuzlanResponse<T>(true, pageUri)
            {
                Result = result
            };
        }

        public static MuzlanResponse<T> FromPartialResult(T result, Uri pageUri, int pageNumber)
        {
            return new MuzlanResponse<T>(false, pageUri, pageNumber)
            {
                Result = result
            };
        }

        public static MuzlanResponse<T> FromException(Exception exception, Uri pageUri, int pageNumber = 0)
        {
            return new MuzlanResponse<T>(false, pageUri, pageNumber)
            {
                Exception = exception
            };
        }
    }
}
