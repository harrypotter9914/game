using UnityEngine;

namespace Bable
{
    // Includes the whole 1.5-unit coin sprite and its vertical idle bob.
    public static class TreasureClearance
    {
        public static readonly Vector2 Size = new Vector2(1.60f, 1.72f);

        public static bool Free(Vector2 center)
        {
            foreach (var collider in Physics2D.OverlapBoxAll(center, Size, 0))
                if (TerrainMotion.Solid(collider)) return false;
            return true;
        }

        public static Vector2 Move(Vector2 from, Vector2 toward, float maximumDistance)
        {
            Vector2 delta = toward - from;
            float distance = Mathf.Min(delta.magnitude, maximumDistance);
            if (distance <= 0 || !Free(from)) return from;
            foreach (var hit in Physics2D.BoxCastAll(from, Size, 0, delta.normalized, distance + .02f))
                if (TerrainMotion.Solid(hit.collider)) distance = Mathf.Min(distance, Mathf.Max(0, hit.distance - .02f));
            return from + delta.normalized * distance;
        }

        public static bool FindDrop(Vector2 origin, out Vector2 position)
        {
            // Search the victim's immediate air space, never another side of a wall.
            for (int row = 0; row <= 8; row++)
                for (int column = 0; column < 3; column++)
                {
                    position = origin + new Vector2(column == 0 ? 0 : column == 1 ? -.5f : .5f, row * .25f);
                    if (Free(position) && CombatGeometry.Clear(origin, position)) return true;
                }
            position = origin;
            return false;
        }
    }
}
