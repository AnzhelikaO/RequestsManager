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
            public string AnnounceText;
            public string DecisionCommandMessage;
            public TaskCompletionSource<Decision> Source;
            public ReadOnlyCollection<ICondition> SenderConditions;
            public ReadOnlyCollection<ICondition> ReceiverConditions;

            public Request(IList<ICondition> SenderConditions, IList<ICondition> ReceiverConditions,
                string AnnounceText, string DecisionCommandMessage)
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
            }

            public void Annouce(object Player)
            {
                if (AnnounceText != null)
                {
                    RequestsManager.TrySendMessage(Player, AnnounceText, 255, 69, 0);
                    if (DecisionCommandMessage != null)
                        RequestsManager.TrySendMessage(Player, DecisionCommandMessage, 255, 69, 0);
                }
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
                        SetDecision(pair1.Key, pair2.Key, Decision.ReceiverFailedCondition, out _, out _);
            foreach (var pair1 in this.Outbox)
                foreach (var pair2 in pair1.Value)
                    if (pair2.Value.SenderConditions.Any(c => (c.GetType() == Type))
                            && RequestsManager.RequestCollections.TryGetValue(pair2.Key,
                            out RequestCollection receiverCollection))
                        receiverCollection.SetDecision(pair1.Key, Player,
                            Decision.SenderFailedCondition, out _, out _);
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

                    if (++request.AnnounceCount >= 6)
                    {
                        SetDecision(pair1.Key, pair2.Key, Decision.Expired, out _, out _);
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
            object Sender, string AnnounceText, ICondition[] SenderConditions,
            ICondition[] ReceiverConditions, string DecisionCommandMessage)
        {
            if (Key is null)
                throw new ArgumentNullException(nameof(Key));
            if (Sender is null)
                throw new ArgumentNullException(nameof(Sender));
            if (!RequestConfigurations.TryGetValue(Key, out RequestConfiguration configuration))
                throw new KeyNotConfiguredException(Key);
            bool emptySender = Sender.Equals(RequestsManager.EmptySender);
            if (!configuration.AllowSendingToMyself && Sender.Equals(Player))
            {
                RequestsManager.TrySendMessage(Sender, "You cannot request yourself.", 255, 0, 0);
                return (Decision.RequestedOwnPlayer, null);
            }

            if (!emptySender
                && Block.TryGetValue(Key, out var block)
                && block.TryGetValue(Sender, out _))
            {
                RequestsManager.TrySendMessage(Sender, RequestsManager.GetPlayerNameFunc(Player) +
                    $" blocked your {Key} requests.", 255, 0, 0);
                return (Decision.Blocked, null);
            }

            if (DecisionCommandMessage == null)
            {
                string specifier = RequestsManager.CommandSpecifier;
                string key = ((Key.Contains(" ") ? $"\"{Key}\"" : Key));
                if (emptySender)
                    DecisionCommandMessage = $"Type «{specifier}+ {key}» to accept " +
                        $"or «{specifier}- {key}» to refuse request.";
                else
                {
                    string senderName = RequestsManager.GetPlayerNameFunc(Sender);
                    DecisionCommandMessage = $"Type «{specifier}+ {key} {senderName}» to accept " +
                        $"or «{specifier}- {key} {senderName}» to refuse request.";
                }
            }

            Key = Key.ToLower();
            RequestCollection senderCollection = (emptySender
                ? null
                : RequestsManager.RequestCollections[Sender]);
            Request request = new Request(SenderConditions,
                ReceiverConditions, AnnounceText, DecisionCommandMessage);
            Inbox.TryAdd(Key, new ConcurrentDictionary<object, Request>());
            senderCollection?.Outbox.TryAdd(Key, new ConcurrentDictionary<object, Request>());
            if (!configuration.AllowSendingMultipleRequests)
            {
                object sentTo = senderCollection?.Outbox[Key].FirstOrDefault().Key;
                if (sentTo != null)
                {
                    RequestsManager.TrySendMessage(Sender, "You have already sent " +
                        $"{Key} request to {RequestsManager.GetPlayerNameFunc(sentTo)}.", 255, 0, 0);
                    return (Decision.AlreadySentToDifferentPlayer, null);
                }
            }
            if ((!emptySender && !senderCollection.Outbox[Key].TryAdd(Player, request))
                || !Inbox[Key].TryAdd(Sender, request))
            {
                RequestsManager.TrySendMessage(Sender, "You have already sent " +
                    $"{Key} request to {RequestsManager.GetPlayerNameFunc(Player)}.", 255, 0, 0);
                return (Decision.AlreadySentToSamePlayer, null);
            }

            if (SenderConditions != null)
                foreach (ICondition condition in SenderConditions)
                    Conditions.TryAdd(condition, 0);
            if (ReceiverConditions != null)
                foreach (ICondition condition in ReceiverConditions)
                    Conditions.TryAdd(condition, 0);
            RequestsManager.TrySendMessage(Sender, $"Sent {Key} request.", 0, 128, 0);
            request.Annouce(Player);

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
                        receiverCollection.SetDecision(Key, Sender, Decision.AcceptedAnother, out _, out _);
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
                    SetDecision(pair1.Key, pair2.Key, Inbox, out _, out _);
            foreach (var pair1 in this.Outbox)
                foreach (var pair2 in pair1.Value)
                    if (RequestsManager.RequestCollections.TryGetValue(pair2.Key,
                            out RequestCollection receiverCollection))
                        receiverCollection.SetDecision(pair1.Key, Player, Outbox, out _, out _);
        }

        #endregion

        #region SetDecision

        public RequestResult SetDecision(string Key, object Sender,
            Decision Decision, out string RealKey, out object RealSender)
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

            #region Annouce

            bool emptySender = Sender.Equals(RequestsManager.EmptySender);
            string senderName = (emptySender ? null : RequestsManager.GetPlayerNameFunc.Invoke(Sender));
            string receiverName = RequestsManager.GetPlayerNameFunc.Invoke(Player);
            switch (Decision)
            {
                case Decision.Accepted:
                    RequestsManager.TrySendMessage(Sender,
                        $"{receiverName} accepted your {Key} request.", 0, 128, 0);
                    if (senderName != null)
                        RequestsManager.TrySendMessage(Player,
                            $"Accepted {Key} request from player {senderName}.", 0, 128, 0);
                    else
                        RequestsManager.TrySendMessage(Player, $"Accepted {Key} request.", 0, 128, 0);
                    break;
                case Decision.AcceptedAnother:
                    RequestsManager.TrySendMessage(Player,
                        $"{senderName} accepted another {Key} request.", 255, 0, 0);
                    break;
                case Decision.Refused:
                    RequestsManager.TrySendMessage(Sender,
                        $"{receiverName} refused your {Key} request.", 255, 0, 0);
                    if (senderName != null)
                        RequestsManager.TrySendMessage(Player,
                            $"Refused {Key} request from player {senderName}.", 0, 128, 0);
                    else
                        RequestsManager.TrySendMessage(Player, $"Refused {Key} request.", 0, 128, 0);
                    break;
                case Decision.Cancelled:
                    RequestsManager.TrySendMessage(Sender,
                        $"Cancelled {Key} request to player {receiverName}.", 0, 128, 0);
                    RequestsManager.TrySendMessage(Player,
                        $"{senderName} cancelled {Key} request.", 255, 0, 0);
                    break;
                case Decision.Disposed:
                    RequestsManager.TrySendMessage(Sender,
                        $"Your {Key} request to {receiverName} was force cancelled.", 255, 0, 0);
                    if (senderName != null)
                        RequestsManager.TrySendMessage(Player,
                            $"{senderName}'s {Key} request was force cancelled.", 255, 0, 0);
                    else
                        RequestsManager.TrySendMessage(Player,
                            $"{Key} request was force cancelled.", 255, 0, 0);
                    break;
                case Decision.SenderFailedCondition:
                    RequestsManager.TrySendMessage(Sender,
                        $"You no longer fit conditions for {Key} request to {receiverName}.", 255, 0, 0);
                    RequestsManager.TrySendMessage(Player,
                        $"{senderName} no longer fits conditions for {Key} request.", 255, 0, 0);
                    break;
                case Decision.ReceiverFailedCondition:
                    RequestsManager.TrySendMessage(Sender,
                        $"{receiverName} no longer fits conditions for {Key} request.", 255, 0, 0);
                    if (senderName != null)
                        RequestsManager.TrySendMessage(Player,
                            $"You no longer fit conditions for {Key} request from {senderName}.", 255, 0, 0);
                    else
                        RequestsManager.TrySendMessage(Player,
                            $"You no longer fit conditions for {Key} request.", 255, 0, 0);
                    break;
                case Decision.SenderLeft:
                    RequestsManager.TrySendMessage(Player,
                        $"{senderName} cancelled {Key} request by leaving.", 255, 0, 0);
                    break;
                case Decision.ReceiverLeft:
                    RequestsManager.TrySendMessage(Sender,
                        $"{receiverName} refused {Key} request by leaving.", 255, 0, 0);
                    break;
                case Decision.Expired:
                    RequestsManager.TrySendMessage(Sender,
                        $"Request {Key} for {receiverName} has expired.", 255, 0, 0);
                    if (senderName != null)
                        RequestsManager.TrySendMessage(Player,
                            $"Request {Key} from {senderName} has expired.", 255, 0, 0);
                    else
                        RequestsManager.TrySendMessage(Player,
                            $"Request {Key} has expired.", 255, 0, 0);
                    break;
            }

            #endregion
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

            return receiverCollection.SetDecision(Key, Player, Decision.Cancelled, out _, out _);
        }

        #endregion
    }
}