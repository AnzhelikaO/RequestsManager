using TShockAPI;
namespace RequestsManagerPlugin
{
    public sealed class PvPCondition : TSPlayerCondition<bool>
    {
        public PvPCondition(bool PvP) : base(PvP) { }

        protected override bool Broke(TSPlayer Player) =>
            (Player.TPlayer.hostile != Value);
    }
}