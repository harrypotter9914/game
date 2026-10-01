using UnityEngine;
using Babel.Runtime.Combat;
using Babel.Runtime.Characters.Player;
namespace Bable
{
    public sealed class BossEncounter : MonoBehaviour
    {
        public bool final;
        public string title;
        HealthComponent health;
        public static BossEncounter Active { get; private set; }
        public HealthComponent Health => health;
        void Awake() { health = GetComponent<HealthComponent>(); health.Died += Died; }
        void Start(){var brain=GetComponent<BossBrain>();if(brain!=null&&brain.profile!=null)title=TowerLore.GuardianTitles[(int)brain.profile.kind];}
        void Update()
        {
            var p = FindFirstObjectByType<PlayerController2D>();
            var brain=GetComponent<BossBrain>();
            if (p != null && (brain!=null?brain.Engaged:Vector2.Distance(p.transform.position, transform.position) < 13) && health.CurrentHealth > 0) { Active = this; BableAudio.Music("boss"); }
            else if (Active == this) { Active = null; BableAudio.Music("tower"); }
        }
        void Died() {
            if (Active == this) Active = null;
            var brain=GetComponent<BossBrain>();
            var session=Babel.Runtime.Core.GameSession.Instance;
            if(brain!=null&&brain.profile!=null&&session!=null&&session.GrantBossMana(brain.profile.kind.ToString()))
                NarrativeGuidance.Instance?.Teach("spirit_"+brain.profile.kind,"SPIRIT DEEPENS","A guardian falls. Your maximum Spirit increases by one.","Maximum Spirit: "+session.MaxMana);
            if (final) {var princess=FindFirstObjectByType<PrincessRescue>();if(princess!=null)princess.Unlock();else BableGameUI.Instance?.Victory();} else BableAudio.Music("tower");
        }
        void OnDestroy() { if (health != null) health.Died -= Died; if (Active == this) Active = null; }
    }
}
