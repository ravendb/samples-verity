using System;
using System.Collections.Generic;
using System.Text;

namespace RavenDB.Samples.Verity.App
{
    public static class Constants
    {
        public static class EnvVars
        {
            public const string MaxGlobalRequestsPer15Minutes = "SAMPLES_VERITY_MAX_GLOBAL_REQUESTS_PER_15_MINUTES";
            public const string MaxSessionRequestsPer30Seconds = "SAMPLES_VERITY_MAX_SESSION_REQUESTS_PER_30_SECONDS";
            public const string OpenAiApiKey = "SAMPLES_VERITY_OPENAI_API_KEY";
            public const string SecEdgarUserAgent = "SAMPLES_VERITY_SEC_EDGAR_USER_AGENT";
            public const string DumpFilePath = "SAMPLES_VERITY_DUMP_FILE_PATH";
        }

        public static class DocumentIds
        {
            public const string GlobalApiUsage = "GlobalApiUsageLimiter/global";
            public static string SessionApiUsage(string sessionId) => $"ApiUsageSession/{sessionId}";
        }

        public static class TimeSeries
        {
            public const string Requests = "Requests";
        }

        public static class Cookies
        {
            public const string SessionId = "samples-verity-session-id";
        }
    }
}
