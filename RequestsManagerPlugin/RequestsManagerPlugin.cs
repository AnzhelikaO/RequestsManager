#region Using
using RequestsManagerAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
#endregion
namespace RequestsManagerPlugin
{
    [ApiVersion(2, 1)]
    public class RequestsManagerPlugin : TerrariaPlugin
    {
        #region Description

        public override string Name => "RequestsManager";
        public override string Author => "Anzhelika";
        public override Version Version => Assembly.GetExecutingAssembly().GetName().Version;
        public override string Description => "API that manages requests with conditions.";

        public RequestsManagerPlugin(Main game) : base(game) { Order = 100; }

        #endregion

        #region Initialize

        public override void Initialize()
        {
            if (Environment.MachineName.ToLower() == "anzhelika-pc")
                TempDebug();

            RequestCommands.Register();
            RequestsManager.SendMessage = OnSendMessage;
            RequestsManager.Initialize(o => ((o is TSPlayer player) ? player?.Name : null),
                ((f, s) =>
                {
                    if ((f is TSPlayer blocker) && (s is TSPlayer blocked))
                        return (GetDynamicPermission(blocker) >= GetDynamicPermission(blocked));
                    else
                        return false;
                }),
                TShock.Config.Settings.CommandSpecifier);
            ServerApi.Hooks.ServerJoin.Register(this, OnServerJoin, int.MinValue);
            ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
            GetDataHandlers.KillMe += OnKillMe;
            GetDataHandlers.PlayerUpdate += OnPlayerUpdate;
            GetDataHandlers.PlayerHP += OnPlayerHP;
            GetDataHandlers.PlayerSlot += OnPlayerSlot;
            PlayerHooks.PlayerPostLogin += OnPlayerPostLogin;
            PlayerHooks.PlayerLogout += OnPlayerLogout;
            GetDataHandlers.PlayerMana += OnPlayerMana;
            GetDataHandlers.TogglePvp += OnTogglePvp;
            GetDataHandlers.PlayerTeam += OnPlayerTeam;
            ServerApi.Hooks.NetSendData.Register(this, OnSendData, int.MinValue);
        }

        #region TempDebug

        private void TempDebug()
        {
            RequestsManager.AddConfiguration("tp1", new RequestConfiguration(false, false, false, true, 15));
            RequestsManager.AddConfiguration("tp2", new RequestConfiguration(true, false, false, true, 15));
            RequestsManager.AddConfiguration("tp4", new RequestConfiguration(false, false, false, true, 15));
            RequestsManager.AddConfiguration("tp5", new RequestConfiguration(false, false, false, true, 6));
            RequestsManager.AddConfiguration("tp6", new RequestConfiguration(false, false, false, false, 15));
            #region GetPlayer

            bool GetPlayer(CommandArgs args, int num, out TSPlayer Player)
            {
                Player = null;
                if (args.Parameters.Count == 1)
                {
                    string name = args.Parameters[0];
                    List<TSPlayer> plrs = TSPlayer.FindByNameOrID(name);
                    if (plrs.Count == 0)
                        args.Player.SendErrorMessage($"Invalid player '{name}'.");
                    else if (plrs.Count > 1)
                        args.Player.SendMultipleMatchError(plrs.Select(p => p.Name));
                    else
                        Player = plrs[0];
                }
                else
                    args.Player.SendErrorMessage($"/tp{num} <player>");
                return (Player != null);
            }

            #endregion

            Commands.ChatCommands.Add(new Command("tp1", (async args =>
            {
                if (!GetPlayer(args, 1, out TSPlayer player))
                    return;

                (Decision Decision, ICondition BrokenCondition) =
                    await RequestsManager.GetDecision(player, args.Player, "tp1",
                    new Messages(null, null, new Dictionary<MessageType, Message>()
                    { [MessageType.AnnounceInbox] = new Message($"{args.Player} requested teleportation.") }));
                if (Decision == Decision.Accepted)
                    args.Player.Teleport(player.X, player.Y);
            }), "tp1"));
            Commands.ChatCommands.Add(new Command("tp2", (async args =>
            {
                if (!GetPlayer(args, 2, out TSPlayer player))
                    return;

                (Decision Decision, ICondition BrokenCondition) =
                    await RequestsManager.GetDecision(player, args.Player, "tp2",
                    new Messages(null, null, new Dictionary<MessageType, Message>()
                    { [MessageType.AnnounceInbox] = new Message($"{args.Player} requested teleportation.") }));
                if (Decision == Decision.Accepted)
                    args.Player.Teleport(player.X, player.Y);
            }), "tp2"));
            Commands.ChatCommands.Add(new Command("tp3", (async args =>
            {
                if (!GetPlayer(args, 3, out TSPlayer player))
                    return;

                (Decision Decision, ICondition BrokenCondition) =
                    await RequestsManager.GetDecision(player, args.Player, "tp2",
                    new Messages(null, null, new Dictionary<MessageType, Message>()
                    { [MessageType.AnnounceInbox] = new Message($"{args.Player} requested teleportation.") }),
                    new ICondition[] { new LoggedInCondition(true) });
                if (Decision == Decision.Accepted)
                    args.Player.Teleport(player.X, player.Y);
            }), "tp3"));
            Commands.ChatCommands.Add(new Command("tp4", (async args =>
            {
                (Decision decision, _) =
                    await RequestsManager.GetDecision(args.Player, "tp4",
                    new Messages(null, null, new Dictionary<MessageType, Message>()
                    {
                        [MessageType.AnnounceInbox] = new Message("OK_4?"),
                        [MessageType.DecisionCommand] = new Message("Type /+ or /-   !!!!!!!1111111111111111")
                    }));
                if (decision == Decision.Accepted)
                    args.Player.SendSuccessMessage("OK_4.");
            }), "tp4"));
            Commands.ChatCommands.Add(new Command("tp4", (async args =>
            {
                (Decision decision, _) =
                    await RequestsManager.GetDecision(args.Player, "tp5", new Messages());
                if (decision == Decision.Accepted)
                    args.Player.SendSuccessMessage("OK_5.");
            }), "tp5"));
            Commands.ChatCommands.Add(new Command("tp4", (async args =>
            {
                (Decision decision, _) =
                    await RequestsManager.GetDecision(args.Player, "tp6", new Messages());
                if (decision == Decision.Accepted)
                    args.Player.SendSuccessMessage("OK_6.");
            }), "tp6"));
        }

        #endregion

        #endregion
        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                RequestCommands.Deregister();
                RequestsManager.SendMessage -= OnSendMessage;
                RequestsManager.Dispose();
                ServerApi.Hooks.ServerJoin.Deregister(this, OnServerJoin);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnServerLeave);
                GetDataHandlers.KillMe -= OnKillMe;
                GetDataHandlers.PlayerUpdate -= OnPlayerUpdate;
                GetDataHandlers.PlayerHP -= OnPlayerHP;
                GetDataHandlers.PlayerSlot -= OnPlayerSlot;
                PlayerHooks.PlayerPostLogin -= OnPlayerPostLogin;
                PlayerHooks.PlayerLogout -= OnPlayerLogout;
                GetDataHandlers.PlayerMana -= OnPlayerMana;
                GetDataHandlers.TogglePvp -= OnTogglePvp;
                GetDataHandlers.PlayerTeam -= OnPlayerTeam;
                ServerApi.Hooks.NetSendData.Deregister(this, OnSendData);
            }
            base.Dispose(disposing);
        }

        #endregion

        #region GetDynamicPermission

        private static readonly Regex PermissionRegex =
            new Regex(@"^requests\.rank\.(?<rank>\d+|\*)$",
                (RegexOptions.IgnoreCase | RegexOptions.Compiled));
        private static int GetDynamicPermission(TSPlayer Player)
        {
            int max = 0;
            foreach (string permission in Player.Group.TotalPermissions)
            {
                Match match = PermissionRegex.Match(permission);
                if (match.Success)
                {
                    if (!int.TryParse(match.Groups["rank"].Value, out int rank))
                        rank = int.MaxValue;
                    max = Math.Max(max, rank);
                }
            }
            return max;
        }

        #endregion

        #region OnSendMessage

        private void OnSendMessage(object Player, string Text, byte R, byte G, byte B)
        {
            if (Player is TSPlayer player)
                player.SendMessage(Text, R, G, B);
        }

        #endregion
        #region OnServerJoin, OnServerLeave

        private void OnServerJoin(JoinEventArgs args) =>
            RequestsManager.PlayerJoined(TShock.Players[args.Who]);
        private void OnServerLeave(LeaveEventArgs args)
        {
            if (TShock.Players[args.Who] is TSPlayer player)
                RequestsManager.PlayerLeft(player);
        }

        #endregion
        #region OnKillMe

        private void OnKillMe(object sender, GetDataHandlers.KillMeEventArgs e)
        {
            TSPlayer player = TShock.Players[e.PlayerId];
            RequestsManager.GetRequestCondition<AliveCondition>(player)?.TryToBreak(player);
        }

        #endregion
        #region OnPlayerUpdate

        private void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs e)
        {
            TSPlayer player = TShock.Players[e.PlayerId];
            RequestsManager.GetRequestCondition<AreaCondition>(player)?.TryToBreak(player);
        }

        #endregion
        #region OnPlayerHP

        private void OnPlayerHP(object sender, GetDataHandlers.PlayerHPEventArgs e)
        {
            TSPlayer player = TShock.Players[e.PlayerId];
            RequestsManager.GetRequestCondition<HPCondition>(player)?.TryToBreak(player);
        }

        #endregion
        #region OnPlayerSlot

        private void OnPlayerSlot(object sender, GetDataHandlers.PlayerSlotEventArgs e)
        {
            TSPlayer player = TShock.Players[e.PlayerId];
            RequestsManager.GetRequestCondition<ItemCondition>(player)?.TryToBreak(player);
        }

        #endregion
        #region OnPlayerPostLogin

        private void OnPlayerPostLogin(PlayerPostLoginEventArgs e)
        {
            TSPlayer player = e.Player;
            RequestsManager.GetRequestCondition<LoggedInCondition>(player)?.TryToBreak(player);
        }

        #endregion
        #region OnPlayerLogout

        private void OnPlayerLogout(PlayerLogoutEventArgs e)
        {
            TSPlayer player = e.Player;
            RequestsManager.GetRequestCondition<LoggedInCondition>(player)?.TryToBreak(player);
        }

        #endregion
        #region OnPlayerMana

        private void OnPlayerMana(object sender, GetDataHandlers.PlayerManaEventArgs e)
        {
            TSPlayer player = TShock.Players[e.PlayerId];
            RequestsManager.GetRequestCondition<ManaCondition>(player)?.TryToBreak(player);
        }

        #endregion
        #region OnTogglePvp

        private void OnTogglePvp(object sender, GetDataHandlers.TogglePvpEventArgs e)
        {
            TSPlayer player = TShock.Players[e.PlayerId];
            RequestsManager.GetRequestCondition<PvPCondition>(player)?.TryToBreak(player);
        }

        #endregion
        #region OnPlayerTeam

        private void OnPlayerTeam(object sender, GetDataHandlers.PlayerTeamEventArgs e)
        {
            TSPlayer player = TShock.Players[e.PlayerId];
            RequestsManager.GetRequestCondition<TeamCondition>(player)?.TryToBreak(player);
        }

        #endregion
        #region OnSendData

        private void OnSendData(SendDataEventArgs args)
        {

        }

        #endregion
    }
}