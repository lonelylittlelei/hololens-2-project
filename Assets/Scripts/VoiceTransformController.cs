using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

/*
 *  Voice Commands Controller for transforming and manipulating the Medical Model
 */
public class VoiceTransformController : MonoBehaviour, IMixedRealitySpeechHandler
{

    [SerializeField] public GameObject obj;
    private GameObject table;

    private Vector3 startingPosition;
    private Vector3 startingScale;
    private Vector3 startingRotation;

    [SerializeField] private float scaleFactor = 0.003f;
    [SerializeField] private int rotationFactor = 90;

    void Start()
    {
        CoreServices.InputSystem?.RegisterHandler<IMixedRealitySpeechHandler>(this);
        table = GameObject.Find("Table");

        startingPosition = obj.transform.position;
        startingScale = obj.transform.localScale;
        startingRotation = obj.transform.eulerAngles;
    }

    public void OnSpeechKeywordRecognized(SpeechEventData eventData)

    {

        switch (eventData.Command.Keyword.ToLower())

        {

            case "scale up":

                obj.transform.localScale += new Vector3(scaleFactor, scaleFactor, scaleFactor);
                Debug.Log("Scale UP");

                break;

            case "scale down":

                obj.transform.localScale -= new Vector3(scaleFactor, scaleFactor, scaleFactor);

                break;

            case "rotate left":

                obj.transform.eulerAngles = new Vector3(
                    obj.transform.eulerAngles.x,
                    obj.transform.eulerAngles.y + rotationFactor,
                    obj.transform.eulerAngles.z
                );

                break;

            case "rotate right":

                obj.transform.eulerAngles = new Vector3(
                    obj.transform.eulerAngles.x,
                    obj.transform.eulerAngles.y - rotationFactor,
                    obj.transform.eulerAngles.z
                );

                break;

            case "rotate up":

                obj.transform.eulerAngles = new Vector3(
                    obj.transform.eulerAngles.x + rotationFactor,
                    obj.transform.eulerAngles.y,
                    obj.transform.eulerAngles.z
                );

                break;

            case "rotate down":

                obj.transform.eulerAngles = new Vector3(
                    obj.transform.eulerAngles.x - rotationFactor,
                    obj.transform.eulerAngles.y,
                    obj.transform.eulerAngles.z
                );

                break;

            case "reset":

                resetObject();

                break;

            default:

                Debug.Log($"Unknown option { eventData.Command.Keyword}");

                break;

        }

    }

    public void resetObject()
    {
        obj.transform.position = new Vector3(table.transform.position.x, table.transform.position.y + 0.25f, table.transform.position.z);
        obj.transform.localScale = startingScale;
        obj.transform.eulerAngles = startingRotation;
    }
}