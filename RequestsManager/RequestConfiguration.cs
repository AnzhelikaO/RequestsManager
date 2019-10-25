namespace RequestsManagerAPI
{
    public class RequestConfiguration
    {
        public bool AllowSendingMultipleRequests;
        public bool AllowSendingToMyself;
        public bool AllowMultiAccept;
        public bool RepeatDecisionCommandMessage;
        public int ExpirationTime;

        public RequestConfiguration(bool AllowSendingMultipleRequests, bool AllowSendingToMyself,
            bool AllowMultiAccept, bool RepeatDecisionCommandMessage, int ExpirationTime)
        {
            this.AllowSendingMultipleRequests = AllowSendingMultipleRequests;
            this.AllowSendingToMyself = AllowSendingToMyself;
            this.AllowMultiAccept = AllowMultiAccept;
            this.RepeatDecisionCommandMessage = RepeatDecisionCommandMessage;
            this.ExpirationTime = ExpirationTime;
        }
    }
}