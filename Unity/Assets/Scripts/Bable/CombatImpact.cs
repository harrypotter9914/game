using UnityEngine;
namespace Bable {
    // Brief angular sparks at contact. No labels and no second sword-swing layer.
    public sealed class CombatImpact:MonoBehaviour {
        Mesh mesh;Material material;float born;Color tint;Vector3[] vertices;Color[] colors;
        public static void Spawn(Vector2 position,Vector2 direction,bool shield) {
            var go=new GameObject(shield?"Shield contact sparks":"Impact sparks");go.transform.position=position;
            go.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
            var fx=go.AddComponent<CombatImpact>();fx.tint=shield?new Color(.65f,.88f,1):new Color(1,.78f,.4f);fx.born=Time.time;
            fx.mesh=new Mesh();go.AddComponent<MeshFilter>().sharedMesh=fx.mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sortingOrder=28;fx.material=new Material(Shader.Find("Sprites/Default"));renderer.sharedMaterial=fx.material;
            fx.vertices=new Vector3[24];fx.colors=new Color[24];var triangles=new int[24];for(int i=0;i<24;i++)triangles[i]=i;
            fx.mesh.vertices=fx.vertices;fx.mesh.triangles=triangles;
        }
        void Update(){
            float t=(Time.time-born)/.22f;if(t>=1){Destroy(gameObject);return;}
            for(int i=0;i<8;i++){
                float a=i*Mathf.PI*.25f;Vector3 d=new Vector3(Mathf.Cos(a),Mathf.Sin(a));Vector3 n=new Vector3(-d.y,d.x);
                float r=.04f+t*(i%2==0?.6f:.38f),length=(1-t)*.16f;
                vertices[i*3]=d*r+n*.024f*(1-t);vertices[i*3+1]=d*r-n*.024f*(1-t);vertices[i*3+2]=d*(r+length);
                for(int j=0;j<3;j++)colors[i*3+j]=new Color(tint.r,tint.g,tint.b,1-t);
            }
            mesh.vertices=vertices;mesh.colors=colors;mesh.RecalculateBounds();
        }
        void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
