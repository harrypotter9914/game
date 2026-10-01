using UnityEngine;
namespace Bable
{
    public enum BossKind { Shockwave, Burrow, Aerial, Crystal, Nero }
    [CreateAssetMenu(menuName="Bable/Boss Profile")]
    public sealed class BossProfile : ScriptableObject
    {
        public BossKind kind;
        public string displayName;
        public int health=24;
        public float tileSize=2, speed=5, meleeRange=4;
        public float burrowTrackSeconds=3, emergeSeconds=.8f, recoverySeconds=1;
        public float waveWindup=1, heavyWindup=1.5f, quickWindup=.5f;
        public float flyWindup=.5f, fanWindup=2, fanCooldown=10, comboCooldown=5, dashWindup=1;
        [Range(.2f,.8f)] public float secondPhaseThreshold=.5f;
        public float phaseTwoSpeedMultiplier=1.3f;
    }
}
