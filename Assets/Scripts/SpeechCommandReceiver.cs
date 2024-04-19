using System.Linq;
using UnityEngine;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;

#if UNITY_WSA && !UNITY_EDITOR
using Windows.ApplicationModel.Core;            // used for restarting the app
#endif

using TMPro;    // For TextMeshProUGUI

public class SpeechCommandReceiver : MonoBehaviour, IMixedRealitySpeechHandler
{

    public GameObject LogPanel;

    public void UpdateLog(string message)
    {
        TextMeshPro textComponent = LogPanel.GetComponent<TextMeshPro>();
        string oldText = textComponent.text;
        textComponent.text = oldText + message;
    }

    public void OnSpeechKeywordRecognized(SpeechEventData eventData)
    {
        switch (eventData.Command.Keyword)
        {
            case "unlock":
                EnableInput();
                break;
            case "lock":
                DisableInput();
                break;
            case "reset":
                // Reset the screen to be in the center of the user's view
                ResetObjects();
                break;
            case "reinitialize":
                ResetScene();
                break;
        }
    }

    private void OnEnable()
    {
        // This registers the speech service with Unity
        CoreServices.InputSystem.RegisterHandler<IMixedRealitySpeechHandler>(this);
    }

    private void OnDisable()
    {
        // This is used to unregister the custom speech service with Unity
        CoreServices.InputSystem.UnregisterHandler<IMixedRealitySpeechHandler>(this);
    }

    private void DisableInput()
    {
        // Disable ObjectManipulator on "Plane" object
        //var planeObject = GameObject.Find("VideoQuad");
        //if (planeObject != null)
        //{
        //    var objectManipulator = planeObject.GetComponent<ObjectManipulator>();
        //    if (objectManipulator != null)
        //    {
        //        objectManipulator.enabled = false;
        //    }
        //}

        // Disable pointers
        PointerUtils.SetHandRayPointerBehavior(PointerBehavior.AlwaysOff);
    }


    private void EnableInput()
    {
        // Enable ObjectManipulator on "Plane" object
        //var planeObject = GameObject.Find("VideoQuad");
        //if (planeObject != null)
        //{
        //    var objectManipulator = planeObject.GetComponent<ObjectManipulator>();
        //    if (objectManipulator != null)
        //    {
        //        objectManipulator.enabled = true;
        //    }
        //}

        // // Enable pointers
        PointerUtils.SetHandRayPointerBehavior(PointerBehavior.AlwaysOn);
    }

    private void ResetObjects()
    {
        // Find all objects with the TrackedObject script attached
        TrackedObject[] trackedObjects = FindObjectsOfType<TrackedObject>();

        if (trackedObjects.Length == 0) return;

        // Get the camera's position and forward direction
        var cameraPosition = CameraCache.Main.transform.position;
        var cameraForward = CameraCache.Main.transform.forward;
        var cameraRight = CameraCache.Main.transform.right;

        float offset = 0.0f; // Initial offset

        for (int i = 0; i < trackedObjects.Length; i++)
        {
            var trackedObject = trackedObjects[i].gameObject;

            // Calculate the position for this object
            Vector3 newPosition;
            if (i == 0)
            {
                // First object directly in front of the camera
                newPosition = cameraPosition + cameraForward * 1.0f;
            }
            else
            {
                // Alternate between right and left placement
                float directionMultiplier = i % 2 == 0 ? 1 : -1;
                offset += trackedObjects[i - 1].gameObject.GetComponent<Renderer>().bounds.size.x / 2 +
                        trackedObject.GetComponent<Renderer>().bounds.size.x / 2;
                newPosition = cameraPosition + cameraForward * 1.0f + cameraRight * offset * directionMultiplier;
            }

            // Set the object's position
            trackedObject.transform.position = newPosition;

            // Set rotation to face the camera
            trackedObject.transform.rotation = Quaternion.LookRotation(cameraForward);


            // Adjust scale while preserving aspect ratio
            Vector3 originalScale = trackedObject.transform.localScale;
            float desiredHeight = 0.4f; // set desired height

            // Calculate scale factor
            float scaleFactor = desiredHeight / originalScale.y;

            // Apply scale factor to all dimensions to maintain proportions
            float newWidth = originalScale.x * scaleFactor;
            float newHeight = desiredHeight;
            float newDepth = originalScale.z * scaleFactor;

            // Update the object's scale
            trackedObject.transform.localScale = new Vector3(newWidth, newHeight, newDepth);

        }
    }


    private void ResetScene()
    {
        PersistenceHandler persistenceHandler = FindObjectOfType<PersistenceHandler>();
        if (persistenceHandler != null)
        {
            // Delete the save file
            persistenceHandler.DeleteSaveFile();

            // Force app restart
#if UNITY_WSA && !UNITY_EDITOR
            CoreApplication.RequestRestartAsync("");
#endif
        }
    }

}
