using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using TMPro;  // Include the TextMeshPro namespace





namespace MRTK.Tutorials.MultiUserCapabilities
{
    public class PhotonRoom : MonoBehaviourPunCallbacks, IInRoomCallbacks, IMixedRealitySpeechHandler
    {
        public static PhotonRoom Room;

        [SerializeField] private GameObject photonUserPrefab = default;
        [SerializeField] private GameObject readyPrefab = default;
        [SerializeField] private GameObject skullPrefab = default;
        [SerializeField] private GameObject brainPrefab = default;
        [SerializeField] private GameObject videoPrefab = default;
        [SerializeField] private Transform roverExplorerLocation = default;

        private string curGameObj = "Ready";
        public GameObject dynamicDescription;
        private Vector3 anchorPosition;
        private Quaternion anchorRotation;
        private PhotonView pv;
        private Player[] photonPlayers;
        private int playersInRoom;
        private int myNumberInRoom;
        private List<GameObject> gameObjectList = new List<GameObject>();

        public void OnSpeechKeywordRecognized(SpeechEventData eventData)

        {

            switch (eventData.Command.Keyword.ToLower())

            {

                case "next":

                    for (int i = 0; i < gameObjectList.Count; i++)
                    {

                        // Perform some operation on each GameObject
                        // For instance, we can just print the GameObject's name:
                        if (checkObjExist(gameObjectList[i]))
                        {
                            int next = (i += 1);
                            if (gameObjectList[next % (gameObjectList.Count)].name == "Ready")
                            {
                                next = (i += 1);
                            }
                            MyCreation(gameObjectList[next % (gameObjectList.Count)]);
                            break;
                        }
                    }

                    break;


                default:

                    Debug.Log($"Unknown option {eventData.Command.Keyword}");

                    break;

            }

        }

        public void update()
        {


        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            base.OnPlayerEnteredRoom(newPlayer);
            photonPlayers = PhotonNetwork.PlayerList;
            playersInRoom++;
        }

        private void Awake()
        {
            if (Room == null)
            {
                Room = this;
            }
            else
            {
                if (Room != this)
                {
                    Destroy(Room.gameObject);
                    Room = this;
                }
            }
        }

        public override void OnEnable()
        {
            base.OnEnable();
            PhotonNetwork.AddCallbackTarget(this);
        }

        public override void OnDisable()
        {
            base.OnDisable();
            PhotonNetwork.RemoveCallbackTarget(this);
        }



        private void Start()
        {

            CoreServices.InputSystem?.RegisterHandler<IMixedRealitySpeechHandler>(this);
            // Allow prefabs not in a Resources folder
            if (PhotonNetwork.PrefabPool is DefaultPool pool)
            {
                if (photonUserPrefab != null) pool.ResourceCache.Add(photonUserPrefab.name, photonUserPrefab);
                if (photonUserPrefab != null) pool.ResourceCache.Add(skullPrefab.name, skullPrefab);
                if (photonUserPrefab != null) pool.ResourceCache.Add(brainPrefab.name, brainPrefab);
                if (photonUserPrefab != null) pool.ResourceCache.Add(readyPrefab.name, readyPrefab);
                if (photonUserPrefab != null) pool.ResourceCache.Add(videoPrefab.name, videoPrefab);

                // Add GameObjects to the list
                this.gameObjectList.Add(readyPrefab);
                this.gameObjectList.Add(skullPrefab);
                this.gameObjectList.Add(brainPrefab);
                this.gameObjectList.Add(videoPrefab);
            }


        }

        public override void OnJoinedRoom()
        {
            base.OnJoinedRoom();

            photonPlayers = PhotonNetwork.PlayerList;
            playersInRoom = photonPlayers.Length;
            myNumberInRoom = playersInRoom;
            PhotonNetwork.NickName = myNumberInRoom.ToString();

            StartGame();
        }


        private void StartGame()
        {
            CreatPlayer();

            if (TableAnchor.Instance != null) CreateInteractableObjects();

            /*    StartCoroutine(synchronizeObj());*/
        }


        private void CreateInteractableObjects()
        {
            MyCreation(readyPrefab);

        }


        private void CreatPlayer()
        {
            var player = PhotonNetwork.Instantiate(photonUserPrefab.name, new Vector3(0f, 0.2f, 0f), Quaternion.identity);


        }




        private void DisableObject(string removingObjName)
        {
            string cloneObjName = removingObjName + "(Clone)";
            GameObject cloneObj = GameObject.Find(cloneObjName);
            if (cloneObj != null)
            {
                cloneObj.SetActive(false);
            }
        }

        private void removeAllObj()
        {

            if (gameObjectList == null)
            {
                gameObjectList.Add(readyPrefab);
                gameObjectList.Add(skullPrefab);
                gameObjectList.Add(brainPrefab);
                gameObjectList.Add(videoPrefab);
            }


            // Iterate through the list of GameObjects
            foreach (GameObject gameObject in gameObjectList)
            {
                // Perform some operation on each GameObject
                // For instance, we can just print the GameObject's name:
                MyDeletion(gameObject);
            }


        }
        private bool checkObjExist(GameObject removingObj)
        {
            bool results = false;
            if (removingObj == null)
            {
                Debug.Log("removing Object is null");
            }
            string objectStringName = removingObj.name + "(Clone)";
            if (GameObject.Find(objectStringName) != null)
                results = true;
            return results;
        }


        public void MyDeletion(GameObject removingObj)
        {
            DisableObject(removingObj.name);
        }

        public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
        {
            object propertyValue;
            if (propertiesThatChanged.TryGetValue("propertyKey", out propertyValue))
            {
                if (propertyValue.ToString() == "empty")
                    removeAllObj();
                else
                {
                    curGameObj = propertyValue.ToString();
                    Debug.Log("Current game obj" + curGameObj);
                    foreach (GameObject gameObject in gameObjectList)
                    {

                        // Perform some operation on each GameObject
                        // For instance, we can just print the GameObject's name:
                        if (gameObject.name != curGameObj)
                            DisableObject(gameObject.name);
                        else
                        {
                            Debug.Log("found Current game obj" + curGameObj);
                            EnableObject(curGameObj);
                            dynamicChangeDescription(curGameObj);
                        }

                    }

                }
            }


        }


        // change the description
        private void dynamicChangeDescription(string objName) {


            string updateText = "Welcome to HoloensDisplayer!";
            if (objName == "brainPrefab")
            {
                updateText = "The brain, the command center of the human nervous system, is divided into the cerebrum, cerebellum, and brainstem. The cerebrum handles thinking and learning, the cerebellum manages coordination, and the brainstem controls vital functions like heart rate and breathing. With its vast network of neurons, the brain enables communication and control throughout the body.";

            }

            else if (objName == "arteryPrefab")
            {
                updateText = "Arteries are blood vessels responsible for carrying oxygenated blood from the heart to the rest of the body. They have thick, elastic walls to withstand the high pressure of the blood pumped directly from the heart. The main artery leaving the heart is the aorta, and it branches into smaller arteries which reach every part of the body.";
            }

            else if (objName == "video") {
                updateText = "Coiling is a minimally invasive technique used to treat aneurysms, particularly in the brain. Coiling can also refer to the winding or looping configuration found in many natural and man-made structures, from DNA molecules to the design of heating elements.";
            }

            TextMeshPro textMeshProComponent = dynamicDescription.GetComponent<TextMeshPro>();

            if (textMeshProComponent != null)
            {
                textMeshProComponent.text = updateText;
            }
            else
            {
                Debug.LogError("No TextMeshProUGUI component found on " + dynamicDescription.name);
            }


        }

        private void updateProperties(string newValue) {

            ExitGames.Client.Photon.Hashtable propsToUpdate = new ExitGames.Client.Photon.Hashtable
            {
                { "propertyKey", newValue }
            };

            PhotonNetwork.CurrentRoom.SetCustomProperties(propsToUpdate);

        }

        private void EnableObject(string newTempObj)
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
            }

        }

        public void MyCreation(GameObject newTempObj)
        {



            if (PhotonNetwork.PrefabPool is DefaultPool pool)
            {
                if (!pool.ResourceCache.ContainsKey(newTempObj.name))
                    if (newTempObj != null) pool.ResourceCache.Add(newTempObj.name, newTempObj);
            }

            // Find the parent object (which should be active)
            GameObject parentObj = GameObject.Find("TableAnchor");
            if (parentObj == null)
                Debug.Log("Null TableAnchor");


            // Find the inactive child object
            Transform objTransform = parentObj.transform.Find(newTempObj.name + "(Clone)");
            // Check if the object is  null (that is, it was not found)
            // never create before
            if (objTransform == null)
            {
                /*   removeAllObj();*/
                var position = roverExplorerLocation.transform.position;
                var positionOnTopOfSurface = new Vector3(position.x, position.y + 0.5f,
                    position.z);
                Quaternion rotation = newTempObj.transform.rotation;
                var go = PhotonNetwork.Instantiate(newTempObj.name, positionOnTopOfSurface, rotation);


            }
            updateProperties(newTempObj.name);


        }
    }
}
        // private void CreateMainLunarModule()
        // {
        //     module = PhotonNetwork.Instantiate(roverExplorerPrefab.name, Vector3.zero, Quaternion.identity);
        //     pv.RPC("Rpc_SetModuleParent", RpcTarget.AllBuffered);
        // }
        //
        // [PunRPC]
        // private void Rpc_SetModuleParent()
        // {
        //     Debug.Log("Rpc_SetModuleParent- RPC Called");
        //     module.transform.parent = TableAnchor.Instance.transform;
        //     module.transform.localPosition = moduleLocation;
        // }

/*    private void CreateInteractableObjects()
{
    var position = roverExplorerLocation.position;
    var positionOnTopOfSurface = new Vector3(position.x, position.y + 0.3f,
        position.z);

    var go = PhotonNetwork.Instantiate(roverExplorerPrefab.name, positionOnTopOfSurface,
        roverExplorerLocation.rotation);

   *//* Debug.Log("init: " + roverExplorerPrefab.GetPhotonView().ViewID);*//*
}*/
/* if (checkObjExist(removingObj))
    {
        createPrivatePV();
        if (pv != null)
        {

            // Check if you are the owner before calling the RPC
            if (pv.IsMine)
            {
                pv.RPC("DisableObject", RpcTarget.All, removingObj.name);
                Debug.LogWarning("PhotonView found on ImageTarget.");
            }
            else
            {
                Debug.LogWarning("Ownership request for PhotonView on ImageTarget was not successful.");
            }
        }
        else
        {
            Debug.LogWarning("PhotonView not found on ImageTarget.");
        }
    }*/
/*createPrivatePV();
if (pv != null)
{

 // Check if you are the owner before calling the RPC
 if (pv.IsMine)
 {
     pv.RPC("EnableObject", RpcTarget.All, newTempObj.name);
     Debug.LogWarning("PhotonView found on ImageTarget.");
 }
 else
 {
     Debug.LogWarning("Ownership request for PhotonView on ImageTarget was not successful.");
 }
}
else
{
 Debug.LogWarning("PhotonView not found on ImageTarget.");
}*/


/*  private IEnumerator synchronizeObj()
  {
      yield return new WaitForSeconds(0.5f);  // Wait for 1 second

      removeAllObj();

      CreateInteractableObjects();

  }
*/

/*        private void createPrivatePV()
        {

            int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
            string userObjName = "User" + playerCount;
            GameObject userObj = GameObject.Find(userObjName);

            if (userObj != null)
            {
                PhotonView photonView = userObj.GetComponent<PhotonView>();
                Debug.Log(userObj.gameObject.name);
                pv = photonView;
            }
            else
                Debug.Log("Current User Obj is Null");
        }

    }
}
     */