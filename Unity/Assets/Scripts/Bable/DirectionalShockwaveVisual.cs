using UnityEngine;
namespace Bable
{
    // Shortening UVs at a wall preserves the column's shape instead of squeezing it into a blob.
    [DefaultExecutionOrder(500)]
    public sealed class DirectionalShockwaveVisual : MonoBehaviour
    {
        Transform source; Babel.Runtime.Characters.Player.PlayerCombatController combat; Vector2 axis;
        float range, thickness, reach, age; bool blocked;
        Mesh mesh; Material material; MeshRenderer beamRenderer;
        readonly Vector3[] vertices = new Vector3[24];
        readonly Vector2[] uv = new Vector2[24];
        readonly Color[] colors = new Color[24];
        static readonly int[] triangles = BuildTriangles();
        static readonly float[] coreOffsets={-.12f,-.09f,-.05f,-.015f};
        static int[] BuildTriangles(){var t=new int[60];for(int n=0;n<9;n++){int i=n*2,k=n*6;t[k]=i;t[k+1]=i+1;t[k+2]=i+2;t[k+3]=i+1;t[k+4]=i+3;t[k+5]=i+2;}int[] cap={20,22,21,22,23,21};System.Array.Copy(cap,0,t,54,6);return t;}
        public float Reach => reach;
        public Vector2 Axis => axis;
        public static DirectionalShockwaveVisual Create(Transform source, Vector2 axis, float range, float thickness)
        {
            var go = new GameObject("Player sustained Shockwave");
            var v = go.AddComponent<DirectionalShockwaveVisual>();
            v.source=source; v.combat=source.GetComponent<Babel.Runtime.Characters.Player.PlayerCombatController>();
            v.axis=axis; v.range=range; v.thickness=thickness;
            v.mesh=new Mesh {name="Clipped Shockwave column"};
            go.AddComponent<MeshFilter>().sharedMesh=v.mesh;
            v.beamRenderer=go.AddComponent<MeshRenderer>();v.beamRenderer.sortingOrder=25;
            v.material=new Material(Shader.Find("Sprites/Default"));
            v.material.mainTexture=Resources.Load<Texture2D>("Bable/NewArt/PlayerBeam45");
            v.beamRenderer.sharedMaterial=v.material;
            return v;
        }
        public void SetReach(float distance,bool contact){reach=Mathf.Max(0,distance);blocked=contact;}
        void LateUpdate()
        {
            if(source==null){Destroy(gameObject);return;}
            age+=Time.deltaTime;
            transform.position=combat!=null?(Vector3)combat.ShockwaveOrigin:source.position;
            transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(axis.y,axis.x)*Mathf.Rad2Deg);
            beamRenderer.enabled=reach>.015f;
            int frame=Mathf.FloorToInt(age*12)%4;
            float bottom=1-(frame+1)*.25f,top=bottom+.25f,half=thickness*.5f;
            // A narrow throat meets the palm, then opens into the column; no rectangular cut at the hand.
            float neck=Mathf.Min(.6f,reach);
            for(int ring=0;ring<10;ring++){
                float x=ring<9?neck*ring/8:reach;
                float width=Mathf.Lerp(.055f,half,Mathf.SmoothStep(0,1,ring/8f));
                float shift=-coreOffsets[frame]*width*2;
                int i=ring*2;vertices[i]=new Vector3(x,-width+shift);vertices[i+1]=new Vector3(x,width+shift);
                float u=Mathf.Lerp(.065f,.975f,Mathf.Clamp01(x/range));uv[i]=new Vector2(u,bottom);uv[i+1]=new Vector2(u,top);
                colors[i]=colors[i+1]=new Color(1,1,1,.88f);
            }
            float cap=Mathf.Min(.48f,reach);
            Quad(20,reach-cap,reach,half,.87f,.975f,bottom,top,blocked?.8f+.15f*Mathf.Sin(age*30):.55f);
            for(int i=20;i<24;i++)vertices[i].y-=coreOffsets[frame]*half*2;
            mesh.vertices=vertices;mesh.uv=uv;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateBounds();
        }
        void Quad(int i,float left,float right,float half,float u0,float u1,float v0,float v1,float alpha)
        {
            vertices[i]=new Vector3(left,-half);vertices[i+1]=new Vector3(right,-half);
            vertices[i+2]=new Vector3(left,half);vertices[i+3]=new Vector3(right,half);
            uv[i]=new Vector2(u0,v0);uv[i+1]=new Vector2(u1,v0);uv[i+2]=new Vector2(u0,v1);uv[i+3]=new Vector2(u1,v1);
            for(int n=i;n<i+4;n++)colors[n]=new Color(1,1,1,alpha);
        }
        void OnDestroy(){if(mesh!=null)Destroy(mesh);if(material!=null)Destroy(material);}
    }
}
