using UnityEngine;
using Photon.Pun;

public class MonsterSync : MonoBehaviourPun, IPunObservable
{
    Vector3 networkPos;
    Vector3 velocity;
    private bool networkDataInitialized = false; // Flag để biết đã nhận data từ network chưa

    void Start()
    {
        // Không set networkPos ở đây nữa, sẽ được set từ network
        // networkPos sẽ được set trong OnPhotonSerializeView lần đầu tiên
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

                // Đánh dấu đã nhận data từ network
                if (!networkDataInitialized)
                {
                    networkDataInitialized = true;
                    // Set position ngay lập tức lần đầu tiên (không interpolate từ 0,0,0)
                    transform.position = networkPos;
                }
            }
        }
    }

    void Update()
    {
        // Only non-master interpolate position
        if (!PhotonNetwork.IsMasterClient)
        {
            // Chỉ interpolate nếu đã nhận data từ network (tránh interpolate từ 0,0,0)
            if (networkDataInitialized)
            {
                transform.position = Vector3.Lerp(transform.position, networkPos, Time.deltaTime * 10f);
            }
        }
    }

    // Call this from MonsterAI to give speed direction
    public void SetVelocity(Vector3 v)
    {
        velocity = v;
    }
}
