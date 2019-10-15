#region Using
using RequestsManagerAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using TShockAPI;
#endregion
namespace RequestsManagerPlugin
{
    public class RequestCommands
    {
        public static string AcceptRefuseMessage(string Key, TSPlayer Sender)
        {
            string specifier = TShock.Config.CommandSpecifier;
            string key = ((Key.Contains(" ") ? $"\"{Key}\"" : Key));
            return $"Type '{specifier}+ {key} {Sender.Name}' " +
                     $"or '{specifier}- {key} {Sender.Name}' to accept or refuse request.";
        }

        private static Command[] Commands;
        internal static void Register()
        {
            Commands = new Command[]
            {
                new Command(Accept, "accept", "+"),
                new Command(Refuse, "refuse", "-"),
                new Command(Cancel, "cancel"),
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
            RequestResult result = RequestsManager.Cancel(Args.Player,
                Args.Parameters.ElementAtOrDefault(0), out string realKey, out object receiver);
            string name = ((TSPlayer)receiver).Name;
            switch (result)
            {
                case RequestResult.NoRequests:
                    Args.Player.SendErrorMessage($"You do not have any active requests to player {name}.");
                    break;
                case RequestResult.NotSpecifiedRequest:
                    Args.Player.SendErrorMessage($"You have more than one active request to player {name}.");
                    break;
                case RequestResult.InvalidRequest:
                    Args.Player.SendErrorMessage("Invalid request name " +
                        $"'{realKey}' or player name '{name}'.");
                    break;
            }
        }

        private static void AcceptRefuse(CommandArgs Args, Decision Decision)
        {
            string key = null;
            TSPlayer sender = null;
            if (Args.Parameters.Count > 0)
            {
                key = Args.Parameters[0];
                if (Args.Parameters.Count > 1)
                {
                    string playerName = string.Join(" ", Args.Parameters.Skip(1));
                    List<TSPlayer> players = TShock.Utils.FindPlayer(playerName);
                    if (players.Count == 0)
                    {
                        Args.Player.SendErrorMessage($"Invalid player '{playerName}'.");
                        return;
                    }
                    else if (players.Count > 1)
                    {
                        TShock.Utils.SendMultipleMatchError(Args.Player, players.Select(p => p.Name));
                        return;
                    }
                    sender = players[0];
                }
            }

            RequestResult result = RequestsManager.SetDecision(Args.Player, key,
                sender, Decision, out string realKey, out object realSender);
            string name = ((TSPlayer)realSender).Name;
            switch (result)
            {
                case RequestResult.NoRequests:
                    Args.Player.SendErrorMessage("You do not have " +
                        $"any active requests from player {sender.Name}.");
                    break;
                case RequestResult.NotSpecifiedRequest:
                    Args.Player.SendErrorMessage("You have more than one active request.");
                    break;
                case RequestResult.InvalidRequest:
                    Args.Player.SendErrorMessage("Invalid request name " +
                        $"'{realKey}' or player name '{realSender}'.");
                    break;
            }
        }
    }
}