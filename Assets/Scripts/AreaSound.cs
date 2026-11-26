using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaSound : MonoBehaviour
{
    [SerializeField] private int areaSoundIndex;
    private bool atArea;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.GetComponent<Player>()!=null)
        {
            atArea = true;

        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<Player>() != null)
        {
            atArea = false;
            AudioManager.instance.StopSFXWithTime(areaSoundIndex);
        }
    }

    private void Update()
    {
        if (atArea)
        {
            AudioManager.instance.PlaySFX(areaSoundIndex);
        }
    }


}
