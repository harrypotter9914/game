using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace Babel.EditorTools
{
    public static class BableArtPolish
    {
        const string Root="Assets/Resources/Bable/NewArt/";
        [Serializable] public class Frame {public int x,y,width,height; public float pivotX=.5f,pivotY;}
        [Serializable] public class Sheet {public string name;public Frame[] frames;public int referenceHeight;}
        [Serializable] public class Catalog {public Sheet[] items;}
        public static readonly Dictionary<string,string[]> ExtraActions=new Dictionary<string,string[]>{
            {"Pilgrim",new[]{"sufferattack","fall","wallslide","dash","charge","crystaldash","shockwave"}},
            {"Nero",new[]{"sufferattack","special","phasechange"}},
            {"MeleeGuard",new[]{"sufferattack"}}, {"ShieldGuard",new[]{"block","sufferattack"}},
            {"RangedGuard",new[]{"upattack","sufferattack"}}, {"GiantGuard",new[]{"windup","sufferattack"}},
            {"FirstPenitent",new[]{"sufferattack"}}, {"BuriedOne",new[]{"sufferattack"}},
            {"SkyJudicator",new[]{"sufferattack"}}, {"CrystalBishop",new[]{"sufferattack"}},
            {"Princess",new[]{"idle"}}
        };
        [MenuItem("Bable/Art/Polish All Character Edges")]
        public static void Clean()
        {
            string backup="../reference/revision3/raw-art";Directory.CreateDirectory(backup);
            var files=ExtraActions.Keys.Select(n=>Root+n+".png").Concat(ExtraActions.Keys.Where(n=>n!="Princess").Select(n=>Root+n+"Extras.png")).ToArray();
            var log=new List<string>();
            foreach(string path in files)
            {
                if(!File.Exists(path))throw new FileNotFoundException(path);
                string original=Path.Combine(backup,Path.GetFileName(path));
                if(!File.Exists(original))File.Copy(path,original);
                var t=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(t,File.ReadAllBytes(original));
                if(path==Root+"CrystalBishop.png"){
                    var patch=new Texture2D(2,2);ImageConversion.LoadImage(patch,File.ReadAllBytes("../reference/pixilart/CrystalBishop-idle-retouched.png"));
                    if(patch.width!=165||patch.height!=247)throw new Exception("Retouch dimensions changed");
                    t.SetPixels(76,843,165,247,patch.GetPixels());UnityEngine.Object.DestroyImmediate(patch);
                }
                var p=t.GetPixels32();int keyed=0,fringe=0,noise=0;
                for(int i=0;i<p.Length;i++)if(p[i].g>120 && p[i].g-Math.Max(p[i].r,p[i].b)>35){p[i]=new Color32(0,0,0,0);keyed++;}
                // Despill only at alpha boundaries; preserve cyan crystals and all interior palette colors.
                for(int y=1;y<t.height-1;y++)for(int x=1;x<t.width-1;x++){
                    int i=y*t.width+x;var c=p[i];if(c.a==0)continue;
                    if(p[i-1].a==0||p[i+1].a==0||p[i-t.width].a==0||p[i+t.width].a==0)
                        if(c.g>Math.Max(c.r,c.b)+10){c.g=Math.Max(c.r,c.b);p[i]=c;fringe++;}
                }
                // Remove isolated dark one-to-three-pixel noise only. Bright magic glints are intentional.
                var seen=new bool[p.Length];var q=new Queue<int>();var component=new List<int>();
                for(int i=0;i<p.Length;i++){
                    if(seen[i]||p[i].a==0)continue;seen[i]=true;q.Enqueue(i);component.Clear();bool bright=false;
                    while(q.Count>0){int j=q.Dequeue();component.Add(j);var c=p[j];if(Math.Max(c.r,Math.Max(c.g,c.b))>150)bright=true;
                        int xx=j%t.width,yy=j/t.width;
                        for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){
                            int nx=xx+dx,ny=yy+dy;if(nx<0||ny<0||nx>=t.width||ny>=t.height)continue;int k=ny*t.width+nx;
                            if(!seen[k]&&p[k].a>0){seen[k]=true;q.Enqueue(k);}
                        }
                    }
                    if(component.Count<=3&&!bright)foreach(int j in component){p[j]=new Color32(0,0,0,0);noise++;}
                }
                t.SetPixels32(p);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);
                log.Add(Path.GetFileName(path)+": keyed="+keyed+", despilled="+fringe+", darkNoise="+noise);
            }
            File.WriteAllLines("../reference/revision3/edge-cleanup.txt",log);AssetDatabase.Refresh();Debug.Log("BABLE_ART_CLEAN: "+files.Length+" sheets");
        }
        [MenuItem("Bable/Art/Import Completed Actions")]
        public static void ImportActions()
        {
            var catalog=JsonUtility.FromJson<Catalog>(File.ReadAllText(Root+"character-slices.json"));
            foreach(var entry in catalog.items.Where(s=>ExtraActions.ContainsKey(s.name)&&s.name!="Princess")){
                string path=Root+entry.name+".png";var imp=(TextureImporter)AssetImporter.GetAtPath(path);
                imp.spritesheet=entry.frames.Select((f,i)=>new SpriteMetaData{name=entry.name+"_"+i.ToString("D2"),rect=new Rect(f.x,f.y,f.width,f.height),alignment=9,pivot=new Vector2(f.pivotX,f.pivotY)}).ToArray();
                EditorUtility.SetDirty(imp);AssetDatabase.WriteImportSettingsIfDirty(path);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            }
            foreach(var pair in ExtraActions){
                string character=pair.Key;bool princess=character=="Princess";string sheet=princess?character:character+"Extras";
                var info=catalog.items.First(s=>s.name==sheet);var baseline=catalog.items.First(s=>s.name==character);
                string path=Root+sheet+".png";var imp=(TextureImporter)AssetImporter.GetAtPath(path);
                imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Multiple;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.maxTextureSize=4096;imp.alphaIsTransparency=true;
                imp.spritePixelsPerUnit=princess?100:100f*info.referenceHeight/baseline.referenceHeight;
                imp.spritesheet=info.frames.Select((f,i)=>new SpriteMetaData{name=sheet+"_"+i.ToString("D2"),rect=new Rect(f.x,f.y,f.width,f.height),alignment=9,pivot=new Vector2(f.pivotX,f.pivotY)}).ToArray();
                EditorUtility.SetDirty(imp);AssetDatabase.WriteImportSettingsIfDirty(path);AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                var sprites=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();
                string controllerPath="Assets/Art/NativeAnimations/"+character+".controller";
                var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath)??AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                var sm=controller.layers[0].stateMachine;
                for(int row=0;row<pair.Value.Length;row++){
                    string action=pair.Value[row],clipPath="Assets/Art/NativeAnimations/"+character+"_"+action+".anim";
                    var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,clipPath);}clip.frameRate=8;
                    AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},Enumerable.Range(0,4).Select(i=>new ObjectReferenceKeyframe{time=i/8f,value=sprites[row*4+i]}).ToArray());
                    var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=action=="idle"||action=="wallslide"||action=="fall";AnimationUtility.SetAnimationClipSettings(clip,settings);
                    var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="right"+action)??sm.AddState("right"+action);state.motion=clip;if(princess)sm.defaultState=state;
                }
                EditorUtility.SetDirty(controller);
            }
            // Preserve crisp alpha and pixels; existing materials remain compatible with RGBA sprites.
            foreach(string path in Directory.GetFiles(Root,"*.png"))if(ExtraActions.ContainsKey(Path.GetFileNameWithoutExtension(path))){
                var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureCompression=TextureImporterCompression.Uncompressed;imp.filterMode=FilterMode.Point;imp.alphaIsTransparency=true;EditorUtility.SetDirty(imp);imp.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();Debug.Log("BABLE_ACTIONS_IMPORTED");
        }
    }
}
