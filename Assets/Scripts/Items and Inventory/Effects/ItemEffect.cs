 using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ItemEffect : ScriptableObject
{
    
    public virtual void ExecuteEffect(Transform _respondPosition)
    {
        Debug.Log("Base Item Effect Executed");
    }


}
