#region Using
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
#endregion
namespace RequestsManagerAPI
{
    internal class RequestCollection
    {
        #region Requests

        private class Request
        {
            public DateTime Creation;
            public byte AnnounceCount;
            public Message AnnounceText;
            public Message DecisionCommandMessage;
            public TaskCompletionSource<Decision> Source;
            public ReadOnlyCollection<ICondition> SenderConditions;
            public ReadOnlyCollection<ICondition> ReceiverConditions;
            public bool RepeatDecisionCommandMessage;
            public int ExpirationTime;

            public Request(IList<ICondition> SenderConditions,
                IList<ICondition> ReceiverConditions, Message AnnounceText, Message DecisionCommandMessage,
                bool RepeatDecisionCommandMessage, int ExpirationTime)
            {
                this.Creation = DateTime.UtcNow;
                this.AnnounceCount = 0;
                this.AnnounceText = AnnounceText;
                this.DecisionCommandMessage = DecisionCommandMessage;
                this.Source = new TaskCompletionSource<Decision>();
                this.SenderConditions = ((SenderConditions == null)
                                    ? new ReadOnlyCollection<ICondition>(new ICondition[0])
                                    : new ReadOnlyCollection<ICondition>(SenderConditions));
                this.ReceiverConditions = ((ReceiverConditions == null)
                                    ? new ReadOnlyCollection<ICondition>(new ICondition[0])
                                    : new ReadOnlyCollection<ICondition>(ReceiverConditions));
                this.RepeatDecisionCommandMessage = RepeatDecisionCommandMessage;
                this.ExpirationTime = ExpirationTime;
            }

            public void Annouce(object Player, bool Force = false)
            {
                if (!Force && !RepeatDecisionCommandMessage)
                    return;
                RequestsManager.TrySendMessage(Player, AnnounceText.ResultingMessage, 255, 69, 0);
                RequestsManager.TrySendMessage(Player, DecisionCommandMessage.ResultingMessage, 255, 69, 0);
            }
        }

        private object Locker = new object();
        private ConcurrentDictionary<string, ConcurrentDictionary<object, Request>> Inbox =
            new ConcurrentDictionary<string, ConcurrentDictionary<object, Request>>();
        private ConcurrentDictionary<string, ConcurrentDictionary<object, Request>> Outbox =
            new ConcurrentDictionary<string, ConcurrentDictionary<object, Request>>();
        internal ConcurrentDictionary<string, ConcurrentDictionary<object, byte>> Block =
            new ConcurrentDictionary<string, ConcurrentDictionary<object, byte>>();
        private ConcurrentDictionary<ICondition, byte> Conditions =
            new ConcurrentDictionary<ICondition, byte>();
        internal IEnumerable<ICondition> GetRequestConditions() => Conditions.Keys;

        #endregion
        internal static ConcurrentDictionary<string, RequestConfiguration> RequestConfigurations =
            new ConcurrentDictionary<string, RequestConfiguration>();
        public object Player;
        public RequestCollection(object Player) => this.Player = Player;

        #region BrokeCondition

        public void BrokeCondition(Type Type)
        {
            foreach (var pair1 in this.Inbox)
                foreach (var pair2 in pair1.Value)
                    if (pair2.Value.ReceiverConditions.Any(c => (c.GetType() == Type)))
                        SetDecision(pair1.Key, pair2.Key, Decision.ReceiverFailedCondition,
                            out _, out _, null);
            foreach (var pair1 in this.Outbox)
                foreach (var pair2 in pair1.Value)
                    if (pair2.Value.SenderConditions.Any(c => (c.GetType() == Type))
                            && RequestsManager.RequestCollections.TryGetValue(pair2.Key,
                            out RequestCollection receiverCollection))
                        receiverCollection.SetDecision(pair1.Key, Player,
                            Decision.SenderFailedCondition, out _, out _, null);
        }

        #endregion

        #region Annouce

        public void Announce()
        {
            DateTime now = DateTime.UtcNow;
            TimeSpan inactiveTime = TimeSpan.FromSeconds(2.5);
            foreach (var pair1 in Inbox)
                foreach (var pair2 in pair1.Value)
                {
                    Request request = pair2.Value;
                    DateTime creation = request.Creation;
                    if ((now - creation) < inactiveTime)
                        continue;

                    if (++request.AnnounceCount >= (request.ExpirationTime / 2.5))
                    {
                        SetDecision(pair1.Key, pair2.Key, Decision.Expired, out _, out _, null);
                        string name = RequestsManager.GetPlayerNameFunc.Invoke(pair2.Key);
                        continue;
                    }

                    if ((request.AnnounceCount % 2) == 0)
                        request.Annouce(Player);
                }
        }

        #endregion

        #region GetDecision

        public async Task<(Decision Decision, ICondition BrokenCondition)> GetDecision(string Key,
            object Sender, Messages Messages, ICondition[] SenderConditions, ICondition[] ReceiverConditions)
        {
            if (Key is null)
                throw new ArgumentNullException(nameof(Key));
            if (Sender is null)
                throw new ArgumentNullException(nameof(Sender));
            if (!RequestConfigurations.TryGetValue(Key, out RequestConfiguration configuration))
                throw new KeyNotConfiguredException(Key);

            if (Messages == null)
                Messages = new Messages();
            bool emptySender = Sender.Equals(RequestsManager.EmptySender);

            if (!configuration.AllowSendingToMyself && Sender.Equals(Player))
            {
                Messages.SendMessage(Sender, Player, Decision.RequestedOwnPlayer, Key);
                return (Decision.RequestedOwnPlayer, null);
            }

            if (!emptySender
                && Block.TryGetValue(Key, out var block)
                && block.TryGetValue(Sender, out _))
            {
                Messages.SendMessage(Sender, Player, Decision.Blocked, Key);
                return (Decision.Blocked, null);
            }

            Request request = new Request
            (
                SenderConditions,
                ReceiverConditions,
                Messages.GetMessage(Messages.MessageType.AnnounceInbox, Player, Sender, Key),
                Messages.GetMessage(Messages.MessageType.DecisionCommand, Player, Sender, Key),
                configuration.RepeatDecisionCommandMessage,
                configuration.ExpirationTime
            );
            Key = Key.ToLower();
            RequestCollection senderCollection = (emptySender
                ? null
                : RequestsManager.RequestCollections[Sender]);
            Inbox.TryAdd(Key, new ConcurrentDictionary<object, Request>());
            senderCollection?.Outbox.TryAdd(Key, new ConcurrentDictionary<object, Request>());
            if (!configuration.AllowSendingMultipleRequests)
            {
                object sentTo = senderCollection?.Outbox[Key].FirstOrDefault().Key;
                if (sentTo != null)
                {
                    Messages.SendMessage(Sender, Player, Decision.AlreadySentToDifferentPlayer,
                        Key, RequestsManager.GetPlayerNameFunc(sentTo));
                    return (Decision.AlreadySentToDifferentPlayer, null);
                }
            }
            if ((!emptySender && !senderCollection.Outbox[Key].TryAdd(Player, request))
                || !Inbox[Key].TryAdd(Sender, request))
            {
                Messages.SendMessage(Sender, Player, Decision.AlreadySentToSamePlayer, Key);
                return (Decision.AlreadySentToSamePlayer, null);
            }

            if (SenderConditions != null)
                foreach (ICondition condition in SenderConditions)
                    Conditions.TryAdd(condition, 0);
            if (ReceiverConditions != null)
                foreach (ICondition condition in ReceiverConditions)
                    Conditions.TryAdd(condition, 0);
            RequestsManager.TrySendMessage(Sender, $"Sent {Key} request " +
                $"to {RequestsManager.GetPlayerNameFunc(Player)}.", 0, 128, 0);
            request.Annouce(Player, true);

            Decision decision = await request.Source.Task;

            lock (Locker)
            {
                Inbox[Key].TryRemove(Sender, out _);
                if (Inbox[Key].Count == 0)
                    Inbox.TryRemove(Key, out _);
            }
            if (!emptySender && !configuration.AllowMultiAccept)
                foreach (var pair in senderCollection.Outbox[Key])
                    if (!pair.Value.Equals(request)
                            && RequestsManager.RequestCollections.TryGetValue(pair.Key,
                            out RequestCollection receiverCollection))
                        receiverCollection.SetDecision(Key, Sender, Decision.AcceptedAnother,
                            out _, out _, null);
            senderCollection?.Outbox.TryRemove(Key, out _);
            if (SenderConditions != null)
                foreach (ICondition condition in SenderConditions)
                    Conditions.TryRemove(condition, out _);
            if (ReceiverConditions != null)
                foreach (ICondition condition in ReceiverConditions)
                    Conditions.TryRemove(condition, out _);

            return (decision, request.ReceiverConditions.FirstOrDefault(c => c.Broken));
        }

        #endregion
        #region SetGlobalDecision

        public void SetGlobalDecision(Decision Inbox, Decision Outbox)
        {
            foreach (var pair1 in this.Inbox)
                foreach (var pair2 in pair1.Value)
                    SetDecision(pair1.Key, pair2.Key, Inbox, out _, out _, null);
            foreach (var pair1 in this.Outbox)
                foreach (var pair2 in pair1.Value)
                    if (RequestsManager.RequestCollections.TryGetValue(pair2.Key,
                            out RequestCollection receiverCollection))
                        receiverCollection.SetDecision(pair1.Key, Player, Outbox, out _, out _, null);
        }

        #endregion

        #region SetDecision

        public RequestResult SetDecision(string Key, object Sender, Decision Decision,
            out string RealKey, out object RealSender, Messages AnnounceMessages)
        {
            RealKey = null;
            RealSender = null;
            ConcurrentDictionary<object, Request> inbox;

            // ?
            if (Key == null)
            {
                if (Inbox.Count > 1)
                    return RequestResult.NotSpecifiedRequest;
                Key = Inbox.FirstOrDefault().Key;
            }
            else
                Key = Key.ToLower();
            RealKey = Key;

            if (Sender == null)
            {
                if (Key == null)
                    return RequestResult.NoRequests;
                else if (!Inbox.TryGetValue(Key, out inbox))
                    return RequestResult.InvalidRequest;
                else if (inbox.Count > 1)
                    return RequestResult.NotSpecifiedRequest;
                else
                    Sender = inbox.FirstOrDefault().Key;
            }
            RealSender = Sender;

            if ((Key == null) || (Sender == null))
                return RequestResult.NoRequests;
            if (!Inbox.TryGetValue(Key, out inbox)
                    || !inbox.TryRemove(Sender, out Request request))
                return RequestResult.InvalidRequest;

            (AnnounceMessages ?? new Messages()).SendMessage(Sender, Player, Decision, Key);
            request.Source.SetResult(Decision);
            return RequestResult.Success;
        }

        #endregion
        #region Cancel

        public RequestResult SenderCancelled(string Key, object Receiver,
            out string RealKey, out object RealReceiver)
        {
            RealKey = null;
            RealReceiver = null;

            if (Key == null)
            {
                if (Outbox.Count > 1)
                    return RequestResult.NotSpecifiedRequest;
                Key = Outbox.FirstOrDefault().Key;
            }
            else
                Key = Key.ToLower();
            RealKey = Key;

            if (Receiver == null)
            {
                if (Key == null)
                    return RequestResult.NoRequests;
                else if (!Outbox.TryGetValue(Key, out var outbox))
                    return RequestResult.InvalidRequest;
                else if (outbox.Count > 1)
                    return RequestResult.NotSpecifiedRequest;
                else
                    Receiver = outbox.FirstOrDefault().Key;
            }
            RealReceiver = Receiver;

            if ((Key == null) || (Receiver == null))
                return RequestResult.NoRequests;
            if (!RequestsManager.RequestCollections.TryGetValue(Receiver,
                    out RequestCollection receiverCollection))
                return RequestResult.InvalidRequest;

            return receiverCollection.SetDecision(Key, Player, Decision.Cancelled, out _, out _, null);
        }

        #endregion
    }
}