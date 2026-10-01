using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Core;
namespace Bable {
 public sealed class ExploredAtlas:MonoBehaviour {
  readonly HashSet<Vector3Int> visited=new();readonly HashSet<Vector3Int> seals=new();Tilemap ground;PlayerController2D player;float next;Texture2D texture;
  public int RevealedCount=>visited.Count;
  public Vector3Int[] Export(){var a=new Vector3Int[visited.Count];visited.CopyTo(a);return a;}
  public void Restore(Vector3Int[] cells){visited.Clear();foreach(var c in cells??System.Array.Empty<Vector3Int>())visited.Add(c);}
  public static ExploredAtlas Current {get{var s=GameSession.Instance;if(s==null)return null;return s.GetComponent<ExploredAtlas>()??s.gameObject.AddComponent<ExploredAtlas>();}}
  void Resolve(){if(ground==null)ground=GameObject.Find("GroundTilemap")?.GetComponent<Tilemap>();if(player==null)player=FindFirstObjectByType<PlayerController2D>();}
  void Update(){if(TowerLoading.Busy||Time.time<next)return;next=Time.time+.2f;Explore();}
  public void Explore(){Resolve();if(ground==null||player==null)return;seals.Clear();foreach(var wall in FindObjectsByType<BreakableWall>(FindObjectsSortMode.None)){foreach(var shape in wall.GetComponentsInChildren<Collider2D>()){if(!shape.enabled||shape.isTrigger)continue;var a=ground.WorldToCell(shape.bounds.min+Vector3.one*.02f);var b=ground.WorldToCell(shape.bounds.max-Vector3.one*.02f);for(int y=a.y;y<=b.y;y++)for(int x=a.x;x<=b.x;x++)seals.Add(new Vector3Int(x,y,0));}}var origin=ground.WorldToCell(player.transform.position);for(int y=-6;y<=6;y++)for(int x=-8;x<=8;x++){if(x*x/64f+y*y/36f>1)continue;Trace(origin,origin+new Vector3Int(x,y,0));}}
  void Trace(Vector3Int start,Vector3Int end){int x=start.x,y=start.y,dx=Mathf.Abs(end.x-x),dy=Mathf.Abs(end.y-y),sx=x<end.x?1:-1,sy=y<end.y?1:-1,err=dx-dy;for(int n=0;n<30;n++){var cell=new Vector3Int(x,y,0);visited.Add(cell);if(cell!=start&&(ground.HasTile(cell)||seals.Contains(cell)))break;if(x==end.x&&y==end.y)break;int e=2*err;if(e>-dy){err-=dy;x+=sx;}if(e<dx){err+=dx;y+=sy;}}}
  public bool IsRevealed(Vector3Int cell)=>visited.Contains(cell);
  public Texture2D Draw(){Resolve();if(ground==null)return null;var bounds=ground.cellBounds;int w=bounds.size.x,h=bounds.size.y;if(texture==null||texture.width!=w||texture.height!=h){if(texture!=null)Destroy(texture);texture=new Texture2D(w,h,TextureFormat.RGBA32,false);texture.filterMode=FilterMode.Point;texture.wrapMode=TextureWrapMode.Clamp;}
   var colors=new Color32[w*h];for(int i=0;i<colors.Length;i++)colors[i]=new Color32(0,0,0,255);
   foreach(var c in visited){int x=c.x-bounds.xMin,y=c.y-bounds.yMin;if(x<0||y<0||x>=w||y>=h)continue;colors[y*w+x]=ground.HasTile(c)?new Color32(156,135,94,255):new Color32(45,58,69,255);}
   if(player!=null){var p=ground.WorldToCell(player.transform.position);for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++){int px=p.x+x-bounds.xMin,py=p.y+y-bounds.yMin;if(px>=0&&py>=0&&px<w&&py<h&&visited.Contains(new Vector3Int(p.x+x,p.y+y,0)))colors[py*w+px]=new Color32(245,222,147,255);}}
   texture.SetPixels32(colors);texture.Apply();return texture;
  }
  void OnDestroy(){if(texture!=null)Destroy(texture);}
 }
}
