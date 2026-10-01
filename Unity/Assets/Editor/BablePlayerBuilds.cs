using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BablePlayerBuilds {
 public static readonly string[] CampaignScenes={"Assets/Scenes/MainMenu.unity","Assets/Scenes/Gameplay_Main.unity"};
 public static readonly string[] PracticeScenes=new[]{"Assets/Scenes/MainMenu.unity"}
  .Concat(Enumerable.Range(1,5).Select(i=>"Assets/Scenes/Boss_Test_"+i+".unity"))
  .Concat(new[]{"Assets/Scenes/Rune_Combat_Lab.unity"}).ToArray();
 [MenuItem("Bable/Build/Build Both Windows Players")]
 public static void Both(){Campaign();Practice();}
 [MenuItem("Bable/Build/Campaign Windows Player")]
 public static void Campaign(){Build(false);}
 [MenuItem("Bable/Build/Practice Windows Player")]
 public static void Practice(){Build(true);}
 [MenuItem("Bable/Preview/Campaign Menus")]
 public static void PreviewCampaign(){SessionState.SetBool("Bable.PracticePreview",false);}
 [MenuItem("Bable/Preview/Practice Menus")]
 public static void PreviewPractice(){SessionState.SetBool("Bable.PracticePreview",true);}
 public static void Configure(){
  PlayerSettings.bundleVersion="0.53.0";PlayerSettings.resizableWindow=true;
  EditorBuildSettings.scenes=CampaignScenes.Select(p=>new EditorBuildSettingsScene(p,true)).ToArray();
  AssetDatabase.SaveAssets();
 }
 static void Build(bool practice){
  Configure();
  var output=Path.GetFullPath(practice?"../Builds/bable-practice/bable-practice.exe":"../Builds/bable/bable.exe");
  Directory.CreateDirectory(Path.GetDirectoryName(output));
  var originalProduct=PlayerSettings.productName;
  try {
   PlayerSettings.productName=practice?"bable-practice":"bable";
   var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
    scenes=practice?PracticeScenes:CampaignScenes,locationPathName=output,
    target=BuildTarget.StandaloneWindows64,options=BuildOptions.None,
    extraScriptingDefines=practice?new[]{"BABLE_PRACTICE"}:Array.Empty<string>()
   });
   if(report.summary.result!=BuildResult.Succeeded)throw new BuildFailedException("Build failed: "+output);
   File.WriteAllText(Path.Combine(Path.GetDirectoryName(output),"BUILD-INFO.txt"),
    "Babel "+Application.version+"\nFlavor: "+(practice?"Practice (no campaign)":"Campaign (no practice)")+"\nBuilt UTC: "+DateTime.UtcNow.ToString("o")+
    "\nShared code, prefabs, Boss profiles and combat parameters.\nScenes:\n"+string.Join("\n",practice?PracticeScenes:CampaignScenes)+"\n");
   Debug.Log("BABLE_BUILD_"+(practice?"PRACTICE":"CAMPAIGN")+"_Succeeded");
  } finally {PlayerSettings.productName=originalProduct;AssetDatabase.SaveAssets();}
 }
}
