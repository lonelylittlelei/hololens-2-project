using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;





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


        private Vector3 anchorPosition;
        private Quaternion anchorRotation;
        private PhotonView pv;
        private Player[] photonPlayers;
        private int playersInRoom;
        private int myNumberInRoom;
        private bool hasObjInRoom = false;
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
        }


        private void CreateInteractableObjects()
        {
            MyCreation(readyPrefab);
        }
        private void CreatPlayer()
        {
            var player = PhotonNetwork.Instantiate(photonUserPrefab.name, new Vector3(0f, 0.2f, 0f), Quaternion.identity);
            

        }



        private void createPrivatePV()
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

            if (checkObjExist(removingObj))
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
            }
        }



        public void MyCreation(GameObject newTempObj)
        {



            removeAllObj();


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
            // Check if the object is not null (that is, it was found)
            if (objTransform != null)
            {
                createPrivatePV();
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
                }
            }
            // never create before
            else
            {
                var position = roverExplorerLocation.transform.position;
                var positionOnTopOfSurface = new Vector3(position.x, position.y + 0.5f,
                    position.z);
                Quaternion rotation = newTempObj.transform.rotation;
                var go = PhotonNetwork.Instantiate(newTempObj.name, positionOnTopOfSurface, rotation);
                Debug.Log("Null Object was inactive was given name was found: " + newTempObj + "(Clone)");
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
    }
}
