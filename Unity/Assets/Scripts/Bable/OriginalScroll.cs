using UnityEngine;
using Babel.Runtime.Characters.Player;
namespace Bable
{
    public sealed class OriginalScroll : MonoBehaviour
    {
        public string art;
        // Authored presentation metadata. Collection is owned by CollectiblePickup
        // so entering a trigger cannot open a second, competing scroll.
    }
}
