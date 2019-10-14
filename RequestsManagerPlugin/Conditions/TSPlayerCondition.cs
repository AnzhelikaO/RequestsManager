#region Using
using RequestsManagerAPI;
using TShockAPI;
#endregion
namespace RequestsManagerPlugin
{
    public abstract class TSPlayerCondition<T> : Condition<T>
    {
        public TSPlayerCondition(T Value) : base(Value) { }

        protected sealed override bool InvalidPlayer(object Player)
        {
            if (Player is TSPlayer player)
                return !player.Active;
            else
                return true;
        }

        protected sealed override bool Broke(object Player) =>
            Broke(Player as TSPlayer);
        protected abstract bool Broke(TSPlayer Player);
    }
}