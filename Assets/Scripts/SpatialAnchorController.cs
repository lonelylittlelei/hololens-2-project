using Microsoft.Azure.SpatialAnchors;
using Microsoft.Azure.SpatialAnchors.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.XR;


namespace MRTK.Tutorials.MultiUserCapabilities
{
    [RequireComponent(typeof(SpatialAnchorManager))]
    public class SpatialAnchorController : MonoBehaviour
    {

        private SpatialAnchorManager _spatialAnchorManager = null;

        private List<GameObject> _foundOrCreatedAnchorGameObjects = new List<GameObject>();
        private GameObject primaryAnchorObject;

        private List<String> _createdAnchorIDs = new List<String>();
        private String primaryAnchorId = String.Empty;

        public GameObject camera;
        public GameObject playground;

        void Start()
        {
            _spatialAnchorManager = GetComponent<SpatialAnchorManager>();
            _spatialAnchorManager.LogDebug += (sender, args) => Debug.Log($"ASA - Debug: {args.Message}");
            _spatialAnchorManager.Error += (sender, args) => Debug.LogError($"ASA - Error: {args.ErrorMessage}");
            _spatialAnchorManager.AnchorLocated += SpatialAnchorManager_AnchorLocated;
        }

        public void ButtonShortTap()
        {
            Vector3 locationOfAnchor = camera.transform.position + (camera.transform.forward * 1.5f);
            locationOfAnchor.y = -0.5f;
            ShortTap(locationOfAnchor);
        }

        private async void ShortTap(Vector3 handPosition)
        {
            await _spatialAnchorManager.StartSessionAsync();
            if (!IsAnchorNearby(handPosition, out GameObject anchorGameObject))
            {
                //No Anchor Nearby, start session and create an anchor
                await CreateAnchor(handPosition);
            }
            else
            {
                //Delete nearby Anchor
                DeleteAnchor(anchorGameObject);
            }
        }

        public async void LongTap()
        {
            if (_spatialAnchorManager.IsSessionStarted)
            {
                // Stop Session and remove all GameObjects. This does not delete the Anchors in the cloud
                _spatialAnchorManager.DestroySession();
            }
                RemoveAllAnchorGameObjects();
                Debug.Log("ASA - Stopped Session and removed all Anchor Objects");
            
                //Start session and search for all Anchors previously created
                await _spatialAnchorManager.StartSessionAsync();
                GetAzureAnchorCube(); //get anchorId from photon
                LocateAnchor();
           
        }

        private void RemoveAllAnchorGameObjects()
        {
            foreach (var anchorGameObject in _foundOrCreatedAnchorGameObjects)
            {
                Destroy(anchorGameObject);
            }
            _foundOrCreatedAnchorGameObjects.Clear();
            Destroy(primaryAnchorObject);
            primaryAnchorObject = null;
        }

        private bool IsAnchorNearby(Vector3 position, out GameObject anchorGameObject)
        {
            anchorGameObject = null;

            if (_foundOrCreatedAnchorGameObjects.Count <= 0)
            {
                return false;
            }


            //Iterate over existing anchor gameobjects to find the nearest
            var (distance, closestObject) = _foundOrCreatedAnchorGameObjects.Aggregate(
                new Tuple<float, GameObject>(Mathf.Infinity, null),
                (minPair, gameobject) =>
                {
                    Vector3 gameObjectPosition = gameobject.transform.position;
                    float distance = (position - gameObjectPosition).magnitude;
                    return distance < minPair.Item1 ? new Tuple<float, GameObject>(distance, gameobject) : minPair;
                });

            if (distance <= 0.15f)
            {
                //Found an anchor within 15cm
                anchorGameObject = closestObject;
                return true;
            }
            else
            {
                return false;
            }
        }

        private async Task CreateAnchor(Vector3 position)
        {
            //Create Anchor GameObject. We will use ASA to save the position and the rotation of this GameObject.
            if (!InputDevices.GetDeviceAtXRNode(XRNode.Head).TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 headPosition))
            {
                headPosition = Vector3.zero;
            }

            Quaternion orientationTowardsHead = Quaternion.LookRotation(position - headPosition, Vector3.forward);

            GameObject anchorGameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            anchorGameObject.GetComponent<MeshRenderer>().material.shader = Shader.Find("Legacy Shaders/Diffuse");
            anchorGameObject.transform.position = position;
            anchorGameObject.transform.rotation = orientationTowardsHead;
            anchorGameObject.transform.localScale = Vector3.one * 0.1f;

            //Add and configure ASA components
            CloudNativeAnchor cloudNativeAnchor = anchorGameObject.AddComponent<CloudNativeAnchor>();
            await cloudNativeAnchor.NativeToCloud();
            CloudSpatialAnchor cloudSpatialAnchor = cloudNativeAnchor.CloudAnchor;
            cloudSpatialAnchor.Expiration = DateTimeOffset.Now.AddDays(3);

            //Collect Environment Data
            while (!_spatialAnchorManager.IsReadyForCreate)
            {
                float createProgress = _spatialAnchorManager.SessionStatus.RecommendedForCreateProgress;
                Debug.Log($"ASA - Move your device to capture more environment data: {createProgress:0%}");
            }

            Debug.Log($"ASA - Saving cloud anchor... ");

            try
            {
                // Now that the cloud spatial anchor has been prepared, we can try the actual save here.
                await _spatialAnchorManager.CreateAnchorAsync(cloudSpatialAnchor);

                bool saveSucceeded = cloudSpatialAnchor != null;
                if (!saveSucceeded)
                {
                    Debug.LogError("ASA - Failed to save, but no exception was thrown.");
                    return;
                }

                Debug.Log($"ASA - Saved cloud anchor with ID: {cloudSpatialAnchor.Identifier}");
                RemoveAllAnchorGameObjects(); // remove previous cube
                primaryAnchorObject = anchorGameObject;
                primaryAnchorId = cloudSpatialAnchor.Identifier;
                anchorGameObject.GetComponent<MeshRenderer>().material.color = Color.green;


                // Automatically share anchorId
                Debug.Log($"ASA - Sharing anchorId to photon: {cloudSpatialAnchor.Identifier}");
                ShareAzureAnchorCube(cloudSpatialAnchor.Identifier);
                Debug.Log($"ASA - Sent to photon (?)");
            }
            catch (Exception exception)
            {
                Debug.Log("ASA - Failed to save anchor: " + exception.ToString());
                Debug.LogException(exception);
            }
        }

        private void LocateAnchor()
        {
            if (primaryAnchorId != String.Empty)
            {
                //Create watcher to look for all stored anchor IDs
                Debug.Log($"ASA - Creating watcher to look for spatial anchor: {primaryAnchorId}");
                AnchorLocateCriteria anchorLocateCriteria = new AnchorLocateCriteria();

                List<String> anchorIdList = new List<String>(); //anchor criteria requires Array
                anchorIdList.Add(primaryAnchorId);
                anchorLocateCriteria.Identifiers = anchorIdList.ToArray();
                _spatialAnchorManager.Session.CreateWatcher(anchorLocateCriteria);
                Debug.Log($"ASA - Watcher created!");
            }
        }

        private void SpatialAnchorManager_AnchorLocated(object sender, AnchorLocatedEventArgs args)
        {
            Debug.Log($"ASA - Anchor recognized as a possible anchor {args.Identifier} {args.Status}");

            if (args.Status == LocateAnchorStatus.Located)
            {
                //Creating and adjusting GameObjects have to run on the main thread. We are using the UnityDispatcher to make sure this happens.
                UnityDispatcher.InvokeOnAppThread(() =>
                {
                    // Read out Cloud Anchor values
                    CloudSpatialAnchor cloudSpatialAnchor = args.Anchor;

                    //Create GameObject
                    GameObject anchorGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    anchorGameObject.transform.localScale = Vector3.one * 0.1f;
                    anchorGameObject.GetComponent<MeshRenderer>().material.shader = Shader.Find("Legacy Shaders/Diffuse");
                    anchorGameObject.GetComponent<MeshRenderer>().material.color = Color.red;
                    anchorGameObject.GetComponent<MeshRenderer>().enabled = false;

                    // Link to Cloud Anchor
                    anchorGameObject.AddComponent<CloudNativeAnchor>().CloudToNative(cloudSpatialAnchor);
                    playground.transform.position = anchorGameObject.transform.position;
                    playground.transform.eulerAngles = new Vector3(0, anchorGameObject.transform.eulerAngles.y, 0);

                    primaryAnchorObject = anchorGameObject;
                    Debug.Log($"ASA - Camera placed at {camera.transform.position}");
                    Debug.Log($"ASA - Anchor placed at {anchorGameObject.transform.position}");
                });
            }
        }

        private async void DeleteAnchor(GameObject anchorGameObject)
        {
            CloudNativeAnchor cloudNativeAnchor = anchorGameObject.GetComponent<CloudNativeAnchor>();
            CloudSpatialAnchor cloudSpatialAnchor = cloudNativeAnchor.CloudAnchor;

            Debug.Log($"ASA - Deleting cloud anchor: {cloudSpatialAnchor.Identifier}");

            //Request Deletion of Cloud Anchor
            await _spatialAnchorManager.DeleteAnchorAsync(cloudSpatialAnchor);

            //Remove local references
            _createdAnchorIDs.Remove(cloudSpatialAnchor.Identifier);
            primaryAnchorId = String.Empty;
            _foundOrCreatedAnchorGameObjects.Remove(anchorGameObject);
            Destroy(anchorGameObject);

            Debug.Log($"ASA - Cloud anchor deleted!");
        }

        public void ShareAzureAnchorCube(string anchorId)
        {
            Debug.Log("\nSharingModuleScript.ShareAzureAnchor()");

            // This is the anchor id to send (change to use first id in anchorIds list)
            GenericNetworkManager.Instance.azureAnchorId = anchorId;
            Debug.Log("GenericNetworkManager.Instance.azureAnchorId: " + GenericNetworkManager.Instance.azureAnchorId);

            var pvLocalUser = GenericNetworkManager.Instance.localUser.gameObject;
            var pu = pvLocalUser.gameObject.GetComponent<PhotonUser>();
            pu.ShareAzureAnchorId();
        }

        // Update AnchorId from photon
        public void GetAzureAnchorCube()
        {
            Debug.Log("\nSharingModuleScript.GetAzureAnchor()");
            Debug.Log("GenericNetworkManager.Instance.azureAnchorId: " + GenericNetworkManager.Instance.azureAnchorId);


            // Send anchorId (GenericNetworkManager.Instance.azureAnchorId) to SpatialAnchorController to add to _createdAnchorIDs
            primaryAnchorId = GenericNetworkManager.Instance.azureAnchorId;
        }

    }
}
