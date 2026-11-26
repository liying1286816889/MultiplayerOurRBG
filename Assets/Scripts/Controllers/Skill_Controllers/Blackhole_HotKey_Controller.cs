using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Blackhole_HotKey_Controller : MonoBehaviour
{
    private SpriteRenderer sr;
    private KeyCode myHotKey;
    private TextMeshProUGUI myText;
    private Transform enemiseTransform;
    private Blackhole_Skill_Controller blackHole;

     

    public void SetupHotKey(KeyCode _myNewHotKey, Transform _myEnemy, Blackhole_Skill_Controller _myblackhole)
    {

        sr = GetComponent<SpriteRenderer>();
        myText = GetComponentInChildren<TextMeshProUGUI>();
        enemiseTransform = _myEnemy;
        blackHole = _myblackhole;

        myText.text=_myNewHotKey.ToString();
        myHotKey = _myNewHotKey;


    }
     
    private void Update()
    {
        // ¸úËæµÐÈËÎ»ÖÃ
        if (enemiseTransform != null)
        {
            transform.position = enemiseTransform.position + new Vector3(0, 2, 0);
        }

        if (Input.GetKeyDown(myHotKey))
        {
            blackHole.AddEnemyToList(enemiseTransform);
            myText.color = Color.clear;
        }
        if (Input.GetKeyDown(myHotKey))
        {
            blackHole.AddEnemyToList(enemiseTransform);

            myText.color= Color.clear;
            sr.color = Color.clear;
        }
    }
}
