#region Using
using RequestsManagerAPI;
using System.Linq;
using TShockAPI;
#endregion
namespace RequestsManagerPlugin
{
    internal class RequestCommands
    {
        public static Command[] Commands;

        public static void Register()
        {
            Commands = new Command[]
            {
                new Command(Accept, "accept", "+"),
                new Command(Refuse, "refuse", "-"),
            };
            TShockAPI.Commands.ChatCommands.AddRange(Commands);
        }
        public static void Deregister()
        {
            foreach (Command command in Commands)
                TShockAPI.Commands.ChatCommands.Remove(command);
        }

        public static void Accept(CommandArgs Args) =>
            AcceptRefuse(Args, Decision.Accepted);

        public static void Refuse(CommandArgs Args) =>
            AcceptRefuse(Args, Decision.Refused);

        private static void AcceptRefuse(CommandArgs Args, Decision Decision)
        {
            switch (RequestsManager.SetDecision(Args.Player,
                Args.Parameters.ElementAtOrDefault(0), Decision))
            {
                case SetDecisionResult.Success:
                    Args.Player.SendSuccessMessage($"{Decision} request.");
                    break;
                case SetDecisionResult.NoRequests:
                    Args.Player.SendSuccessMessage("You do not have any active requests.");
                    break;
                case SetDecisionResult.NotSpecifiedRequest:
                    Args.Player.SendSuccessMessage("You have more than one active request.");
                    break;
                case SetDecisionResult.InvalidRequest:
                    Args.Player.SendSuccessMessage("Invalid request name.");
                    break;
            }
        }
    }
}