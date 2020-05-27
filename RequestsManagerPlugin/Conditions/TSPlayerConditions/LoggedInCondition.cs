using TShockAPI;
namespace RequestsManagerPlugin
{
    public sealed class LoggedInCondition : TSPlayerCondition<bool>
    {
        public LoggedInCondition(bool LoggedIn) : base(LoggedIn) { }

        protected override bool Broke(TSPlayer Player) =>
            (Value ? (!Player.IsLoggedIn || Player.Account == null)
                   : (Player.IsLoggedIn && Player.Account != null));
    }
}