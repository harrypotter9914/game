using UnityEngine;
using Babel.Runtime.Core;
namespace Bable {
    public sealed class CombatSoundLoop:MonoBehaviour {
        AudioSource source;float gain,radius=32;bool paused,movement;
        public static CombatSoundLoop Begin(Transform owner,string cue,float volume,float radius=32,bool movement=false){
            var clip=CombatAudio.Load(cue);if(clip==null)return null;
            var go=new GameObject(cue+" audio loop");go.transform.SetParent(owner,false);var loop=go.AddComponent<CombatSoundLoop>();
            loop.source=go.AddComponent<AudioSource>();loop.source.clip=clip;loop.source.loop=true;loop.source.playOnAwake=false;loop.gain=volume;loop.radius=radius;loop.movement=movement;
            loop.source.volume=0;loop.source.Play();return loop;
        }
        void Update(){bool p=Time.timeScale==0||GameSession.Instance!=null&&GameSession.Instance.IsPaused;
            if(p!=paused){if(p)source.Pause();else source.UnPause();paused=p;}
            var camera=Camera.main;float attenuation=camera==null?0:Mathf.Clamp01((radius-Vector2.Distance(camera.transform.position,transform.position))/(radius*.625f));if(movement)attenuation*=CombatAudio.MovementMix;
            source.volume=Mathf.MoveTowards(source.volume,gain*BableAudio.MasterVolume*GameSettings.Current.effects*attenuation,Time.unscaledDeltaTime*3);
        }
        public void Stop(){if(source!=null)source.Stop();Destroy(gameObject);}
        void OnDisable(){if(source!=null)source.Stop();}
    }
}
