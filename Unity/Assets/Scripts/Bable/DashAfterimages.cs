using UnityEngine;
namespace Bable
{
    public sealed class DashAfterimages : MonoBehaviour
    {
        float until,next;
        public void Begin(float duration){until=Time.time+duration;next=0;}
        void Update()
        {
            if(Time.time>=until || Time.time<next)return;next=Time.time+.065f;
            var body=GetComponent<Rigidbody2D>();if(body==null||body.linearVelocity.sqrMagnitude<4)return;
            Vector2 d=body.linearVelocity.normalized;
            var original=GetComponent<CharacterPresentation>()?.animator?.GetComponent<SpriteRenderer>();
            if(original!=null){
                var ghost=new GameObject("Dash pose afterimage");ghost.transform.SetPositionAndRotation(original.transform.position,original.transform.rotation);ghost.transform.localScale=original.transform.lossyScale;
                var renderer=ghost.AddComponent<SpriteRenderer>();renderer.sprite=original.sprite;renderer.flipX=original.flipX;renderer.sortingOrder=original.sortingOrder-1;
                renderer.color=new Color(.35f,.8f,1,.35f);ghost.AddComponent<DashPoseFade>();
            }
            RelicFX.Burst("DashTrail",(Vector2)transform.position-d*.7f,d,new Vector2(2.2f,.6f),.18f);
        }
    }
    public sealed class DashPoseFade : MonoBehaviour
    {
        float age;SpriteRenderer image;
        void Awake(){image=GetComponent<SpriteRenderer>();}
        void Update(){age+=Time.deltaTime;if(age>=.24f){Destroy(gameObject);return;}var c=image.color;c.a=.35f*(1-age/.24f);image.color=c;}
    }
}
