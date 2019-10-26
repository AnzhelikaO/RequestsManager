namespace RequestsManagerAPI
{
    public enum MessageType
    {
        AnnounceOutbox,
        AnnounceInbox,
        DecisionCommand
    }

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

        public static Message Get(Message Original, Message? Override)
        {
            string with = (Override.HasValue
                            ? Override.Value.MessageWithSenderName
                            : Original.MessageWithSenderName);
            string without = (Override.HasValue
                                ? Override.Value.MessageWithoutSenderName
                                : Original.MessageWithoutSenderName);

            return new Message
            (
                with, without,
                (Override?.R ?? Original.R),
                (Override?.G ?? Original.G),
                (Override?.B ?? Original.B)
            );
        }

        public static Message Get(Message Original, Message? Override, string Key,
            string SenderName, string ReceiverName, string AnotherPlayerName = null)
        {
            Message message = Get(Original, Override);

            bool hasSender = !(SenderName is null);
            string msg = (hasSender
                            ? message.MessageWithSenderName
                            : message.MessageWithoutSenderName);
            if (msg is null)
                return message;

            if (Key != null)
                msg = msg.Replace("{KEY}", Key);
            if (ReceiverName != null)
                msg = msg.Replace("{RECEIVER}", ReceiverName);
            if (hasSender)
                msg = msg.Replace("{SENDER}", SenderName);
            if (AnotherPlayerName != null)
                msg = msg.Replace("{ANOTHERPLAYER}", AnotherPlayerName);
            message.ResultingMessage = msg;

            return message;
        }
    }
}