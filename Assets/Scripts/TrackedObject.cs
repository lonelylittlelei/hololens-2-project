
using System;
using UnityEngine;

using TMPro;
using Microsoft.MixedReality.Toolkit;    // For TextMeshProUGUI

public class TrackedObject : MonoBehaviour
{
    private const byte OBJECT_NONE = 0;
    private const byte OBJECT_VIDEO_STREAM = 1;
    private const byte OBJECT_MESH = 2;

    private NetworkController networkController;
    private Vector3 lastPosition;
    private Vector3 lastRotation;
    private Vector3 lastScale;
    private float lastAspectRatio;
    private byte[] serializationBuffer;

    // Target transforms
    public Vector3 targetPosition = new Vector3(1, 0, 1);
    public Quaternion targetRotation;
    public Vector3 targetScale;
    // Lerp speed
    public float moveSpeed = 5.0f;
    public float rotateSpeed = 5.0f;
    // Thresholds for determining significant change
    public float positionChangeThreshold = 0.1f; // Adjust as needed
    public float rotationChangeThreshold = 0.1f; // Adjust as needed
    public float scaleChangeThreshold = 0.1f; // Adjust as needed
    private bool isReceivingNetworkUpdate = false;      // This will be used to lock the object when being updated by the network, but freeing it when being updated by the user



    public bool isShared = true;        // This means that object manupulation is shared
    public bool isHidden = false;       // This means that object is hidden and will be non active in this users scene
    public bool isLocked = false;       // This means that object is locked and will not be updated by the host or other users
    // Will need to figure out if dont render or not active better later
    public string id;
    public int stream_id = 1000;         // -1 means not a video stream object, initialising to 1000 to force update on first frame
    private int old_stream_id = -1;    // Used to check if the stream id has changed
    public byte object_type = OBJECT_NONE;       // 0 null object, 1 video stream, 2 mesh object
    private h264Stream videoStream;
    private Material videoMaterial;


    public TextMeshPro uiText;
    private int count = 0;

    private void Awake()
    {
        // Scene must always contain a NetworkController object or else will get null reference exception
        networkController = FindObjectOfType<NetworkController>();

    }

    private void Start()
    {
        // Set the postion of this object to 0,0,100 == this will spawn it a long way away and it will move on the first update
        transform.position = new Vector3((float)-0.5, 0, 1);
        // Set the rotation of this object to 0,0,0
        transform.rotation = Quaternion.Euler(0, 0, 0);
        // Set the scale of this object to 1,1,1
        transform.localScale = new Vector3(1, 1, 1);

        // Set the last position to the same
        lastPosition = transform.position;
        lastRotation = transform.rotation.eulerAngles;
        lastScale = transform.localScale;

        // Set aspect to 1
        lastAspectRatio = 1;
       
        //  Find and use "LogText" object in the scene
        uiText = GameObject.Find("DynamicDescription").GetComponent<TextMeshPro>();

    }
    // when another client connects, tracked objects of the meshes already present are being sent again creating 4 objects, 2 viewabl on 1 and the other 2 viewable on the other
    void OnDestroy()
    {
        // Make sure we stop trying to update
        isReceivingNetworkUpdate = false;
    }

    private void Update()
    {
        if (isReceivingNetworkUpdate)       // Only do if we are receiving a network update
        {
            count++;
            

            // Determine if the object is close enough to its target to be considered as having reached it
            bool hasReachedTarget = Vector3.Distance(transform.position, targetPosition) <= positionChangeThreshold &&
                                    Quaternion.Angle(transform.rotation, targetRotation) <= rotationChangeThreshold &&
                                    Vector3.Distance(transform.localScale, targetScale) <= scaleChangeThreshold;

            // If the object has reached its target, it is no longer receiving a network update
            if (hasReachedTarget)
            {
                isReceivingNetworkUpdate = false;
                // Set the target = to the current position -- otherwise it will jump back if moved locally
                targetPosition = transform.position;
                targetRotation = transform.rotation;
                targetScale = transform.localScale;
            }
            else
            {
                /*Debug.Log("Not reached target, target is " + targetPosition + " and current is " + transform.position);
                // Also write what the differential is that has caused this
                Debug.Log($"Position Difference: {Vector3.Distance(transform.position, targetPosition)} <= {positionChangeThreshold}");
                Debug.Log($"Rotation Difference: {Quaternion.Angle(transform.rotation, targetRotation)} <= {rotationChangeThreshold}");
                Debug.Log($"Scale Difference: {Vector3.Distance(transform.localScale, targetScale)} <= {scaleChangeThreshold}");
                */
                // Apply smooth interpolation towards the target transform 

                transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime + 0.1f);       // Add 0.1f to move speed is not close to 0
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotateSpeed * Time.deltaTime + 0.1f);
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, moveSpeed * Time.deltaTime + 0.1f);
            }

            // Change video stream if we need to
            if (stream_id != -1 && stream_id != old_stream_id && object_type == OBJECT_VIDEO_STREAM)
            {
                videoStream = networkController.GetVideoStream(stream_id);
                if (videoStream != null)
                {
                    videoMaterial = videoStream.GetMaterial();
                    Debug.Log("Changed to video stream " + videoStream);
                    GetComponent<Renderer>().material = videoMaterial;
                    old_stream_id = stream_id;
                }
                else
                {
                    Debug.LogError("Video stream with ID " + stream_id + " does not exist.");
                }
            }

            // Check and change aspect ratio if necessary
            float currentAspectRatio = transform.localScale.x / transform.localScale.y;
            float imageAspectRatio = videoStream != null ? videoStream.GetAspectRatio() : 1;
            if (currentAspectRatio != imageAspectRatio
            && object_type == OBJECT_VIDEO_STREAM)
            {
                setAspectRatio(imageAspectRatio);
            }

        }
        else        // Or... we are not receiving a network update
        {

            // Determine if a serialization and network update should occur from this client to the host
            bool shouldSerialize = !isReceivingNetworkUpdate && (
                Vector3.Distance(transform.position, lastPosition) > positionChangeThreshold ||
                Quaternion.Angle(transform.rotation, Quaternion.Euler(lastRotation)) > rotationChangeThreshold ||
                Vector3.Distance(transform.localScale, lastScale) > scaleChangeThreshold ||
                stream_id != old_stream_id);

            // Serialize and send object data if there's been a significant change
            if (shouldSerialize)
            {
                lastPosition = transform.position;
                lastRotation = transform.rotation.eulerAngles;
                lastScale = transform.localScale;
                lastAspectRatio = transform.localScale.x / transform.localScale.y;
                Serialize(lastPosition, lastRotation, lastScale);
                networkController.SendObjectData(serializationBuffer);
                old_stream_id = stream_id; // Ensure this is updated to reflect any changes in stream ID
            }
        }

    }


    public float getAspectRatio()
    {
        return lastAspectRatio;
    }

    public void setAspectRatio(float aspectRatio)
    {
        lastAspectRatio = aspectRatio;

        Vector3 newScale = transform.localScale;
        newScale.x = aspectRatio * newScale.y;

        // Update the scale
        transform.localScale = newScale;

        // update lastscale and target
        lastScale = newScale;
        targetScale = newScale;
    }

    // This method will be called to update the transforms of the object from the network
    public void ReceiveObjectUpdate(Vector3 position, Vector3 rotation, Vector3 scale, int new_stream_id)
    {
        //Debug.Log("TRACKED update for " + id + " with position " + position + " and rotation " + rotation + " and scale " + scale);
        // Flag that an update has been received
        isReceivingNetworkUpdate = true;

        // Instead of directly setting the transform's position, rotation, and scale,
        // set them as targets for smooth interpolation in the Update method.
        targetPosition = position;
        targetRotation = Quaternion.Euler(rotation); // Convert Euler angles to Quaternion
        targetScale = scale;

        // Update these last values to prevent sending back the same data,
        // especially important to avoid infinite loops of updates.
        lastPosition = position;
        lastRotation = rotation;
        lastScale = scale;

        // Set the new stream id if it has changed
        if (stream_id != new_stream_id)
        {
            stream_id = new_stream_id;
        }
    }

    private void InitializeSerializationBuffer()
    {
        ushort msgSize = (ushort)(16 + 1 + 3 * 4 * 3 + 1); // 16 bytes for GUID + 1 for type + 3 floats * 3 (position, rotation, scale) * 4 bytes each +1 for stream id
        serializationBuffer = new byte[2 + 1 + 2 + msgSize]; // Initialize buffer

        // Static parts of the buffer
        ushort magicNumber = 0xABCD;
        byte[] magicNumberBytes = BitConverter.GetBytes((short)magicNumber);
        Array.Copy(magicNumberBytes, 0, serializationBuffer, 0, magicNumberBytes.Length);
        serializationBuffer[2] = 1; // OBJECT_UPDATE
        byte[] msgSizeBytes = BitConverter.GetBytes(msgSize);
        Array.Copy(msgSizeBytes, 0, serializationBuffer, 3, msgSizeBytes.Length);
        byte[] guidBytes = new Guid(id).ToByteArray();
        Array.Copy(guidBytes, 0, serializationBuffer, 5, guidBytes.Length);
    }


    private void Serialize(Vector3 position, Vector3 rotation, Vector3 scale)
    {
        id = id ?? System.Guid.NewGuid().ToString();    
     
        // Lazy initialization of the serialization buffer
        if (serializationBuffer == null)
        {
            InitializeSerializationBuffer();
        }

        int dataIndex = 5 + 16; // GUID is 16 bytes long, starting at index 5 for header

        // Serialize the object type (1 byte)
        if (gameObject.name == "MeshLoader") //for initial instantiation of mesh, to make sure that it's identified as a mesh
        {
            object_type = OBJECT_MESH;
        }
        serializationBuffer[dataIndex] = (byte)object_type;

        // increment the index
        dataIndex++;

        // Serialize position
        Buffer.BlockCopy(BitConverter.GetBytes(position.x), 0, serializationBuffer, dataIndex, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(position.y), 0, serializationBuffer, dataIndex + 4, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(position.z), 0, serializationBuffer, dataIndex + 8, 4);

        dataIndex += 12; // Move past the position data

        // Serialize rotation
        Buffer.BlockCopy(BitConverter.GetBytes(rotation.x), 0, serializationBuffer, dataIndex, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(rotation.y), 0, serializationBuffer, dataIndex + 4, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(rotation.z), 0, serializationBuffer, dataIndex + 8, 4);

        dataIndex += 12; // Move past the rotation data

        // Serialize scale
        Buffer.BlockCopy(BitConverter.GetBytes(scale.x), 0, serializationBuffer, dataIndex, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(scale.y), 0, serializationBuffer, dataIndex + 4, 4);
        Buffer.BlockCopy(BitConverter.GetBytes(scale.z), 0, serializationBuffer, dataIndex + 8, 4);

        dataIndex += 12; // Move past the scale data
        // Serialize stream id
        serializationBuffer[dataIndex] = (byte)stream_id;
    }

}
