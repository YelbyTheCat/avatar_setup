using UnityEngine;
using UnityEditor;
#if VRC_SDK_VRCSDK2
using VRCSDK2;
#endif


public class DesAdd2 : MonoBehaviour
{
    public void Desadd2(GameObject o)
    {
        o.AddComponent<VRC.SDKBase.VRC_AvatarDescriptor>();
        Debug.LogError("Component Added");
    }
}