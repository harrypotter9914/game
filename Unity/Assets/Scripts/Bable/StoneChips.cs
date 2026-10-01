using UnityEngine;
namespace Bable {
 public sealed class StoneChips:MonoBehaviour {
  Mesh mesh;Material material;MeshRenderer visual;Vector2 velocity;float life,spin;
  public static void Burst(Vector2 position,Color color){for(int i=0;i<4;i++){var go=new GameObject("Shattered masonry chip");go.transform.position=position+new Vector2(Random.Range(-.3f,.3f),Random.Range(-.2f,.2f));var chip=go.AddComponent<StoneChips>();chip.Create(color);}}
  void Create(Color color){
   mesh=new Mesh();mesh.vertices=new[]{new Vector3(-.10f,-.065f),new Vector3(.055f,-.08f),new Vector3(.13f,.025f),new Vector3(.035f,.12f),new Vector3(-.11f,.07f)};mesh.triangles=new[]{0,1,2,0,2,3,0,3,4};mesh.colors=new[]{color*.7f,color*.6f,color,color*1.3f,color*.9f};mesh.RecalculateBounds();gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;visual=gameObject.AddComponent<MeshRenderer>();material=new Material(Shader.Find("Sprites/Default"));visual.sharedMaterial=material;visual.sortingOrder=12;velocity=new Vector2(Random.Range(-2.5f,2.5f),Random.Range(2,4));spin=Random.Range(-180,180);life=.65f;
  }
  void Update(){float dt=Time.deltaTime;life-=dt;if(life<=0){Destroy(gameObject);return;}velocity.y-=14*dt;transform.position+=(Vector3)(velocity*dt);transform.Rotate(0,0,spin*dt);material.color=new Color(1,1,1,Mathf.Clamp01(life*4));}
  void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
 }
}
