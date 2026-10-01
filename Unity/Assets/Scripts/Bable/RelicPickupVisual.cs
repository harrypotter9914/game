using UnityEngine;
namespace Bable
{
    public sealed class RelicPickupVisual : MonoBehaviour
    {
        Transform art;Vector3 local;
        void Start(){art=transform.Find("Relic Visual");if(art!=null)local=art.localPosition;}
        void Update(){if(art!=null)art.localPosition=local+Vector3.up*Mathf.Sin(Time.time*2.2f)*.12f;}
    }
}
