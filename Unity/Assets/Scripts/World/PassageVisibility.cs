using UnityEngine;
namespace Babel.Runtime.World {
    // Keep the serialized name for existing scenes. Zoom is authored by region;
    // walls, broken gates and nearby props never change the ordinary view.
    [RequireComponent(typeof(Camera))]
    public class PassageVisibility : MonoBehaviour {
        public Transform target;
        public float minimumSize = 3.5f, maximumSize = 7;
        [SerializeField] Rect labyrinth = new Rect(257, -88, 82, 54);
        Camera view;
        Rect[] bossRooms;
        bool initialized, inLabyrinth;
        public bool InLabyrinth => inLabyrinth;
        public float DesiredSize => inLabyrinth ? minimumSize : maximumSize;
        public float VisibleBottom => view.transform.position.y - view.orthographicSize;
        public float VisibleTop => view.transform.position.y + view.orthographicSize;
        public bool IsCellVisible(Vector3Int cell) => true;

        void Awake() { view = GetComponent<Camera>(); }
        void Start() { ResetTracking(); }

        bool ContainsLabyrinth(Vector2 position, bool keepClose) {
            if (gameObject.scene.name != "Gameplay_Main") return false;
            var area = labyrinth;
            // Separate enter/exit edges prevent repeated zoom changes when walking on a seam.
            if (keepClose) { area.xMin -= .8f; area.xMax += .8f; area.yMin -= .8f; area.yMax += .8f; }
            if (!area.Contains(position)) return false;
            foreach (var authoredRoom in bossRooms) {
                var room = authoredRoom;
                if (!keepClose) { room.xMin -= .8f; room.xMax += .8f; room.yMin -= .8f; room.yMax += .8f; }
                if (room.Contains(position)) return false;
            }
            return true;
        }

        public void ResetTracking() {
            if (view == null) view = GetComponent<Camera>();
            if (bossRooms == null) {
                var bosses = FindObjectsByType<Bable.BossBrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                bossRooms = new Rect[bosses.Length];
                for (int i = 0; i < bosses.Length; i++)
                    bossRooms[i] = new Rect(bosses[i].arenaCenter - bosses[i].arenaSize * .5f, bosses[i].arenaSize);
            }
            initialized = false;
            if (target == null) return;
            inLabyrinth = ContainsLabyrinth(target.position, false);
            view.orthographicSize = DesiredSize;
            initialized = true;
        }

        void Update() {
            if (target == null || Time.timeScale <= 0 || Time.deltaTime <= 0 ||
                Core.GameSession.Instance != null && Core.GameSession.Instance.IsPaused) return;
            if (!initialized) { ResetTracking(); return; }
            inLabyrinth = ContainsLabyrinth(target.position, inLabyrinth);
            view.orthographicSize = Mathf.MoveTowards(view.orthographicSize, DesiredSize, .8f * Time.deltaTime);
        }
    }
}
