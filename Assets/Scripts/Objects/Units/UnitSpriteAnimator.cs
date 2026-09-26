using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(UnitCore), typeof(UnitMovement), typeof(SpriteRenderer))]
public sealed class UnitSpriteAnimator : MonoBehaviour
{
    private UnitCore core;
    private UnitMovement movement;
    private UnitCombat combat;
    private UnitHealth health;
    private SpriteRenderer spriteRenderer;
    private UnitAnimationSet animationSet;
    private Vector3 lastPosition;
    private float elapsed;
    private int frame;
    private bool attacking;
    private bool down;

    private void Awake()
    {
        core = GetComponent<UnitCore>();
        movement = GetComponent<UnitMovement>();
        combat = GetComponent<UnitCombat>();
        health = GetComponent<UnitHealth>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        lastPosition = transform.position;
        core.DataChanged += SetData;
        if (combat != null) combat.AttackPerformed += PlayAttack;
        if (core.Data != null) SetData(core.Data);
    }

    private void OnDestroy()
    {
        if (core != null) core.DataChanged -= SetData;
        if (combat != null) combat.AttackPerformed -= PlayAttack;
    }

    private void SetData(UnitData data)
    {
        animationSet = data.AnimationSet;
        elapsed = 0f;
        frame = 0;
        attacking = false;
        down = false;
        spriteRenderer.flipX = false;
        if (animationSet != null && (animationSet.idle != null || animationSet.walk.Length > 0))
            spriteRenderer.sprite = animationSet.idle != null
                ? animationSet.idle
                : animationSet.walk[0];
    }

    private void PlayAttack()
    {
        if (down || animationSet == null || animationSet.attack.Length == 0) return;
        attacking = true;
        elapsed = 0f;
        frame = 0;
        if (core.CurrentTarget != null)
        {
            float dx = core.CurrentTarget.transform.position.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.001f) spriteRenderer.flipX = dx < 0f;
        }
        spriteRenderer.sprite = animationSet.attack[0];
    }

    private void LateUpdate()
    {
        Vector3 position = transform.position;
        float dx = position.x - lastPosition.x;
        lastPosition = position;
        if (GameplayPauseController.IsPaused || animationSet == null) return;

        if (health != null && !health.IsAlive && !down)
        {
            down = true;
            attacking = false;
            elapsed = 0f;
            frame = 0;
        }

        if (down)
        {
            Advance(animationSet.down, false);
            return;
        }

        if (attacking)
        {
            if (Advance(animationSet.attack, false))
            {
                attacking = false;
                elapsed = 0f;
            }
            else return;
        }

        if (movement.IsMoving && animationSet.walk.Length > (animationSet.idle == null ? 1 : 0))
        {
            if (Mathf.Abs(dx) > 0.001f) spriteRenderer.flipX = dx < 0f;
            Advance(animationSet.walk, true);
        }
        else if (animationSet.idle != null || animationSet.walk.Length > 0)
        {
            frame = 0;
            elapsed = 0f;
            spriteRenderer.flipX = false;
            spriteRenderer.sprite = animationSet.idle != null
                ? animationSet.idle
                : animationSet.walk[0];
        }
    }

    // 별도 Idle이 없는 기존 시트만 walk의 첫 프레임을 반복에서 제외합니다.
    private bool Advance(Sprite[] sprites, bool walk)
    {
        if (sprites.Length == 0) return true;
        elapsed += Time.deltaTime;
        int first = walk && animationSet.idle == null ? 1 : 0;
        int next = first + Mathf.FloorToInt(elapsed * animationSet.framesPerSecond);
        bool finished = next >= sprites.Length;
        frame = walk ? first + (next - first) % (sprites.Length - first) : Mathf.Min(next, sprites.Length - 1);
        spriteRenderer.sprite = sprites[frame];
        return finished;
    }
}
