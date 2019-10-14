#region Using
using Microsoft.Xna.Framework;
using System;
using TShockAPI;
#endregion
namespace RequestsManagerPlugin
{
    public sealed class AreaCondition : TSPlayerCondition<(Rectangle[] Areas, bool In)>
    {
        public AreaCondition(Rectangle[] Areas, bool In) : base((Areas, In))
        {
            if (Areas is null)
                throw new ArgumentNullException(nameof(Areas));
            if (Areas.Length == 0)
                throw new Exception("Areas array is empty.");
        }

        protected override bool Broke(TSPlayer Player)
        {
            int x = Player.TileX, y = Player.TileY;
            foreach (Rectangle area in Value.Areas)
                if (area.Contains(x, y))
                    return Value.In;
            return !Value.In;
        }
    }
}