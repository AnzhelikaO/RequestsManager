#region Using
using RequestsManagerAPI;
using System.Collections.Generic;
using System.Linq;
using TShockAPI;
#endregion
namespace RequestsManagerPlugin
{
    public class RequestCommands
    {
        private static Command[] Commands;
        internal static void Register()
        {
            Commands = new Command[]
            {
                new Command(Accept, "accept", "+")
                {
                    AllowServer = false,
                    HelpText = $"{TShock.Config.Settings.CommandSpecifier}+ [request] [player]"
                },
                new Command(Refuse, "refuse", "-")
                {
                    AllowServer = false,
                    HelpText = $"{TShock.Config.Settings.CommandSpecifier}- [request] [player]"
                },
                new Command(Cancel, "cancel")
                {
                    AllowServer = false,
                    HelpText = $"{TShock.Config.Settings.CommandSpecifier}cancel [request] [player]"
                },
                new Command(Block, "block")
                {
                    AllowServer = false,
                    HelpText = $"{TShock.Config.Settings.CommandSpecifier}block <request> <player>"
                }
            };
            TShockAPI.Commands.ChatCommands.AddRange(Commands);
        }
        internal static void Deregister()
        {
            foreach (Command command in Commands)
                TShockAPI.Commands.ChatCommands.Remove(command);
        }

        private static void Accept(CommandArgs Args) =>
            AcceptRefuse(Args, Decision.Accepted);

        private static void Refuse(CommandArgs Args) =>
            AcceptRefuse(Args, Decision.Refused);

        private static void Cancel(CommandArgs Args)
        {
            if (!GetKeyAndPlayer(Args, out string key, out TSPlayer receiver))
                return;

            RequestResult result = RequestsManager.SenderCancelled(Args.Player,
                key, receiver, out string realKey, out object realReceiver);
            string name = (receiver?.Name ?? ((TSPlayer)realReceiver)?.Name);
            switch (result)
            {
                case RequestResult.NoRequests:
                    Args.Player.SendErrorMessage("You do not have any active " +
                        ((realKey == null) ? "requests" : $"{realKey} requests") +
                        ((name == null) ? "." : $" to player {name}."));
                    break;
                case RequestResult.NotSpecifiedRequest:
                    Args.Player.SendErrorMessage($"You have more than one active " +
                        ((realKey == null) ? "request" : $"{realKey} request") +
                        ((name == null) ? "." : $" to player {name}."));
                    break;
                case RequestResult.InvalidRequest:
                    Args.Player.SendErrorMessage($"Invalid request name '{realKey}'" +
                        ((name == null) ? "." : $" or player name '{name}'."));
                    break;
            }
        }

        private static void Block(CommandArgs args)
        {
            if (args.Parameters.Count < 2)
            {
                args.Player.SendErrorMessage($"{TShock.Config.Settings.CommandSpecifier}block <request> <player>");
                return;
            }

            if (!GetKeyAndPlayer(args, out string key, out TSPlayer player))
                return;

            if (!RequestsManager.RequestKeys.Contains(key))
            {
                args.Player.SendErrorMessage($"Invalid request type '{key}'.");
                return;
            }

            if (!RequestsManager.CanBlockFunc(args.Player, player))
            {
                args.Player.SendErrorMessage($"You cannot block {player.Name}.");
                return;
            }

            if (RequestsManager.IsBlocked(args.Player, key, player))
            {
                RequestsManager.Block(args.Player, key, player, false);
                args.Player.SendSuccessMessage($"Allowed {player.Name} to send you {key} requests.");
            }
            else
            {
                RequestsManager.Block(args.Player, key, player, true);
                args.Player.SendSuccessMessage($"Forbade {player.Name} to send you {key} requests.");
            }
        }

        private static void AcceptRefuse(CommandArgs Args, Decision Decision)
        {
            if (!GetKeyAndPlayer(Args, out string key, out TSPlayer sender))
                return;

            RequestResult result = RequestsManager.SetDecision(Args.Player, key,
                sender, Decision, out string realKey, out object realSender);
            string name = (sender?.Name ?? ((realSender is TSPlayer plr) ? plr.Name : null));
            switch (result)
            {
                case RequestResult.NoRequests:
                    Args.Player.SendErrorMessage("You do not have any active " +
                        ((realKey == null) ? "requests" : $"{realKey} requests") +
                        ((name == null) ? "." : $" from player {name}."));
                    break;
                case RequestResult.NotSpecifiedRequest:
                    Args.Player.SendErrorMessage($"You have more than one active " +
                        ((realKey == null) ? "request" : $"{realKey} request"));
                    break;
                case RequestResult.InvalidRequest:
                    Args.Player.SendErrorMessage($"Invalid request name '{realKey}'" +
                        ((name == null) ? "." : $" or player name '{name}'."));
                    break;
            }
        }

        private static bool GetKeyAndPlayer(CommandArgs Args, out string Key, out TSPlayer Player)
        {
            Key = Args.Parameters.ElementAtOrDefault(0);
            Player = null;
            if (Args.Parameters.Count < 2)
                return true;

            string playerName = string.Join(" ", Args.Parameters.Skip(1));
            List<TSPlayer> players = TSPlayer.FindByNameOrID(playerName);
            if (players.Count == 0)
                Args.Player.SendErrorMessage($"Invalid player '{playerName}'.");
            else if (players.Count > 1)
                Args.Player.SendMultipleMatchError(players.Select(p => p.Name));
            else
                Player = players[0];
            return (Player != null);
        }
    }
}