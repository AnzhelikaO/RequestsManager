using TShockAPI;
namespace RequestsManagerPlugin
{
    public sealed class AliveCondition : TSPlayerCondition<bool>
    {
        public AliveCondition(bool Alive) : base(Alive) { }

        protected override bool Broke(TSPlayer Player) =>
            (Value ? (!Player.Dead && (Player.TPlayer.statLife > 0))
                   : (Player.Dead || (Player.TPlayer.statLife == 0)));
    }
}