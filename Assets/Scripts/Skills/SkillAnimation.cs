using UnityEngine;
using System.Collections;
public class SkillAnimation : MonoBehaviour
{
    public SpriteRenderer sr;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public Coroutine Play(Sprite[] frames, float fps, bool loop = false, bool autoDisable = true, float playTime = 0f)
    {
        if (frames == null || frames.Length == 0)
        {
            sr.enabled = false;
            return null;
        }

        sr.enabled = true;
        return StartCoroutine(PlayAnim(frames, fps, loop, autoDisable, playTime));
    }

    IEnumerator PlayAnim(Sprite[] frames, float fps, bool loop, bool autoDisable, float playTime)
    {
        float delay = 1f / fps;
        float startTime = Time.time;

        bool usePlayTime = playTime > 0f;

        while (true)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                sr.sprite = frames[i];
                yield return new WaitForSeconds(delay);

                if (usePlayTime && Time.time - startTime >= playTime)
                {
                    if (autoDisable) sr.enabled = false;
                    yield break;
                }
            }

            if (!loop) break;
        }

        if (autoDisable)
            sr.enabled = false;
    }
}
