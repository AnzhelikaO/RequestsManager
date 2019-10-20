namespace RequestsManagerAPI
{
    public class RequestConfiguration
    {
        public bool AllowSendingMultipleRequests;
        public bool AllowSendingToMyself;
        public bool AllowMultiAccept;

        public RequestConfiguration(bool AllowSendingMultipleRequests, bool AllowSendingToMyself,
            bool AllowMultiAccept)
        {
            this.AllowSendingMultipleRequests = AllowSendingMultipleRequests;
            this.AllowSendingToMyself = AllowSendingToMyself;
            this.AllowMultiAccept = AllowMultiAccept;
        }
    }
}