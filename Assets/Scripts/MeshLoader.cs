//David Hocking's code integrated in
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.XR.ARFoundation;
using MRTK.Tutorials.MultiUserCapabilities;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine.Rendering;
using System.Threading.Tasks;
using System.Threading;
using TMPro;
using System.Linq;
using Microsoft.MixedReality.Toolkit.UI.BoundsControlTypes;

#if WINDOWS_UWP // Only have these namespaces if on UWP devices <- may not need these anymore, was only using when getting meshes from hololens storage
using Windows.Storage; 
using System.Runtime.InteropServices.WindowsRuntime;
#endif

public class MeshLoader : MonoBehaviour
{
    public Material objectMaterial;
    public GameObject dynamicDescription;
    TextMeshPro text;
    private Mesh mesh = null;
    ObjectList list;

    //private PhotonRoom photonRoom;

    // Start is called before the first frame update
    void Start()
    {
        text = dynamicDescription.GetComponent<TextMeshPro>(); //for if you need to display any messages to user in hololens app
        list = GameObject.Find("ObjectList").GetComponent<ObjectList>(); 

    }
    public void LoadModel(byte[] array)
    {
        list = GameObject.Find("ObjectList").GetComponent<ObjectList>(); //for some reason, list was coming up unassinged?? start may not have been called
        mesh = ReadByteSTL(array); //create the mesh
        AddMesh(); //add it to object (make it viewable in scene)  
        list.CheckRenderStatus(); //check and make sure that only one mesh is rendered in scene
    }
    

    private void AddMesh() { 
        if (mesh != null)
        {
            gameObject.SetActive(true); // Make sure the GameObject (that the script is attached to) is active

            //meshfilter and meshrenderer needed for mesh to show
            MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

            MeshRenderer meshRenderer = gameObject.GetComponent<MeshRenderer>();
            if (meshRenderer == null) meshRenderer = gameObject.AddComponent<MeshRenderer>();
            
            //assign mesh
            meshFilter.mesh = mesh;

            // Use the custom material if it has been assigned in the editor
            if (objectMaterial != null)
            {
                meshRenderer.material = objectMaterial;
            }
            else
            {
                Debug.Log("No material set, using standard material");
                // Fallback to a default material if no custom material is provided
                meshRenderer.material = new Material(Shader.Find("Standard"));
            }

            // Add components to GameObject
            AddComponents(mesh);
            //Debug.Log("Loaded STL file: " + name);
            return;
        }
        else
        {
            
            Debug.LogError("Failed to load STL file.");
            return;
        }


    }
    //detachmesh and retachmesh intended for use with objectlist
   
    
    private void AddComponents(Mesh mesh)
    {
        try
        {   //check if components are already on gameobject before adding them
            AddBoxCollider(mesh); 
            //parent table anchor may not be needed, left over from photon multiuser functionality, but it is good for keeping an "origin" of sorts
            TableAnchorAsParent anchorComponent = gameObject.GetComponent<TableAnchorAsParent>();
            if (anchorComponent == null)
            {
                gameObject.AddComponent<TableAnchorAsParent>();
            }
            
            //following are to make sure users can interact with object
            NearInteractionGrabbable grabbableComponent = gameObject.GetComponent<NearInteractionGrabbable>();
            if (grabbableComponent == null)
            {
                gameObject.AddComponent<NearInteractionGrabbable>();
            }
           ConstraintManager manager = gameObject.GetComponent<ConstraintManager>();
            if (manager == null)
            {
                gameObject.AddComponent<ConstraintManager>();
            }
            ObjectManipulator manipulator = gameObject.GetComponent<ObjectManipulator>();
            if (manipulator == null)
            {
                gameObject.AddComponent<ObjectManipulator>();
            }
            BoundsControl bounds = gameObject.GetComponent<BoundsControl>();
            if (bounds == null)
            {
                bounds = gameObject.AddComponent<BoundsControl>(); 
                bounds.BoundsControlActivation = BoundsControlActivationType.ActivateManually;
            }
           

            //persistence handler  (create object) already adds a tracked object class
            //if that functionality is removed from create object for some reason use:

           /*TrackedObject trackedObject = meshObject.GetComponent<TrackedObject>();
            if (trackedObject == null) { 
                meshObject.AddComponent<TrackedObject>(); 
            }*/

            //enables voice command transformations, would be for all objects active in scene at the time
            VoiceTransformController voiceTransform = gameObject.GetComponent<VoiceTransformController>();
            if (voiceTransform == null)
            {
                gameObject.AddComponent<VoiceTransformController>();
            }
            

        }
        catch (Exception ex)
        {
            Debug.LogWarning("Error adding components: " + ex.Message);
        }
    }

    private void AddBoxCollider(Mesh mesh) 
    {
        BoxCollider collider = gameObject.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<BoxCollider>();
        }
        collider.center = mesh.bounds.center;
        collider.size = mesh.bounds.size;
    }

    
    private Mesh ReadByteSTL(byte[] data) //if using the tcp transfer, use this probably?
    {
        if (data == null)
        {
            Debug.LogError("No data packet received");
            return null;
        }

        try
        {

            //Skip header
            int indexCounter = 80;

            //Read # of triangles
            uint triangleCount = BitConverter.ToUInt32(data, indexCounter);
            indexCounter += sizeof(uint);

            Vector3[] vertices = new Vector3[triangleCount * 3];
            int[] triangles = new int[triangleCount * 3];

            //Read triangles
            for (int i = 0; i < triangleCount; i++)
            {
                //Skip normal
                indexCounter += 12;

                float baseScale = 0.01f;

                // Read vertices
                for (int j = 0; j < 3; j++)
                {
                    float x = BitConverter.ToSingle(data, indexCounter) * baseScale;
                    indexCounter += sizeof(float);
                    float y = BitConverter.ToSingle(data, indexCounter) * baseScale;
                    indexCounter += sizeof(float);
                    float z = BitConverter.ToSingle(data, indexCounter) * baseScale;
                    indexCounter += sizeof(float);
                    vertices[i * 3 + j] = new Vector3(x, y, z);
                }

                //skip attribute
                indexCounter += sizeof(UInt16);

                //Setup triangles
                triangles[i * 3] = i * 3;
                triangles[i * 3 + 1] = i * 3 + 1;
                triangles[i * 3 + 2] = i * 3 + 2;
            }
            //set up mesh from interpreted bytes
            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            
            return mesh;
        }
        catch (IOException e)
        {
            Debug.LogError("Error reading STL file: " + e.Message);
            
            return null;
        }
    }


}
