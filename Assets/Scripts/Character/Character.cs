using Assets.Scripts.Utils;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using UnityEngine;

public class Character :MonoBehaviourPun
{
    public enum Clazz { Knight, Assassin, Markman, Wizard }

    [SerializeField]
    private Clazz m_Class;

    public Clazz GetClass()
    { return m_Class; }

    public int damage = 10;

    [Header("Health Settings")]
    [SerializeField] private int maxHP = 100;
    public int currentHealth { private set; get; }
    public int maxHP_public => maxHP; // Public property để UI có thể truy cập

    [Header("Respawn Settings")]
    [SerializeField] private float respawnDelay = 2f; // Thời gian chờ trước khi respawn

    private PlayerMovementController m_Controller;
    private HealthBar healthBar;
    private bool isDead = false;
    private Vector3 spawnPosition; // Vị trí spawn ban đầu
    private bool spawnPositionSet = false;
    public GameObject hitEffect;

    public bool IsDead => isDead;

    private void Start()
    {
        m_Controller = GetComponent<PlayerMovementController>();
        healthBar = GetComponentInChildren<HealthBar>();
        
        // Lưu vị trí spawn ban đầu (chỉ lần đầu tiên)
        if (!spawnPositionSet)
        {
            spawnPosition = transform.position;
            spawnPositionSet = true;
        }
        
        // Initialize health
        currentHealth = maxHP;
        
        // Sync initial health to all clients
        if (photonView != null && photonView.IsMine)
        {
            photonView.RPC(nameof(RPC_SyncHealth), RpcTarget.AllBuffered, currentHealth, maxHP);
        }
        
        // Update UI health bar on start (for local player)
        if (photonView != null && photonView.IsMine)
        {
            UpdateUIHealthBar();
        }
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
        if (isDead || photonView == null) return;
        
        // Send damage request to owner's client to process (authoritative)
        int attackerID = attacker != null && attacker.GetComponent<PhotonView>() != null 
            ? attacker.GetComponent<PhotonView>().ViewID 
            : -1;
        photonView.RPC(nameof(RPC_ApplyDamage), RpcTarget.All, (int)damage, attackerID);
    }

    [PunRPC]
    void RPC_ApplyDamage(int dmg, int attackerID)
    {
        if (isDead) return;

        // Apply damage
        currentHealth -= dmg;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHP);

        // Sync health to all clients
        if (photonView != null)
        {
            photonView.RPC(nameof(RPC_SyncHealth), RpcTarget.AllBuffered, currentHealth, maxHP);
        }

        // Handle hit effect and animation
        if (m_Controller != null)
        {
            m_Controller.HandleGetHit();
        }

        if (hitEffect != null)
        {
            GameObject hit = Instantiate(hitEffect, transform.position + new Vector3(0, 50, 0), Quaternion.identity);
            Destroy(hit, 0.5f);
        }

        // Check for death
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    [PunRPC]
    void RPC_SyncHealth(int health, int max)
    {
        currentHealth = health;
        maxHP = max;

        // Update world-space health bar (nếu có)
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth, maxHP);
        }

        // Update UI health bar (screen-space)
        UpdateUIHealthBar();

        // Check for death state
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    private void UpdateUIHealthBar()
    {
        // Tìm PlayerHealthBarUI trong scene và update
        PlayerHealthBarUI uiHealthBar = FindAnyObjectByType<PlayerHealthBarUI>();
        if (uiHealthBar != null && photonView != null && photonView.IsMine)
        {
            uiHealthBar.OnHealthChanged(currentHealth, maxHP);
        }
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log($"[Character] {gameObject.name} DIED");

        // Disable movement and other components
        if (m_Controller != null)
        {
            m_Controller.enabled = false;
        }

        // Hide health bar
        if (healthBar != null)
        {
            healthBar.Hide();
        }

        // Tự động respawn sau một khoảng thời gian
        if (photonView != null && photonView.IsMine)
        {
            StartCoroutine(RespawnAfterDelay());
        }
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        Respawn();
    }

    public void Respawn()
    {
        if (!isDead) return;

        isDead = false;
        
        // Hồi đầy máu
        currentHealth = maxHP;

        // Đưa về vị trí spawn ban đầu
        if (spawnPositionSet)
        {
            transform.position = spawnPosition;
        }
        else
        {
            // Nếu chưa có spawn position, dùng vị trí hiện tại làm spawn (fallback)
            spawnPosition = transform.position;
            spawnPositionSet = true;
            Debug.LogWarning($"[Character] Spawn position chưa được set, sử dụng vị trí hiện tại: {spawnPosition}");
        }

        // Sync health và position
        if (photonView != null && photonView.IsMine)
        {
            photonView.RPC(nameof(RPC_SyncHealth), RpcTarget.AllBuffered, currentHealth, maxHP);
            // Sync position qua network
            photonView.RPC(nameof(RPC_SyncPosition), RpcTarget.All, transform.position);
        }

        // Re-enable movement
        if (m_Controller != null)
        {
            m_Controller.enabled = true;
        }

        // Show health bar
        if (healthBar != null)
        {
            healthBar.Show();
        }

        Debug.Log($"[Character] {gameObject.name} RESPAWNED at {transform.position}");
    }

    [PunRPC]
    void RPC_SyncPosition(Vector3 position)
    {
        transform.position = position;
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
