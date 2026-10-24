using UnityEngine;

namespace Babel.Runtime.World {
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow2D : MonoBehaviour {
        [SerializeField] Transform target;
        [SerializeField] Vector3 offset = new Vector3(0, 0, -10);
        [SerializeField] Vector2 deadZoneSize = new Vector2(.24f, .18f);
        Camera view;
        Vector3 trackedPosition, dampVelocity;
        bool initialized;

        public Vector2 DeadZoneSize => deadZoneSize;
        Vector3 FramingOffset => offset + Vector3.up * (view.orthographicSize * .16f);
        Vector2 HalfDeadZone => new Vector2(view.orthographicSize * view.aspect * deadZoneSize.x,
                                            view.orthographicSize * deadZoneSize.y);

        void Awake() { view = GetComponent<Camera>(); }

        void Start() {
            var zoom = GetComponent<PassageVisibility>() ?? gameObject.AddComponent<PassageVisibility>();
            zoom.target = target;
            if (!initialized) SettleAtTarget();
        }

        void LateUpdate() {
            if (target == null) return;
            if (!initialized) SettleAtTarget();
            if (Time.timeScale > 0 && Time.deltaTime > 0 &&
                (Core.GameSession.Instance == null || !Core.GameSession.Instance.IsPaused)) {
                Vector3 anchor = target.position + FramingOffset;
                Vector2 half = HalfDeadZone;
                Vector3 desired = trackedPosition;
                // Follow only the part of the movement that crosses the rectangle.
                // A turn or a short jump inside it must not recenter the camera.
                desired.x += OutsideDistance(anchor.x - trackedPosition.x, half.x);
                desired.y += OutsideDistance(anchor.y - trackedPosition.y, half.y);
                desired.z = anchor.z;
                if ((desired - trackedPosition).sqrMagnitude < .0000001f) dampVelocity = Vector3.zero;
                else {
                    // Catch up sooner near the screen edge, especially during Crystal Dash.
                    // Ordinary motion still uses the gentler damping and the same dead zone.
                    float extent = Mathf.Max(Mathf.Abs(anchor.x - trackedPosition.x) / (view.orthographicSize * view.aspect),
                                             Mathf.Abs(anchor.y - trackedPosition.y) / view.orthographicSize);
                    float smoothing = Mathf.Lerp(.22f, .075f, Mathf.InverseLerp(.45f, .8f, extent));
                    trackedPosition = Vector3.SmoothDamp(trackedPosition, desired, ref dampVelocity,
                                                        smoothing, Mathf.Infinity, Time.deltaTime);
                }
            }
            // Damage shake is presentation only: it cannot move the dead zone or accumulate drift.
            transform.position = trackedPosition + Bable.PlayerHitFeedback.CameraOffset;
        }

        static float OutsideDistance(float distance, float halfSize) =>
            distance - Mathf.Clamp(distance, -halfSize, halfSize);

        public void SettleAtTarget() {
            if (target == null) return;
            if (view == null) view = GetComponent<Camera>();
            dampVelocity = Vector3.zero;
            trackedPosition = target.position + FramingOffset;
            transform.position = trackedPosition;
            initialized = true;
        }

        public void SetTarget(Transform value) {
            target = value;
            var zoom = GetComponent<PassageVisibility>();
            if (zoom != null) { zoom.target = target; zoom.ResetTracking(); }
            SettleAtTarget();
        }
    }
}
