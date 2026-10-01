using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using Bable;
public static class BableRevision41Visual {
 public static void Map(){
  var root=new GameObject("Temporary map annotations");root.hideFlags=HideFlags.HideAndDontSave;
  var material=new Material(Shader.Find("Sprites/Default"));
  foreach(var boss in Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None)){
   var p=boss.arenaCenter;var s=boss.arenaSize;Outline(root,material,new Rect(p-s*.5f,s),Color.cyan,.16f);
   Label(root,boss.profile.kind.ToString(),new Vector3(p.x,p.y+s.y*.5f+1,-5),.55f,Color.cyan);
  }
  var gates=GameObject.Find("Revision 41 shockwave passages");
  foreach(Transform group in gates.transform){var cs=group.GetComponentsInChildren<Collider2D>();var b=cs[0].bounds;foreach(var c in cs)b.Encapsulate(c.bounds);Outline(root,material,new Rect(b.min,b.size),new Color(1,.65f,.1f),.2f);}
  var camgo=new GameObject("Map review camera");camgo.transform.SetParent(root.transform);var cam=camgo.AddComponent<Camera>();cam.orthographic=true;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=Color.black;
  Render(cam,new Vector2(195,18),112,5000,2000,"map-after");
  Render(cam,new Vector2(76,-49),21,1800,1100,"first-room-after");
  Render(cam,new Vector2(281,-54),19,1800,1100,"korah-room-after");
  Object.DestroyImmediate(root);Object.DestroyImmediate(material);
 }
 static void Outline(GameObject root,Material mat,Rect r,Color color,float width){var go=new GameObject("Boundary");go.transform.SetParent(root.transform);var l=go.AddComponent<LineRenderer>();l.sharedMaterial=mat;l.sortingOrder=100;l.startColor=l.endColor=color;l.startWidth=l.endWidth=width;l.positionCount=5;l.SetPositions(new[]{new Vector3(r.xMin,r.yMin,-4),new Vector3(r.xMin,r.yMax,-4),new Vector3(r.xMax,r.yMax,-4),new Vector3(r.xMax,r.yMin,-4),new Vector3(r.xMin,r.yMin,-4)});}
 static void Label(GameObject root,string value,Vector3 p,float size,Color color){var go=new GameObject("Map label");go.transform.SetParent(root.transform);go.transform.position=p;var t=go.AddComponent<TextMesh>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=42;t.characterSize=size;t.anchor=TextAnchor.LowerCenter;t.text=value;t.color=color;var mr=go.GetComponent<MeshRenderer>();mr.sharedMaterial=t.font.material;mr.sortingOrder=101;}
 static void Render(Camera cam,Vector2 center,float size,int w,int h,string name){cam.orthographicSize=size;cam.aspect=w/(float)h;cam.transform.position=new Vector3(center.x,center.y,-50);var rt=new RenderTexture(w,h,24);var tex=new Texture2D(w,h,TextureFormat.RGB24,false);var old=RenderTexture.active;try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,w,h),0,0);tex.Apply();File.WriteAllBytes("../reference/revision41/"+name+".png",tex.EncodeToPNG());}finally{RenderTexture.active=old;cam.targetTexture=null;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);}}
 public static void Capture(){
  var root=new GameObject("Temporary action review");root.hideFlags=HideFlags.HideAndDontSave;
  var material=new Material(Shader.Find("Sprites/Default"));
  string[] families={"Nero","SkyJudicator"};string[][] actions={new[]{"idle","slamright","slamright","slamleft"},new[]{"idle","rising","airside","airdown"}};int[][] frames={new[]{0,1,2,2},new[]{0,2,2,2}};
  for(int row=0;row<2;row++){
   var boss=Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None).First(b=>b.profile.kind==(row==0?BossKind.Nero:BossKind.Aerial));
   var original=boss.GetComponent<CharacterPresentation>().animator.GetComponent<SpriteRenderer>();
   for(int col=0;col<4;col++){
    var go=new GameObject(families[row]+actions[row][col]);go.transform.SetParent(root.transform);go.layer=31;go.transform.position=new Vector3(col*5,-row*6,0);go.transform.localScale=original.transform.lossyScale;
    var sr=go.AddComponent<SpriteRenderer>();sr.sharedMaterial=original.sharedMaterial;sr.color=Color.white;
    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/NativeAnimations/"+families[row]+"_"+actions[row][col]+".anim");var bind=AnimationUtility.GetObjectReferenceCurveBindings(clip).First(b=>b.propertyName=="m_Sprite");sr.sprite=(Sprite)AnimationUtility.GetObjectReferenceCurve(clip,bind)[frames[row][col]].value;
    var label=new GameObject("Label");label.transform.SetParent(root.transform);label.layer=31;label.transform.position=new Vector3(col*5,-row*6-1,-1);var text=label.AddComponent<TextMesh>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=36;text.characterSize=.10f;text.anchor=TextAnchor.MiddleCenter;text.text=actions[row][col]+" / "+frames[row][col];label.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;
   }
   var line=new GameObject("Sole baseline");line.transform.SetParent(root.transform);line.layer=31;var l=line.AddComponent<LineRenderer>();l.sharedMaterial=material;l.startColor=l.endColor=Color.gray;l.startWidth=l.endWidth=.025f;l.positionCount=2;l.SetPositions(new[]{new Vector3(-3,-row*6,1),new Vector3(18,-row*6,1)});
  }
  var camgo=new GameObject("Review camera");camgo.transform.SetParent(root.transform);var cam=camgo.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=7.4f;cam.aspect=1.75f;cam.transform.position=new Vector3(7.5f,-.6f,-20);cam.cullingMask=1<<31;cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.11f,.12f,.15f);
  var rt=new RenderTexture(2100,1200,24);var tex=new Texture2D(2100,1200,TextureFormat.RGB24,false);var old=RenderTexture.active;try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,2100,1200),0,0);tex.Apply();File.WriteAllBytes("../reference/revision41/action-review.png",tex.EncodeToPNG());}finally{RenderTexture.active=old;cam.targetTexture=null;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);Object.DestroyImmediate(root);Object.DestroyImmediate(material);}
 }
}
