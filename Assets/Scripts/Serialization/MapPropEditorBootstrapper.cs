using UnityEngine;

namespace Assets.Scripts.Serialization
{
    // Attach to a scene object and point to the Props root and map path.
    public sealed class MapPropEditorBootstrapper : MonoBehaviour
    {
        [SerializeField] private Transform propsRoot;
        [SerializeField] private string mapPath = "Maps/Test/Test";
        [SerializeField] private bool loadOnStart = true;
        [SerializeField] private bool clearExisting = true;

        private void Start()
        {
            if (loadOnStart && propsRoot != null && !string.IsNullOrWhiteSpace(mapPath))
            {
                PropMapIO.LoadInto(propsRoot, mapPath, clearExisting);
            }
        }

        [ContextMenu("Load Map Now")]
        private void LoadNow()
        {
            if (propsRoot == null) return;
            PropMapIO.LoadInto(propsRoot, mapPath, clearExisting: true);
        }
    }
}