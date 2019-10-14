#region Using
using System;
using TShockAPI;
#endregion
namespace RequestsManagerPlugin
{
    public sealed class ManaCondition : TSPlayerCondition<(int Mana, bool Greater, bool OrEquals)>
    {
        public ManaCondition(int Mana, bool Greater, bool OrEquals) : base((Mana, Greater, OrEquals))
        {
            if ((Mana < 0) || (Mana > short.MaxValue))
                throw new ArgumentOutOfRangeException(nameof(Mana));
        }

        protected override bool Broke(TSPlayer Player) =>
            (Value.Greater
                ? (Value.OrEquals
                    ? (Player.TPlayer.statMana < Value.Mana)
                    : (Player.TPlayer.statMana <= Value.Mana))
                : (Value.OrEquals
                    ? (Player.TPlayer.statMana > Value.Mana)
                    : (Player.TPlayer.statMana >= Value.Mana)));
    }
}