using Assets.Scripts.Utils;
using Photon.Pun;
using Photon.Realtime;
using System;
using UnityEngine;

public class Character :MonoBehaviourPun
{
    public enum Clazz { Knight, Assassin, Markman, Wizard }

    [SerializeField]
    private Clazz m_Class;

    public Clazz GetClass()
    { return m_Class; }

    private PlayerMovementController m_Controller;
    public GameObject hitEffect;

    private void Start()
    {
        m_Controller = GetComponent<PlayerMovementController>();
    }

    public CharacterPart GetWeaponType()
    {
        CharacterPart weapon = CharacterPart.Gun;
        switch (m_Class)
        {
            case Clazz.Assassin:
                weapon = CharacterPart.Knive;
                break;
            case Clazz.Knight:
                weapon = CharacterPart.Sword;
                break;
            case Clazz.Wizard:
                weapon = CharacterPart.Staff;
                break;
            case Clazz.Markman:
                weapon = CharacterPart.Gun;
                break;
        }

        return weapon;
    }

    public void TakeDamage(float damage, GameObject attacker)
    {
        var pv = GetComponent<PhotonView>();
        pv.RPC("RPC_TakeDamage", RpcTarget.All, damage, this.photonView.ViewID);
    }
    [PunRPC]
    public void RPC_TakeDamage(float damage, int attackerID)
    {
        m_Controller.HandleGetHit();

        GameObject hit = Instantiate(hitEffect, transform.position + new Vector3(0, 50, 0), Quaternion.identity);
        Destroy(hit, 0.5f);
    }

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                Vector3 targetPosition = new Vector3(1034.6f, 481.5147f, transform.position.z);
                transform.position = targetPosition;
            }
        }
    }


  
}
