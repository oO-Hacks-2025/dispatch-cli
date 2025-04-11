namespace Utils.Constants
{
    public class RequestPath
    {
        public const string Next = url + "/calls/next";
        public const string Queue = url + "/calls/queue";
        public const string Reset = url + "/control/reset";
        public const string Stop = url + "/control/stop";
        public const string Status = url + "/control/status";
        public const string Search = url + "/medical/search";
        public const string SearchByCity = url + "/medical/searchbycity";
        public const string Dispatch = url + "/medical/dispatch";

        public const string GetLocations = url + "/locations";

        private const string url = "http://localhost:5000";
    }
}