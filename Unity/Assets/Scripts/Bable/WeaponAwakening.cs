using System.Collections;
using UnityEngine;
using Babel.Runtime.Core;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Combat;
namespace Bable
{
    public sealed class WeaponAwakening:MonoBehaviour
    {
        public bool IsRunning {get;private set;}
        void Start(){if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.StartsWith("Boss_Test"))GameSession.Instance?.UnlockWeapon();}
        public void Begin(){if(!IsRunning)StartCoroutine(Sequence());}
        IEnumerator Sequence(){
            IsRunning=true;var player=GetComponent<PlayerController2D>();var combat=GetComponent<PlayerCombatController>();var body=GetComponent<Rigidbody2D>();var health=GetComponent<HealthComponent>();var art=GetComponent<CharacterPresentation>();
            var interpolation=body.interpolation;body.interpolation=RigidbodyInterpolation2D.None;
            bool enabledPlayer=player.enabled,enabledCombat=combat.enabled,invincible=health.Invincible,simulated=body.simulated;
            player.enabled=false;combat.enabled=false;body.linearVelocity=Vector2.zero;health.Invincible=true;
            var box=GetComponent<BoxCollider2D>();
            bool settled=false;
            if(TerrainMotion.FindLanding(box,transform.position,out var floor)&&body.position.y-floor.y<8){
                body.simulated=true;art.Act("fall",2);float deadline=Time.time+2;while(body.position.y>floor.y+.06f&&Time.time<deadline)yield return new WaitForFixedUpdate();
                if(Mathf.Abs(body.position.y-floor.y)<.2f){settled=true;body.position=floor;transform.position=floor;Physics2D.SyncTransforms();}
            }
            if(!settled){GameSession.Instance.UnlockWeapon();art.ReleaseAction();body.simulated=simulated;body.interpolation=interpolation;health.Invincible=invincible;player.enabled=enabledPlayer;combat.enabled=enabledCombat;IsRunning=false;yield break;}
            body.simulated=false;
            art.Act("receive",2.2f);Babel.Runtime.UI.ScreenMessagePresenter.ShowCenter("A light answers your vow.",2.2f);
            var blade=new GameObject("The descending blade");var visual=blade.AddComponent<SpriteRenderer>();visual.sprite=Resources.Load<Sprite>("Bable/NewArt/ConsecratedBlade");visual.sortingOrder=30;blade.transform.rotation=Quaternion.Euler(0,0,-60);
            if(visual.sprite!=null)blade.transform.localScale=Vector3.one*(1.6f/visual.sprite.bounds.size.x);
            Vector2 end=(Vector2)transform.position+Vector2.up*.65f;
            RelicFX.Burst("RelicStar",end+Vector2.up*2,Vector2.up,new Vector2(1,1),1.4f);
            for(float t=0;t<1.7f;t+=Time.deltaTime){blade.transform.position=Vector2.Lerp(end+Vector2.up*3,end,Mathf.SmoothStep(0,1,t/1.7f));visual.color=new Color(1,1,.85f,Mathf.Min(1,t*4));yield return null;}
            CombatAudio.Play("star",transform.position,.85f);RelicFX.Burst("RelicStar",end,Vector2.right,new Vector2(2.5f,2.5f),.6f);Destroy(blade);
            yield return new WaitForSeconds(.45f);GameSession.Instance.UnlockWeapon();art.ReleaseAction();
            body.simulated=simulated;body.interpolation=interpolation;health.Invincible=invincible;player.enabled=enabledPlayer;combat.enabled=enabledCombat;IsRunning=false;
            Babel.Runtime.UI.ScreenMessagePresenter.ShowCenter("BLADE AWAKENED\nX — Strike",3);
        }
    }
}
