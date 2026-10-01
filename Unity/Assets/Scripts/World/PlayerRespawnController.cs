using Babel.Runtime.Characters.Player;
using Babel.Runtime.Combat;
using Babel.Runtime.Core;
using UnityEngine;

namespace Babel.Runtime.World
{
    [RequireComponent(typeof(PlayerRuntimeState))]
    public sealed class PlayerRespawnController : MonoBehaviour
    {
        [SerializeField] private float respawnDelay = 0.25f;
        [SerializeField] private int manaRestoreOnRespawn = 0;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private float snapHeight = 6f;
        [SerializeField] private float snapDistance = 20f;

        private PlayerRuntimeState runtimeState;
        private HealthComponent health;
        private ManaComponent mana;
        private GameSession session;
        private Collider2D selfCollider;
        private bool respawning;

        private void Awake()
        {
            runtimeState = GetComponent<PlayerRuntimeState>();
            health = GetComponent<HealthComponent>();
            mana = GetComponent<ManaComponent>();
            selfCollider = GetComponent<Collider2D>();
            ResolveSession();
        }

        private void Start()
        {
            ResolveSession();
            SnapToGround();
        }

        private void Update()
        {
            ResolveSession();
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.Died += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Died -= HandleDeath;
            }
        }

        private void HandleDeath()
        {
            ResolveSession();
            if (respawning || session == null)
            {
                return;
            }

            if (session.ConsumeReviveIfAvailable())
            {
                runtimeState.SynchronizeFromSession();
                // Woman revives at the death position. A ray starting above the
                // player can otherwise teleport them onto a different floor.
                var body = GetComponent<Rigidbody2D>();
                if (body != null) body.linearVelocity = Vector2.zero;
                Bable.CombatAudio.Play("heal",transform.position,.65f);
                Babel.Runtime.UI.ScreenMessagePresenter.ShowCenter("WOMAN  |  LIFE RESTORED");
                return;
            }

            respawning = true;
            if (Bable.BableGameUI.Instance != null) Bable.BableGameUI.Instance.Death();
            else Invoke(nameof(Respawn), respawnDelay);
        }

        public void Respawn() { if(Bable.BableGameUI.Instance!=null)Bable.TowerLoading.Respawn();else Respawn(true); }

        public void Respawn(bool playRebirth)
        {
            GetComponent<HitRecoil>()?.Cancel();
            ResolveSession();
            if (session == null)
            {
                respawning = false;
                return;
            }

            transform.position = session.RespawnPoint;
            GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            var marker=FindFirstObjectByType<CheckpointService>()?.CurrentCheckpoint;
            var altar=marker!=null?marker.GetComponent<Bable.AltarCheckpoint>():null;
            if(altar!=null){var position=altar.PlayerPosition(GetComponent<PlayerController2D>());transform.position=position;GetComponent<Rigidbody2D>().position=position;}
            else SnapToGround();
            health.Configure(session.MaxHealth, session.MaxHealth);
            mana.Configure(session.MaxMana, session.MaxMana);
            session.SetHealth(health.CurrentHealth, health.MaxHealth);
            session.SetMana(mana.CurrentMana, mana.MaxMana);
            respawning = false;
            if(playRebirth && altar!=null){var rebirth=GetComponent<Bable.AltarRebirth>()??gameObject.AddComponent<Bable.AltarRebirth>();rebirth.Begin();}
        }

        private void ResolveSession()
        {
            session = runtimeState.Session != null ? runtimeState.Session : (GameSession.Instance != null ? GameSession.Instance : FindObjectOfType<GameSession>());
        }

        private void SnapToGround()
        {
            var origin = (Vector2)transform.position + Vector2.up * snapHeight;
            var hits = Physics2D.RaycastAll(origin, Vector2.down, snapDistance, groundMask);
            foreach (var hit in hits)
            {
                if (!Bable.TerrainMotion.Solid(hit.collider) || hit.collider == selfCollider || hit.collider.transform.IsChildOf(transform) || hit.collider.isTrigger)
                {
                    continue;
                }

                var extentsY = selfCollider != null ? selfCollider.bounds.extents.y : 0.8f;
                transform.position = new Vector3(transform.position.x, hit.point.y + extentsY + 0.02f, transform.position.z);
                return;
            }
        }
    }
}

