using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using Bable;
using TMPro;
using Object=UnityEngine.Object;
public static class BableMenuVisualAudit
{
    public static void Run(){var h=new GameObject("Menu visual audit");Object.DontDestroyOnLoad(h);h.AddComponent<BableTestHost>().StartCoroutine(Test(h));}
    static IEnumerator Test(GameObject host)
    {
        var results=new List<string>();var ui=BableGameUI.Instance;yield return new WaitForSecondsRealtime(.5f);
        var ink=Object.FindFirstObjectByType<IlluminatedMenuPanel>();results.Add("Main ink mesh rendered: "+(ink.canvasRenderer.GetMesh().vertexCount>0));
        var button=GameObject.Find("START GAME").GetComponent<ManuscriptMenuButton>();button.OnPointerEnter(null);yield return new WaitForSecondsRealtime(.35f);BableVerification.Capture("revision24/main-menu-hover.png");
        ui.Begin();yield return new WaitForSecondsRealtime(1);var opening=Object.FindFirstObjectByType<TowerPrologue>();
        for(int i=0;i<7;i++){
            typeof(TowerPrologue).GetProperty("PageIndex").SetValue(opening,i);typeof(TowerPrologue).GetMethod("Show",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(opening,null);yield return new WaitForSecondsRealtime(1.2f);
            bool fits=true;foreach(var t in opening.GetComponentsInChildren<TMP_Text>()){t.ForceMeshUpdate();fits&=!t.isTextOverflowing;}
            results.Add("Page "+(i+1)+" captions fit: "+fits);BableVerification.Capture("revision24/prologue-"+(i+1)+".png");
        }
        opening.Skip();float end=Time.realtimeSinceStartup+20;while(Time.realtimeSinceStartup<end&&(BableGameUI.Instance==null||BableGameUI.Instance.IsTitleScene||!BableGameUI.Instance.Ready))yield return null;
        yield return new WaitForSecondsRealtime(1);ui=BableGameUI.Instance;ui.Pause();yield return new WaitForSecondsRealtime(.3f);ink=Object.FindFirstObjectByType<IlluminatedMenuPanel>();results.Add("Pause ink renders and stays translucent: "+(ink.canvasRenderer.GetMesh().vertexCount>0&&ink.color.a<.8f));BableVerification.Capture("revision24/pause.png");
        ui.Death();yield return new WaitForSecondsRealtime(.3f);BableVerification.Capture("revision24/death.png");ui.MainMenu();
        File.WriteAllText("../reference/revision24/visual-audit.txt",string.Join("\n",results.ToArray()));Object.Destroy(host);
    }
}
