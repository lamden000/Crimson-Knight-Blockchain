using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using Unity.VisualScripting;

public class SkillObject : MonoBehaviour
{
    public SkillObjectData data;

    public SkillAnimation mainAnim;
    public SkillAnimation sparkleAnim;
    public SkillAnimation aftermathAnim;

    private Transform target;
    private Vector3 mousePos;
    private Vector3 casterPos;
    private Coroutine mainAnimCor;
    private SkillMovementType movementType;
    public bool exploded { get; private set; } = false;

    private bool isExplosive = false;
    public float skyHeight = 200f;
    public System.Action<SkillObject> onExplode;
    public void Init(SkillObjectData d,Vector3 casterPos ,Vector3 mousePos, bool isExplosive,SkillMovementType movementType, Transform targetFollow = null)
    {
        data = d;
        this.target = targetFollow;
        this.mousePos = mousePos;
        this.casterPos = casterPos;
        this.isExplosive = isExplosive;
        this.movementType = movementType;
        transform.localScale= data.scale;
        PlayAnimations();
        StartCoroutine(AutoExplodeTimer());
        StartCoroutine(RunMovement());
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
        if (target == null) yield break;

        Vector2 dir = (Vector2)target.position - (Vector2)transform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        transform.rotation = Quaternion.Euler(0, 0, angle + 90f);

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (dir.x >= 0 ? -1 : 1);
        transform.localScale = scale;

        while (target != null && Vector3.Distance(transform.position, target.position) > 1f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                data.speed * Time.deltaTime
            );
            yield return null;
        }

        if (target != null)
            yield return Explosion();
    }



    IEnumerator Homing()
    {
        while (target != null && Vector3.Distance(transform.position, target.position) > 1f)
        {
            Vector2 dir = target.position - transform.position;
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            // sprite mặc định nhìn xuống → thêm +90
            transform.rotation = Quaternion.Euler(0, 0, angle + 90f);

            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                data.speed * Time.deltaTime
            );

            yield return null;
        }

        yield return Explosion();
    }

    IEnumerator Explosion()
    {
        exploded = true;

        // Rung camera nếu được bật
        if (data.shakeCameraOnExplode)
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
                afterCo = aftermathAnim.Play(data.aftermathFrames, data.aftermathFPS,data.aftermathLoop,true,data.aftermathPlayTime);
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
        Destroy(gameObject);
    }


    IEnumerator AutoExplodeTimer()
    {
        yield return new WaitForSeconds(data.autoExplosionTime);

        if (!exploded)
            StartCoroutine(Explosion());
    }
}
