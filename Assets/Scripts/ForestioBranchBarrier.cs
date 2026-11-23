using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public sealed class ForestioBranchBarrier : MonoBehaviour
{
    public bool IsBroken { get; private set; }

    public bool TryBreak(ForestioStoneProjectile projectile)
    {
        if (IsBroken || projectile == null || !projectile.CanHitTarget || !projectile.IsCharged)
            return false;

        IsBroken = true;
        projectile.ConsumeOnHit();
        gameObject.SetActive(false);
        return true;
    }
}
