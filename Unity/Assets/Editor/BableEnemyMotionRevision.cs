using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace Babel.EditorTools
{
    public static class BableEnemyMotionRevision
    {
        const string Root="Assets/Resources/Bable/NewArt/";
        public static void Clean(string path){var t=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(t,File.ReadAllBytes(path));var p=t.GetPixels32();for(int i=0;i<p.Length;i++)if(p[i].g>120&&p[i].g-Math.Max(p[i].r,p[i].b)>35)p[i]=new Color32(0,0,0,0);
            for(int y=1;y<t.height-1;y++)for(int x=1;x<t.width-1;x++){int i=y*t.width+x;var c=p[i];if(c.a==0)continue;if((p[i-1].a==0||p[i+1].a==0||p[i-t.width].a==0||p[i+t.width].a==0)&&c.g>Math.Max(c.r,c.b)+10){c.g=Math.Max(c.r,c.b);p[i]=c;}}
            t.SetPixels32(p);t.Apply();File.WriteAllBytes(path,t.EncodeToPNG());UnityEngine.Object.DestroyImmediate(t);
        }
        public static void Apply(){
            Import("EnemyLocomotion",new[]{"MeleeGuard","ShieldGuard","RangedGuard","GiantGuard"},new[]{12f,10f,12f,8f});
            Import("BossLocomotion",new[]{"FirstPenitent","BuriedOne","SkyJudicator","Nero"},new[]{8f,12f,12f,10f});
            foreach(string name in new[]{"PilgrimRun8","PilgrimLocomotion","ShopIcons"})Clean(Root+name+".png");
            AssetDatabase.Refresh();BableLocomotionRevision.NormalizeTiming();AssetDatabase.SaveAssets();
        }
        static void Import(string atlas,string[] characters,float[] rates){
            string path=Root+atlas+".png";Clean(path);var source=new Texture2D(2,2,TextureFormat.RGBA32,false);ImageConversion.LoadImage(source,File.ReadAllBytes(path));
            var catalog=JsonUtility.FromJson<BableArtPolish.Catalog>(File.ReadAllText("../reference/revision4/"+atlas+"-slices.json")).items[0];
            var bases=JsonUtility.FromJson<BableArtPolish.Catalog>(File.ReadAllText(Root+"character-slices.json"));
            for(int row=0;row<4;row++){
                string name=characters[row];var frames=catalog.frames.Skip(row*8).Take(8).ToArray();int width=frames.Max(f=>f.width)+20,height=frames.Max(f=>f.height)+20;var texture=new Texture2D(width*8,height,TextureFormat.RGBA32,false);texture.SetPixels32(new Color32[width*8*height]);
                for(int i=0;i<8;i++){
                    // The rightmost cleaver touched the generated atlas edge; use the
                    // intact matching contact pose instead of importing a cut weapon.
                    var f=frames[atlas=="BossLocomotion"&&row==0&&i==7?3:i];var colors=Isolate(source.GetPixels(f.x,f.y,f.width,f.height),f.width,f.height);
                    if(atlas=="BossLocomotion"&&row==2&&i==2){
                        var reference=frames[0];var clean=Isolate(source.GetPixels(reference.x,reference.y,reference.width,reference.height),reference.width,reference.height);
                        for(int y=0;y<52;y++)for(int x=0;x<65;x++){
                            int targetX=Mathf.RoundToInt(f.width*.52f)+x,targetY=f.height-1-y;int sourceX=Mathf.RoundToInt(reference.width*.52f)+x,sourceY=reference.height-1-y;
                            if(targetX<f.width&&sourceX<reference.width&&targetY>=0&&sourceY>=0)colors[targetY*f.width+targetX]=clean[sourceY*reference.width+sourceX];
                        }
                    }
                    if(i>=4)for(int y=0;y<f.height*.43f;y++)for(int x=0;x<f.width;x++){
                        int k=y*f.width+x;var c=colors[k];if(c.a>.5f&&c.r>c.g*1.03f&&c.r<c.g*2.5f&&c.r>c.b*1.15f){float shade=x>f.width*.51f?.7f:1.2f;colors[k]=new Color(Mathf.Min(1,c.r*shade),Mathf.Min(1,c.g*shade),Mathf.Min(1,c.b*shade),c.a);}
                    }
                    texture.SetPixels(i*width+(width-f.width)/2,10,f.width,f.height,colors);
                }
                texture.Apply();string output=Root+name+"Locomotion.png";File.WriteAllBytes(output,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(output,ImportAssetOptions.ForceUpdate);
                var imp=(TextureImporter)AssetImporter.GetAtPath(output);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Multiple;imp.spritePixelsPerUnit=100f*(height-20)/bases.items.First(s=>s.name==name).referenceHeight;imp.spritesheet=Enumerable.Range(0,8).Select(i=>new SpriteMetaData{name=name+"Locomotion_"+i,rect=new Rect(i*width,0,width,height),alignment=9,pivot=new Vector2(.5f,10f/height)}).ToArray();imp.filterMode=FilterMode.Point;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.alphaIsTransparency=true;imp.maxTextureSize=4096;imp.SaveAndReimport();
                var sprites=AssetDatabase.LoadAllAssetsAtPath(output).OfType<Sprite>().OrderBy(s=>s.name).ToArray();var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Art/NativeAnimations/"+name+"_run.anim");clip.frameRate=rates[row];var keys=sprites.Select((s,i)=>new ObjectReferenceKeyframe{time=i/rates[row],value=s}).ToArray();AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys);var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=true;AnimationUtility.SetAnimationClipSettings(clip,settings);EditorUtility.SetDirty(clip);
            }UnityEngine.Object.DestroyImmediate(source);
        }
        static Color[] Isolate(Color[] colors,int width,int height){
            var visited=new bool[colors.Length];var queue=new Queue<int>();var largest=new List<int>();
            for(int seed=0;seed<colors.Length;seed++){
                if(visited[seed]||colors[seed].a<.05f)continue;var component=new List<int>();visited[seed]=true;queue.Enqueue(seed);
                while(queue.Count>0){int index=queue.Dequeue();component.Add(index);int x=index%width,y=index/width;
                    for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++){int nx=x+dx,ny=y+dy;if(nx<0||ny<0||nx>=width||ny>=height)continue;int k=ny*width+nx;if(!visited[k]&&colors[k].a>=.05f){visited[k]=true;queue.Enqueue(k);}}
                }if(component.Count>largest.Count)largest=component;
            }
            var keep=new bool[colors.Length];foreach(int i in largest)keep[i]=true;for(int i=0;i<colors.Length;i++)if(!keep[i])colors[i]=Color.clear;return colors;
        }
    }
}
