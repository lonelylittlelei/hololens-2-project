using UnityEngine;

public class AudioController : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip artery;
    public AudioClip brain;
    public AudioClip coiling;

    private void Start()
    {
        // Assuming audioSource is assigned in the Inspector or programmatically
        audioSource = GetComponent<AudioSource>();
    }



    public void PlayAudio(string curObjectStr)
    {
        Debug.Log("audio: " + curObjectStr);
        if(curObjectStr == "artery")
            audioSource.clip = artery;

        else if(curObjectStr == "brain")
            audioSource.clip = brain;

        else if(curObjectStr == "video")
            audioSource.clip= coiling;

        audioSource.Play();
    }

    public void StopAudio() {
        audioSource.Stop();
    }
}
