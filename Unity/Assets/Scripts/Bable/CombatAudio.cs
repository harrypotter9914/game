using System.Collections.Generic;
using UnityEngine;
using Babel.Runtime.Characters.Enemies;
namespace Bable
{
    public static class CombatAudio
    {
        static readonly Dictionary<string,AudioClip> clips=new Dictionary<string,AudioClip>();
        static readonly Dictionary<string,float> deadlines=new Dictionary<string,float>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset(){clips.Clear();deadlines.Clear();PlayedCount=0;LastCue=null;variant=0;foregroundUntil=0;}
        static float foregroundUntil;
        public static float MovementMix=>Time.time<foregroundUntil?.4f:1f;
        static int variant;
        public static AudioClip Load(string cue){AudioClip clip;if(!clips.TryGetValue(cue,out clip)){clip=Resources.Load<AudioClip>("Bable/Sfx49/"+cue)??Resources.Load<AudioClip>("Bable/Sfx48/"+cue)??Resources.Load<AudioClip>("Bable/Sfx/"+cue);clips[cue]=clip;}return clip;}
        public static void Movement(string cue,Vector3 position,float volume,float radius){var camera=Camera.main;if(camera==null)return;float gain=Mathf.Clamp01((radius-Vector2.Distance(camera.transform.position,position))/(radius*.65f));if(gain<=0)return;Play(cue,position,volume*gain*MovementMix);}
        public static void UI(string cue,float gain=.65f){float ready;if(deadlines.TryGetValue("ui/"+cue,out ready)&&Time.unscaledTime<ready)return;deadlines["ui/"+cue]=Time.unscaledTime+.06f;var clip=Load(cue);if(clip==null)return;BableAudio.PlayUI(clip,gain);LastCue=cue;PlayedCount++;}
        public static string LastCue {get;private set;}
        public static int PlayedCount {get;private set;}
        public static void Play(string cue,Vector3 position,float volume=1f)
        {
            if(Babel.Runtime.Core.GameSession.Instance!=null&&Babel.Runtime.Core.GameSession.Instance.IsPaused)return;
            var camera=Camera.main;if(camera==null)return;
            float distance=Vector2.Distance(camera.transform.position,position);
            float gain=Mathf.Clamp01((32-distance)/20f);if(gain<=0)return;
            float ready;if(deadlines.TryGetValue(cue,out ready)&&Time.time<ready)return;
            deadlines[cue]=Time.time+(cue=="beam_contact"?.2f:cue=="stone_break"?.12f:.075f);
            AudioClip clip=Load(cue);
            if(clip==null){Debug.LogWarning("Missing sound cue: "+cue);return;}
            if(!cue.StartsWith("step_")&&!cue.StartsWith("land_")&&cue!="land"&&cue!="jump")foregroundUntil=Time.time+.35f;
            BableAudio.PlayEffect(clip,volume*gain);LastCue=cue;PlayedCount++;
        }
        public static string Actor(GameObject actor)
        {
            var boss=actor.GetComponent<BossBrain>();if(boss!=null&&boss.profile!=null)return "boss"+((int)boss.profile.kind+1);
            if(actor.GetComponent<Babel.Runtime.Characters.Player.PlayerController2D>()!=null)return "player";
            if(actor.GetComponent<ShieldSentinelController>()!=null)return "shield";
            if(actor.GetComponent<RangedEnemyController>()!=null)return "ranged";
            if(actor.GetComponent<GiantEnemyController>()!=null)return "giant";
            return "melee";
        }
        public static void Hurt(GameObject actor){Play(Actor(actor)+"_hurt_"+(variant++%3),actor.transform.position,.9f);}
        public static void Boss(GameObject actor,string action,float gain=.8f){Play(Actor(actor)+"_"+action,actor.transform.position,gain);}
        public static void Attack(GameObject actor){string who=Actor(actor);if(who.StartsWith("boss")){Boss(actor,"swing");return;}Play(who=="melee"?"sword":who=="ranged"?"arrow":who+"_attack",actor.transform.position,.8f);}
    }
}
