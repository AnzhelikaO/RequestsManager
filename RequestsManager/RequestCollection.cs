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
            public byte Announced;
            public string AnnounceText;
            public TaskCompletionSource<Decision> Source;
            public ReadOnlyCollection<ICondition> Conditions;

            public Request(IList<ICondition> Conditions, string AnnounceText)
            {
                this.Creation = DateTime.UtcNow;
                this.Announced = 0;
                this.AnnounceText = AnnounceText;
                this.Source = new TaskCompletionSource<Decision>();
                this.Conditions = ((Conditions == null)
                                    ? new ReadOnlyCollection<ICondition>(new ICondition[0])
                                    : new ReadOnlyCollection<ICondition>(Conditions));
            }
        }

        private ConcurrentDictionary<string, Request> Requests =
            new ConcurrentDictionary<string, Request>();
        public ICondition[] GetRequestConditions() =>
            Requests.Values.SelectMany(r => r.Conditions).ToArray();

        #endregion
        public object Player;
        public RequestCollection(object Player) => this.Player = Player;

        #region PlayerLeft

        public void PlayerLeft()
        {
            foreach (var pair in Requests)
                pair.Value.Source.SetResult(Decision.Left);
        }

        #endregion
        #region ForceCancel

        public void ForceCancel()
        {
            foreach (var pair in Requests)
                pair.Value.Source.SetResult(Decision.ForceCancelled);
        }

        #endregion
        #region BrokeCondition

        public void BrokeCondition(Type Type)
        {
            foreach (var pair in Requests)
                if (pair.Value.Conditions.Any(c => (c.GetType() == Type)))
                    SetDecision(pair.Key, Decision.FailedCondition);
        }

        #endregion

        #region Annouce

        public void Announce()
        {
            DateTime now = DateTime.UtcNow;
            foreach (var pair in Requests)
            {
                DateTime creation = pair.Value.Creation;
                if (now < creation.AddSeconds(2.5))
                    continue;

                if (++pair.Value.Announced >= 6)
                {
                    SetDecision(pair.Key, Decision.Expired);
                    RequestsManager.SendMessage?.Invoke(Player,
                        $"Request '{pair.Key}' has expired.", 255, 0, 0);
                    continue;
                }

                if (((pair.Value.Announced % 2) == 0)
                        && (pair.Value.AnnounceText != null))
                    RequestsManager.SendMessage?.Invoke(Player, pair.Value.AnnounceText, 255, 69, 0);
            }
        }

        #endregion

        #region GetDecision

        public async Task<(Decision Decision, ICondition BrokenCondition)> GetDecision(string Key,
            ICondition[] Conditions, string AnnounceText)
        {
            if (Key is null)
                throw new ArgumentNullException(nameof(Key));
            if (Requests.ContainsKey(Key))
                throw new ArgumentException($"Key '{Key}' is already in use.", nameof(Key));

            Request request = new Request(Conditions, AnnounceText);
            Requests.TryAdd(Key, request);
            RequestsManager.SendMessage?.Invoke(Player, AnnounceText, 255, 69, 0);
            Decision decision = await request.Source.Task;
            Requests.TryRemove(Key, out _);
            return (decision, request.Conditions.FirstOrDefault(c => c.Broken));
        }

        #endregion
        #region SetDecision

        public SetDecisionResult SetDecision(string Key, Decision Decision)
        {
            Request request;
            if (Key == null)
            {
                if (Requests.Count == 0)
                    return SetDecisionResult.NoRequests;
                else if (Requests.Count > 1)
                    return SetDecisionResult.NotSpecifiedRequest;
                Requests.TryRemove(Requests.ElementAt(0).Key, out request);
            }
            else if (!Requests.TryRemove(Key, out request))
                return SetDecisionResult.InvalidRequest;

            request.Source.SetResult(Decision);
            return SetDecisionResult.Success;
        }

        #endregion
    }
}