using UnityEngine;

namespace Babel.Runtime.Combat
{
    public readonly struct DamageInfo
    {
        public DamageInfo(int amount, Vector2 point, Vector2 force, GameObject source, TeamAlignment sourceTeam)
        {
            Amount = amount;
            Point = point;
            Force = force;
            Source = source;
            SourceTeam = sourceTeam;
        }

        public int Amount { get; }
        public Vector2 Point { get; }
        public Vector2 Force { get; }
        public GameObject Source { get; }
        public TeamAlignment SourceTeam { get; }
    }
}
