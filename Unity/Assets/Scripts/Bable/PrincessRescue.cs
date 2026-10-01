using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Combat;
using Babel.Runtime.World;
namespace Bable
{
    public sealed class PrincessRescue : MonoBehaviour
    {
        public bool unlocked;
        public GameObject sealedGate;
        public Vector2 exitPosition=new Vector2(153,80);
        public Vector2 arrival=new Vector2(-67,75.8f);
        public bool Arrived {get;private set;}
        public bool Rescued {get;private set;}
        public bool IsRunning {get;private set;}
        public void RestoreProgress(bool open,bool arrived,bool complete){unlocked=open;Arrived=arrived;Rescued=complete;IsRunning=false;if(open&&sealedGate!=null)sealedGate.SetActive(false);}
        public void Unlock(){unlocked=true;if(sealedGate!=null)StartCoroutine(OpenGate());BableAudio.Music("tower");}
        IEnumerator OpenGate(){var collider=sealedGate.GetComponent<Collider2D>();if(collider!=null)collider.enabled=false;var sprites=sealedGate.GetComponentsInChildren<SpriteRenderer>();for(float t=0;t<1.2f;t+=Time.deltaTime){foreach(var s in sprites){var c=s.color;c.a=1-t/1.2f;s.color=c;}yield return null;}sealedGate.SetActive(false);TowerDialogue.Speak("gate_open");}
        void Update()
        {
            if(Babel.Runtime.Core.GameSession.Instance!=null&&Babel.Runtime.Core.GameSession.Instance.IsPaused)return;
            if(!unlocked||Rescued||IsRunning)return;var p=FindFirstObjectByType<PlayerController2D>();if(p==null)return;
            Vector2 delta=(Vector2)p.transform.position-exitPosition;
            if(!Arrived&&Mathf.Abs(delta.x)<2.8f&&Mathf.Abs(delta.y)<3.5f){StartCoroutine(Travel(p));return;}
            if(Arrived&&Vector2.Distance(p.transform.position,transform.position)<3.2f)StartCoroutine(Rescue(p));
        }
        IEnumerator Travel(PlayerController2D p)
        {
            IsRunning=true;var combat=p.GetComponent<PlayerCombatController>();p.enabled=false;if(combat!=null)combat.enabled=false;var body=p.GetComponent<Rigidbody2D>();body.linearVelocity=Vector2.zero;body.simulated=false;
            var canvas=new GameObject("Road to dawn",typeof(Canvas));canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=3000;
            var shade=new GameObject("Fade",typeof(RectTransform),typeof(Image)).GetComponent<Image>();shade.transform.SetParent(canvas.transform,false);shade.rectTransform.anchorMin=Vector2.zero;shade.rectTransform.anchorMax=Vector2.one;shade.rectTransform.offsetMin=shade.rectTransform.offsetMax=Vector2.zero;
            for(float t=0;t<.6f;t+=Time.deltaTime){shade.color=new Color(0,0,0,t/.6f);yield return null;}shade.color=Color.black;
            body.position=arrival;p.transform.position=arrival;Arrived=true;Babel.Runtime.Core.GameSession.Instance.SetRespawnPoint(arrival);FindFirstObjectByType<Babel.Runtime.World.CheckpointService>()?.ClearCheckpoint();Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow2D>()?.SetTarget(p.transform);
            yield return new WaitForSeconds(.25f);body.simulated=true;
            for(float t=0;t<.8f;t+=Time.deltaTime){shade.color=new Color(0,0,0,1-t/.8f);yield return null;}Destroy(canvas);p.enabled=true;if(combat!=null)combat.enabled=true;IsRunning=false;TowerDialogue.Speak("princess_call",transform);
        }
        IEnumerator Rescue(PlayerController2D p)
        {
            p.GetComponent<CharacterPresentation>()?.Act("idle",120);
            IsRunning=true;var combat=p.GetComponent<PlayerCombatController>();var hp=p.GetComponent<HealthComponent>();hp.Invincible=true;p.enabled=false;if(combat!=null)combat.enabled=false;p.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;
            var camera=Camera.main;var follow=camera.GetComponent<CameraFollow2D>();var mask=camera.GetComponent<PassageVisibility>();if(follow!=null)follow.enabled=false;if(mask!=null)mask.enabled=false;
            Vector3 start=camera.transform.position,target=new Vector3((transform.position.x+p.transform.position.x)*.5f,transform.position.y+1.5f,-10);float initial=camera.orthographicSize;
            for(float t=0;t<1.5f;t+=Time.deltaTime){float f=Mathf.SmoothStep(0,1,t/1.5f);camera.transform.position=Vector3.Lerp(start,target,f);camera.orthographicSize=Mathf.Lerp(initial,6,f);yield return null;}
            yield return TowerDialogue.Exchange(transform,"princess_reunion","pilgrim_rescue","princess_tower","pilgrim_home","princess_home","princess_promise","ending_narrator");
            Rescued=true;IsRunning=false;BableGameUI.Instance?.Victory();
        }
    }
}
