using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class Scanning : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform roverExplorerLocation;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void updateObj(GameObject newObj) {


        if (PhotonNetwork.PrefabPool is DefaultPool pool)
        {
            if (newObj != null) pool.ResourceCache.Add(newObj.name, newObj);
        }



        var position = roverExplorerLocation.position;
        var positionOnTopOfSurface = new Vector3(position.x, position.y + 0.3f,
            position.z);

        var go = PhotonNetwork.Instantiate(newObj.name, positionOnTopOfSurface,
            Quaternion.identity);

    }
    
    
}
