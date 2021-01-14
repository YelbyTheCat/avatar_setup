using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Presets;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;

#if VRC_SDK_VRCSDK3 || VRC_SDK_VRCSDK2
using VRCSDK2;
#endif

public class Avatar_Setup : EditorWindow
{
    //Attributes
    string avName = "";
    GameObject avModel;
    Scene scene;
    
    [MenuItem("Yelby/Avatar Setup")]
    public static void ShowWindow()
    {
        GetWindow<Avatar_Setup>("Avatar Setup");
    }

    void OnGUI()
    {
        //General Information
        GUILayout.Label("Instructions"
                   + "\n 1) Name Avatar"
                   + "\n 2) Click or Drag FBX into box"
                   + "\n 3) Click Generate Model"
                   + "\n 4) Set Avatar to Humanoid and rig");


        //Get Avatar Name
        EditorGUIUtility.labelWidth = 90;
        avName = EditorGUILayout.TextField("Avatar Name: ", avName);

        //Get Avatar Model
        EditorGUILayout.BeginHorizontal();
        avModel = EditorGUILayout.ObjectField("Avatar FBX: ", avModel, typeof(GameObject), false) as GameObject;
        if (!AssetDatabase.GetAssetPath(avModel).Contains(".fbx")) { avModel = null; }
        if (GUILayout.Button("Select"))//Select Avatar
        {
            avModel = Selection.activeGameObject;
            if (!AssetDatabase.GetAssetPath(avModel).Contains(".fbx")) { avModel = null; }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Generate Model"))
        {
            if (avName == "")//Check if name is blank
            {
                Debug.LogWarning("No Name");
                EditorUtility.DisplayDialog("Avatar Failed", "Name Field is empty!", "Ok");
                return;
            }
            else if(AssetDatabase.IsValidFolder("Assets/Avatars/" + avName))//Check if name is taken
            {
                Debug.LogWarning("Name Taken");
                EditorUtility.DisplayDialog("Avatar Failed", "Name Taken!", "Ok");
                return;
            }
            if(!avModel == false)//If there is a model it'll run
            {
                createFolders(avName);
                moveAvatar(avModel, avName);
                importSettings(avModel);
                ExtractMaterials(AssetDatabase.GetAssetPath(avModel), "Assets/Avatars/" + avName + "/UMats/");
                createScene(avModel, avName);
                createPrefab(avModel, avName);
                pullVRCAnims(avName);
                createSugEyePos(avName);
                createFloor(avName);
                Debug.Log("Avatar Setup Finished");
                EditorUtility.DisplayDialog("Avatar Success", "Model Generated\nCheck Assets/Avatars/"+avName, "Ok");
            }
            else
            {
                Debug.LogWarning("No Model");
                EditorUtility.DisplayDialog("Avatar Failed", "No Model Selected", "Ok");
                return;
            }
        }
        /*if (GUILayout.Button("Check Comps"))
        {
            GameObject selection = Selection.activeGameObject;
            checkComps(selection);
        }*/
    }

    //~~~~Main Methods~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    void createFolders(string avN)
    {
        //Create Avatars folder
        if(!AssetDatabase.IsValidFolder("Assets/Avatars"))
        {
            AssetDatabase.CreateFolder("Assets", "Avatars");
           // Debug.Log("Folder Generated: Avatars");
        }
        //Create "AvatarName" Folder
        if(!AssetDatabase.IsValidFolder("Assets/Avatars/"+avN))
        {
            AssetDatabase.CreateFolder("Assets/Avatars",avN);

            if (AssetDatabase.IsValidFolder("Assets/Avatars/" + avN))
            { /*Debug.Log("Folder Generated: " + avN);*/}
            else { Debug.LogError("Folder Generation Failed"); }
        }

        //Generate Sub-folders
        string[] subFolders = { "Animations", "FBX", "Prefabs", "Textures", "UMats" };
        subFolder(subFolders, avN);
        Debug.Log("Folders Generated");
    }
    void moveAvatar(GameObject avM, string avN)
    {
        if(AssetDatabase.IsValidFolder("Assets/Avatars/" + avN + "/FBX"))
        {
            AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(avM), "Assets/Avatars/" + avN + "/FBX/" + avN + ".fbx");
            Debug.Log("Avatar Moved");
        }
        else
        {
            Debug.LogError("Invalid Location");
        }
        AssetDatabase.Refresh();
    }
    void importSettings(GameObject avM)
    {
        var preset = AssetDatabase.LoadAssetAtPath<Preset>("Assets/Yelby/Programs/Avatar Setup/YelbyImportPreset.preset");
        if (preset.CanBeAppliedTo(AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(avM))))
        {
            preset.ApplyTo(AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(avM)));
            Debug.Log("Settings Applied");
        }
        else
        {
            Debug.LogError("Can't Apply Preset");
        }
    }
    void createScene(GameObject avM, string avN)
    {
        scene = SceneManager.GetActiveScene();
        if (scene.name == "" || scene.name == "SampleScene")
        {
            addCharacter(avM);
            EditorSceneManager.SaveScene(scene, "Assets/Avatars/"+avN+"/"+avN+".unity", false);
        }
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
            addCharacter(avM);
            EditorSceneManager.SaveScene(scene, "Assets/Avatars/" + avN + "/" + avN + ".unity", false);
        }
    }
    void createPrefab(GameObject avM, string avN)
    {
        GameObject prefab = GameObject.Find(avN);
#if VRC_SDK_VRCSDK3
            prefab.AddComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
#endif
#if VRC_SDK_VRCSDK2
        prefab.AddComponent<VRC_AvatarDescriptor>();
#endif
        PrefabUtility.SaveAsPrefabAssetAndConnect(prefab, "Assets/Avatars/" + avN + "/Prefabs/" + avN + ".prefab", InteractionMode.AutomatedAction);
        Debug.Log("Prefab Created");
    }
    void pullVRCAnims(string avN)
    {
#if VRC_SDK_VRCSDK3
        string folder = "Assets/VRCSDK/Examples3/Animation/Controllers/";
        string[] oCont =
        {
            "vrc_AvatarV3LocomotionLayer",  //Base
            "vrc_AvatarV3ActionLayer",      //Action
            "vrc_AvatarV3HandsLayer2",      //FX
            "vrc_AvatarV3SittingLayer",     //Sitting
            "vrc_AvatarV3UtilityTPose",     //TPose
            "vrc_AvatarV3UtilityIKPose"     //IK Pose
        };
        string[] mCont =
        {
            "Base (Locomotion)",            //Base
            "Action (Major Animations)",    //Action
            "FX (Blends and Effects Only)", //FX
            "Sitting",                      //Sitting
            "TPose",                        //TPose
            "IKPose"                        //IK Pose
        };
        for(int i = 0;i<oCont.Length;i++)
        {
            AssetDatabase.CopyAsset(folder + oCont[i] + ".controller",
                "Assets/Avatars/"+avN+"/Animations/VRC_Controllers/"+mCont[i]+"_"+avN+".controller");
        }
#endif
#if VRC_SDK_VRCSDK2
        AssetDatabase.CopyAsset("Assets/VRChat Examples/Examples2/Animation/SDK2/CustomOverrideEmpty.overrideController",
            "Assets/Avatars/"+avN+"/Animations/VRC_Controllers/"+avN+"_controller.overrideController");
        AssetDatabase.CopyAsset("Assets/VRChat Examples/Examples2/Animation/SDK2/CustomOverrideEmpty.overrideController",
            "Assets/Avatars/" + avN + "/Animations/VRC_Controllers/"+"Sit_" + avN + "_controller.overrideController");
#endif
    }
    void createSugEyePos(string avN)
    {
        var eyeBone = GameObject.Find(avN + "/Armature/Hips/Spine/Chest/Neck/Head/Eye_L");
        if(eyeBone == true)
        {
            GameObject eyeLoc = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeLoc.name = "Suggested Eye Location";
            eyeLoc.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            Vector3 eyeLocPos = new Vector3(0, eyeBone.transform.position.y, eyeBone.transform.position.z);
            eyeLoc.transform.position = eyeLocPos;
            EditorSceneManager.SaveScene(scene);
        }
    }
    void createFloor(string avN)
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        EditorSceneManager.SaveScene(scene);
    }
    /*void checkComps(GameObject avM)
    {
        Component[] components = avM.GetComponents(typeof(Component));
        foreach (Component component in components)
        {
            Debug.Log(component.ToString());
        }
    }*/
    //~~~~Helper Methods~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    void subFolder(string[] subN, string avN)
    {
        for(int i = 0;i<subN.Length;i++)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Avatars/" + avN + "/" + subN[i]))
            {
                AssetDatabase.CreateFolder("Assets/Avatars/" + avN, subN[i]);

                if (AssetDatabase.IsValidFolder("Assets/Avatars/" + avN + "/" + subN[i])){/*Debug.Log("SubFolder Generated: " + subN[i]);*/}
                else { Debug.LogError("Sub Folder: " + subN[i] + " Failed"); }
            }
        }
        if(AssetDatabase.IsValidFolder("Assets/Avatars/"+avN+"/Animations"))
        {
            Debug.LogWarning("It Does exist");
            AssetDatabase.CreateFolder("Assets/Avatars/" + avN + "/Animations", "VRC_Controllers");
            if(AssetDatabase.IsValidFolder("Assets/Avatars/"+avN+"/Animations/VRC_Controllers"))
            {
                Debug.Log("It did a thing");
            }
        }
    }
    void addCharacter(GameObject avM)
    {
        var av = Instantiate(avM);
        av.name = avM.name;
    }

    //~~~~Online Helpers~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    public static void ExtractMaterials(string assetPath, string destinationPath)
    {
        HashSet<string> hashSet = new HashSet<string>();
        IEnumerable<Object> enumerable = from x in AssetDatabase.LoadAllAssetsAtPath(assetPath)
                                         where x.GetType() == typeof(Material)
                                         select x;
        foreach (Object item in enumerable)
        {
            string path = System.IO.Path.Combine(destinationPath, item.name) + ".mat";
            path = AssetDatabase.GenerateUniqueAssetPath(path);
            string value = AssetDatabase.ExtractAsset(item, path);
            if (string.IsNullOrEmpty(value))
            {
                hashSet.Add(assetPath);
            }
        }

        foreach (string item2 in hashSet)
        {
            AssetDatabase.WriteImportSettingsIfDirty(item2);
            AssetDatabase.ImportAsset(item2, ImportAssetOptions.ForceUpdate);
        }
        Debug.Log("Materials Pulled");
    }
}