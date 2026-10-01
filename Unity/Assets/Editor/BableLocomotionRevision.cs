using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
namespace Babel.EditorTools
{
    public static class BableLocomotionRevision
    {
        const string Root="Assets/Resources/Bable/NewArt/",Clips="Assets/Art/NativeAnimations/";
        public static void Apply()
        {
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(texture,File.ReadAllBytes(Root+"PilgrimLocomotion.png"));var pixels=texture.GetPixels32();
            for(int i=0;i<pixels.Length;i++)if(pixels[i].g>120&&pixels[i].g-Math.Max(pixels[i].r,pixels[i].b)>35)pixels[i]=new Color32(0,0,0,0);
            texture.SetPixels32(pixels);texture.Apply();File.WriteAllBytes(Root+"PilgrimLocomotion.png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.Refresh();
            var sheet=JsonUtility.FromJson<BableArtPolish.Catalog>(File.ReadAllText("../reference/revision4/locomotion-slices.json")).items[0];
            var meta=sheet.frames.Select((f,i)=>new SpriteMetaData{name="PilgrimLocomotion_"+i.ToString("D2"),rect=new Rect(f.x,f.y,f.width,f.height),alignment=9,pivot=new Vector2(.5f,0)}).ToArray();
            var jump=Import("PilgrimLocomotion",meta,100f*sheet.frames[19].height/164);
            var run=Import("PilgrimRun8",Enumerable.Range(0,8).Select(i=>new SpriteMetaData{name="PilgrimRun8_"+i.ToString("D2"),rect=new Rect(i*320,0,320,288),alignment=9,pivot=new Vector2(.5f,27f/288)}).ToArray(),100f*238/164);
            Clip("run",run,12,true);Clip("takeoff",new[]{jump[8],jump[9]},14,false);Clip("jump",new[]{jump[10],jump[11]},8,false);Clip("fall",new[]{jump[13],jump[14]},7,false);Clip("land",jump.Skip(16).Take(4).ToArray(),16,false);
            NormalizeTiming();
            AssetDatabase.SaveAssets();Debug.Log("Locomotion imported with exact frame timing");
        }
        public static void NormalizeTiming(){
            foreach(string id in AssetDatabase.FindAssets("t:AnimationClip",new[]{Clips.TrimEnd('/')})){
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(id));
                foreach(var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip)){
                    var keys=AnimationUtility.GetObjectReferenceCurve(clip,binding);if(keys.Length!=3&&keys.Length!=5&&keys.Length!=9)continue;
                    var settings=AnimationUtility.GetAnimationClipSettings(clip);bool extra=settings.loopTime?keys[keys.Length-1].value==keys[0].value:keys[keys.Length-1].value==keys[keys.Length-2].value;
                    if(!extra)continue;keys=keys.Take(keys.Length-1).ToArray();AnimationUtility.SetObjectReferenceCurve(clip,binding,keys);settings.stopTime=keys.Length/clip.frameRate;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);
                }
            }
        }
        static Sprite[] Import(string name,SpriteMetaData[] meta,float ppu){string path=Root+name+".png";var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Multiple;imp.spritePixelsPerUnit=ppu;imp.spritesheet=meta;imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.alphaIsTransparency=true;imp.maxTextureSize=4096;imp.SaveAndReimport();return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name).ToArray();}
        static void Clip(string action,Sprite[] frames,float fps,bool loop){string path=Clips+"Pilgrim_"+action+".anim";var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}clip.frameRate=fps;
            var keys=frames.Select((f,i)=>new ObjectReferenceKeyframe{time=i/fps,value=f}).ToArray();AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys);var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=loop;AnimationUtility.SetAnimationClipSettings(clip,settings);
            var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(Clips+"Pilgrim.controller");var sm=controller.layers[0].stateMachine;var state=sm.states.Select(s=>s.state).FirstOrDefault(s=>s.name=="right"+action)??sm.AddState("right"+action);state.motion=clip;EditorUtility.SetDirty(controller);
        }
    }
}
