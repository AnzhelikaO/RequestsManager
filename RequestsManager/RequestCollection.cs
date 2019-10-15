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
            public DecisionDelegate OnDecision;
            public TaskCompletionSource<Decision> Source;
            public ReadOnlyCollection<ICondition> SenderConditions;
            public ReadOnlyCollection<ICondition> ReceiverConditions;

            public Request(IList<ICondition> SenderConditions, IList<ICondition> ReceiverConditions,
                string AnnounceText, DecisionDelegate OnDecision)
            {
                this.Creation = DateTime.UtcNow;
                this.AnnounceCount = 0;
                this.AnnounceText = AnnounceText;
                this.OnDecision = OnDecision;
                this.Source = new TaskCompletionSource<Decision>();
                this.SenderConditions = ((SenderConditions == null)
                                    ? new ReadOnlyCollection<ICondition>(new ICondition[0])
                                    : new ReadOnlyCollection<ICondition>(SenderConditions));
                this.ReceiverConditions = ((ReceiverConditions == null)
                                    ? new ReadOnlyCollection<ICondition>(new ICondition[0])
                                    : new ReadOnlyCollection<ICondition>(ReceiverConditions));
            }
        }

        private ConcurrentDictionary<string, ConcurrentDictionary<object, Request>> Inbox =
            new ConcurrentDictionary<string, ConcurrentDictionary<object, Request>>();
        // Multiple requests? This should be configurable (RequestTypeSettings instead of string key maybe)
        private ConcurrentDictionary<string, (object Player, Request Request)> Outbox =
            new ConcurrentDictionary<string, (object, Request)>();
        private ConcurrentDictionary<ICondition, byte> Conditions =
            new ConcurrentDictionary<ICondition, byte>();
        internal IEnumerable<ICondition> GetRequestConditions() => Conditions.Keys;

        #endregion
        public object Player;
        public RequestCollection(object Player) => this.Player = Player;

        #region PlayerLeft

        public void PlayerLeft()
        {
            foreach (var pair1 in Inbox)
                foreach (var pair2 in pair1.Value)
                    SetDecision(pair1.Key, pair2.Key, Decision.ReceiverLeft, out _, out _);
            foreach (var pair in Outbox)
                if (RequestsManager.RequestCollections.TryGetValue(pair.Value.Player,
                        out RequestCollection receiver))
                    receiver.SetDecision(pair.Key, Player, Decision.SenderLeft, out _, out _);
        }

        #endregion
        #region Dispose

        public void Dispose()
        {
            foreach (var pair1 in Inbox)
                foreach (var pair2 in pair1.Value)
                    SetDecision(pair1.Key, pair2.Key, Decision.Disposed, out _, out _);
            foreach (var pair in Outbox)
                if (RequestsManager.RequestCollections.TryGetValue(pair.Value.Player,
                        out RequestCollection receiver))
                    receiver.SetDecision(pair.Key, Player, Decision.Disposed, out _, out _);
        }

        #endregion
        #region BrokeCondition

        public void BrokeCondition(Type Type)
        {
            foreach (var pair1 in Inbox)
                foreach (var pair2 in pair1.Value)
                    if (pair2.Value.ReceiverConditions.Any(c => (c.GetType() == Type)))
                        SetDecision(pair1.Key, pair2.Key, Decision.ReceiverFailedCondition, out _, out _);
            foreach (var pair in Outbox)
                if (pair.Value.Request.SenderConditions.Any(c => (c.GetType() == Type))
                        && RequestsManager.RequestCollections.TryGetValue(pair.Value.Player,
                        out RequestCollection receiver))
                    receiver.SetDecision(pair.Key, Player, Decision.SenderFailedCondition, out _, out _);
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
                    DateTime creation = pair2.Value.Creation;
                    if ((now - creation) < inactiveTime)
                        continue;

                    if (++pair2.Value.AnnounceCount >= 6)
                    {
                        SetDecision(pair1.Key, pair2.Key, Decision.Expired, out _, out _);
                        string name = RequestsManager.GetPlayerNameFunc.Invoke(pair2.Key);
                        continue;
                    }

                    if (((pair2.Value.AnnounceCount % 2) == 0)
                            && (pair2.Value.AnnounceText != null))
                        RequestsManager.SendMessage?.Invoke(Player, pair2.Value.AnnounceText, 255, 69, 0);
                }
        }

        #endregion

        #region GetDecision

        public async Task<(Decision Decision, ICondition BrokenCondition)> GetDecision(string Key,
            object Sender, ICondition[] SenderConditions, ICondition[] ReceiverConditions,
            string AnnounceText, DecisionDelegate OnDecision)
        {
            if (Key is null)
                throw new ArgumentNullException(nameof(Key));
            if (Sender is null)
                throw new ArgumentNullException(nameof(Sender));

            RequestCollection senderCollection = RequestsManager.RequestCollections[Sender];
            Request request = new Request(SenderConditions, ReceiverConditions, AnnounceText, OnDecision);
            Inbox.TryAdd(Key, new ConcurrentDictionary<object, Request>());
            if (!senderCollection.Outbox.TryAdd(Key, (Player, request))
                    || !Inbox[Key].TryAdd(Sender, request))
                return (Decision.AlreadySent, null);

            if (SenderConditions != null)
                foreach (ICondition condition in SenderConditions)
                    Conditions.TryAdd(condition, 0);
            if (ReceiverConditions != null)
                foreach (ICondition condition in ReceiverConditions)
                    Conditions.TryAdd(condition, 0);
            RequestsManager.SendMessage?.Invoke(Sender, $"Sent {Key} request", 0, 128, 0);
            RequestsManager.SendMessage?.Invoke(Player, AnnounceText, 255, 69, 0);

            Decision decision = await request.Source.Task;

            Inbox[Key].TryRemove(Sender, out _);
            senderCollection.Outbox.TryRemove(Key, out _);
            if (SenderConditions != null)
                foreach (ICondition condition in SenderConditions)
                    Conditions.TryRemove(condition, out _);
            if (ReceiverConditions != null)
                foreach (ICondition condition in ReceiverConditions)
                    Conditions.TryRemove(condition, out _);
            return (decision, request.ReceiverConditions.FirstOrDefault(c => c.Broken));
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
            if (Sender == null)
            {
                if ((Key == null) || !Inbox.TryGetValue(Key, out inbox))
                    return RequestResult.NoRequests;
                if (inbox.Count > 1)
                    return RequestResult.NotSpecifiedRequest;
                Sender = inbox.FirstOrDefault().Key;
            }

            if ((Key == null) || (Sender == null) || !Inbox.TryGetValue(Key, out inbox))
                return RequestResult.NoRequests;
            if (!inbox.TryRemove(Sender, out Request request))
                return RequestResult.InvalidRequest;

            RealKey = Key;
            RealSender = Sender;
            #region Annouce

            string senderName = RequestsManager.GetPlayerNameFunc.Invoke(Sender);
            string receiverName = RequestsManager.GetPlayerNameFunc.Invoke(Player);
            switch (Decision)
            {
                case Decision.Accepted:
                    RequestsManager.SendMessage?.Invoke(Sender,
                        $"{receiverName} accepted your {Key} request.", 0, 128, 0);
                    RequestsManager.SendMessage?.Invoke(Player,
                        $"Accepted {Key} request from player {senderName}.", 0, 128, 0);
                    break;
                case Decision.Refused:
                    RequestsManager.SendMessage?.Invoke(Sender,
                        $"{receiverName} refused your {Key} request.", 255, 0, 0);
                    RequestsManager.SendMessage?.Invoke(Player,
                        $"Refused {Key} request from player {senderName}.", 0, 128, 0);
                    break;
                case Decision.Cancelled:
                    RequestsManager.SendMessage?.Invoke(Sender,
                        $"Cancelled {Key} request to player {receiverName}.", 0, 128, 0);
                    RequestsManager.SendMessage?.Invoke(Player,
                        $"{senderName} cancelled {Key} request.", 255, 0, 0);
                    break;
                case Decision.Disposed:
                    RequestsManager.SendMessage?.Invoke(Sender,
                        $"Your {Key} request to {receiverName} was force cancelled.", 255, 0, 0);
                    RequestsManager.SendMessage?.Invoke(Player,
                        $"{senderName}'s {Key} request was force cancelled.", 255, 0, 0);
                    break;
                case Decision.SenderFailedCondition:
                    RequestsManager.SendMessage?.Invoke(Sender,
                        $"You no longer fit conditions for {Key} request to {receiverName}.", 255, 0, 0);
                    RequestsManager.SendMessage?.Invoke(Player,
                        $"{senderName} no longer fits conditions for {Key} request.", 255, 0, 0);
                    break;
                case Decision.ReceiverFailedCondition:
                    RequestsManager.SendMessage?.Invoke(Sender,
                        $"{receiverName} no longer fits conditions for {Key} request.", 255, 0, 0);
                    RequestsManager.SendMessage?.Invoke(Player,
                        $"You no longer fit conditions for {Key} request from {senderName}.", 255, 0, 0);
                    break;
                case Decision.SenderLeft:
                    RequestsManager.SendMessage?.Invoke(Player,
                        $"{senderName} cancelled {Key} request by leaving.", 255, 0, 0);
                    break;
                case Decision.ReceiverLeft:
                    RequestsManager.SendMessage?.Invoke(Sender,
                        $"{receiverName} refused {Key} request by leaving.", 255, 0, 0);
                    break;
                case Decision.AlreadySent:
                    RequestsManager.SendMessage?.Invoke(Sender,
                        $"You have already sent {Key} request to {receiverName}.", 255, 0, 0);
                    break;
                case Decision.Expired:
                    RequestsManager.SendMessage?.Invoke(Sender,
                        $"Request '{Key}' for {receiverName} has expired.", 255, 0, 0);
                    RequestsManager.SendMessage?.Invoke(Player,
                        $"Request '{Key}' from {senderName} has expired.", 255, 0, 0);
                    break;
            }

            #endregion
            request.OnDecision?.Invoke(Sender, Player, Decision);
            request.Source.SetResult(Decision);
            return RequestResult.Success;
        }

        #endregion
        #region Cancel

        public RequestResult SenderCancelled(string Key, out string RealKey, out object Receiver)
        {
            RealKey = null;
            Receiver = null;
            if (Key == null)
            {
                if (Outbox.Count > 1)
                    return RequestResult.NotSpecifiedRequest;
                Key = Outbox.FirstOrDefault().Key;
            }

            if ((Key == null)
                    || !Outbox.TryGetValue(Key, out (object Player, Request) request)
                    || !RequestsManager.RequestCollections.TryGetValue(request.Player,
                    out RequestCollection receiver))
                return RequestResult.NoRequests;

            Receiver = receiver.Player;
            return receiver.SetDecision(Key, Player, Decision.Cancelled, out RealKey, out _);
        }

        #endregion
    }
}