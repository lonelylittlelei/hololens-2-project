using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;

/*namespace MRTK.Tutorials.MultiUserCapabilities*/
public class UserModified : MonoBehaviour
{

    private PhotonView pv;

    [PunRPC]
    public void DisableObject(string removingObjName)
    {
        string cloneObjName = removingObjName + "(Clone)";
        GameObject cloneObj = GameObject.Find(cloneObjName);
        if (cloneObj != null)
        {
            cloneObj.SetActive(false);
            Debug.LogWarning("GameObject " + cloneObjName + " found.");
        }
        else
        {
            Debug.LogWarning("GameObject " + cloneObjName + " not found. It may have been destroyed.");
        }
    }

    [PunRPC]
    public void EnableObject(string newTempObj)
    {
        // Find the parent object (which should be active)
        GameObject parentObj = GameObject.Find("TableAnchor");
        if (parentObj == null)
            Debug.Log("Null TableAnchor");


        // Find the inactive child object
        Transform objTransform = parentObj.transform.Find(newTempObj + "(Clone)");

        // Check if the object is not null (that is, it was found)
        if (objTransform != null)
        {
            // Set the object to active
            objTransform.gameObject.SetActive(true);
            Debug.Log(objTransform.name);
        }

    }
}
