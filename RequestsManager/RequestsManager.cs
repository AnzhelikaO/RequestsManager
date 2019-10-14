#region Using
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
#endregion
namespace RequestsManagerAPI
{
    public delegate void SendMessage(object Player, string Text, byte R, byte G, byte B);
    public static class RequestsManager
    {
        public static SendMessage SendMessage;
        private static ConcurrentDictionary<object, RequestCollection> RequestCollections =
            new ConcurrentDictionary<object, RequestCollection>();
        private static Timer AnnouceTimer = new Timer(2500) { AutoReset = true };

        #region Initialize

        public static void Initialize()
        {
            AnnouceTimer.Elapsed += OnElapsed;
            AnnouceTimer.Start();
        }

        #endregion
        #region Dispose

        public static void Dispose()
        {
            AnnouceTimer.Elapsed -= OnElapsed;
            AnnouceTimer.Stop();

            foreach (var pair in RequestCollections)
            {
                RequestCollections.TryRemove(pair.Key, out RequestCollection collection);
                collection.ForceCancel();
            }
        }

        #endregion

        #region PlayerJoined

        public static void PlayerJoined(object Player)
        {
            if (Player is null)
                return;

            if (RequestCollections.TryGetValue(Player, out RequestCollection old))
                RequestCollections.TryUpdate(Player, new RequestCollection(Player), old);
            else
                RequestCollections.TryAdd(Player, new RequestCollection(Player));
        }

        #endregion
        #region PlayerLeft

        public static void PlayerLeft(object Player)
        {
            if (!RequestCollections.ContainsKey(Player))
                return;

            RequestCollections.TryRemove(Player, out RequestCollection collection);
            collection.PlayerLeft();
        }

        #endregion

        #region GetRequestConditions

        public static ICondition[] GetRequestConditions(object Player) =>
            ((Player != null) && RequestCollections.TryGetValue(Player, out RequestCollection collection)
                ? collection.GetRequestConditions()
                : new ICondition[0]);

        #endregion
        #region BrokeCondition

        internal static void BrokeCondition(object Player, Type Type) =>
            RequestCollections[Player].BrokeCondition(Type);

        #endregion

        #region OnElapsed

        private static void OnElapsed(object sender, ElapsedEventArgs e)
        {
            foreach (RequestCollection collection in RequestCollections.Values)
                collection.Announce();
        }

        #endregion

        #region GetDecision

        public static async Task<(Decision Decision, ICondition BrokenCondition)> GetDecision(object Player,
            string Key, string AnnounceText, params ICondition[] Conditions)
        {
            if (!RequestCollections.ContainsKey(Player))
                RequestCollections.TryAdd(Player, new RequestCollection(Player));

            return await RequestCollections[Player].GetDecision(Key, Conditions, AnnounceText);
        }

        #endregion
        #region SetDecision

        public static SetDecisionResult SetDecision(object Player, string Key, Decision Decision)
        {
            if (!RequestCollections.TryGetValue(Player, out RequestCollection collection))
                return SetDecisionResult.NoRequests;

            return collection.SetDecision(Key, Decision);
        }

        #endregion
    }
}