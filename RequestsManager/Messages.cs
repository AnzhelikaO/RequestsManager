#region Using
using System;
using System.Collections.Generic;
#endregion
namespace RequestsManagerAPI
{
    public class Messages
    {
        #region Data

        private Dictionary<Decision, Message>[] DecisionFormatOverrides =
            new Dictionary<Decision, Message>[]
            {
                new Dictionary<Decision, Message>(),
                new Dictionary<Decision, Message>()
            };
        private Dictionary<MessageType, Message> OtherFormatOverrides =
            new Dictionary<MessageType, Message>();

        #endregion
        #region Constructor

        public Messages(Dictionary<Decision, Message> SenderFormatOverrides = null,
            Dictionary<Decision, Message> ReceiverFormatOverrides = null,
            Dictionary<MessageType, Message> OtherFormatOverrides = null)
        {
            this.DecisionFormatOverrides[0] =
                (SenderFormatOverrides ?? new Dictionary<Decision, Message>());
            this.DecisionFormatOverrides[1] =
                (ReceiverFormatOverrides ?? new Dictionary<Decision, Message>());
            this.OtherFormatOverrides =
                (OtherFormatOverrides ?? new Dictionary<MessageType, Message>());
        }

        #endregion

        #region GetDefaultDecisionMessageFormat

        public static Message GetDefaultDecisionMessageFormat(bool ToSender, Decision Decision)
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
                case Decision.ReceiverLeft:
                    return (ToSender
                            ? new Message("{RECEIVER} refused your {KEY} request by leaving.", 255, 0, 0)
                            : new Message("Refused {KEY} request from player {SENDER} by leaving.",
                                          "Refused {KEY} request.", 0, 128, 0));
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
        #region GetDefaultOtherMessageFormat

        public static Message GetDefaultOtherMessageFormat(MessageType Type)
        {
            switch (Type)
            {
                case MessageType.AnnounceInbox:
                    return new Message
                    (
                        "You have received {KEY} request from player {SENDER}.",
                        "You have received {KEY} request.",
                        255, 69, 0
                    );
                case MessageType.AnnounceOutbox:
                    return new Message("Sent {KEY} request to {RECEIVER}.", 0, 128, 0);
                case MessageType.DecisionCommand:
                    string specifier = RequestsManager.CommandSpecifier;
                    return new Message
                    (
                        $"Type «{specifier}+ {{KEY}} {{SENDER}}» to accept " +
                        $"or «{specifier}- {{KEY}} {{SENDER}}» to refuse request.",
                        $"Type «{specifier}+ {{KEY}}» to accept " +
                        $"or «{specifier}- {{KEY}}» to refuse request.",
                        255, 69, 0
                    );
                default:
                    throw new NotImplementedException();
            }
        }

        #endregion
        #region GetMessage

        public Dictionary<object, Message> GetMessages(object Sender, object Receiver,
            Decision Decision, string Key, string AnotherPlayerName = null)
        {
            if (Receiver is null)
                throw new ArgumentNullException(nameof(Receiver));
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
                    if (DecisionFormatOverrides[i].TryGetValue(Decision, out Message msg))
                        messageOverride = msg;

                    messages[players[i]] = Message.Get(GetDefaultDecisionMessageFormat((i == 0), Decision),
                        messageOverride, Key, senderName, receiverName, AnotherPlayerName);
                }
            return messages;
        }

        public Message GetMessage(MessageType Type, object Sender, object Receiver, string Key)
        {
            if (Receiver is null)
                throw new ArgumentNullException(nameof(Receiver));
            if (Key is null)
                throw new ArgumentNullException(nameof(Key));

            Message? messageOverride = null;
            if (OtherFormatOverrides.TryGetValue(Type, out Message msg))
                messageOverride = msg;

            string senderName = ((Sender == null || Sender.Equals(RequestsManager.EmptySender))
                                    ? null
                                    : RequestsManager.GetPlayerNameFunc?.Invoke(Sender));
            string receiverName = RequestsManager.GetPlayerNameFunc?.Invoke(Receiver);
            return Message.Get(GetDefaultOtherMessageFormat(Type),
                messageOverride, Key, senderName, receiverName, null);
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

        public void SendMessage(object SendTo, object Sender,
            object Receiver, MessageType Type, string Key)
        {
            Message message = GetMessage(Type, Sender, Receiver, Key);
            RequestsManager.TrySendMessage(SendTo, message.ResultingMessage,
                (message.R ?? 255), (message.G ?? 255), (message.B ?? 255));
        }

        #endregion
    }
}