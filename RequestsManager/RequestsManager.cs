#region Using
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
#endregion
namespace RequestsManagerAPI
{
    public delegate void SendMessage(object Player, string Text, byte R, byte G, byte B);

    public static class RequestsManager
    {
        public static SendMessage SendMessage;
        public static Func<object, string> GetPlayerNameFunc { get; private set; }
        public static Func<object, object, bool> CanBlockFunc { get; private set; }
        internal static string CommandSpecifier;
        internal static ConcurrentDictionary<object, RequestCollection> RequestCollections =
            new ConcurrentDictionary<object, RequestCollection>();
        internal static object EmptySender = new object();
        private static Timer AnnouceTimer = new Timer(2500) { AutoReset = true };
        public static string[] RequestKeys => RequestCollection.RequestConfigurations.Keys.ToArray();
        
        #region Initialize

        public static void Initialize(Func<object, string> GetPlayerNameFunc,
            Func<object, object, bool> CanBlockFunc, string CommandSpecifier)
        {
            RequestsManager.GetPlayerNameFunc = GetPlayerNameFunc;
            RequestsManager.CanBlockFunc = CanBlockFunc;
            RequestsManager.CommandSpecifier = CommandSpecifier;
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
                collection.SetGlobalDecision(Decision.Disposed, Decision.Disposed);
            }
        }

        #endregion

        #region AddConfiguration

        public static void AddConfiguration(string Key, RequestConfiguration Configuration)
        {
            if (Key is null)
                throw new ArgumentNullException(nameof(Key));
            if (Configuration is null)
                throw new ArgumentNullException(nameof(Configuration));

            RequestCollection.RequestConfigurations[Key.ToLower()] = Configuration;
        }

        #endregion
        #region RemoveConfiguration

        public static void RemoveConfiguration(string Key)
        {
            if (Key is null)
                throw new ArgumentNullException(nameof(Key));

            RequestCollection.RequestConfigurations.TryRemove(Key.ToLower(), out _);
        }

        #endregion

        #region PlayerJoined

        public static void PlayerJoined(object Player) =>
            RequestCollections[Player] = new RequestCollection(Player);

        #endregion
        #region PlayerLeft

        public static void PlayerLeft(object Player)
        {
            if (RequestCollections.TryRemove(Player, out RequestCollection collection))
                collection.SetGlobalDecision(Decision.ReceiverLeft, Decision.SenderLeft);
            foreach (var pair1 in RequestCollections)
                foreach (var pair2 in pair1.Value.Block)
                    pair2.Value.TryRemove(Player, out _);
        }

        #endregion

        #region GetRequestConditions

        public static IEnumerable<ICondition> GetRequestConditions(object Player) =>
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

        #region SendMessage

        internal static void TrySendMessage(object Player, string Text, byte R, byte G, byte B)
        {
            if (!Player.Equals(EmptySender))
                SendMessage?.Invoke(Player, Text, R, G, B);
        }

        #endregion

        #region GetDecision

        public static async Task<(Decision Decision, ICondition BrokenCondition)> GetDecision(object Player,
                object Sender, string Key, string AnnounceText, ICondition[] SenderConditions = null,
                ICondition[] ReceiverConditions = null, string DecisionCommandMessage = null) =>
            await RequestCollections[Player].GetDecision(Key, Sender, AnnounceText,
                SenderConditions, ReceiverConditions, DecisionCommandMessage);

        public static async Task<(Decision Decision, ICondition BrokenCondition)> GetDecision(object Player,
                string Key, string AnnounceText, ICondition[] SenderConditions = null,
                ICondition[] ReceiverConditions = null, string DecisionCommandMessage = null) =>
            await RequestCollections[Player].GetDecision(Key, EmptySender, AnnounceText,
                SenderConditions, ReceiverConditions, DecisionCommandMessage);

        #endregion
        #region SetDecision

        public static RequestResult SetDecision(object Player, string Key,
                object Sender, Decision Decision, out string RealKey, out object RealSender) =>
            RequestCollections[Player].SetDecision(Key, Sender, Decision, out RealKey, out RealSender);

        #endregion
        #region SenderCancelled

        public static RequestResult SenderCancelled(object Player, string Key,
                object Receiver, out string RealKey, out object RealReceiver) =>
            RequestCollections[Player].SenderCancelled(Key, Receiver, out RealKey, out RealReceiver);

        #endregion

        #region IsBlocked

        public static bool IsBlocked(object Blocker, string Key, object Blocked) =>
            (RequestCollections.TryGetValue(Blocker, out RequestCollection collection)
            && collection.Block.TryGetValue(Key, out var block)
            && block.TryGetValue(Blocked, out _));

        #endregion
        #region Block

        public static bool Block(object Blocker, string Key, object Blocked, bool Block)
        {
            if (!RequestCollections.TryGetValue(Blocker, out RequestCollection collection))
                return false;

            if (Block)
            {
                collection.Block.TryAdd(Key, new ConcurrentDictionary<object, byte>());
                return collection.Block[Key].TryAdd(Blocked, 0);
            }
            else if (!collection.Block.TryGetValue(Key, out var block))
                return false;
            else
                return block.TryRemove(Blocked, out _);
        }
        
        #endregion
    }
}