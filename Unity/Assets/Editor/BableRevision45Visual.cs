using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class BableRevision45Visual {
 public static void Shield(string suffix) {
  var root=new GameObject("Shield review"); root.hideFlags=HideFlags.HideAndDontSave;
  string[] states={"idle","run","attack","block","sufferattack"};
  for(int r=0;r<states.Length;r++) {
   var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/NativeAnimations/ShieldGuard_"+states[r]+".anim");
   var frames=AnimationUtility.GetObjectReferenceCurve(clip,AnimationUtility.GetObjectReferenceCurveBindings(clip).First()).Select(k=>(Sprite)k.value).Distinct().ToArray();
   for(int c=0;c<4;c++) {
    int frame=states[r]=="run"?c*6:Mathf.Min(c,frames.Length-1);
    var go=new GameObject(states[r]+frame);go.transform.SetParent(root.transform);go.layer=31;go.transform.position=new Vector3(c*4,-r*3.8f,0);go.AddComponent<SpriteRenderer>().sprite=frames[frame];
    var label=new GameObject("Label");label.transform.SetParent(root.transform);label.layer=31;label.transform.position=go.transform.position+new Vector3(0,-.35f,-1);var t=label.AddComponent<TextMesh>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=40;t.characterSize=.12f;t.anchor=TextAnchor.MiddleCenter;t.text=states[r]+" "+frame;label.GetComponent<MeshRenderer>().sharedMaterial=t.font.material;
   }
  }
  var cgo=new GameObject("Review camera");cgo.transform.SetParent(root.transform);var cam=cgo.AddComponent<Camera>();cam.cullingMask=1<<31;Render(cam,new Vector2(6,-6.3f),9.5f,1700,1900,"shield-"+suffix);Object.DestroyImmediate(root);
 }
 public static void Map(string name,Vector2 center,float size,int w=1600,int h=1000){var go=new GameObject("Review camera");var cam=go.AddComponent<Camera>();Render(cam,center,size,w,h,name);Object.DestroyImmediate(go);}
 static void Render(Camera cam,Vector2 center,float size,int w,int h,string name){cam.orthographic=true;cam.orthographicSize=size;cam.aspect=w/(float)h;cam.transform.position=new Vector3(center.x,center.y,-50);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.08f,.085f,.10f);var rt=new RenderTexture(w,h,24);var tex=new Texture2D(w,h,TextureFormat.RGB24,false);var old=RenderTexture.active;try{cam.targetTexture=rt;cam.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,w,h),0,0);tex.Apply();File.WriteAllBytes("../reference/revision45/"+name+".png",tex.EncodeToPNG());}finally{RenderTexture.active=old;cam.targetTexture=null;Object.DestroyImmediate(tex);Object.DestroyImmediate(rt);}}
}

