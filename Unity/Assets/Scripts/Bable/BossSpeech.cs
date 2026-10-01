using System.Collections;
using UnityEngine;
using Babel.Runtime.Characters.Player;
using Babel.Runtime.Combat;
namespace Bable
{
    [RequireComponent(typeof(BossBrain))]
    public sealed class BossSpeech : MonoBehaviour
    {
        BossBrain brain;HealthComponent health;bool introduced,phase;float nextLine;string lastState;int lineIndex;
        PlayerController2D heldPlayer;bool priorInvincible;
        string Prefix => "boss"+((int)brain.profile.kind+1);
        void Awake(){brain=GetComponent<BossBrain>();health=GetComponent<HealthComponent>();health.Died+=OnDeath;}
        void Update()
        {
            if(TowerLoading.Busy)return;
            if(Babel.Runtime.Core.GameSession.Instance!=null&&Babel.Runtime.Core.GameSession.Instance.IsPaused)return;
            if(brain.profile==null||health.CurrentHealth<=0)return;
            var player=FindFirstObjectByType<PlayerController2D>();if(player==null)return;
            if(brain.DialogueHold)brain.FaceDialogueTarget(player.transform);
            if(!introduced&&brain.CanStartEncounter(player)&&!TowerDialogue.StoryActive){introduced=true;StartCoroutine(Intro(player));return;}
            if(!brain.Engaged||TowerDialogue.StoryActive)return;
            if(brain.PhaseTwo&&!phase){phase=true;TowerDialogue.Speak(Prefix+"_phase",transform);nextLine=Time.time+10;}
            if(Time.time>=nextLine&&!TowerDialogue.IsSpeaking){
                bool attackLine=brain.State=="Windup"||brain.State=="Dig"||brain.State=="FlightAim"||brain.State=="Combo";
                TowerDialogue.Speak(Prefix+(attackLine?"_attack":"_taunt")+(lineIndex++%4+1),transform);nextLine=Time.time+Random.Range(7f,10f);
            }
            lastState=brain.State;
        }
        IEnumerator Intro(PlayerController2D player)
        {
            heldPlayer=player;priorInvincible=player.GetComponent<HealthComponent>().Invincible;
            brain.FaceDialogueTarget(player.transform);
            player.GetComponent<CharacterPresentation>()?.Act("idle",30);
            brain.SetDialoguePose(true);var combat=player.GetComponent<PlayerCombatController>();var hp=player.GetComponent<HealthComponent>();bool invincible=hp.Invincible;hp.Invincible=true;player.enabled=false;if(combat!=null)combat.enabled=false;player.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;
            if(brain.profile.kind==BossKind.Nero)yield return TowerDialogue.Exchange(transform,Prefix+"_intro",Prefix+"_allegory","pilgrim_nero",Prefix+"_reply");
            else yield return TowerDialogue.Exchange(transform,Prefix+"_intro",Prefix+"_allegory","pilgrim_reply");
            if(player!=null){hp.Invincible=invincible;player.enabled=true;if(combat!=null)combat.enabled=true;}
            brain.SetDialoguePose(false);nextLine=Time.time+6;
            player.GetComponent<CharacterPresentation>()?.ReleaseAction();heldPlayer=null;
        }
        void OnDeath(){if(brain.profile!=null)TowerDialogue.Speak(Prefix+"_death",transform);}
        void OnDisable(){if(heldPlayer!=null){StopAllCoroutines();TowerDialogue.AbortStory();heldPlayer.enabled=true;heldPlayer.GetComponent<HealthComponent>().Invincible=priorInvincible;var combat=heldPlayer.GetComponent<PlayerCombatController>();if(combat!=null)combat.enabled=true;heldPlayer.GetComponent<CharacterPresentation>()?.ReleaseAction();heldPlayer=null;}if(brain!=null)brain.SetDialoguePose(false);}
        void OnDestroy(){if(health!=null)health.Died-=OnDeath;}
    }
}

