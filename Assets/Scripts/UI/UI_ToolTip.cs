using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UI_ToolTip : MonoBehaviour
{
    [SerializeField] private float xLimit=Screen.width*0.5f;
    [SerializeField] private float yLimit = Screen.height*0.5f;

    [SerializeField] private float xOffset;
    [SerializeField] private float yOffset;
    




    public virtual void  AdjustPosition()
    {
        Vector2 mousePosition=Input.mousePosition;

        float newXoffset = 0;
        float newYoffset = 0;

        if(mousePosition.x>xLimit)
        {
            newXoffset=-xOffset;
        }else
        {
            newXoffset = xOffset;
        }

        if (mousePosition.y > yLimit) 
        {
         newYoffset=-yOffset;
        }
        else
        {
            newYoffset = yOffset;
        }
        transform.position=new Vector2(mousePosition.x+newXoffset, mousePosition.y+newYoffset);
    }

}
