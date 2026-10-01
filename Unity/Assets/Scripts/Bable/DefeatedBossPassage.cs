using UnityEngine;
using UnityEngine.Tilemaps;
using Babel.Runtime.Combat;
namespace Bable {
    [DefaultExecutionOrder(-300)]
    public sealed class DefeatedBossPassage : MonoBehaviour {
        public BossBrain boss;
        public BreakableWall[] walls;
        public bool IsRevealed {get;private set;}
        struct SavedTile { public Vector3Int position; public TileBase tile; public Color color; public Matrix4x4 matrix; }
        readonly System.Collections.Generic.List<SavedTile> tiles=new();
        readonly System.Collections.Generic.List<Bounds> bounds=new();
        Tilemap ground; HealthComponent health; bool defeated;float revealAfter;
        void Awake(){
            ground=GameObject.Find("GroundTilemap")?.GetComponent<Tilemap>();
            foreach(var wall in walls){if(wall==null)continue;
                var box=wall.GetComponent<Collider2D>();if(box!=null){bounds.Add(box.bounds);
                    if(ground!=null){var min=ground.WorldToCell(box.bounds.min+Vector3.one*.01f);var max=ground.WorldToCell(box.bounds.max-Vector3.one*.01f);
                        for(int x=min.x;x<=max.x;x++)for(int y=min.y;y<=max.y;y++){
                            var cell=new Vector3Int(x,y,0);var tile=ground.GetTile(cell);if(tile==null)continue;
                            tiles.Add(new SavedTile{position=cell,tile=tile,color=ground.GetColor(cell),matrix=ground.GetTransformMatrix(cell)});ground.SetTile(cell,null);
                        }
                    }
                }wall.gameObject.SetActive(false);
            }
        }
        void Start(){if(boss!=null){health=boss.GetComponent<HealthComponent>();health.Died+=Defeated;}}
        void Defeated(){defeated=true;revealAfter=Time.time+.65f;}
        void Update(){
            if(!defeated||IsRevealed||Time.time<revealAfter)return;
            foreach(var area in bounds)foreach(var collider in Physics2D.OverlapBoxAll(area.center,area.size+Vector3.one*.2f,0))
                if(collider.GetComponentInParent<Babel.Runtime.Characters.Player.PlayerController2D>()!=null)return;
            if(ground!=null)foreach(var tile in tiles){ground.SetTile(tile.position,tile.tile);ground.SetTileFlags(tile.position,TileFlags.None);ground.SetColor(tile.position,tile.color);ground.SetTransformMatrix(tile.position,tile.matrix);}
            foreach(var wall in walls)if(wall!=null){wall.gameObject.SetActive(true);RelicFX.Burst("Shockwave",wall.transform.position,Vector2.up,new Vector2(2,2),.4f);}
            IsRevealed=true;
        }
        void OnDestroy(){if(health!=null)health.Died-=Defeated;}
    }
}
