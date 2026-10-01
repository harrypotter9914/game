using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using UnityEngine;
using TMPro;
using Bable;
using Babel.Runtime.Core;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Characters.Enemies;
using Babel.Runtime.Combat;
public static class BableEleventhTests {
 static List<string> checks=new(),failures=new();
 static BindingFlags flags=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Check(string name,bool pass){checks.Add(name);if(!pass)failures.Add(name);File.WriteAllText("../reference/revision11/tests.json",JsonUtility.ToJson(new BableVerification.Report{checks=checks.ToArray(),failures=failures.ToArray()},true));}
 static void Clear(NarrativeGuidance g){((IList)typeof(NarrativeGuidance).GetField("queue",flags).GetValue(g)).Clear();typeof(NarrativeGuidance).GetField("current",flags).SetValue(g,null);}
 public static void Run(){if(!UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Play Mode required");checks.Clear();failures.Clear();new GameObject("Guidance verification").AddComponent<BableTestHost>().StartCoroutine(Test());}
 static IEnumerator Test(){
  BableGameUI.Instance.Begin();yield return new WaitForSeconds(.6f);
  var g=NarrativeGuidance.Instance;var s=GameSession.Instance;var p=Object.FindFirstObjectByType<PlayerController2D>();p.SetTestInput(0,0);p.GetComponent<HealthComponent>().Invincible=true;
  foreach(var speech in Object.FindObjectsByType<BossSpeech>(FindObjectsSortMode.None))speech.enabled=false;
  foreach(var enemy in Object.FindObjectsByType<EnemyControllerBase>(FindObjectsSortMode.None))enemy.enabled=false;
  TowerDialogue.AbortStory();yield return new WaitForSeconds(.5f);
  Check("Initial objective points to unarmed sword ledge",g.CurrentId=="seek_blade"&&g.Stage==0);yield return new WaitForSeconds(.5f);BableVerification.Capture("revision11/initial-guidance.png");
  Check("Both themed TMP fonts load",Resources.Load<TMP_FontAsset>("Bable/GuideFonts/CinzelDecorative-Regular SDF")!=null&&Resources.Load<TMP_FontAsset>("Bable/GuideFonts/CrimsonText-Regular SDF")!=null);
  Check("Frame loaded as a sprite",Resources.Load<Sprite>("Bable/NewArt/GuidanceFrame")!=null);
  Check("Guide never captures pointer input",Object.FindObjectsByType<UnityEngine.UI.Graphic>(FindObjectsSortMode.None).Where(x=>x.transform.IsChildOf(g.transform)).All(x=>!x.raycastTarget));
  int count=g.PendingCount;Check("Once-only hint does not enqueue twice",!g.Enqueue("seek_blade")&&g.PendingCount==count);
  Clear(g);p.GetComponent<Rigidbody2D>().simulated=false;s.UnlockWeapon();yield return new WaitForSeconds(.7f);Check("Sword unlock automatically posts next story objective",g.CurrentId=="blade"&&g.Stage==1);BableVerification.Capture("revision11/blade-guidance.png");
  TowerDialogue.Speak("merchant_hint");if(!TowerDialogue.IsSpeaking)TowerDialogue.Speak("boss1_intro");yield return null;Check("Spoken dialogue suppresses story panel",g.PanelAlpha==0);TowerDialogue.AbortStory();yield return new WaitForSeconds(.2f);Check("Panel resumes after dialogue",g.PanelAlpha>0);
  BableGameUI.Instance.Pause();yield return null;Check("Pause hides guide canvas",!g.transform.Find("Babel story guidance").GetComponent<Canvas>().enabled);BableGameUI.Instance.Resume();
  Clear(g);p.transform.position=new Vector3(195,-43,0);yield return new WaitForSeconds(.6f);Check("Eastern seal warns before obtaining Shockwave",g.HasSeen("sealed_east"));
  var kinds=new[]{BossKind.Shockwave,BossKind.Burrow,BossKind.Aerial,BossKind.Crystal};var ids=new[]{"shockwave","walljump","doublejump","dash"};
  p.transform.position=new Vector3(-24,-2,0);
  for(int i=0;i<kinds.Length;i++){
   Clear(g);TowerDialogue.AbortStory();var boss=Object.FindObjectsByType<BossBrain>(FindObjectsSortMode.None).First(b=>b.profile.kind==kinds[i]);boss.GetComponent<HealthComponent>().Invincible=false;boss.GetComponent<HealthComponent>().ApplyDamage(9999);yield return null;Check("Reward waits for death speech "+ids[i],TowerDialogue.IsSpeaking&&g.PanelAlpha==0);float deadline=Time.time+15;while(TowerDialogue.IsSpeaking&&Time.time<deadline)yield return null;yield return new WaitForSeconds(.7f);
   Check("Actual boss defeat posts "+ids[i],g.CurrentId==ids[i]&&g.Stage==i+2);Check("Recall objective matches "+ids[i],g.ObjectiveId==ids[i]);
   if(i==0)BableVerification.Capture("revision11/shockwave-guidance.png");
  }
  Clear(g);g.Recall();yield return new WaitForSeconds(.7f);Check("G recall can restore current route",g.CurrentId=="dash");BableVerification.Capture("revision11/crown-guidance.png");
  Clear(g);Object.FindFirstObjectByType<PrincessRescue>().Unlock();yield return new WaitForSeconds(.7f);Check("Final gate produces rescue direction",g.HasSeen("dawn")&&g.ObjectiveId=="dawn");
  var lines=JsonUtility.FromJson<TowerDialogue.Script>(Resources.Load<TextAsset>("Bable/Dialogue/lines").text).lines;Check("All 65 voiced script lines load",lines.Length==65&&lines.All(l=>Resources.Load<AudioClip>("Bable/Dialogue/Voices/"+l.id)!=null));
  Clear(g);Babel.Runtime.UI.ScreenMessagePresenter.ShowCenter("A Test Memory\nThe builders left a promise in these stones.",4);TowerDialogue.AbortStory();yield return new WaitForSeconds(.7f);Check("Existing narrative messages use new frame",g.CurrentId=="notice");
  var labels=g.GetComponentsInChildren<TMP_Text>();Check("Current text stays inside layout",labels.All(t=>!t.isTextOverflowing));
  File.WriteAllText("../reference/revision11/finished.txt",failures.Count+" failures / "+checks.Count+" checks");
 }
}
