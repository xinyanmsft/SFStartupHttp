using System.Threading;

namespace Application1.ValuesService.Utility
{
    internal static class HttpCorrelation
    {
        public static string GetRequestCorrelationId()
        {
            return RequestCorrelationId.Value;
        }

        public static void SetRequestCorrelationId(string value)
        {
            RequestCorrelationId.Value = value;
        }

        public static string GetRequestOrigin()
        {
            return RequestOrigin.Value;
        }

        public static void SetRequestOrigin(string value)
        {
            RequestOrigin.Value = value;
        }

        private static AsyncLocal<string> RequestCorrelationId = new AsyncLocal<string>();
        private static AsyncLocal<string> RequestOrigin = new AsyncLocal<string>();

        public const string CorrelationHeaderName = "__CorrelationId";
        public const string RequestOriginHeaderName = "__RequestOrigin";
    }
}
