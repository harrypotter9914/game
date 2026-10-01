using UnityEngine;
namespace Bable { public sealed class MenuMaterialLifetime : MonoBehaviour { public Material material; void OnDestroy(){if(material!=null)Destroy(material);} } }
