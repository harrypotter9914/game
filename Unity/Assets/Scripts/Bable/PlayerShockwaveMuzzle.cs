using UnityEngine;
using Babel.Runtime.Characters.Player;

namespace Bable
{
    public static class PlayerShockwaveMuzzle
    {
        public static Vector2 Resolve(PlayerCombatController combat)
        {
            var sr=combat.GetComponent<CharacterPresentation>()?.animator?.GetComponent<SpriteRenderer>();
            if(sr==null||sr.sprite==null)return combat.transform.position;
            // Measured on the existing extended-palm release frame and the whole-body run sheet.
            Vector2 socket=sr.sprite.name.StartsWith("Pilgrim_run_")?new Vector2(.572f,.35f):new Vector2(.956f,.79f);
            var bounds=sr.sprite.bounds;
            Vector3 local=bounds.min+Vector3.Scale(bounds.size,new Vector3(socket.x,socket.y,0));
            if(sr.flipX)local.x=-local.x;
            return (Vector2)sr.transform.TransformPoint(local)+combat.LastShockwaveAxis*.055f;
        }
    }
}
