using UnityEngine;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Core;
using Babel.Runtime.Combat;
namespace Bable {
 public sealed partial class BableGameUI {
  public void ChamberTools(){
   var lab=FindFirstObjectByType<RuneCombatLab>();var practice=FindFirstObjectByType<BossPractice>();
   if(lab==null&&practice==null){Pause();return;}
   Panel("tools",lab!=null?"THE SCRIBE'S WORKSHOP":"TRIALS OF THE GUARDIANS","NewArt/Release51/Panel");
   Label(overlay,"This chamber never changes your saved journey.",new Vector2(0,225),new Vector2(1180,45),22);
   Button(overlay,"RUNE REPOSITORY",new Vector2(-320,130),Runes,570,65);
   Button(overlay,"RESTORE VITALITY & SPIRIT",new Vector2(320,130),()=>{
    var player=FindFirstObjectByType<PlayerController2D>();player.GetComponent<HealthComponent>().Heal(999);GameSession.Instance.RestoreMana(999);
    Notice("Vitality and Spirit restored.",ChamberTools);
   },570,65);
   if(lab!=null){
    Button(overlay,"RESPAWN OPPONENTS",new Vector2(-320,20),()=>{lab.RespawnEnemies();Resume();},570,65);
    Button(overlay,"EMPTY REPOSITORY",new Vector2(320,20),()=>{session.RuneInventory.ResetInventory();ChamberTools();},570,65);
    Button(overlay,"RECOVER ALL EIGHT RUNES",new Vector2(-320,-90),()=>{lab.GrantRunes();ChamberTools();},570,65);
    Button(overlay,"TEST ALTAR REBIRTH",new Vector2(320,-90),Respawn,570,65);
   } else {
    var player=FindFirstObjectByType<PlayerController2D>();var hp=player.GetComponent<HealthComponent>();
    Button(overlay,"INVULNERABILITY: "+(practice.Invulnerable?"ON":"OFF"),new Vector2(-320,20),()=>{practice.Invulnerable=!practice.Invulnerable;ChamberTools();},570,65);
    Button(overlay,"RESTART THIS ROOM",new Vector2(320,20),()=>Load(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name),570,65);
    var boss=FindFirstObjectByType<BossBrain>();
    if(boss!=null&&boss.profile.kind==BossKind.Nero)Button(overlay,"TEST NERO'S SECOND PHASE",new Vector2(-320,-90),()=>{boss.GetComponent<HealthComponent>().Configure(boss.profile.health,Mathf.Max(1,boss.profile.health/2));Resume();},570,65);
    Button(overlay,"CHOOSE ANOTHER GUARDIAN",new Vector2(320,-90),Practice,570,65);
   }
   Button(overlay,"MAIN MENU",new Vector2(-320,-250),MainMenu,570,65);
   Button(overlay,"RETURN TO CHAMBER",new Vector2(320,-250),Resume,570,65);
  }
 }
}
