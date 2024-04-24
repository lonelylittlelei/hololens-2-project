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

        startingPosition = gameObject.transform.position;
        startingScale = gameObject.transform.localScale;
        startingRotation = gameObject.transform.eulerAngles;
    }

    public void OnSpeechKeywordRecognized(SpeechEventData eventData)

    {

        switch (eventData.Command.Keyword.ToLower())

        {

            case "scale up":

                gameObject.transform.localScale += new Vector3(scaleFactor, scaleFactor, scaleFactor);
                Debug.Log("Scale UP");

                break;

            case "scale down":

                gameObject.transform.localScale -= new Vector3(scaleFactor, scaleFactor, scaleFactor);

                break;

            case "rotate left":

                gameObject.transform.eulerAngles = new Vector3(
                    gameObject.transform.eulerAngles.x,
                    gameObject.transform.eulerAngles.y + rotationFactor,
                    gameObject.transform.eulerAngles.z
                );

                break;

            case "rotate right":

                gameObject.transform.eulerAngles = new Vector3(
                    gameObject.transform.eulerAngles.x,
                    gameObject.transform.eulerAngles.y - rotationFactor,
                    gameObject.transform.eulerAngles.z
                );

                break;

            case "rotate up":

                gameObject.transform.eulerAngles = new Vector3(
                    gameObject.transform.eulerAngles.x + rotationFactor,
                    gameObject.transform.eulerAngles.y,
                    gameObject.transform.eulerAngles.z
                );

                break;

            case "rotate down":

                gameObject.transform.eulerAngles = new Vector3(
                    gameObject.transform.eulerAngles.x - rotationFactor,
                    gameObject.transform.eulerAngles.y,
                    gameObject.transform.eulerAngles.z
                );

                break;

            case "reset":

                resetObject();

                break;

            default:

                break;

        }

    }

    public void resetObject()
    {
        gameObject.transform.position = new Vector3(table.transform.position.x, table.transform.position.y + 0.25f, table.transform.position.z);
        gameObject.transform.localScale = startingScale;
        gameObject.transform.eulerAngles = startingRotation;
    }
}