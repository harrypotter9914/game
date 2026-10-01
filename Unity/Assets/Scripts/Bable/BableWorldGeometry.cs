using UnityEngine;
using UnityEngine.Tilemaps;

namespace Bable
{
    [DefaultExecutionOrder(-1000)]
    public sealed class BableWorldGeometry : MonoBehaviour
    {
        void Start()
        {
            foreach(var map in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            {
                map.RefreshAllTiles();
                var collider=map.GetComponent<TilemapCollider2D>();
                if(collider!=null){collider.enabled=false;collider.enabled=true;collider.ProcessTilemapChanges();}
                var composite=map.GetComponent<CompositeCollider2D>();
                if(composite!=null)composite.GenerateGeometry();
                // Tile colours (including the lamp foothold masks) are authored in the scene.
            }
        }
    }
}
