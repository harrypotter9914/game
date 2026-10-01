using UnityEngine;
using Babel.Runtime.Combat;
namespace Bable {
    // Subscribes after damage has been accepted. Never fires from body contact or shield blocks.
    public sealed class PlayerHitFeedback:MonoBehaviour {
        HealthComponent health; float started=-100, strength=1;
        static PlayerHitFeedback instance;
        public int HitCount {get;private set;}
        public float FlashAlpha=>Visible?Mathf.Max(0,1-(Time.time-started)/.14f)*.18f*GameSettings.Current.flash:0;
        bool Visible=>Time.timeScale>0&&!TowerLoading.Busy&&health!=null;
        public static Vector3 CameraOffset {
            get {if(instance==null||!instance.Visible)return Vector3.zero;float t=Time.time-instance.started;
                if(t<0||t>.24f)return Vector3.zero;
                float a=.085f*GameSettings.Current.shake*instance.strength*Mathf.Pow(1-t/.24f,2);
                return new Vector3(Mathf.Sin(t*125)*a,Mathf.Cos(t*91)*a*.6f,0);}
        }
        void Awake(){instance=this;health=GetComponent<HealthComponent>();health.Damaged+=OnDamage;}
        void OnDamage(DamageInfo info){started=Time.time;strength=Mathf.Clamp(1+(info.Amount-1)*.12f,1,1.35f);HitCount++;}
        void OnGUI(){if(Event.current.type!=EventType.Repaint||FlashAlpha<=0||Camera.main==null)return;
            var r=Camera.main.pixelRect;r.y=Screen.height-r.yMax;var old=GUI.color;int depth=GUI.depth;GUI.depth=-900;
            GUI.color=new Color(1,.91f,.83f,FlashAlpha);GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;GUI.depth=depth;
        }
        void OnDestroy(){if(health!=null)health.Damaged-=OnDamage;if(instance==this)instance=null;}
    }
}
