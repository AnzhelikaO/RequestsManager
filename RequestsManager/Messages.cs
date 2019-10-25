#region Using
using System;
using System.Collections.Generic;
#endregion
namespace RequestsManagerAPI
{
    public class Messages
    {
        #region Data

        Message? AnnounceOutboxMessageOverride, AnnounceInboxMessageOverride, DecisionCommandMessageOverride;
        private Dictionary<Decision, Message>[] FormatOverrides =
            new Dictionary<Decision, Message>[]
            {
                new Dictionary<Decision, Message>(),
                new Dictionary<Decision, Message>()
            };
        
        #endregion
        #region Constructor

        public Messages(Message? AnnounceMessageOverride = null,
            Message? DecisionCommandMessageOverride = null,
            Dictionary<Decision, Message> SenderFormatOverrides = null,
            Dictionary<Decision, Message> ReceiverFormatOverrides = null)
        {
            this.AnnounceInboxMessageOverride = AnnounceMessageOverride;
            this.DecisionCommandMessageOverride = DecisionCommandMessageOverride;
            this.FormatOverrides[0] = (SenderFormatOverrides ?? new Dictionary<Decision, Message>());
            this.FormatOverrides[1] = (ReceiverFormatOverrides ?? new Dictionary<Decision, Message>());
        }

        #endregion

        #region GetDefaultMessageFormat

        public static Message GetDefaultMessageFormat(bool ToSender, Decision Decision)
        {
            switch (Decision)
            {
                case Decision.Accepted:
                    return (ToSender
                            ? new Message("{RECEIVER} accepted your {KEY} request.", 0, 128, 0)
                            : new Message("Accepted {KEY} request from player {SENDER}.",
                                          "Accepted {KEY} request.", 0, 128, 0));
                case Decision.AcceptedAnother:
                    return (ToSender
                            ? new Message("You already accepted another {KEY} request.", 255, 0, 0)
                            : new Message("{SENDER} accepted another {KEY} request.",
                                          "{KEY} request was cancelled.", 255, 0, 0));
                case Decision.Refused:
                    return (ToSender
                            ? new Message("{RECEIVER} refused your {KEY} request.", 255, 0, 0)
                            : new Message("Refused {KEY} request from player {SENDER}.",
                                          "Refused {KEY} request.", 0, 128, 0));
                case Decision.Cancelled:
                    return (ToSender
                            ? new Message("Cancelled {KEY} request to player {RECEIVER}.", 0, 128, 0)
                            : new Message("{SENDER} cancelled {KEY} request.",
                                          "{KEY} request was cancelled.", 255, 0, 0));
                case Decision.Disposed:
                    return (ToSender
                            ? new Message("Your {KEY} request to {RECEIVER} was force cancelled.", 255, 0, 0)
                            : new Message("{SENDER}'s {KEY} request was force cancelled.",
                                          "{KEY} request was force cancelled.", 255, 0, 0));
                case Decision.RequestedOwnPlayer:
                    return (ToSender
                            ? new Message("You cannot request yourself.", 255, 0, 0)
                            : new Message(null, 255, 0, 0));
                case Decision.Blocked:
                    return (ToSender
                            ? new Message("{RECEIVER} blocked your {KEY} requests.", 255, 0, 0)
                            : new Message("{SENDER} tried to send {KEY} request while being blocked.",
                                          null, 255, 0, 0));
                case Decision.SenderFailedCondition:
                    return (ToSender
                            ? new Message("You no longer fit conditions for {KEY} request to {RECEIVER}.",
                                          255, 0, 0)
                            : new Message("{SENDER} no longer fits conditions for {KEY} request.",
                                          "{KEY} request was force cancelled.", 255, 0, 0));
                case Decision.ReceiverFailedCondition:
                    return (ToSender
                            ? new Message("{RECEIVER} no longer fits conditions for {KEY} request.",
                                          255, 0, 0)
                            : new Message("You no longer fit conditions for {KEY} request from {SENDER}.",
                                          "You no longer fit conditions for {KEY} request.", 255, 0, 0));
                case Decision.SenderLeft:
                    return (ToSender
                            ? new Message("You cancelled {KEY} request by leaving.", 255, 0, 0)
                            : new Message("{SENDER} cancelled {KEY} request by leaving.",
                                          "{KEY} request was force cancelled.", 255, 0, 0));
                case Decision.AlreadySentToSamePlayer:
                    return (ToSender
                            ? new Message("You have already sent {KEY} request to {RECEIVER}.", 255, 0, 0)
                            : new Message(null, 255, 0, 0));
                case Decision.AlreadySentToDifferentPlayer:
                    return (ToSender
                            ? new Message("You have already sent {KEY} request to {ANOTHERPLAYER}.",
                                          255, 0, 0)
                            : new Message(null, 255, 0, 0));
                case Decision.Expired:
                    return (ToSender
                            ? new Message("Request '{KEY}' for {RECEIVER} has expired.", 255, 0, 0)
                            : new Message("Request '{KEY}' from {SENDER} has expired.",
                                          "Request '{KEY}' has expired.", 255, 0, 0));
                default:
                    throw new NotImplementedException();
            }
        }

        #endregion
        #region GetMessage

        public enum MessageType
        {
            AnnounceOutbox,
            AnnounceInbox,
            DecisionCommand
        }

        public Message GetMessage(MessageType Type, object Receiver, object Sender, string Key)
        {
            if (Receiver is null)
                throw new ArgumentNullException(nameof(Receiver));
            if (Key is null)
                throw new ArgumentNullException(nameof(Key));

            string receiverName = RequestsManager.GetPlayerNameFunc?.Invoke(Receiver);
            string senderName = ((Sender == null || Sender.Equals(RequestsManager.EmptySender))
                                    ? null
                                    : RequestsManager.GetPlayerNameFunc?.Invoke(Sender));
            Message? messageOverride;
            Message messageOriginal;
            switch (Type)
            {
                case MessageType.AnnounceOutbox:
                    messageOverride = AnnounceOutboxMessageOverride;
                    messageOriginal = new Message
                    (
                        "Sent {KEY} request to {RECEIVER}.",
                        0, 128, 0
                    );
                    break;
                case MessageType.AnnounceInbox:
                    messageOverride = AnnounceInboxMessageOverride;
                    messageOriginal = new Message
                    (
                        "You have received {KEY} request from player {SENDER}.",
                        "You have received {KEY} request.",
                        255, 69, 0
                    );
                    break;
                case MessageType.DecisionCommand:
                    messageOverride = DecisionCommandMessageOverride;
                    string specifier = RequestsManager.CommandSpecifier;
                    string key = ((Key.Contains(" ") ? $"\"{Key}\"" : Key));
                    messageOriginal = new Message
                    (
                        $"Type «{specifier}+ {key} {senderName}» to accept " +
                        $"or «{specifier}- {key} {senderName}» to refuse request.",
                        $"Type «{specifier}+ {key}» to accept " +
                        $"or «{specifier}- {key}» to refuse request.",
                        255, 69, 0
                    );
                    break;
                default:
                    throw new NotImplementedException();
            }

            string with = (messageOverride?.MessageWithSenderName
                ?? messageOriginal.MessageWithSenderName);
            string without = (messageOverride?.MessageWithoutSenderName
                ?? messageOriginal.MessageWithoutSenderName);

            return new Message
            (
                with, without,
                (messageOverride?.R ?? messageOriginal.R),
                (messageOverride?.G ?? messageOriginal.G),
                (messageOverride?.B ?? messageOriginal.B)
            )
            {
                ResultingMessage =
                    ((senderName is null)
                        ? Replace(without, Key, senderName, receiverName, null)
                        : Replace(with, Key, senderName, receiverName, null))
            };
        }

        public Dictionary<object, Message> GetMessages(object Sender, object Receiver,
            Decision Decision, string Key, string AnotherPlayerName = null)
        {
            if (Receiver is null)
                throw new ArgumentNullException(nameof(Sender));
            if (Key is null)
                throw new ArgumentNullException(nameof(Key));

            string senderName = ((Sender == null || Sender.Equals(RequestsManager.EmptySender))
                                    ? null
                                    : RequestsManager.GetPlayerNameFunc?.Invoke(Sender));
            string receiverName = RequestsManager.GetPlayerNameFunc?.Invoke(Receiver);
            object[] players = new object[] { Sender, Receiver };
            Dictionary<object, Message> messages = new Dictionary<object, Message>();
            for (int i = 0; i < players.Length; i++)
                if (players[i] != null)
                {
                    Message? messageOverride = null;
                    if (FormatOverrides[i].TryGetValue(Decision, out Message msg))
                        messageOverride = msg;
                    Message messageOriginal = GetDefaultMessageFormat((i == 0), Decision);

                    string with = (messageOverride?.MessageWithSenderName
                        ?? messageOriginal.MessageWithSenderName);
                    string without = (messageOverride?.MessageWithoutSenderName
                        ?? messageOriginal.MessageWithoutSenderName);

                    messages[players[i]] = new Message
                    (
                        with, without,
                        (messageOverride?.R ?? messageOriginal.R),
                        (messageOverride?.G ?? messageOriginal.G),
                        (messageOverride?.B ?? messageOriginal.B)
                    )
                    {
                        ResultingMessage =
                            ((senderName is null)
                                ? Replace(without, Key, senderName, receiverName, AnotherPlayerName)
                                : Replace(with, Key, senderName, receiverName, AnotherPlayerName))
                    };
                }
            return messages;
        }

        #endregion
        #region SendMessage

        public void SendMessage(object Sender, object Receiver, Decision Decision,
            string Key, string AnotherPlayerName = null)
        {
            foreach (var pair in GetMessages(Sender, Receiver, Decision, Key, AnotherPlayerName))
                RequestsManager.TrySendMessage(pair.Key, pair.Value.ResultingMessage,
                    (pair.Value.R ?? 255), (pair.Value.G ?? 255), (pair.Value.B ?? 255));
        }

        #endregion
        #region Replace

        private string Replace(string Input, string Key, string SenderName,
            string ReceiverName, string AnotherPlayerName)
        {
            if (Input is null)
                return null;

            string result = Input.Replace("{KEY}", Key)
                                 .Replace("{RECEIVER}", ReceiverName);
            if (SenderName != null)
                result = result.Replace("{SENDER}", SenderName);
            if (AnotherPlayerName != null)
                result = result.Replace("{ANOTHERPLAYER}", AnotherPlayerName);
            return result;
        }

        #endregion
    }
}