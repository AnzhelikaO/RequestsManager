#region Using
using System;
using TShockAPI;
#endregion
namespace RequestsManagerPlugin
{
    public sealed class HPCondition : TSPlayerCondition<(int HP, bool Greater, bool OrEquals)>
    {
        public HPCondition(int HP, bool Greater, bool OrEquals) : base((HP, Greater, OrEquals))
        {
            if ((HP < 0) || (HP > short.MaxValue))
                throw new ArgumentOutOfRangeException(nameof(HP));
        }

        protected override bool Broke(TSPlayer Player) =>
            (Value.Greater
                ? (Value.OrEquals
                    ? (Player.TPlayer.statLife < Value.HP)
                    : (Player.TPlayer.statLife <= Value.HP))
                : (Value.OrEquals
                    ? (Player.TPlayer.statLife > Value.HP)
                    : (Player.TPlayer.statLife >= Value.HP)));
    }
}