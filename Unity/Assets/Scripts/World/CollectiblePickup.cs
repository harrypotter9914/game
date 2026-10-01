using Babel.Runtime.Characters.Player;
using Babel.Runtime.Core;
using Babel.Runtime.Runes;
using Babel.Runtime.UI;
using UnityEngine;

namespace Babel.Runtime.World
{
    public sealed class CollectiblePickup : MonoBehaviour
    {
        private enum PickupKind
        {
            Heal,
            Mana,
            Rune,
            Ability,
            Checkpoint,
            Gold,
            Story,
        }

        [SerializeField] private PickupKind pickupKind;
        [SerializeField] private int amount = 1;
        [SerializeField] private RuneDefinition rune;
        [SerializeField] private AbilityId abilityId;
        [SerializeField] private bool destroyOnCollect = true;
        [SerializeField] private string storyId;
        [SerializeField] private string storyTitle;
        [TextArea]
        [SerializeField] private string storyMessage;
        [SerializeField] private float messageDuration = 4.5f;

        private Transform playerTransform;
        private bool collected;
        public string PersistentId {get;private set;}
        public bool RequiresInteraction=>pickupKind!=PickupKind.Gold&&!collected;
        public string DisplayTitle=>pickupKind==PickupKind.Rune&&rune!=null?rune.DisplayName:!string.IsNullOrEmpty(storyTitle)?storyTitle:pickupKind==PickupKind.Checkpoint?"Resting place":pickupKind.ToString();
        string Description=>pickupKind==PickupKind.Rune&&rune!=null?rune.Description:!string.IsNullOrEmpty(storyMessage)?storyMessage:pickupKind==PickupKind.Checkpoint?"Your return point is remembered. Your current vitality and spirit are preserved.":pickupKind==PickupKind.Ability?"A new power answers your vow: "+GetAbilityDisplayName(abilityId)+".":pickupKind==PickupKind.Heal?"Restored "+amount+" vitality.":pickupKind==PickupKind.Mana?"Restored "+amount+" spirit.":"A memory of Babel has been recovered.";
        private void Start(){PersistentId=Bable.CampaignStore.Identity(this);if(Bable.CampaignStore.IsCampaign&&Bable.CampaignStore.WasRemoved(PersistentId)){gameObject.SetActive(false);return;}var art=GetComponent<Bable.AnimatedTreasure>();if(art==null)art=gameObject.AddComponent<Bable.AnimatedTreasure>();art.coin=pickupKind==PickupKind.Gold;if(FindFirstObjectByType<Bable.PickupInteractor>()==null)new GameObject("Pickup interaction").AddComponent<Bable.PickupInteractor>();}

        public void ConfigureGoldAmount(int newAmount)
        {
            pickupKind = PickupKind.Gold;
            amount = Mathf.Max(1, newAmount);
        }

        public void ConfigureRune(RuneDefinition definition)
        {
            pickupKind = PickupKind.Rune;
            rune = definition;
        }

        public void ConfigureAbility(AbilityId ability, string title)
        {
            pickupKind = PickupKind.Ability;
            abilityId = ability;
            storyTitle = title;
        }

        public void ConfigureStory(string id, string title, string message, float duration = 4.5f)
        {
            pickupKind = PickupKind.Story;
            storyId = id;
            storyTitle = title;
            storyMessage = message;
            messageDuration = duration;
        }

        private void Update()
        {
            if (collected || pickupKind != PickupKind.Gold)
            {
                return;
            }

            var session = GameSession.Instance;
            if (session == null || session.IsPaused || session.CoinMagnetRadius <= 0f)
            {
                return;
            }

            if (playerTransform == null)
            {
                var player = FindObjectOfType<PlayerRuntimeState>();
                playerTransform = player != null ? player.transform : null;
            }

            if (playerTransform == null)
            {
                return;
            }

            var delta = playerTransform.position - transform.position;
            if (delta.sqrMagnitude <= session.CoinMagnetRadius * session.CoinMagnetRadius)
            {
                if (Bable.CombatGeometry.Clear(transform.position, playerTransform.position))
                    transform.position = Bable.TreasureClearance.Move(transform.position, playerTransform.position, 8f * Time.deltaTime);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if(collected||pickupKind!=PickupKind.Gold)return;
            var state = other.GetComponentInParent<PlayerRuntimeState>();
            if (state != null && !Bable.CombatGeometry.Clear(transform.position, state.transform.position)) return;
            Collect(state);
        }

        private void OnTriggerStay2D(Collider2D other) { OnTriggerEnter2D(other); }

        public void Interact(PlayerRuntimeState state){if(RequiresInteraction)Collect(state);}
        void Collect(PlayerRuntimeState state)
        {
            if(collected)return;
            if (state == null)
            {
                return;
            }

            var session = state.Session != null ? state.Session : GameSession.Instance;
            if (session == null)
            {
                return;
            }

            if (session.IsPaused) return;
            collected=true;Bable.CampaignStore.MarkRemoved(PersistentId);
            string scrollArt=GetComponent<Bable.OriginalScroll>()?.art;
            bool sword=pickupKind==PickupKind.Story&&name.IndexOf("Sword",System.StringComparison.OrdinalIgnoreCase)>=0;
            if(sword)scrollArt="hoxi Sword";
            if(pickupKind==PickupKind.Rune&&rune!=null){
                string id=rune.RuneId.ToString();scrollArt="hoxi "+(id.Equals("Rings",System.StringComparison.OrdinalIgnoreCase)?"rings":id);
            }
            if(!string.IsNullOrEmpty(scrollArt)){
                var journal=FindFirstObjectByType<Bable.ScrollJournal>();
                if(journal!=null)journal.Recover(scrollArt,false);
            }
            if(pickupKind!=PickupKind.Gold)Bable.BableGameUI.Instance?.QueuePickup(scrollArt,DisplayTitle,Description);
            if(pickupKind==PickupKind.Story&&name.IndexOf("Sword",System.StringComparison.OrdinalIgnoreCase)>=0&&!session.HasWeapon){
                var intro=state.GetComponent<Bable.WeaponAwakening>();if(intro==null)intro=state.gameObject.AddComponent<Bable.WeaponAwakening>();intro.Begin();
            }

            switch (pickupKind)
            {
                case PickupKind.Heal:
                    state.Health.Heal(amount);
                    ScreenMessagePresenter.ShowCenter($"Recovered {amount} HP");
                    break;
                case PickupKind.Mana:
                    state.Mana.Restore(amount);
                    ScreenMessagePresenter.ShowCenter($"Recovered {amount} MP");
                    break;
                case PickupKind.Rune:
                    Bable.CombatAudio.Play("star",transform.position,.65f);
                    session.RestoreMana(1);
                    session.CollectRune(rune);
                    break;
                case PickupKind.Ability:
                    Bable.CombatAudio.Play("star",transform.position,.8f);
                    session.UnlockAbility(abilityId);
                    break;
                case PickupKind.Checkpoint:
                    var marker = GetComponent<CheckpointMarker>();
                    var service = FindObjectOfType<CheckpointService>();
                    if (marker != null && service != null)
                    {
                        service.RegisterCheckpoint(marker);
                    }
                    else
                    {
                        session.SetRespawnPoint(transform.position);
                    }
                    break;
                case PickupKind.Gold:
                    session.AddGold(amount);
                    Bable.CombatAudio.Play("coin",transform.position,.75f);
                    ScreenMessagePresenter.ShowCenter($"Collected {amount} gold");
                    break;
                case PickupKind.Story:
                    Bable.CombatAudio.Play("scroll",transform.position,.55f);
                    session.RestoreMana(1);
                    var title = string.IsNullOrWhiteSpace(storyTitle) ? "Memory" : storyTitle;
                    break;
            }

            if (destroyOnCollect)
            {
                var art=GetComponent<Bable.AnimatedTreasure>();if(art!=null)art.Collect();else Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private static string GetAbilityDisplayName(AbilityId ability)
        {
            switch (ability)
            {
                case AbilityId.Shockwave:
                    return "Shockwave";
                case AbilityId.CrystalDash:
                    return "Crystal Dash";
                case AbilityId.WallJump:
                    return "Wall Jump";
                case AbilityId.DoubleJump:
                    return "Double Jump";
                default:
                    return ability.ToString();
            }
        }
    }
}
