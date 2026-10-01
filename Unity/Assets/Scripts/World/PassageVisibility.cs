using UnityEngine;
using UnityEngine.Tilemaps;
namespace Babel.Runtime.World {
 // The serialized type name is retained for existing scenes. This component only
 // adjusts zoom; there is no visibility mask, fog mesh or camera confinement.
 [RequireComponent(typeof(Camera))]
 public class PassageVisibility:MonoBehaviour {
  public Transform target;
  public float minimumSize=3.5f,maximumSize=8;
  Camera view;Tilemap ground;
  float layerBottom,layerTop,roomLeft,roomRight,boundsTimer,crossingTime,nextBarrierScan;
  bool layerInitialized;
  Bable.BreakableWall[] breakables;
  readonly System.Collections.Generic.List<Bounds> barrierBounds=new();
  Vector2 gridSize;
  public float VisibleBottom=>layerBottom;
  public float VisibleTop=>layerTop;
  // Compatibility for historical editor audits; all world geometry is visible.
  public bool IsCellVisible(Vector3Int cell)=>true;
  public void ResetTracking(){layerInitialized=false;boundsTimer=0;crossingTime=0;}
  Vector3Int Cell(Vector2 p)=>ground.WorldToCell(p);
  Vector2 Corner(Vector3Int c)=>ground.CellToWorld(c);
  void Start(){view=GetComponent<Camera>();ground=GameObject.Find("GroundTilemap")?.GetComponent<Tilemap>();if(ground!=null)gridSize=ground.CellToWorld(Vector3Int.one)-ground.CellToWorld(Vector3Int.zero);}
        float WallDistance(Vector2 origin,Vector2 direction,float radius)
        {
            for(float d=.4f;d<radius;d+=.25f){
                Vector2 point=origin+direction*d;
                var cell=Cell(point);
                if(ground.HasTile(cell)){
                    Vector2 lower=Corner(cell),upper=lower+(Vector2)gridSize;
                    return Mathf.Max(.05f,direction.x>0?lower.x-origin.x:direction.x<0?origin.x-upper.x:direction.y>0?lower.y-origin.y:origin.y-upper.y);
                }
                foreach(var bounds in barrierBounds)if(point.x>=bounds.min.x&&point.x<=bounds.max.x&&point.y>=bounds.min.y&&point.y<=bounds.max.y)return d;
            }
            return radius;
        }
        void Update()
        {
            if(target==null||ground==null)return;
            if(breakables==null||Time.unscaledTime>=nextBarrierScan){breakables=FindObjectsByType<Bable.BreakableWall>(FindObjectsSortMode.None);nextBarrierScan=Time.unscaledTime+1;}
            barrierBounds.Clear();foreach(var wall in breakables)if(wall!=null&&wall.gameObject.activeInHierarchy){var c=wall.GetComponent<Collider2D>();if(c!=null&&c.enabled)barrierBounds.Add(c.bounds);}
            bool initialView=!layerInitialized;
            Vector2 origin=target.position;
            // Keep the current storey's bounds across openings. Looking through a stairwell
            // must not reveal the next floor before the player actually crosses its entrance.
            var player=target.GetComponent<Babel.Runtime.Characters.Player.PlayerController2D>();
            bool grounded=player==null||player.IsGrounded;
            bool crossing=layerInitialized&&(origin.y<layerBottom-.8f||origin.y>layerTop+.8f||origin.x<roomLeft-.8f||origin.x>roomRight+.8f);
            crossingTime=crossing?crossingTime+Time.deltaTime:0;
            boundsTimer-=Time.deltaTime;
            bool resample=!layerInitialized||boundsTimer<=0&&(grounded||crossingTime>.35f);
            if(resample){
                // Sample from a stable height while grounded. A jump past a ledge
                // does not replace the room's horizontal bounds on its first frame.
                origin.y=Mathf.Round(origin.y*2)/2f;
                layerBottom=origin.y-WallDistance(origin,Vector2.down,maximumSize*2);
                layerTop=origin.y+WallDistance(origin,Vector2.up,maximumSize*2);
                layerInitialized=true;
                roomLeft=origin.x-WallDistance(origin,Vector2.left,maximumSize*8);
                roomRight=origin.x+WallDistance(origin,Vector2.right,maximumSize*8);
                boundsTimer=.3f;
            }
            float fitHeight=(layerTop-layerBottom+2.6f)*.5f;
            float fitWidth=(roomRight-roomLeft+1.2f)/(2*view.aspect);
            float desired=Mathf.Clamp(Mathf.Min(fitHeight,fitWidth),minimumSize,maximumSize);
            view.orthographicSize=initialView?desired:Mathf.MoveTowards(view.orthographicSize,desired,.8f*Time.deltaTime);
        }
 }
}
