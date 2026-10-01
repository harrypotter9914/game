using UnityEngine;
namespace Bable
{
    public sealed class BableAudio : MonoBehaviour
    {
        static BableAudio instance;
        AudioSource music, effects, ui;
        bool paused;
        string playing;
        public static float MasterVolume {get;private set;}=.7f;
        void Awake() { instance = this; music = gameObject.AddComponent<AudioSource>(); effects = gameObject.AddComponent<AudioSource>();ui=gameObject.AddComponent<AudioSource>(); music.loop = true; music.volume = .28f; effects.volume = ui.volume = MasterVolume*GameSettings.Current.effects; }
        public static void Music(string name)
        {
            if (instance == null || instance.playing == name) return;
            instance.playing = name;
            instance.music.clip = Resources.Load<AudioClip>("Bable/audio/" + name);
            instance.music.Play();
        }
        public static void Sfx(string name) { if (instance != null) { var clip = Resources.Load<AudioClip>("Bable/audio/" + name); if (clip != null) instance.effects.PlayOneShot(clip); } }
        public static void PlayEffect(AudioClip clip,float gain=1f){if(instance!=null&&clip!=null)instance.effects.PlayOneShot(clip,gain);}
        public static void PlayUI(AudioClip clip,float gain=1f){if(instance!=null&&clip!=null)instance.ui.PlayOneShot(clip,gain);}
        void Update(){effects.volume=ui.volume=MasterVolume*GameSettings.Current.effects;if(music!=null)music.volume=Mathf.Lerp(music.volume,MasterVolume*GameSettings.Current.music*(TowerDialogue.IsSpeaking||TowerPrologue.IsNarrating?.16f:.4f),Time.unscaledDeltaTime*6);bool p=Time.timeScale==0||Babel.Runtime.Core.GameSession.Instance!=null&&Babel.Runtime.Core.GameSession.Instance.IsPaused;if(p!=paused){if(p)effects.Pause();else effects.UnPause();paused=p;}}
        public static void Volume(float value) { MasterVolume=Mathf.Clamp01(value);if (instance != null) { instance.music.volume = MasterVolume * GameSettings.Current.music * .4f; instance.effects.volume = instance.ui.volume = MasterVolume*GameSettings.Current.effects; } }
    }
}
