using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Presets;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.IO;

#if VRC_SDK_VRCSDK3 || VRC_SDK_VRCSDK2
using VRCSDK2;
#endif

public class Avatar_Setup : EditorWindow
{
    //Attributes
    string avatarName = "";
    GameObject avatarModel;
    Scene scene;
    //Advanced
    bool showPosition = false;
    bool pullVRCAnimationControllers = true;
    bool addFloor = true;
    bool addToActiveScene = false;
    bool overrideAvatarToggle = false;

    [MenuItem("Yelby/Avatar Setup")]
    public static void ShowWindow()
    {
        GetWindow<Avatar_Setup>("Avatar Setup");
    }

    void OnGUI()
    {
        //General Information
        GUILayout.Label("Version: 2.4");
        GUILayout.Label("Instructions"
                   + "\n 1) Name Avatar"
                   + "\n 2) Click or Drag FBX into box"
                   + "\n 3) Click Generate Model"
                   + "\n 4) Set Avatar to Humanoid and rig");


        //Get Avatar Name
        EditorGUIUtility.labelWidth = 90;
        avatarName = EditorGUILayout.TextField("Avatar Name: ", avatarName);

        //Get Avatar Model
        EditorGUILayout.BeginHorizontal();
        avatarModel = EditorGUILayout.ObjectField("Avatar FBX: ", avatarModel, typeof(GameObject), false) as GameObject;
        if (AssetDatabase.GetAssetPath(avatarModel).Contains(".fbx") == false) { avatarModel = null; }
        if (GUILayout.Button("Select"))//Select Avatar
        {
            avatarModel = Selection.activeGameObject;
            if (AssetDatabase.GetAssetPath(avatarModel).Contains(".fbx") == false) { avatarModel = null; }
        }
        EditorGUILayout.EndHorizontal();

        //Advanced
        showPosition = EditorGUILayout.Foldout(showPosition, "Advanced");
        if(showPosition)
        {
            EditorGUIUtility.labelWidth = 120;
            addFloor = EditorGUILayout.Toggle("Add Floor", addFloor);
            pullVRCAnimationControllers = EditorGUILayout.Toggle("Copy Controllers", pullVRCAnimationControllers);
            addToActiveScene = EditorGUILayout.Toggle("Active Scene", addToActiveScene);
            overrideAvatarToggle = EditorGUILayout.Toggle("Override", overrideAvatarToggle);
        }

        //Big setup button
        if (GUILayout.Button("Setup Avatar"))
        {
            if(overrideAvatarToggle != true)
            {
                if (avatarName == "")//Check if name is blank
                {
                    Debug.LogWarning("No Name");
                    EditorUtility.DisplayDialog("Avatar Failed", "Name Field is empty!", "Ok");
                    return;
                }
                else if (AssetDatabase.IsValidFolder("Assets/Avatars/" + avatarName) == true)//Check if name is taken
                {
                    Debug.LogWarning("Name Taken");
                    EditorUtility.DisplayDialog("Avatar Failed", "Name Taken!", "Ok");
                    return;
                }
            }
            
            if(!avatarModel == false)//If there is a model it'll run
            {
                if(overrideAvatarToggle) {overrideAvatar(avatarName);}
                createAvatarFolders(avatarName);
                moveAvatar(avatarModel, avatarName);
                importSettingsFromPreset(avatarModel);
                ExtractMaterials(AssetDatabase.GetAssetPath(avatarModel), "Assets/Avatars/" + avatarName + "/UMats/");
                createScene(avatarModel, avatarName);
                createAvatarPrefab(avatarModel, avatarName);
                if (pullVRCAnimationControllers) { pullVRCAnimations(avatarName); }
                if (addFloor) { createFloor(); }
                Debug.Log("Avatar Setup Finished");
                EditorUtility.DisplayDialog("Avatar Success", "Model Imported\nCheck Assets/Avatars/"+avatarName, "Ok");
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
    void createAvatarFolders(string avatarName)
    {
        //Create Avatars folder
        if(AssetDatabase.IsValidFolder("Assets/Avatars") == false)
        {
            AssetDatabase.CreateFolder("Assets", "Avatars");
        }
        //Create "AvatarName" Folder
        if(AssetDatabase.IsValidFolder("Assets/Avatars/"+avatarName) == false)
        {
            AssetDatabase.CreateFolder("Assets/Avatars",avatarName);
        }

        //Generate Sub-folders
        string[] subFoldersList = { "Animations", "FBX", "Prefabs", "Textures", "UMats" };
        CreateSubAvatarFolders(subFoldersList, avatarName);
        Debug.Log("Folders Generated");
    }
    void moveAvatar(GameObject avatarModel, string avatarName)
    {
        if(AssetDatabase.IsValidFolder("Assets/Avatars/" + avatarName + "/FBX") == true)
        {
            AssetDatabase.MoveAsset(AssetDatabase.GetAssetPath(avatarModel), "Assets/Avatars/" + avatarName + "/FBX/" + avatarName + ".fbx");
            Debug.Log("Avatar Moved");
        }
        else
        {
            Debug.LogError("Invalid Location");
            EditorUtility.DisplayDialog("Avatar Failed", "Folder Location Invalid", "Ok");
        }
        AssetDatabase.Refresh();
    }
    void importSettingsFromPreset(GameObject avatarModel)
    {
        var preset = AssetDatabase.LoadAssetAtPath<Preset>("Assets/Yelby/Programs/Avatar Setup/YelbyImportPreset.preset");
        if (preset.CanBeAppliedTo(AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(avatarModel))) == true)
        {
            preset.ApplyTo(AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(avatarModel)));
            Debug.Log("Settings Applied");
        }
        else
        {
            Debug.LogError("Can't Apply Preset");
            EditorUtility.DisplayDialog("Avatar Failed", "Avatar Invalid", "Ok");
        }
    }
    void createScene(GameObject avatarModel, string avatarName)
    {
        scene = SceneManager.GetActiveScene();
        if(addToActiveScene)
        {
            addCharacter(avatarModel);
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }
        else if (scene.name == "" || scene.name == "SampleScene")
        {
            addCharacter(avatarModel);
            EditorSceneManager.SaveScene(scene, "Assets/Avatars/"+avatarName+"/"+avatarName+".unity", false);
        }
        else
        {
            scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
            addCharacter(avatarModel);
            EditorSceneManager.SaveScene(scene, "Assets/Avatars/" + avatarName + "/" + avatarName + ".unity", false);
        }
    }
    void createAvatarPrefab(GameObject avatarModel, string avatarName)
    {
        GameObject prefab = GameObject.Find(avatarName);
#if VRC_SDK_VRCSDK3
            var avatarDescriptor = prefab.AddComponent<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
            var eyePosition = createSuggestedEyeLocation(avatarName);
            avatarDescriptor.ViewPosition = eyePosition;

            avatarDescriptor.lipSync = VRC.SDKBase.VRC_AvatarDescriptor.LipSyncStyle.VisemeBlendShape;
            Dictionary<string, GameObject> unpackedBones = UnpackBones(prefab, new Dictionary<string, GameObject>());
            if(unpackedBones.ContainsKey("Body"))
            {
                var skinMesh = unpackedBones["Body"].GetComponent<SkinnedMeshRenderer>();
                List<string> blendshapeNames = new List<string>();
                blendshapeNames.Add("-none-");
                for (int i = 0; i < skinMesh.sharedMesh.blendShapeCount; i++)
                {
                    var blendName = skinMesh.sharedMesh.GetBlendShapeName(i);
                    blendshapeNames.Add(blendName);
                }
                avatarDescriptor.VisemeSkinnedMesh = skinMesh;
            if (avatarDescriptor.VisemeBlendShapes == null || avatarDescriptor.VisemeBlendShapes.Length != (int)VRC.SDKBase.VRC_AvatarDescriptor.Viseme.Count)
                avatarDescriptor.VisemeBlendShapes = new string[(int)VRC.SDKBase.VRC_AvatarDescriptor.Viseme.Count];
            AutoDetectVisemes(avatarDescriptor, blendshapeNames);
            }
            
            else { Debug.LogError("Body doesn't exist"); }
#endif
#if VRC_SDK_VRCSDK2
            var AvatarDescriptor = prefab.AddComponent<VRC_AvatarDescriptor>();
            var eyePosition = createSuggestedEyeLocation(avatarName);
            AvatarDescriptor.ViewPosition = eyePosition;
        
#endif
        PrefabUtility.SaveAsPrefabAssetAndConnect(prefab, "Assets/Avatars/" + avatarName + "/Prefabs/" + avatarName + ".prefab", InteractionMode.AutomatedAction);
        AssetDatabase.Refresh();
        Debug.Log("Prefab Created");
    }
    void pullVRCAnimations(string avatarName)
    {
#if VRC_SDK_VRCSDK3
        string VRCFolderLocation = "Assets/VRCSDK/Examples3/Animation/Controllers/";
        string[] originalController =
        {
            "vrc_AvatarV3LocomotionLayer",  //Base
            "vrc_AvatarV3ActionLayer",      //Action
            "vrc_AvatarV3HandsLayer2",      //FX
            "vrc_AvatarV3SittingLayer",     //Sitting
            "vrc_AvatarV3UtilityTPose",     //TPose
            "vrc_AvatarV3UtilityIKPose",    //IK Pose
            "vrc_AvatarV3IdleLayer",        //Additive
            "vrc_AvatarV3HandsLayer",       //Gesture
        };
        string[] movedController =
        {
            "Base (Locomotion)",            //Base
            "Action (Major Animations)",    //Action
            "FX (Blends and Effects Only)", //FX
            "Sitting",                      //Sitting
            "TPose",                        //TPose
            "IKPose",                       //IK Pose
            "Additive (Original + this",    //Additive
            "Gesture (Specific Movements"   //Gesture
        };
        for(int i = 0;i<originalController.Length;i++)
        {
            AssetDatabase.CopyAsset(VRCFolderLocation + originalController[i] + ".controller",
                "Assets/Avatars/"+avatarName+"/Animations/VRC_Controllers/"+movedController[i]+"_"+avatarName+".controller");
        }
#endif
#if VRC_SDK_VRCSDK2
        AssetDatabase.CopyAsset("Assets/VRChat Examples/Examples2/Animation/SDK2/CustomOverrideEmpty.overrideController",
            "Assets/Avatars/"+avatarName+"/Animations/VRC_Controllers/"+ avatarName + "_controller.overrideController");
        AssetDatabase.CopyAsset("Assets/VRChat Examples/Examples2/Animation/SDK2/CustomOverrideEmpty.overrideController",
            "Assets/Avatars/" + avatarName + "/Animations/VRC_Controllers/"+"Sit_" + avatarName + "_controller.overrideController");
#endif
    }
    Vector3 createSuggestedEyeLocation(string avatarName)
    {
        var eyeBone = GameObject.Find(avatarName + "/Armature/Hips/Spine/Chest/Neck/Head/Eye_L");
        if(eyeBone == true)
        {
            //GameObject eyeLocation = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            //eyeLocation.name = "Suggested Eye Location";
            //eyeLocation.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            Vector3 eyeLocPos = new Vector3(0, eyeBone.transform.position.y, eyeBone.transform.position.z);
            //eyeLocation.transform.position = eyeLocPos;
            //EditorSceneManager.SaveScene(scene);
            return eyeLocPos;
        }
        return Vector3.zero;
    }
    void createFloor()
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
    void overrideAvatar(string avatarName)
    {
        AssetDatabase.DeleteAsset("Assets/Avatars/" + avatarName);
    }
    //~~~~Helper Methods~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    void CreateSubAvatarFolders(string[] subN, string avatarName)
    {
        for(int i = 0;i<subN.Length;i++)
        {
            if (AssetDatabase.IsValidFolder("Assets/Avatars/" + avatarName + "/" + subN[i]) == false)
            {
                AssetDatabase.CreateFolder("Assets/Avatars/" + avatarName, subN[i]);

                if (AssetDatabase.IsValidFolder("Assets/Avatars/" + avatarName + "/" + subN[i]) == true){ }
                else { Debug.LogError("Sub Folder: " + subN[i] + " Failed"); }
            }
        }
        if(AssetDatabase.IsValidFolder("Assets/Avatars/"+avatarName+"/Animations") == true)
        {
            AssetDatabase.CreateFolder("Assets/Avatars/" + avatarName + "/Animations", "VRC_Controllers");
        }
    }
    void addCharacter(GameObject avatarModel)
    {
        var avModel = Instantiate(avatarModel);
        avModel.name = avatarModel.name;
    }
    Dictionary<string, GameObject> UnpackBones(GameObject bone, Dictionary<string, GameObject> targetBones)
    {
        foreach (Transform child in bone.transform)
        {
            targetBones.Add(child.name, child.gameObject);
            targetBones = UnpackBones(child.gameObject, targetBones); //Recursion
        }
        return targetBones;
    }
    void AutoDetectVisemes(VRC.SDK3.Avatars.Components.VRCAvatarDescriptor avatarDescriptor, List<string> blendShapeNames)
    {
        // prioritize strict - but fallback to looser - naming and don't touch user-overrides

        List<string> blendShapes = new List<string>(blendShapeNames);
        blendShapes.Remove("-none-");

        for (int v = 0; v < avatarDescriptor.VisemeBlendShapes.Length; v++)
        {
            if (string.IsNullOrEmpty(avatarDescriptor.VisemeBlendShapes[v]))
            {
                string viseme = ((VRC.SDKBase.VRC_AvatarDescriptor.Viseme)v).ToString().ToLowerInvariant();

                foreach (string s in blendShapes)
                {
                    if (s.ToLowerInvariant() == "vrc.v_" + viseme)
                    {
                        avatarDescriptor.VisemeBlendShapes[v] = s;
                        goto next;
                    }
                }
                foreach (string s in blendShapes)
                {
                    if (s.ToLowerInvariant() == "v_" + viseme)
                    {
                        avatarDescriptor.VisemeBlendShapes[v] = s;
                        goto next;
                    }
                }
                foreach (string s in blendShapes)
                {
                    if (s.ToLowerInvariant().EndsWith(viseme))
                    {
                        avatarDescriptor.VisemeBlendShapes[v] = s;
                        goto next;
                    }
                }
                foreach (string s in blendShapes)
                {
                    if (s.ToLowerInvariant() == viseme)
                    {
                        avatarDescriptor.VisemeBlendShapes[v] = s;
                        goto next;
                    }
                }
                foreach (string s in blendShapes)
                {
                    if (s.ToLowerInvariant().Contains(viseme))
                    {
                        avatarDescriptor.VisemeBlendShapes[v] = s;
                        goto next;
                    }
                }
            next: { }
            }

        }

        //shouldRefreshVisemes = false;
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
