#region Using
using System;
using System.Linq;
using TShockAPI;
#endregion
namespace RequestsManagerPlugin
{
    public sealed class TeamCondition : TSPlayerCondition<(int[] Teams, bool In)>
    {
        public TeamCondition(int[] Teams, bool In) : base((Teams, In))
        {
            if (Teams is null)
                throw new ArgumentNullException(nameof(Teams));
            if (Teams.Length == 0)
                throw new Exception("Teams array is empty.");
            foreach (int team in Teams)
                if ((team < 0) || (team >= 6))
                    throw new Exception($"Invalid team '{team}'.");
        }

        protected override bool Broke(TSPlayer Player) =>
            (Value.Teams.Contains(Player.Team) != Value.In);
    }
}