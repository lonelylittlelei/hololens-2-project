using Microsoft.MixedReality.Toolkit.Input;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

//not fully working, with multiple clients, duplicate of active object is created on newly connected client (not viewable/syncing on already connected client)
//stream ids are also all converted to active object's stream id. 

//what i want to do: when "next", lock current object, turn off mesh so you can't see it, unlock next object, turn on mesh for next object. 
//issue: current implementation for "locking" is not disabling interaction with object but turning off pointers for that user, so the user can't interact with any objects. 
//can make objects invisible, but user can still interact with invisible objects
//videos should follow same principle of disabling mesh renderer = invisible

//would be nice to have a way to identify what the mesh shows and change info to match
// or at least guarantee stream id matches a type of mesh (i.e. stream id 1 will always be artery, 2 will always be brain, etc.)

//maybe combine with speechcommandreceiver??? given that it also contains speech commands. idk.

public class ObjectList : MonoBehaviour
{
    private List<GameObject> list = new List<GameObject>();
    private TextMeshPro description;
    private AudioController audioController;
    private string ActiveObject;
    // Start is called before the first frame update
    void Start()
    {
        description = GameObject.Find("DynamicDescription").GetComponent<TextMeshPro>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    //From Photon room previously, used for voice controls to cycle through models and descriptions
  
    public void OnSpeechKeywordRecognized(SpeechEventData eventData)

    {

        switch (eventData.Command.Keyword.ToLower())

        {

            case "next":
                audioController.StopAudio();

                for (int i = 0; i < list.Count; i++)
                {

                    if (CheckActiveMesh(list[i])) //instead of existing, this needs to check if mesh renderer is active
                    {
                        int next = (i += 1);
                        ChangeActiveObject(list[next % (list.Count)]); //modulo keeps index number in valid range
                        break;
                    }
                }

                break;

            case "back":
                audioController.StopAudio();

                //find index should return the index of the currently instantiated gameobject
                int currIndex = list.FindIndex(CheckActiveMesh);

                if (currIndex != -1)
                {
                    //goes to previous index, add # of objects to account for negatives and modulo to stay within valid index range
                    int prev = (currIndex - 1 + list.Count) % list.Count;

                    ChangeActiveObject(list[prev]);
                }

                break;

            case "mute":
                audioController.StopAudio();
                break;

            case "read":
                audioController.PlayAudio(ActiveObject);

                break;


            default:

                Debug.Log($"Unknown option {eventData.Command.Keyword}");

                break;

        }

    }

    public void Add(GameObject newObj)
    {
        int streamID = newObj.GetComponent<TrackedObject>().stream_id;
        if (list.Count == 0)
        {
            list.Add(newObj);
            description.text += "added first object to list, "+newObj.name;
           Debug.Log("added first object to list, "+newObj.name);
            DescriptionUpdate(streamID);
        }
        else 
        {
            //have to do something where when adding and there's already objects in list, lock the one being added and turn off mesh. 
            list.Add(newObj);
            DetachMesh(newObj);
            description.text += "/n added another object to list: "+newObj.name;
            Debug.Log("added another object to list: "+newObj.name);

        }

    }

    public int GetListCount()
    {
        return list.Count;
    }
    
    private bool CheckActiveMesh(GameObject obj)
    {
        return obj.GetComponent<MeshRenderer>().enabled;
    }
    public void DetachMesh(GameObject obj)
    {
        obj.GetComponent<MeshRenderer>().enabled = false;
    }

    public void RetachMesh(GameObject obj)
    {
        obj.GetComponent<MeshRenderer>().enabled = true;
    }
    private void ChangeActiveObject(GameObject obj)
    {
        string activatingObj = obj.name;
        foreach(GameObject listObj in list)
        {
            if (listObj.name != activatingObj)
            {
                DetachMesh (listObj);
            }
        }

        RetachMesh(obj);
        int streamID = obj.GetComponent<TrackedObject>().stream_id;
        DescriptionUpdate(streamID);
    }

    void DescriptionUpdate(int id)
    {   
        if(id == 1)
        {// assuming id 1 will always be artery
            description.text = "Arteries are blood vessels responsible for carrying oxygenated blood from the heart to the rest of the body. " +
                "They have thick, elastic walls to withstand the high pressure of the blood pumped directly from the heart. " +
                "The main artery leaving the heart is the aorta, and it branches into smaller arteries which reach every part of the body.";
            ActiveObject = "artery";
        }
        else if(id == 2)
        {// assuming id 2 will always be brain
            description.text = "The brain, the command center of the human nervous system, is divided into the cerebrum, cerebellum, and brainstem. " +
                "The cerebrum handles thinking and learning, the cerebellum manages coordination, and the brainstem controls vital functions like heart rate and breathing. " +
                "With its vast network of neurons, the brain enables communication and control throughout the body.";
            ActiveObject = "brain";
        }
        else if (id == 3)
        {// assuming id 3 will always be coiling vid
            description.text = "Coiling is a minimally invasive technique used to treat aneurysms, particularly in the brain. " +
                "Coiling can also refer to the winding or looping configuration found in many natural and man-made structures, from DNA molecules to the design of heating elements.";
            ActiveObject = "video";
        }
        else
        {// if none of these, clear description
            description.text = "";
        }
    }

}
