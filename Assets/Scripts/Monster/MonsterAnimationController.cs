using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Monster))]
public class MonsterAnimationController : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCollider;
    private MonsterName _name;
    private MonsterSpriteDatabase database;
    private Monster monster;

    private float frameRate = 0.2f;
    private float timer;
    private int currentFrame;

    private MonsterState syncedState;

    private void Awake()
    {
        monster = GetComponent<Monster>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();
        database = MonsterSpriteDatabase.Instance;
    }

    public void InitializeMonster()
    {
        syncedState = monster.currentState;
        _name = monster.monsterName;
        database.LoadSprites(_name);
        AdjustColliderToSprite();
    }

    public void AdjustColliderToSprite()
    {
        if (spriteRenderer.sprite == null)
        {
            Debug.LogWarning("Không có Sprite để điều chỉnh Collider.");
            return;
        }
        Sprite idleSprite = database.GetSprites(_name, MonsterState.Idle)[0];
        Bounds spriteBounds = idleSprite.bounds;

        boxCollider.size = spriteBounds.size;

        boxCollider.offset = spriteBounds.center;
    }
    void Update()
    {
        PlayAnimation(monster.currentState);
    }


    private void PlayAnimation(MonsterState state)
    {
        if (database == null) return;

        timer += Time.deltaTime;
        if (timer >= frameRate)
        {
            timer = 0f;
            currentFrame++;
        }

        List<Sprite> frames = null;

        frames = database.GetSprites(_name, state);

        if ((frames == null || frames.Count == 0))
        {
            frames = database.GetSprites(_name, MonsterState.Idle);
        }
        else
        {
            int frameIndex = currentFrame % frames.Count;
            spriteRenderer.sprite = frames[frameIndex];
        }
    }

}
