using UnityEngine;
using Photon.Pun;

public class changeLocation : MonoBehaviourPun
{
    [PunRPC]
    public GameObject imageTarget;
    private Vector3 imageTargetPosition;
    public GameObject testingObj;
    


    public void UpdatePosition()
    {
        imageTargetPosition = imageTarget.transform.position;
        testingObj.transform.position = imageTargetPosition;
    }

    public void SomeMethod()
    {
        PhotonView photonView = testingObj.GetComponent<PhotonView>();
        photonView.RPC("UpdatePosition", RpcTarget.All);
    }

    private void Update()
    {
        SomeMethod();
    }
}