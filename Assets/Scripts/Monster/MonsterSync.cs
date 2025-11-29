using UnityEngine;
using Photon.Pun;

public class MonsterSync : MonoBehaviourPun, IPunObservable
{
    Vector3 networkPos;
    Vector3 velocity;

    void Start()
    {
        networkPos = transform.position;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // SEND from Master
            if (PhotonNetwork.IsMasterClient)
            {
                stream.SendNext(transform.position);
                stream.SendNext(velocity);
            }
        }
        else
        {
            // RECEIVE by non-master
            if (!PhotonNetwork.IsMasterClient)
            {
                networkPos = (Vector3)stream.ReceiveNext();
                velocity = (Vector3)stream.ReceiveNext();
            }
        }
    }

    void Update()
    {
        // Only non-master interpolate position
        if (!PhotonNetwork.IsMasterClient)
        {
            transform.position = Vector3.Lerp(transform.position, networkPos, Time.deltaTime * 10f);
        }
    }

    // Call this from MonsterAI to give speed direction
    public void SetVelocity(Vector3 v)
    {
        velocity = v;
    }
}
