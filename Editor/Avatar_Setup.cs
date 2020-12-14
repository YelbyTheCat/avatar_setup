using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.Presets;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System;
using Object = UnityEngine.Object;

public class Avatar_Setup : EditorWindow
{
    //Attributes
    string myString = "";
    string testString = "";
    public UnityEngine.Object source;
    GameObject avatar;
    Scene scene;
    bool addFloor = true;
    bool pullAnims = true;
    string sdkV = "1";
    
    [MenuItem("Yelby/Avatar Setup")]
    public static void ShowWindow()
    {
        GetWindow<Avatar_Setup>("Avatar Setup");
    }

    void OnGUI()
    {
        checkSDK();
        GUILayout.Label("SDK Version: " + sdkV);

        //Starting Instruction
        GUILayout.Label("How to Use", EditorStyles.boldLabel);
        GUILayout.Label("1. Import your avatar into unity\n" +
                        "2. Name the avatar below\n" +
                        "3. Drag the FBX into the box below\n" +
                        "4. Click Generate");

        //Text Field
        myString = EditorGUILayout.TextField("Name of Avatar", myString);
        testString = (myString + " bacon");
        
        //Object Field
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Drag Avatar Here: ");
        source = EditorGUILayout.ObjectField(source, typeof(GameObject), false);
        EditorGUILayout.EndHorizontal();

        //Checks
        addFloor = EditorGUILayout.Toggle("Floor Plane", addFloor);
        pullAnims = EditorGUILayout.Toggle("Pull VRC Animations", pullAnims);

        //Button
        if (GUILayout.Button("Generate"))
        {
            if (source != null)
            {
                if (myString == "")
                {
                    Debug.LogWarning("No Name");
                    EditorUtility.DisplayDialog("Avatar Failed", "Name Field is empty!", "Ok");
                    return;
                }
                if (!AssetDatabase.IsValidFolder("Assets/Avatars/" + myString))
                {
                    //General Stuff
                    CreateFolders();
                    Debug.Log("Folders Finished");
                    inspectorOptions();
                    Debug.Log("Properties Finished");
                    moveAvatar();
                    Debug.Log("Moving Avatar Finished");
                    createScene();
                    Debug.Log("Scene Creation Finished");
                    addDescriptor();
                    Debug.Log("Descriptor Finished");
                    grabAnimations();
                    Debug.Log("Animations Grabbed");

                    Selection.activeGameObject = null;
                    //Floor
                    if (addFloor)
                    {
                        generateFloor();
                        Debug.Log("Floor Generated");
                    }

                    Debug.Log("Avatar Generated");
                    EditorUtility.DisplayDialog("Avatar Success", "Avatar has finished generating!\nCheck your 'Avatars' folder", "Ok");
                    source = null;
                }
                else
                {
                    Debug.LogWarning("Name Taken");
                    EditorUtility.DisplayDialog("Avatar Failed", "Name already taken!\nTry another name.", "Ok");
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Avatar Failed", "Avatar import field empty!", "ok");
                Debug.LogWarning("No Avatar in Field");
            }

        }
        /*if(GUILayout.Button("Find Comps"))
        {
            checkComps();
        }*/
        

        //Other Items
        GUILayout.Label("Everything can't be automated.\nPlease follow these instructions", EditorStyles.boldLabel);
        GUILayout.Label("1. Set your avatar to humanoid\n" +
                        "2. Configure Your avatar\n"
                        );

    }
    //~~~~Methods~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
    void CreateFolders()
    {
        string folder = "";
        string newFolderPath = "";

        //Make avatar folder if avatar folder doesn't exist
        if (!AssetDatabase.IsValidFolder("Assets/Avatars"))
        {
            folder = AssetDatabase.CreateFolder("Assets", "Avatars");
            newFolderPath = AssetDatabase.GUIDToAssetPath(folder);
            Debug.Log("Avatars Folder Created");
        }
        //Make the <Character File> if it doesn't exist
        if (!AssetDatabase.IsValidFolder("Assets/Avatars/" + myString))
        {
            folder = AssetDatabase.CreateFolder("Assets/Avatars", myString);
            newFolderPath = AssetDatabase.GUIDToAssetPath(folder);
            Debug.Log(myString + " Folder Created");
        }

        //Creates Subfolder Names
        string[] subfoldernames = { "Animations", "FBX", "Prefabs", "Textures", "UMats" };
        string[] subfolders = new string[subfoldernames.Length];

        //Genterat subfolders
        for (int i = 0; i < subfoldernames.Length; i++)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Avatars/" + myString + "/" + subfoldernames[i]))
            {
                subfolders[i] = AssetDatabase.CreateFolder("Assets/Avatars/" + myString, subfoldernames[i]);
                newFolderPath = AssetDatabase.GUIDToAssetPath(subfolders[i]);
                Debug.Log("Subfolder " + subfoldernames[i] + " generated");
            }
        }
    }

    void moveAvatar()
    {
        //string oldpath = "Assets/" + source.name + ".fbx";
        string oldpath = AssetDatabase.GetAssetPath(source);
        string newpath = ("Assets/Avatars/" + myString + "/" + "FBX/" + myString + ".fbx");
        AssetDatabase.MoveAsset(oldpath, newpath);
        AssetDatabase.Refresh();
        Debug.Log("Avatar moved from " + oldpath + " to " + newpath);
    }

    void inspectorOptions()
    {
        //Load premade preset to avatar in "Assets/Yelby/Programs/Avatar Setup/ImportPreset.preset"
        string pSource = AssetDatabase.GetAssetPath(source);
        UnityEngine.Object nSource = AssetImporter.GetAtPath(pSource);
        var preset = AssetDatabase.LoadAssetAtPath<Preset>("Assets/Yelby/Programs/Avatar Setup/ImportPreset.preset");
        if (preset.CanBeAppliedTo(nSource))
        {
            preset.ApplyTo(nSource);
            Debug.Log("Preset Applied to " + source.name);
        }

        //Export Materials to "Assets/Avatars/ <avatar name> / UMats"
        string destinationPath = ("Assets/Avatars/" + myString + "/" + "UMats/");
        ExtractMaterials(pSource, destinationPath);
        preset.UpdateProperties(nSource);
    }

    void createScene()
    {
        scene = SceneManager.GetActiveScene();
        string destinationPath = ("Assets/Avatars/" + myString + "/" + myString + ".unity");

        if (scene.name == "" || scene.name == "SampleScene")
        {
            //Place avatar into scene at 0,0,0
            string nSource = ("Assets/Avatars/" + myString + "/FBX/" + myString + ".fbx");
            Object oSource = AssetDatabase.LoadAssetAtPath<Object>(nSource);
            var clone = Instantiate(oSource);
            clone.name = myString;
            fromPrefab();

            //Save scene
            EditorSceneManager.SaveScene(scene, destinationPath, false);
        }
        else
        {
            //Create scene with name of avatar and saved to "Assets/Avatars/ <Avatar Name>
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
            scene = newScene;
            //Place avatar into scene at 0,0,0
            string nSource = ("Assets/Avatars/" + myString + "/FBX/" + myString + ".fbx");
            Object oSource = AssetDatabase.LoadAssetAtPath<Object>(nSource);
            var clone = Instantiate(oSource);
            clone.name = myString;

            //Add Prefab to Scene
            fromPrefab();

            //Save scene
            EditorSceneManager.SaveScene(newScene, destinationPath, false);
        }
        Debug.Log("Scene Done");
    }

    void addDescriptor()
    {
        //Add avatar Descriptor to avatar
        doDescriptor();

        //Generate Suggested Eye position based off bone location
        avatar = GameObject.Find(myString + "/Armature/Hips/Spine/Chest/Neck/Head/Eye_L");
        if (avatar == true)
        {
            GameObject eyeloc = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eyeloc.name = "Suggested Eye Location";
            eyeloc.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);
            Vector3 eyelocpos = new Vector3(0, avatar.transform.position.y, avatar.transform.position.z);
            eyeloc.transform.position = eyelocpos;
            Debug.Log(eyeloc.name + " has been generated");
        }
        else
        {
            Debug.LogWarning("Eye Bone Doesn't Exist [/Armature/Hips/Spine/Chest/Neck/Head/Eye_L]");
        }

        EditorSceneManager.SaveScene(scene);
    }

    void generateFloor()
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        EditorSceneManager.SaveScene(scene);
    }

    void grabAnimations()
    {
        if (pullAnims)
        {
            if (sdkV == "3")
            {
                string folder = "Assets/VRCSDK/Examples3/Animation/Controllers";
                string[] original = {   "vrc_AvatarV3LocomotionLayer" ,   //1
                                        "vrc_AvatarV3IdleLayer" ,         //2
                                        "vrc_AvatarV3HandsLayer2",        //3
                                        "vrc_AvatarV3ActionLayer",        //4
                                        "vrc_AvatarV3HandsLayer2",        //5
                                        "vrc_AvatarV3SittingLayer"        //6
                                    };
                string[] moved = {  "Base (Locomotion)" ,               //1
                                    "Additive (Not Active)" ,           //2
                                    "Gesture (Specific Movements",      //3
                                    "Action (Dances)",                  //4
                                    "FX (Everything that isn't bone)",  //5
                                    "Sitting"                           //6
                             };
                for (int i = 0; i < original.Length; i++)
                {
                    AssetDatabase.CopyAsset(folder+"/" + original[i] + ".controller",
                                            "Assets/Avatars/" + myString + "/Animations/" + moved[i] + ".controller");
                }
                Debug.Log("SDK3 Animations Pulled");
            }
            else if (sdkV == "2")
            {
                string folder = "Assets/VRChat Examples/Examples2/Animation/SDK2";
                AssetDatabase.CopyAsset(folder + "/CustomOverrideEmpty.overrideController",
                                        "Assets/Avatars/" + myString + "/Animations/" + myString + ".overrideController");
                AssetDatabase.CopyAsset(folder + "/CustomOverrideEmpty.overrideController",
                                        "Assets/Avatars/" + myString + "/Animations/" + myString + "_sitting" + ".overrideController");
                Debug.Log("SDK2 Animations Pulled");
            }
            else
            {
                Debug.LogWarning("Folder Does Not Exist");
                return;
            }
        }
    }

    void checkSDK()
    {
        var sdk3 = System.IO.File.Exists("Assets/VRCSDK/Plugins/VRCSDK3A.dll");
        var sdk2 = System.IO.File.Exists("Assets/VRCSDK/Plugins/VRCSDK2.dll");
        if (sdk3)
        {
            sdkV = "3";
        }
        else if (sdk2)
        {
            sdkV = "2";
        }
        else
        {
            Debug.LogError("SDK Missing");
        }
    }

    void fromPrefab()
    {
        //Add Prefab to Scene
        avatar = GameObject.Find(myString);
        PrefabUtility.SaveAsPrefabAssetAndConnect(avatar, "Assets/Avatars/" + myString + "/Prefabs/" + myString + ".prefab", InteractionMode.AutomatedAction);
        //Object temp = AssetDatabase.LoadAssetAtPath<Object>("Assets/Avatars/" + myString + "/Prefabs/" + myString + ".prefab");
        //PrefabUtility.InstantiatePrefab(temp);
        AssetDatabase.Refresh();
        Debug.LogWarning("Prefab Added");
    }

    void doDescriptor()
    {
        if (sdkV == "3")
        {
            DesAdd3 ad3 = new DesAdd3();
            ad3.Desadd3(avatar);
        }
        else if (sdkV == "2")
        {
            DesAdd2 ad2 = new DesAdd2();
            ad2.Desadd2(avatar);
        }
    }

    void checkComps()
    {
        avatar = (GameObject)source;
        Component[] components = avatar.GetComponents(typeof(Component));
        foreach (Component component in components)
        {
            Debug.Log(component.ToString());
        }
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
    }
}