using UnityEngine;

[CreateAssetMenu(fileName = "FreezeEffect", menuName = "Data/ItemEffect/FreezeEffect")]
public class FreezeEnemis_Effect : ItemEffect
{
    [SerializeField]private float duration;
    [SerializeField]private float radius=2;

    [Range(0, 1)]
    [SerializeField]private float minHealthPercent;

     
    public override void ExecuteEffect(Transform _transform)
    {
        PlayerStats playerStats = _transform.GetComponent<PlayerStats>();
        Collider2D[] colliders = Physics2D.OverlapCircleAll(_transform.position, radius);
        foreach (var hit in colliders)
        {
            
            if (hit.GetComponent<Enemy>() != null&&(playerStats.currentHealth<=minHealthPercent*playerStats.maxHealth.GetValue()))
            {
                hit.GetComponent<Enemy>().FreezeTimeFor(duration);
            }
        }

    }
}

    

