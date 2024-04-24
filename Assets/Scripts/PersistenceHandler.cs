// Will not need save and load functions in the future
// so consider removing this class entirely and moving the create
// object functionality to the tracket object class


using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;

[System.Serializable]
public class ObjectData
{
    public string id;
    public string objectType; // New field to store the type of object
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
}

/*[System.Serializable]
public class SaveData
{
    public List<ObjectData> objects;
}
*/

//   NOT REALLY USING THIS AT THE MOMENT
//    TODO:  GET RID OF THIS! We are now loading from the network controller

public class PersistenceHandler : MonoBehaviour
{
    public string saveFileName = "savefile.json";
    public GameObject quadPrototype; // Reference to the prototype object in the scene - This is public and set in the editor, set prototype object inactive in the scene
    public GameObject meshPrototype; //prototypes might not be needed right now, since i have it loading locally 

    private const byte OBJECT_NONE = 0;
    private const byte OBJECT_VIDEO_STREAM = 1;
    private const byte OBJECT_MESH = 2;
    public byte byteObjectType = OBJECT_NONE; //might want to make this unserializable so not able to change in editor
    //private int streamCount = 0;
    TextMeshPro uiText;
    ObjectList list;
    private void Start()
    {
        //finds object in scene with name and then gets component of that object
        uiText = GameObject.Find("DynamicDescription").GetComponent<TextMeshPro>();
        list = GameObject.Find("ObjectList").GetComponent<ObjectList>();
        //LoadOnStartup();
        //StartCoroutine(AutoSave());
    }

    //CreateObject is the only method referenced outside of Persistence handler
    //could probably be added to network controller to get rid of this script

    //Called from network controller when host transmits saved objects 
    
    // Method to dynamically create an object based on its type
    public GameObject CreateObject(string objectType, string id, int streamID)
    {
        // Use the provided ID or generate a new one if none is provided or if the one provided is null
        //originally, in argument "string id = null" but this resulted in multiple id's being created for a single object
        string objectId = id ?? Guid.NewGuid().ToString();

        GameObject obj = null;

        // Clone the prototype object
        if ("VideoPanel".Equals(objectType))
        {
            obj = Instantiate(quadPrototype);
            byteObjectType = OBJECT_VIDEO_STREAM;
        }
        else if ("MeshObject".Equals(objectType))
        {
            obj = Instantiate(meshPrototype);
            byteObjectType = OBJECT_MESH; 
        }
        else
        {
            Debug.LogError("Unknown object type: " + objectType);
            return null;
        }

        obj.name = objectType + objectId;
        Debug.Log(obj.name);
        //list.Add(obj);  
        // Reset the object's transformations or other properties
        obj.transform.position = Vector3.zero;
        obj.transform.rotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        // Activate the object if the prototype was inactive
        obj.SetActive(true);

       // Add TrackedObject component and set its ID
       
        TrackedObject trackedObject = obj.AddComponent<TrackedObject>();
        trackedObject.id = objectId;
        trackedObject.object_type = byteObjectType;
        //streamCount++;
        trackedObject.stream_id = streamID;        // Set to first stream for now, make dynamic later!!!!!!!!!!!!!!

        list.Add(obj); //for associated info
        return obj;
    }


    /*public void Save()
    {
        List<ObjectData> objectDataList = new List<ObjectData>();

        // Following will save all objects with TrackedObject, regardless of whether they are active or not
        // This may not be desired functionality later
        foreach (var trackedObject in FindObjectsOfType<TrackedObject>())
        {
            GameObject obj = trackedObject.gameObject;
            ObjectData objectData = new ObjectData
            {
                id = trackedObject.id,
                position = obj.transform.position,
                rotation = obj.transform.rotation,
                scale = obj.transform.localScale,
                objectType = obj.name.Contains("VideoPanel") ? "VideoPanel" : "Unknown" //SHOULD CHANGE THIS
            };

            objectDataList.Add(objectData);
        }

        SaveData saveData = new SaveData { objects = objectDataList };
        string json = JsonUtility.ToJson(saveData, true);

        // Combine the persistent data path with your file name
        string filePath = Path.Combine(Application.persistentDataPath, saveFileName);

        // Write the JSON to the file
        System.IO.File.WriteAllText(filePath, json);
    }


    public void Load()
    {
        bool loadedAnyObjects = false;

        // Combine the persistent data path with your file name
        string filePath = Path.Combine(Application.persistentDataPath, saveFileName);

        if (System.IO.File.Exists(filePath))
        {
            try     // This will allow continue if JSON is corrupted or does not contain valid data
            {
                // Read the JSON from the file
                string json = System.IO.File.ReadAllText(filePath);
                SaveData saveData = JsonUtility.FromJson<SaveData>(json);

                foreach (var objectData in saveData.objects)
                {
                    GameObject obj = CreateObject(objectData.objectType, objectData.id);
                    if (obj != null)
                    {
                        // Note id is set in the creatobject call
                        // TrackedObject is attached in the creation call

                        obj.transform.position = objectData.position;
                        obj.transform.rotation = objectData.rotation;
                        obj.transform.localScale = objectData.scale;

                        loadedAnyObjects = true;
                    }
                }
            }
            catch
            {
                Debug.LogError("Error loading save file");
            }
        }

        // If no objects were loaded, create a default VideoPanel so that something is in the scene
        if (!loadedAnyObjects)
        {
            CreateDefaultVideoPanel();
        }
    }*/

    
    // Method to create a default VideoPanel if none exist
  /*  //no references for these, not needed rn
     
     private void CreateDefaultVideoPanel()
    {
        // Log creation of default panel
        Debug.Log("No objects loaded, creating default VideoPanel");

        GameObject newPanel = CreateObject("VideoPanel");
        newPanel.transform.position = new Vector3(0, 0, 1); // Default position
        newPanel.transform.rotation = Quaternion.identity;
        newPanel.transform.localScale = Vector3.one;
    }  
  
    public void LoadOnStartup()
    {
        Load();
    }

    private IEnumerator AutoSave()
    {
        while (true)
        {
            Save();

            //string filePath = Path.Combine(Application.persistentDataPath, saveFileName);
            //Debug.Log("File has been saved at: " + filePath);

            yield return new WaitForSeconds(10);
        }
    }
 */
    public void DeleteSaveFile()
    {
        string[] files = Directory.GetFiles(Application.persistentDataPath);
        string[] dirs = Directory.GetDirectories(Application.persistentDataPath);

        foreach (string file in files)
        {
            File.Delete(file);
        }

        foreach (string dir in dirs)
        {
            Directory.Delete(dir, true);
        }

        Debug.Log("Save files and subfolders deleted.");
    }

 
}
