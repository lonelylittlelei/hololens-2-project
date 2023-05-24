using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using MRTK.Tutorials.MultiUserCapabilities;
using UnityEngine;

/*
 *  Voice Commands Controller for transforming and manipulating the spatial anchors and table
 */
public class VoiceAnchorController : MonoBehaviour, IMixedRealitySpeechHandler
{

    void Start()
    {
        CoreServices.InputSystem?.RegisterHandler<IMixedRealitySpeechHandler>(this);
    }

    public void OnSpeechKeywordRecognized(SpeechEventData eventData)

    {

        switch (eventData.Command.Keyword.ToLower())

        {

            case "create anchor":

                gameObject.GetComponent<SpatialAnchorController>().ButtonShortTap();

                break;

            case "locate anchor":

                gameObject.GetComponent<SpatialAnchorController>().LongTap();

                break;

            default:

                Debug.Log($"Unknown option { eventData.Command.Keyword}");

                break;

        }

    }
}
