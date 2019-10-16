namespace RequestsManagerAPI
{
    public class RequestConfiguration
    {
        public bool AllowSendingMultipleRequests;
        public string AnnounceTextFormat;

        public RequestConfiguration(bool AllowSendingMultipleRequests, string AnnounceTextFormat)
        {
            this.AllowSendingMultipleRequests = AllowSendingMultipleRequests;
            this.AnnounceTextFormat = AnnounceTextFormat;
        }
    }
}