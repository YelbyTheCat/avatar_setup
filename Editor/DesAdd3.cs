using UnityEngine;
using UnityEditor;

public class DesAdd3 : Editor
{
    public void Desadd3(GameObject o)
    {
        o.AddComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
        Debug.LogError("Component Added");
    }
}