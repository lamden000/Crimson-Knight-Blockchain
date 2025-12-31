using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using Unity.VisualScripting;
using Photon.Pun;

public class SkillObject : MonoBehaviourPun, IPunObservable, IPunInstantiateMagicCallback
{
    public SkillObjectData data;

    public SkillAnimation mainAnim;
    public SkillAnimation sparkleAnim;
    public SkillAnimation aftermathAnim;

    private Transform target;
    private int targetViewID = -1; // PhotonView ID của target
    private Vector3 mousePos;
    private Vector3 casterPos;
    private Coroutine mainAnimCor;
    private SkillMovementType movementType;
    public bool exploded { get; private set; } = false;

    private bool isExplosive = false;
    public float skyHeight = 200f;
    public System.Action<SkillObject> onExplode;

    // Network sync variables
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private Vector3 networkScale;
    private bool networkDataInitialized = false; // Flag để biết đã nhận data từ network chưa

    public void Init(SkillObjectData d, Vector3 casterPos, Vector3 mousePos, bool isExplosive, SkillMovementType movementType, Transform targetFollow = null)
    {
        data = d;
        this.target = targetFollow;
        this.mousePos = mousePos;
        this.casterPos = casterPos;
        this.isExplosive = isExplosive;
        this.movementType = movementType;

        // Lưu target ViewID để sync
        if (targetFollow != null)
        {
            PhotonView targetPV = targetFollow.GetComponent<PhotonView>();
            if (targetPV != null)
            {
                targetViewID = targetPV.ViewID;
            }
        }

        if (data != null)
        {
            transform.localScale = data.scale;
        }

        // Chỉ local player điều khiển movement
        if (photonView != null && photonView.IsMine)
        {
            PlayAnimations();
            StartCoroutine(AutoExplodeTimer());
            StartCoroutine(RunMovement());
        }
        else if (photonView != null)
        {
            // Remote client: chỉ play animation, không điều khiển movement
            // Target sẽ được sync qua OnPhotonSerializeView
            PlayAnimations();
        }
        else
        {
            // Không có PhotonView: chạy như bình thường (single player)
        PlayAnimations();
        StartCoroutine(AutoExplodeTimer());
        StartCoroutine(RunMovement());
        }
    }

    /// <summary>
    /// Callback khi object được instantiate qua PhotonNetwork
    /// </summary>
    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        // Remote client nhận object với InstantiationData
        if (photonView != null && !photonView.IsMine && info.photonView.InstantiationData != null && info.photonView.InstantiationData.Length > 0)
        {
            object[] data = info.photonView.InstantiationData;
            
            // Parse InstantiationData: [skillID, targetViewID, casterPos, mousePos, isExplosive, movementType]
            int skillID = (int)data[0];
            int targetID = (int)data[1];
            Vector3 casterPos = (Vector3)data[2];
            Vector3 mousePos = (Vector3)data[3];
            bool isExplosive = (bool)data[4];
            SkillMovementType movementType = (SkillMovementType)(int)data[5];

            // Set position ngay lập tức (không để interpolate từ 0,0,0)
            // Position đã được set khi PhotonNetwork.Instantiate, nhưng cần đảm bảo networkPosition cũng được set
            networkPosition = transform.position;
            networkRotation = transform.rotation;
            networkScale = transform.localScale;

            // Load SkillObjectData từ SkillDatabase
            if (SkillDatabase.Instance != null)
            {
                SkillObjectData skillData = SkillDatabase.Instance.GetSkillByID(skillID);
                if (skillData != null)
                {
                    this.data = skillData;
                    this.casterPos = casterPos;
                    this.mousePos = mousePos;
                    this.isExplosive = isExplosive;
                    this.movementType = movementType;

                    // Tìm target từ ViewID
                    if (targetID >= 0)
                    {
                        PhotonView targetPV = PhotonView.Find(targetID);
                        if (targetPV != null)
                        {
                            target = targetPV.transform;
                            targetViewID = targetID;
                        }
                    }

                    // Set scale và play animation
                    transform.localScale = skillData.scale;
                    networkScale = skillData.scale; // Cũng set networkScale để tránh interpolate
                    PlayAnimations();
                }
                else
                {
                    Debug.LogWarning($"[SkillObject] Không tìm thấy SkillObjectData với ID: {skillID}");
                }
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Gửi data từ local player
            if (photonView != null && photonView.IsMine)
            {
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
                stream.SendNext(transform.localScale);
                stream.SendNext(targetViewID);
                stream.SendNext(exploded);
            }
        }
        else
        {
            // Nhận data từ remote client
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
            networkScale = (Vector3)stream.ReceiveNext();
            targetViewID = (int)stream.ReceiveNext();
            bool remoteExploded = (bool)stream.ReceiveNext();

            // Đánh dấu đã nhận data từ network
            if (!networkDataInitialized)
            {
                networkDataInitialized = true;
                // Set position ngay lập tức lần đầu tiên (không interpolate)
                transform.position = networkPosition;
                transform.rotation = networkRotation;
                transform.localScale = networkScale;
            }

            // Update target từ ViewID
            if (targetViewID >= 0)
            {
                PhotonView targetPV = PhotonView.Find(targetViewID);
                if (targetPV != null)
                {
                    target = targetPV.transform;
                }
            }

            // Nếu remote đã explode, trigger explosion
            if (remoteExploded && !exploded)
            {
                StartCoroutine(Explosion());
            }
        }
    }

    void Update()
    {
        // Remote client: interpolate position từ network
        if (photonView != null && !photonView.IsMine)
        {
            // Chỉ interpolate nếu đã nhận data từ network (tránh interpolate từ 0,0,0)
            if (networkDataInitialized)
            {
                transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10f);
                transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 5f);
                transform.localScale = Vector3.Lerp(transform.localScale, networkScale, Time.deltaTime * 5f);
            }
        }
    }

    void PlayAnimations()
    {
        // MAIN
        mainAnimCor= mainAnim.Play(data.mainFrames, data.mainFPS,data.mainLoop,data.autoDisableAfterMain);

        // AFTERMATH (ảnh hưởng sau nổ)
        aftermathAnim.sr.enabled = false;
    }

    IEnumerator RunMovement()
    {
        switch (movementType)
        {
            case SkillMovementType.Projectile:
                yield return Projectile();
                break;

            case SkillMovementType.Homing:
                if(target == null)
                {
                    yield return Projectile();
                }
                yield return Homing();
                break;

            case SkillMovementType.PersistentArea:
                yield return PersistentAreaRoutine();
                break;
        }
    }

    IEnumerator PersistentAreaRoutine()
    {
       // Nếu mainLoop = true → main animation sẽ loop mãi → không bao giờ explode
        // nên tôi fallback: nếu mainLoop = true → explode sau autoExplosionTime
        if (data.mainLoop)
        {
            yield return new WaitForSeconds(data.autoExplosionTime);
            yield return Explosion();
            yield break;
        }

        // Đợi main animation chạy hết
        if (mainAnimCor != null)
            yield return mainAnimCor;

        // Nổ ngay sau main animation
        yield return Explosion();
    }


    IEnumerator Projectile()
    {
        // Chỉ chạy movement nếu là owner hoặc không có PhotonView
        if (photonView != null && !photonView.IsMine)
        {
            yield break; // Remote client không chạy movement
        }

        if (target == null)
        {
            // Không có target, explode ngay
            yield return Explosion();
            yield break;
        }

        Vector2 dir = (Vector2)target.position - (Vector2)transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, angle + 90f);

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (dir.x >= 0 ? -1 : 1);
        transform.localScale = scale;

        float maxDistance = 1000f; // Max distance để tránh bay vô tận
        float traveledDistance = 0f;

        while (target != null && Vector3.Distance(transform.position, target.position) > 1f && traveledDistance < maxDistance)
        {
            float moveDistance = data.speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                moveDistance
            );
            traveledDistance += moveDistance;
            yield return null;
        }

        // Explode khi đến target hoặc quá xa
            yield return Explosion();
    }



    IEnumerator Homing()
    {
        // Chỉ chạy movement nếu là owner hoặc không có PhotonView
        if (photonView != null && !photonView.IsMine)
        {
            yield break; // Remote client không chạy movement
        }

        if (target == null)
        {
            // Không có target, explode ngay
            yield return Explosion();
            yield break;
        }

        float maxDistance = 1000f; // Max distance để tránh bay vô tận
        float traveledDistance = 0f;

        while (target != null && Vector3.Distance(transform.position, target.position) > 1f && traveledDistance < maxDistance)
        {
            Vector2 dir = target.position - transform.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // sprite mặc định nhìn xuống → thêm +90
            transform.rotation = Quaternion.Euler(0, 0, angle + 90f);

            float moveDistance = data.speed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                moveDistance
            );
            traveledDistance += moveDistance;

            yield return null;
        }

        // Explode khi đến target hoặc quá xa
        yield return Explosion();
    }

    IEnumerator Explosion()
    {
        exploded = true;

        // Rung camera nếu được bật (chỉ cho local player)
        if (data.shakeCameraOnExplode && (photonView == null || photonView.IsMine))
        {
            CameraFollow cameraFollow = Camera.main?.GetComponent<CameraFollow>();
            if (cameraFollow != null)
            {
                // Sử dụng duration và magnitude từ SkillObjectData
                cameraFollow.ShakeCamera(data.cameraShakeDuration, data.cameraShakeMagnitude);
            }
        }

        if (isExplosive)
        {
            Coroutine afterCo = null;
            Coroutine sparkleCo = null;

            mainAnim.gameObject.SetActive(false);

            if (!data.explosionRotatesWithMovement)
                transform.rotation = Quaternion.Euler(0, 0, 0);

            // AFTERMATH
            if (data.aftermathFrames != null && data.aftermathFrames.Length > 0)
            {
                afterCo = aftermathAnim.Play(data.aftermathFrames, data.aftermathFPS, data.aftermathLoop, true, data.aftermathPlayTime);
            }

            // SPARKLE
            if (data.sparkleFrames != null && data.sparkleFrames.Length > 0)
            {
                sparkleCo = sparkleAnim.Play(data.sparkleFrames, data.sparkleFPS);
            }
            else
            {
                sparkleAnim.sr.enabled = false;
            }

            // Chờ aftermath chạy xong
            if (afterCo != null)
                yield return afterCo;

            // Chờ sparkle chạy xong
            if (sparkleCo != null)
                yield return sparkleCo;
        }
        onExplode?.Invoke(this);

        // Destroy qua PhotonNetwork nếu có PhotonView
        if (photonView != null && photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
        Destroy(gameObject);
        }
    }


    IEnumerator AutoExplodeTimer()
    {
        yield return new WaitForSeconds(data.autoExplosionTime);

        if (!exploded)
            StartCoroutine(Explosion());
    }
}
