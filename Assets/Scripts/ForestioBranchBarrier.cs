using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class ForestioBranchBarrier : MonoBehaviour
{
    public bool IsBroken { get; private set; }
    public float LastNormalHitTime { get; private set; } = float.NegativeInfinity;

    public bool TryBreak(ForestioStoneProjectile projectile)
    {
        if (IsBroken || projectile == null || !projectile.CanHitTarget) return false;
        if (!projectile.IsCharged)
        {
            LastNormalHitTime = Time.time;
            return false;
        }

        IsBroken = true;
        projectile.ConsumeOnHit();
        gameObject.SetActive(false);
        return true;
    }
}
