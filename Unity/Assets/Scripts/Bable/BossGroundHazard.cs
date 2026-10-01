using UnityEngine;
using Babel.Runtime.Combat;
using Babel.Runtime.Characters.Player;
namespace Bable {
    // Floor effects own an explicit warning, one active window and a harmless fade.
    public sealed class BossGroundHazard:MonoBehaviour {
        BossBrain owner; SpriteRenderer visual; float created,warning; bool trap,hit,sounded; int damage;
        public bool Armed=>Time.time-created>=warning&&Time.time-created<warning+(trap?.55f:.22f);
        public static BossGroundHazard Create(BossBrain boss,Vector2 foot,bool crystal,float anticipation,int amount){
            var go=new GameObject(crystal?"Crystal floor trap":"Sword ground impact");go.transform.position=foot;
            var fx=go.AddComponent<BossGroundHazard>();fx.owner=boss;fx.trap=crystal;fx.warning=anticipation;fx.damage=amount;fx.created=Time.time;
            fx.visual=go.AddComponent<SpriteRenderer>();fx.visual.sortingOrder=12;
            if(crystal)CombatAudio.Play("trap_warn",foot,.6f);
            fx.visual.sprite=CombatVfxFrames.Get(crystal?"CrystalTrap41":"GroundImpact41",0);
            var full=CombatVfxFrames.Get(crystal?"CrystalTrap41":"GroundImpact41",2);
            if(full!=null)go.transform.localScale=Vector3.one*(crystal?2.3f:1.2f)/full.bounds.size.y;
            return fx;
        }
        void Update(){
            if(Time.timeScale==0)return;
            if(owner==null||!owner.Engaged||owner.GetComponent<HealthComponent>().CurrentHealth<=0){Destroy(gameObject);return;}
            float age=Time.time-created,active=trap?.55f:.22f;
            if(trap&&!sounded&&age>=warning&&!owner.DialogueHold&&!TowerDialogue.StoryActive){sounded=true;CombatAudio.Play("trap_fire",transform.position,.75f);}
            int frame=age<warning?(age<warning*.65f?0:1):age<warning+active?2:3;
            visual.sprite=CombatVfxFrames.Get(trap?"CrystalTrap41":"GroundImpact41",frame);
            visual.color=new Color(1,1,1,age<warning?Mathf.Lerp(.45f,1,warning>0?age/warning:1):Mathf.Clamp01(1-(age-warning-active)/.35f));
            if(age>=warning+active+.35f){Destroy(gameObject);return;}
            if(!Armed||hit||owner.DialogueHold||TowerDialogue.StoryActive)return;
            Vector2 size=trap?new Vector2(1.2f,1.7f):new Vector2(2.1f,.65f);
            foreach(var c in Physics2D.OverlapBoxAll((Vector2)transform.position+Vector2.up*size.y*.5f,size,0)){
                var player=c.GetComponentInParent<PlayerController2D>();if(player==null)continue;
                var hp=player.GetComponent<HealthComponent>();
                if(hp!=null&&CombatGeometry.Clear((Vector2)transform.position+Vector2.up*.15f,c.bounds.center)){
                    hp.ReceiveDamage(new DamageInfo(damage,transform.position,Vector2.up*2,owner.gameObject,TeamAlignment.Enemy));hit=true;break;
                }
            }
        }
    }
}
