using UnityEngine;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Core;
namespace Bable
{
    public sealed class AbilityGate : MonoBehaviour
    {
        public AbilityId required;
        public bool shockwaveOnly;
        public bool Open { get; private set; }
        public void Strike(Vector2 center, Vector2 size)
        {
            if (required != AbilityId.Shockwave || GameSession.Instance == null || !GameSession.Instance.HasAbility(required)) return;
            if (GetComponent<Collider2D>().bounds.Intersects(new Bounds(center, size))) Unlock();
        }
        void Update() { if (!shockwaveOnly && GameSession.Instance != null && GameSession.Instance.HasAbility(required)) Unlock(); }
        public void RestoreOpen(){Open=true;gameObject.SetActive(false);}
        void Unlock() { if(Open)return;CampaignStore.MarkRemoved(CampaignStore.Identity(this)); Open = true; CombatAudio.Play("stone_break",transform.position,.8f); gameObject.SetActive(false); }
    }
}
