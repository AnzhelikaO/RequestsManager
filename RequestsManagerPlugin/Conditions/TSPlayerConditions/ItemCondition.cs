#region Using
using System;
using System.Linq;
using Terraria;
using TShockAPI;
#endregion
namespace RequestsManagerPlugin
{
    public sealed class ItemCondition : TSPlayerCondition<(bool Has, int ItemID, Item[][] In)>
    {
        public ItemCondition(bool Has, int ItemID, Item[][] In) : base((Has, ItemID, In))
        {
            if ((ItemID <= 0) || (ItemID > Terraria.ID.ItemID.Count))
                throw new ArgumentOutOfRangeException(nameof(ItemID));
            if ((In is null) || In.Any(i => (i is null)))
                throw new ArgumentNullException(nameof(In));
            if (In.Sum(i => i.Length) == 0)
                throw new Exception("Items array is empty.");
        }

        protected override bool Broke(TSPlayer Player)
        {
            int id = Value.ItemID;
            foreach (Item[] items in Value.In)
                foreach (Item item in items)
                    if (item?.netID == id)
                        return Value.Has;
            return !Value.Has;
        }
    }
}