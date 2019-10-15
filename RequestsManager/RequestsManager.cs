#region Using
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Timers;
#endregion
namespace RequestsManagerAPI
{
    public delegate void SendMessage(object Player, string Text, byte R, byte G, byte B);
    public delegate void AcceptedDelegate(object Sender, object Receiver);

    public static class RequestsManager
    {
        public static SendMessage SendMessage;
        public static Func<object, string> GetPlayerNameFunc { get; private set; }
        internal static ConcurrentDictionary<object, RequestCollection> RequestCollections =
            new ConcurrentDictionary<object, RequestCollection>();
        private static Timer AnnouceTimer = new Timer(2500) { AutoReset = true };
        
        #region Initialize

        public static void Initialize(Func<object, string> GetPlayerNameFunc)
        {
            RequestsManager.GetPlayerNameFunc = GetPlayerNameFunc;
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
                object Sender, string Key, string AnnounceText, AcceptedDelegate OnAccepted = null,
                ICondition[] SenderConditions = null, ICondition[] ReceiverConditions = null) =>
            await RequestCollections[Player].GetDecision(Key, Sender,
                SenderConditions, ReceiverConditions, AnnounceText, OnAccepted);

        #endregion
        #region SetDecision

        public static RequestResult SetDecision(object Player, string Key,
                object Sender, Decision Decision, out string RealKey, out object RealSender) =>
            RequestCollections[Player].SetDecision(Key, Sender, Decision, out RealKey, out RealSender);

        #endregion
        #region Cancel

        public static RequestResult Cancel(object Player, string Key,
                out string RealKey, out object Receiver) =>
            RequestCollections[Player].Cancel(Key, out RealKey, out Receiver);

        #endregion
    }
}