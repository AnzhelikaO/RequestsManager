#region Using
using RequestsManagerAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            Commands.ChatCommands.Add(new Command("rmtp", (async args =>
            {
                if (args.Parameters.Count != 1)
                {
                    args.Player.SendErrorMessage("/rmtp <player>");
                    return;
                }

                string name = args.Parameters[0];
                List<TSPlayer> plrs = TShock.Utils.FindPlayer(name);
                if (plrs.Count == 0)
                    args.Player.SendErrorMessage($"Invalid player '{name}'.");
                else if (plrs.Count > 1)
                    TShock.Utils.SendMultipleMatchError(args.Player, plrs.Select(p => p.Name));
                else
                {
                    TSPlayer player = plrs[0];
                    (Decision Decision, ICondition BrokenCondition) =
                        await RequestsManager.GetDecision(player, args.Player, "tp",
                            $"{args.Player.Name} requested teleportation. " +
                            RequestCommands.AcceptRefuseMessage("tp", args.Player),
                            (s, r, d) =>
                            {
                                if (d == Decision.Accepted)
                                {
                                    TSPlayer sender = (TSPlayer)s, receiver = (TSPlayer)r;
                                    sender.Teleport(receiver.X, receiver.Y);
                                }
                            });
                }
            }), "rmtp"));

            /*
            Commands.ChatCommands.Add(new Command("rmtzt", (async args =>
            {
                _ = System.Threading.Tasks.Task.Delay(2000).ContinueWith(_ =>
                    RequestsManager.SetDecision(args.Player, "tzt", Decision.Accepted));
                Console.WriteLine((await RequestsManager.GetDecision(args.Player, "tzt")).Decision);
            }), "rmtzt"));
            */
            RequestCommands.Register();
            RequestsManager.SendMessage += OnSendMessage;
            RequestsManager.Initialize(o => ((o is TSPlayer player) ? player?.Name : null));
            ServerApi.Hooks.ServerJoin.Register(this, OnServerJoin, int.MinValue);
            ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
            return;
            GetDataHandlers.KillMe.Register(OnKillMe);
            GetDataHandlers.PlayerUpdate.Register(OnPlayerUpdate);
            GetDataHandlers.PlayerHP.Register(OnPlayerHP);
            GetDataHandlers.PlayerSlot.Register(OnPlayerSlot);
            PlayerHooks.PlayerPostLogin += OnPlayerPostLogin;
            PlayerHooks.PlayerLogout += OnPlayerLogout;
            GetDataHandlers.PlayerMana.Register(OnPlayerMana);
            GetDataHandlers.TogglePvp.Register(OnTogglePvp);
            GetDataHandlers.PlayerTeam.Register(OnPlayerTeam);
            ServerApi.Hooks.NetSendData.Register(this, OnSendData, int.MinValue);
        }

        #endregion
        #region Dispose

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                RequestCommands.Deregister();
                RequestsManager.SendMessage += OnSendMessage;
                RequestsManager.Dispose();
                ServerApi.Hooks.ServerJoin.Deregister(this, OnServerJoin);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnServerLeave);
                GetDataHandlers.KillMe.UnRegister(OnKillMe);
                GetDataHandlers.PlayerUpdate.UnRegister(OnPlayerUpdate);
                GetDataHandlers.PlayerHP.UnRegister(OnPlayerHP);
                GetDataHandlers.PlayerSlot.UnRegister(OnPlayerSlot);
                PlayerHooks.PlayerPostLogin -= OnPlayerPostLogin;
                PlayerHooks.PlayerLogout -= OnPlayerLogout;
                GetDataHandlers.PlayerMana.UnRegister(OnPlayerMana);
                GetDataHandlers.TogglePvp.UnRegister(OnTogglePvp);
                GetDataHandlers.PlayerTeam.UnRegister(OnPlayerTeam);
                ServerApi.Hooks.NetSendData.Deregister(this, OnSendData);
            }
            base.Dispose(disposing);
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
        private void OnServerLeave(LeaveEventArgs args) =>
            RequestsManager.PlayerLeft(TShock.Players[args.Who]);

        #endregion
        #region OnKillMe

        private void OnKillMe(object sender, GetDataHandlers.KillMeEventArgs e)
        {
            TSPlayer player = TShock.Players[e.PlayerId];
            RequestsManager.GetRequestConditions(player)
                           .FirstOrDefault(c => c is AliveCondition)?
                           .TryToBreak(player);
        }

        #endregion
        #region OnPlayerUpdate

        private void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs e)
        {
            TSPlayer player = TShock.Players[e.PlayerId];
            RequestsManager.GetRequestConditions(player)
                           .FirstOrDefault(c => c is AreaCondition)?
                           .TryToBreak(player);
        }

        #endregion
        #region OnPlayerHP

        private void OnPlayerHP(object sender, GetDataHandlers.PlayerHPEventArgs e)
        {
            TSPlayer player = TShock.Players[e.PlayerId];
            RequestsManager.GetRequestConditions(player)
                           .FirstOrDefault(c => c is HPCondition)?
                           .TryToBreak(player);
        }

        #endregion
        #region OnPlayerSlot

        private void OnPlayerSlot(object sender, GetDataHandlers.PlayerSlotEventArgs e)
        {
            TSPlayer player = TShock.Players[e.PlayerId];
            RequestsManager.GetRequestConditions(player)
                           .FirstOrDefault(c => c is ItemCondition)?
                           .TryToBreak(player);
        }

        #endregion
        #region OnPlayerPostLogin

        private void OnPlayerPostLogin(PlayerPostLoginEventArgs e)
        {
            TSPlayer player = e.Player;
            RequestsManager.GetRequestConditions(player)
                           .FirstOrDefault(c => c is LoggedInCondition)?
                           .TryToBreak(player);
        }

        #endregion
        #region OnPlayerLogout

        private void OnPlayerLogout(PlayerLogoutEventArgs e)
        {
            TSPlayer player = e.Player;
            RequestsManager.GetRequestConditions(player)
                           .FirstOrDefault(c => c is LoggedInCondition)?
                           .TryToBreak(player);
        }

        #endregion
        #region OnPlayerMana

        private void OnPlayerMana(object sender, GetDataHandlers.PlayerManaEventArgs e)
        {
            TSPlayer player = TShock.Players[e.PlayerId];
            RequestsManager.GetRequestConditions(player)
                           .FirstOrDefault(c => c is ManaCondition)?
                           .TryToBreak(player);
        }

        #endregion
        #region OnTogglePvp

        private void OnTogglePvp(object sender, GetDataHandlers.TogglePvpEventArgs e)
        {
            TSPlayer player = TShock.Players[e.PlayerId];
            RequestsManager.GetRequestConditions(player)
                           .FirstOrDefault(c => c is PvPCondition)?
                           .TryToBreak(player);
        }

        #endregion
        #region OnPlayerTeam

        private void OnPlayerTeam(object sender, GetDataHandlers.PlayerTeamEventArgs e)
        {
            TSPlayer player = TShock.Players[e.PlayerId];
            RequestsManager.GetRequestConditions(player)
                           .FirstOrDefault(c => c is TeamCondition)?
                           .TryToBreak(player);
        }

        #endregion
        #region OnSendData

        private void OnSendData(SendDataEventArgs args)
        {

        }

        #endregion
    }
}