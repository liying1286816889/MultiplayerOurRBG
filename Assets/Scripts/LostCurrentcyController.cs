using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LostCurrentcyController : MonoBehaviour
{
    public int currentcy;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.GetComponent<Player>() != null)
        {
            PlayerManager.instance.currency += currentcy;
            NetWorld.Despawn(this.gameObject);
        }

    }

}
