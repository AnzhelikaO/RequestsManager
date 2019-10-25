namespace RequestsManagerAPI
{
    public struct Message
    {
        public string MessageWithSenderName, MessageWithoutSenderName;
        public string ResultingMessage;
        public byte? R, G, B;

        public Message(string MessageWithSenderName, string MessageWithoutSenderName,
            byte? R = null, byte? G = null, byte? B = null)
        {
            this.MessageWithSenderName = MessageWithSenderName;
            this.MessageWithoutSenderName = MessageWithoutSenderName;
            this.ResultingMessage = null;
            this.R = R;
            this.G = G;
            this.B = B;
        }

        public Message(string Message, byte? R = null, byte? G = null, byte? B = null)
        {
            this.MessageWithSenderName = this.MessageWithoutSenderName = Message;
            this.ResultingMessage = null;
            this.R = R;
            this.G = G;
            this.B = B;
        }
    }
}