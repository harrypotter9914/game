using UnityEngine;
namespace Bable {
    public static class CombatGeometry {
        public static float Reach(Vector2 origin, Vector2 direction, float range) {
            float result=range;
            foreach(var hit in Physics2D.RaycastAll(origin,direction,range))
                if(TerrainMotion.Solid(hit.collider))result=Mathf.Min(result,Mathf.Max(0,hit.distance-.025f));
            return result;
        }
        public static bool Clear(Vector2 from,Vector2 to) {
            var delta=to-from;
            return delta.sqrMagnitude<.0001f || Reach(from,delta.normalized,delta.magnitude)>=delta.magnitude-.03f;
        }
    }
}
