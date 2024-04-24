using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

#if !UNITY_EDITOR  // UWP platform 
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using Windows.Networking.Connectivity;
using Windows.Networking;
#endif


using TMPro;    // For TextMeshProUGUI

[System.Serializable]
public class UDPMessageEvent : UnityEvent<string, string, byte[]>
{
    // Define an event that will be triggered when a UDP message is received
}

public class NetworkController : MonoBehaviour
{

    // Define message types using enum - cant use define in C#
    // Define message types using const
    //these are for messages sent over connection with host
    private const byte MSG_TYPE_IMAGE = 0;
    private const byte MSG_TYPE_OBJECT = 1;
    private const byte MSG_TYPE_MESH_DATA = 2;

    // Define object types using const
    //these are for object types defined in TrackedObject class
    private const byte OBJECT_NONE = 0;
    private const byte OBJECT_VIDEO_STREAM = 1;
    private const byte OBJECT_MESH = 2;

    //NEED TO MODIFY TO YOUR OWN IP ADDRESS:
    //private string ServerIpAddress;
    private string ServerIpAddress = "192.168.0.120";           // should be petergrp5g IP on my laptop

    public const int Port = 12345;

    private const int UdpPort = 6666;  // Port for UDP broadcast receiver
    public UDPMessageEvent udpEvent = null;

    private TcpClient client;
    private Thread receiverThread;  //Threads used to run multiple functions at a time
                                    //With Unity, a lot of things have to happen on main dispatcher thread

    private volatile bool continueReceiving = true;
    private NetworkStream stream;


    // Track position so only send if changed
    private Vector3 lastSentPosition;

    // Dictionary to store h264 streams
    private Dictionary<int, h264Stream> h264Streams = new Dictionary<int, h264Stream>();

    // For debug output
    public TextMeshPro uiText; //needs to be assigned in Editor
    public GameObject h264StreamHandler;

    //Trying to get rid of persistence handler
    public GameObject quadPrototype; // Reference to the prototype object in the scene - This is public and set in the editor, set prototype object inactive in the scene
    public GameObject meshPrototype; //prototypes might not be needed right now, since i have it loading locally 
    private int streamCount = 0;
    private ObjectList list;

    private void Start()
    {

        // Clear UI text
        uiText.text = "";
        list = GameObject.Find("ObjectList").GetComponent<ObjectList>();

        // Initialize the lastSentPosition to an impossible value so that it will always be sent on first update
        lastSentPosition = Vector3.negativeInfinity;

        // Start TCP connection setup
        client = new TcpClient();
        receiverThread = new Thread(ReceiverThread);
        receiverThread.IsBackground = true;
        receiverThread.Start();

#if !UNITY_EDITOR //UDP connection not doing much right now except transmitting over IP address, may change if David Hocking updates, he was looking
        // Start listening for UDP broadcasts using DatagramSocket
        StartUdpListener();
#else
        // For non-UWP platforms, use the original UDP listener
        Thread udpListenerThread = new Thread(() => ListenForUdpBroadcast());
        udpListenerThread.IsBackground = true;
        udpListenerThread.Start();
#endif
    }

    private void OnDestroy()            // Called on application quit
    {
        continueReceiving = false;
        if (receiverThread != null)
        {
            receiverThread.Join();
        }

        if (stream != null)
        {
            stream.Close();
            stream = null;
        }

        if (client != null)
        {
            client.Close();
        }
    }


    private void Update()
    {

    }

    // A method to get the h264 stream at index 
     public h264Stream GetVideoStream(int index)
     {
         if (h264Streams.ContainsKey(index))
         {
             return h264Streams[index];
         }
         else
         {
             return null;
         }
     }

    // Following is the UDP listener code in UWP environment
#if !UNITY_EDITOR
    private DatagramSocket  socket;
    private async void StartUdpListener()
    {

        if (udpEvent == null)
        {
            udpEvent = new UDPMessageEvent();
            udpEvent.AddListener(UDPMessageReceived);
        }

        socket = new DatagramSocket();
        socket.MessageReceived += DatagramSocket_MessageReceived;

        try
        {
            await socket.BindServiceNameAsync(UdpPort.ToString());
            //uiText.text += "\nUDP listener started on port " + UdpPort;
        }
        catch (Exception ex)
        {
            Debug.LogError("Exception when starting UDP listener: " + ex.Message);
            // Also write to the ui
            uiText.text += "\nException when starting UDP listener: " + ex.Message;
        }
    }

    private void UDPMessageReceived(string host, string port, byte[] data)
    {
        Debug.Log($"UDP message received from {host} on port {port}, {data.Length} bytes");
        MainThreadDispatcher.Enqueue(() =>
        {
            //uiText.text += $"UDP message received from {host} on port {port}, {data.Length} bytes";
        });     
    }

    private async void DatagramSocket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
    {
        MainThreadDispatcher.Enqueue(() =>
        {
           // uiText.text += "\nReceived UDP message of some sort";
        });     

        try
        {
            DataReader reader = args.GetDataReader();
            uint length = reader.UnconsumedBufferLength;
            string receivedMessage = reader.ReadString(length);

            Debug.Log("Received UDP message: " + receivedMessage);
            // Write to ui using the MainThreadDispatcher
            MainThreadDispatcher.Enqueue(() =>
            {
                //uiText.text += "\nReceived UDP message: " + receivedMessage;
            });            

            // Assuming the received string is a valid IP address
            ServerIpAddress = receivedMessage;
        }
        catch (Exception ex)
        {
            Debug.LogError("Error when receiving message: " + ex.Message);
            // Also write to the ui with main thread dispatcher
            MainThreadDispatcher.Enqueue(() =>
            {
                uiText.text += "\nError when receiving message: " + ex.Message;
            });

        }
    }
#endif

    //actually don't know if i should be looking at UDP rn, will leave it in for future
    //but i think current stable ver. uses TCP
    private void ListenForUdpBroadcast()
    {
        using (UdpClient udpClient = new UdpClient(UdpPort))
        {
            IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, UdpPort);

            try
            {
                
                Debug.Log("Listening for UDP broadcast on port " + UdpPort);
                // Listening loop
                while (true)
                {
                    byte[] receivedBytes = udpClient.Receive(ref remoteEndPoint);
                    string receivedString = Encoding.ASCII.GetString(receivedBytes);
                    Debug.Log("Received UDP broadcast from " + remoteEndPoint + " with message: " + receivedString);

                    // Assuming the received string is a valid IP address
                    ServerIpAddress = receivedString;
                    break; // Exit the loop once we receive the broadcast
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("Error in UDP receive: " + ex.Message);
            }
        }
    }

    // This method will be called by tracked objects to send their data
    public void SendObjectData(byte[] data)
    {
        if (stream != null && stream.CanWrite)
        {
            stream.Write(data, 0, data.Length);
        }

    }

    private void ReceiverThread()
    {
        while (continueReceiving)   // Outer loop to restart the connection if it is lost
        {
            try
            {
                Debug.Log("Attempting to connect to server: " + ServerIpAddress);

                // Wait for the server IP address to be received from the UDP broadcast
                if (string.IsNullOrEmpty(ServerIpAddress))
                {
                    Debug.LogWarning("Server IP address not received yet.");
                    Thread.Sleep(1000); // Wait for a while before retrying
                    continue;
                }

                // If we get here then we have a valid IP address

                client = new TcpClient();
                client.Connect(ServerIpAddress, Port);
                if (!client.Connected)
                {
                    Debug.LogError("Failed to connect to server.");
                    continue;
                }
                stream = client.GetStream();
                //If we get here, a connection has been established with server
                while (continueReceiving)
                {
                    // All messages header is
                    // magic number (2 bytes)
                    // message type (1 byte)
                    // message size (4 bytes)
                    // total 7 bytes

                    // Receive magic number
                    byte[] magicData = new byte[2];
                    int bytesRead = 0;
                    while (bytesRead < 2)
                    {
                        bytesRead += stream.Read(magicData, bytesRead, 2 - bytesRead);
                    }
                    ushort magic = BitConverter.ToUInt16(magicData, 0);

                    if (magic != 0xABCD)
                    {
                        Debug.LogError("Received invalid magic number: " + magic);
                        continue;
                    }
                    else
                    {
                        // print out the magic number
                        //Debug.Log("Received magic number: " + magic);
                    }

                    // Receive rest of header data
                    // have already read 2 bytes for the magic number
                    // so read 5, 1 for message type and 4 for message size
                    byte[] headerData = new byte[5];
                    bytesRead = 0;
                    while (bytesRead < 5)
                    {
                        bytesRead += stream.Read(headerData, bytesRead, 5 - bytesRead);
                    }
                    byte msgType = headerData[0];
                    int msgSize = (int)BitConverter.ToUInt32(headerData, 1);
                    // // Convert to host order?

                    // // print out the msgType as a binary string
                    //Debug.Log("Received message type: " + Convert.ToString(msgType, 2));
                    //Debug.Log("Received message size: " + msgSize);

                    // Note stream.read requires int, not uint

                    //Now accept the rest of the message data (actual data to be processed) based on the given size
                    byte[] dataPacket = new byte[msgSize];
                    bytesRead = 0;
                    while (bytesRead < msgSize)
                    {
                        bytesRead += stream.Read(dataPacket, bytesRead, msgSize - bytesRead);
                    }

                    // Rest should be message type dependant and contained in dataPacket               
                    switch (msgType)
                    {
                        case MSG_TYPE_IMAGE: //for video streams
                            ProcessImagePacket(dataPacket, msgSize);
                            break;
                        case MSG_TYPE_OBJECT: //any tracked objects
                            ProcessObjectPacket(dataPacket, msgSize);
                            break;
                        case MSG_TYPE_MESH_DATA: //any stl files
                            ProcessMeshData(dataPacket, msgSize);
                            break;
                        default:
                            
                            Debug.LogError("Unknown message type received: " + Convert.ToString(msgType, 2));
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                // Ignore now as this will throw if the connection is closed
                Debug.LogError("An exception was thrown by the network stream: " + e.Message);
            }
            finally
            {
                client.Close();
            }   // end try loop
        }   // end while loop
    }

    // Methods for decoding specific message types
    private void ProcessImagePacket(byte[] messageData, int messageSize) //video stream related, not necessary but doesn't hurt to keep
    {
        byte id = messageData[0];
        int offset = 1;

        ushort received_width = ParseUShortFromByteArray(messageData, offset);
        offset += sizeof(ushort);

        ushort received_height = ParseUShortFromByteArray(messageData, offset);
        offset += sizeof(ushort);

        // Get the image data from the message
        byte[] imgData = new byte[messageSize - offset];
        Array.Copy(messageData, offset, imgData, 0, messageSize - offset);

        int imageSize = messageSize - offset;

        // Print that have recevie image data of size
        //Debug.Log("Received image data of size " + imageSize + " bytes.");

        // It is important that this stuff is done on the main threa
        // as it is Unity API calls and things like texture and object
        // ajustments will fail if not done on the main thread

        //for video stream specifically
          try
         {
             if (!h264Streams.ContainsKey(id))
             {
                 // This will execute on the main thread
                 MainThreadDispatcher.Enqueue(() =>
                 {
                     InitializeAndAddStream(id, received_width, received_height);
                 });

                 // now need to return and not process the frame
                 // otherwise could get a null reference if main thread has not
                 // had a chance to create the stream!!                
             }
             else
             {
                 // If we get here then decoder should be initialized
                 h264Stream videoStream = h264Streams[id];

                 // // Submit H.264 data to the decoder
                 int submitResult = videoStream.ProcessFrame(imgData, received_width, received_height);
                 if (submitResult != 0)  // Failed
                 {
                     Debug.LogWarning("Failed to get output from the decoder -- Probably just need more frames");
                     return;
                 }

                 // call GetOutput - will decode the frame as soon as it arrives
                 videoStream.GetOutput();
             }

         }
         catch (Exception ex)
         {
             Debug.LogError("An error occurred while processing the image packet: " + ex.Message);
         }

         // The textures in the stream object should now be valid. 
    }

    private void ProcessObjectPacket(byte[] messageData, int messageSize) //"Tracked Object" class o "PFObject" on pathfinder
    {

        // Deserialize - the first 16 bytes is the GUID
        byte[] guidBytes = new byte[16];
        Array.Copy(messageData, 0, guidBytes, 0, 16);
        Guid update_id = new Guid(guidBytes);

        // Deserialize the data
        int offset = 16;

        // First the object type
        byte objectType = messageData[offset];      // At the moment the object type only matters to the creation of the object, so dont pass it yet
        offset += 1;

        // Then the position, rotation and scale
        Vector3 position = new Vector3(
            BitConverter.ToSingle(messageData, offset),
            BitConverter.ToSingle(messageData, offset + 4),
            BitConverter.ToSingle(messageData, offset + 8)
        );
        offset += 12;

        Vector3 rotation = new Vector3(
            BitConverter.ToSingle(messageData, offset),
            BitConverter.ToSingle(messageData, offset + 4),
            BitConverter.ToSingle(messageData, offset + 8)
        );
        offset += 12;

        Vector3 scale = new Vector3(
            BitConverter.ToSingle(messageData, offset),
            BitConverter.ToSingle(messageData, offset + 4),
            BitConverter.ToSingle(messageData, offset + 8)
        );
        offset += 12;

        // Next is the streamID, which is a single byte
        byte streamIDByte = messageData[offset]; 
        // First cast to sbyte to interpret it as signed, then to int if needed
        int streamID = (sbyte)streamIDByte;

        // Print out the received data
        //Debug.Log("Received object update for " + update_id + " with position " + position + " and rotation " + rotation + " and scale " + scale + " and streamID " + streamID);

        // Use MainThreadDispatcher to execute on the main thread !! VERY IMPORTANT
        MainThreadDispatcher.Enqueue(() =>
        {
        // Loop through all TrackedObjects, if the object id = guid, then update it with the new data
        // only one should exist so return after updating
        // Maybe change this later to use a dictionary for faster lookup
        foreach (var obj in FindObjectsOfType<TrackedObject>())
        {
            if (obj.id == update_id.ToString() || obj.stream_id == streamID) //had modified to update if either guid or stream id matched, may not be needed to check streamid
            {
                //Debug.Log("Updating object " + obj.id);
                obj.ReceiveObjectUpdate(position, rotation, scale, streamID);
                return;
            }
        }
            //uiText.text += "\nObject type: " + objectType;
            Debug.Log("Object with id " + update_id.ToString() + " not found, creating new object");
            Debug.Log("Object type: " + objectType);

            // if we get here, then the object doesn't exist, so create it
            // Find the PersistenceHandler and call CreateObject -- there should be only 1 

            //is it possible to just move create obj function here or to tracked objects? that's all persistence handler seems to be used for
            //i don't think foreach is needed if getting rid of persistence handler? - did not end up getting rid of persistence handler
            foreach (var obj in FindObjectsOfType<PersistenceHandler>())
            {
                // Note "obj" here is the PersistenceHandler, not the object
                // Not sending "object type" yet so just hard code it for now as VideoPanel                

                // If the object type is a video stream, then create a video stream object
                GameObject createdObject;
                if (objectType == OBJECT_VIDEO_STREAM)
                {
                    createdObject = obj.CreateObject((string)"VideoPanel", update_id.ToString(), streamID);        // This creates the GAME OBJECT
                }
                else if (objectType == OBJECT_MESH)
                {
                    createdObject = obj.CreateObject((string)"MeshObject", update_id.ToString(), streamID);        // This creates the GAME OBJECT
                    
                }
                else
                {
                    Debug.LogError("Unknown object type received: " + objectType);
                    // Dump the data
                    return;
                }
            }
        }); 
    }
    private void ProcessMeshData(byte[] messageData, int messageSize)
    {   //would be great if guid could be used instead of stream id, for future
        //copy stream id
        int id = messageData[0];
        int offset = 1;


        //copy stl bytes into an array 
        byte[] meshData = new byte[messageSize - offset];
        Array.Copy(messageData, offset, meshData, 0, messageSize - offset);

        //int count = 0;
        int listCount = list.GetListCount();
        
        MainThreadDispatcher.Enqueue(() => {

            //added because of the duplicating models with multiple clients but I think it was an issue in persistence handler assigning streamid
            //this checking of list may not be needed
            if (listCount != 0)
            {
                List<GameObject> allObj = list.GetAll();
                foreach (GameObject gameObj in allObj)
                {
                    if(gameObj.GetComponent<TrackedObject>().stream_id == id)
                    {
                        GameObject parent = gameObj.gameObject; //get the gameobject that the tracked object script is attached to
                        parent.SetActive(true); //findobjectsoftype should only return active objects, but just in case
                        MeshLoader mesh = parent.GetComponent<MeshLoader>(); //get the meshloader script that should be attached to the object
                        mesh.LoadModel(meshData);//load the model again with the meshdata that was just sent

                        return;
                    }
                }
            }
            foreach (var trackObj in FindObjectsOfType<TrackedObject>())
            {
                //count++;Debug.Log(count+ "tracked objects so far");
                if (trackObj.stream_id == id)
                {
                    //Debug.Log("object with stream id: "+ trackObj.stream_id + "and guid: " +trackObj.id +"already exists");

                    //enter here if there is a tracked object already (should be if one client has already connected, and this is sending from another connected cliet)
                    //load the mesh of the tracked object again (make sure that all clients see the same object)
                    
                    GameObject parent = trackObj.gameObject; //get the gameobject that the tracked object script is attached to
                    parent.SetActive(true); //findobjectsoftype should only return active objects, but just in case
                    MeshLoader mesh = parent.GetComponent<MeshLoader>(); //get the meshloader script that should be attached to the object
                    mesh.LoadModel(meshData);//load the model again with the meshdata that was just sent

                    return;

                }

            }

            //if here, no mesh object already created.

            //basically create object function in persistence handler
            //uiText.text += ("No object with streamid" + id + "exists, creating one");
            Debug.Log("No object with streamid " + id + " exists, creating one");
            string objectID = Guid.NewGuid().ToString();

            GameObject obj = null;
            obj = Instantiate(meshPrototype); //create an instance of the inactive gameobject meshprototype (has meshloader preattached)
            obj.name = "Mesh:" + objectID;

            obj.transform.position = Vector3.zero;
            obj.transform.rotation = Quaternion.identity;
            obj.transform.localScale = Vector3.one;

            // Activate the object if the prototype was inactive
            obj.SetActive(true);



            // Add TrackedObject component and set its ID

            TrackedObject trackedObject = obj.AddComponent<TrackedObject>();
            trackedObject.id = objectID;
            trackedObject.object_type = OBJECT_MESH;
            streamCount++;
            trackedObject.stream_id = streamCount;

            //Load the mesh that was just sent
            MeshLoader meshLoader = obj.GetComponent<MeshLoader>();
            meshLoader.LoadModel(meshData);

            //if (!allObj.Contains(obj))
            //{
                list.Add(obj); //for associated info
            //}
           
        });
        //MAKE SURE TO USE MAIN THREAD DISPATCHER!!!!

    }

    //for video streaming/ remoting
    private void InitializeAndAddStream(int id, int width, int height)
    {

        // Even inside the enqueue, it's good to check again in case multiple calls were queued before the first had a chance to execute
        if (h264Streams.ContainsKey(id))
         {
             Debug.Log($"Stream with id {id} was added before this call could execute. Skipping.");
             return; // Skip if another task already created the stream
         }

        // Create a GameObject based on the id and add the h264Stream script to it
        // Find the game object called "h264StreamPrototype"           
        if (h264StreamHandler == null)
        {
            // Unable to find prototype
            Debug.LogError("Unable to find h264StreamPrototype in the scene.");
            return;
        }

        GameObject go = Instantiate(h264StreamHandler, Vector3.zero, Quaternion.identity);
        go.name = "h264Stream_" + id;
        h264Stream videoStream = go.GetComponent<h264Stream>();

        // Log that we have added the game object and the script
        Debug.Log("Added h264Stream script to GameObject: " + go.name);

        // Initialize the stream with the width and height
        try
         {
             int result = videoStream.Initialize(width, height);
             if (result != 0)
             {
                 Debug.LogError("Failed to initialize h264Stream for id " + id);
                 uiText.text += "\nFailed to initialize h264Stream for id " + id + " with width " + width + " and height " + height;

                 // delete the game object
                 Destroy(go);
                 return;
             }
             else
             {
                 Debug.Log("Initialized h264Stream for id " + id);
                 uiText.text += "\nInitialized h264Stream for id " + id + " with width " + width + " and height " + height;
             }
         }
         catch (Exception ex)
         {
             Debug.LogError("An error occurred while initializing h264Stream for id " + id + ": " + ex.Message);
             uiText.text += "\nAn error occurred while initializing h264Stream for id " + id + " with width " + width + " and height " + height + ": " + ex.Message;
         }



         // Add the stream to the list
         h264Streams.Add(id, videoStream);

         // Print the size of the h264Streams dictionary
         Debug.Log("h264Streams dictionary size: " + h264Streams.Count);

         // We are now referencing the h264Stream object MATERIAL, so dont need to
         // specifically handle textures in this class

    }



    /// ///////////////////////////////////////////////////////    Helper functions

    public static ushort ParseUShortFromByteArray(byte[] data, int startIndex)
    {
        if (data == null || data.Length < startIndex + 2)
            throw new ArgumentException("Invalid data array or startIndex.");

        return (ushort)((data[startIndex] << 8) | data[startIndex + 1]);
    }

    public static float ParseFloatFromByteArray(byte[] data, int startIndex)
    {
        if (data == null || data.Length < startIndex + 4)
            throw new ArgumentException("Invalid data array or startIndex.");

        return BitConverter.ToSingle(data, startIndex);
    }

}
