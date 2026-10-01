using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;
using Babel.Runtime.Core;
namespace Bable
{
    public sealed class SummoningEncounter:MonoBehaviour
    {
        public GameObject enemyPrefab;
        public GameObject[] wavePrefabs;
        public Vector2 feet;
        public int waves=1;
        public float interval=1.8f;
        public bool Triggered {get;private set;}
        public bool Completed {get;private set;}
        public bool IsSummoning {get;private set;}
        public int SpawnedCount {get;private set;}
        public EnemyControllerBase ActiveEnemy {get;private set;}
        Tilemap ground;
        void Awake(){ground=GameObject.Find("GroundTilemap")?.GetComponent<Tilemap>();}
        void OnTriggerEnter2D(Collider2D other){TryTrigger(other);}
        void OnTriggerStay2D(Collider2D other){TryTrigger(other);}
        void TryTrigger(Collider2D other){var player=other.GetComponentInParent<PlayerController2D>();if(player!=null&&!Triggered&&Ready(player))StartCoroutine(Run(player));}
        bool Ready(PlayerController2D p)=>!TowerDialogue.StoryActive&&GameSession.Instance!=null&&!GameSession.Instance.IsPaused&&p.IsGrounded&&Mathf.Abs(p.transform.position.y-feet.y)<2.5f;
        public bool IsSafeSpawn()=>IsSafeSpawn(enemyPrefab);
        public bool IsSafeSpawn(GameObject prefab)
        {
            if(ground==null)ground=GameObject.Find("GroundTilemap")?.GetComponent<Tilemap>();
            if(ground==null||prefab==null)return false;
            foreach(var room in FindObjectsByType<BossRoomBoundary>(FindObjectsSortMode.None))if(room.Contains(feet))return false;
            foreach(var boss in FindObjectsByType<BossBrain>(FindObjectsSortMode.None))if(Mathf.Abs(feet.x-boss.arenaCenter.x)<boss.arenaSize.x*.5f+5&&Mathf.Abs(feet.y-boss.arenaCenter.y)<boss.arenaSize.y*.5f+5)return false;
            var cell=ground.WorldToCell(feet+Vector2.down*.1f);
            var box=prefab.GetComponent<BoxCollider2D>();
            float width=box!=null?box.size.x*prefab.transform.localScale.x:2,height=box!=null?box.size.y*prefab.transform.localScale.y:2;
            foreach(var sr in prefab.GetComponentsInChildren<SpriteRenderer>())if(sr.sprite!=null){width=Mathf.Max(width,sr.sprite.bounds.size.x*Mathf.Abs(sr.transform.lossyScale.x));height=Mathf.Max(height,sr.sprite.bounds.size.y*Mathf.Abs(sr.transform.lossyScale.y));}
            int half=Mathf.Max(1,Mathf.CeilToInt(width*.5f+.4f)),roof=Mathf.Max(3,Mathf.CeilToInt(height+.4f));
            for(int x=-half;x<=half;x++){if(!ground.HasTile(cell+new Vector3Int(x,0,0)))return false;for(int y=1;y<=roof;y++)if(ground.HasTile(cell+new Vector3Int(x,y,0)))return false;}
            return true;
        }
        IEnumerator Run(PlayerController2D player)
        {
            if(!IsSafeSpawn())yield break;Triggered=true;
            int count=wavePrefabs!=null&&wavePrefabs.Length>0?wavePrefabs.Length:waves;
            for(int wave=0;wave<count;wave++){
                while(player!=null&&(!Ready(player)||Vector2.Distance(player.transform.position,feet)>24))yield return null;
                if(player==null)yield break;
                var prefab=wavePrefabs!=null&&wavePrefabs.Length>0?wavePrefabs[wave]:enemyPrefab;
                if(!IsSafeSpawn(prefab))continue;
                yield return Summon(prefab);
                while(ActiveEnemy!=null&&ActiveEnemy.GetComponent<HealthComponent>().CurrentHealth>0)yield return null;
                yield return new WaitForSeconds(interval);
            }
            Completed=true;
        }
        IEnumerator Summon(GameObject prefab)
        {
            IsSummoning=true;CombatAudio.Play("summon",feet,.6f);
            var seal=new GameObject("Summoning seal");seal.transform.position=feet;
            var frames=Resources.LoadAll<Sprite>("Bable/NewArt/VotiveAtlas").OrderBy(s=>s.name).ToArray();
            var glyphs=new SpriteRenderer[6];for(int i=0;i<glyphs.Length;i++){var go=new GameObject("Circling rune");go.transform.SetParent(seal.transform,false);glyphs[i]=go.AddComponent<SpriteRenderer>();glyphs[i].sprite=frames[4];glyphs[i].sortingOrder=7;go.transform.localScale=Vector3.one*.2f/frames[4].bounds.size.y;}
            for(float t=0;t<.6f;t+=Time.deltaTime){AnimateSeal(glyphs,t,1);yield return null;}
            var goEnemy=Instantiate(prefab,feet,Quaternion.identity);goEnemy.name="Summoned "+prefab.name;ActiveEnemy=goEnemy.GetComponent<EnemyControllerBase>();ActiveEnemy.enabled=false;
            var body=goEnemy.GetComponent<Rigidbody2D>();body.simulated=false;var collider=goEnemy.GetComponent<BoxCollider2D>();collider.enabled=false;
            var health=goEnemy.GetComponent<HealthComponent>();health.Invincible=true;var sprites=goEnemy.GetComponentsInChildren<SpriteRenderer>();
            var presentation=goEnemy.GetComponent<CharacterPresentation>();if(presentation!=null)presentation.enabled=false;
            float footOffset=(collider.offset.y-collider.size.y*.5f)*goEnemy.transform.localScale.y;
            Vector3 landing=new Vector3(feet.x,feet.y-footOffset+.035f,0);
            goEnemy.transform.position=landing;
            float lift=2.5f;
            // Low passages use a ground-level materialization; never rise through the ceiling.
            foreach(var sprite in sprites){var b=sprite.bounds;for(float x=b.min.x;x<=b.max.x;x+=.4f)for(float y=.15f;y<=2.6f;y+=.15f)if(ground!=null&&ground.HasTile(ground.WorldToCell(new Vector3(x,b.max.y+y,0)))){lift=Mathf.Min(lift,Mathf.Max(0,y-.25f));break;}}
            for(float t=0;t<1.1f;t+=Time.deltaTime){float f=Mathf.Clamp01(t/1.1f);goEnemy.transform.position=landing+Vector3.up*lift*(1-Mathf.SmoothStep(0,1,f));foreach(var sprite in sprites){var c=sprite.color;c.a=f;sprite.color=c;}AnimateSeal(glyphs,t+.6f,1-f*.7f);yield return null;}
            goEnemy.transform.position=landing;CombatAudio.Play("land",feet,.7f);foreach(var sprite in sprites)sprite.color=Color.white;collider.enabled=true;body.simulated=true;health.Invincible=false;
            if(presentation!=null)presentation.enabled=true;ActiveEnemy.enabled=true;SpawnedCount++;IsSummoning=false;Destroy(seal);Physics2D.SyncTransforms();
        }
        void AnimateSeal(SpriteRenderer[] glyphs,float time,float alpha){for(int i=0;i<glyphs.Length;i++){float angle=time*4+i*Mathf.PI/3;glyphs[i].transform.localPosition=new Vector3(Mathf.Cos(angle)*1.4f,.15f+Mathf.Sin(angle)*.28f,0);glyphs[i].color=new Color(.55f,.8f,1,alpha);}}
    }
}
