using UnityEngine;
namespace Bable
{
    public sealed class BossRoomBoundary:MonoBehaviour
    {
        public Rect area;
        public bool Contains(Vector2 point)=>area.Contains(point);
        void OnDrawGizmosSelected(){Gizmos.color=Color.red;Gizmos.DrawWireCube(area.center,area.size);}
    }
}
