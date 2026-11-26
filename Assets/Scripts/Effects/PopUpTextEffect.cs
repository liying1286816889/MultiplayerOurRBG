using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PopUpTextEffect : MonoBehaviour
{
    private TextMeshPro myText;
    [SerializeField] private float speed;
    [SerializeField] private float disapperSpeed;
    [SerializeField] private float colordisapperSpeed;

    [SerializeField] private float lifeTime;

    private float textTimer;

    private void Start()
    {
        myText=GetComponent<TextMeshPro>();
        textTimer=lifeTime;
    }

    private void Update()
    {
        transform.position = Vector2.MoveTowards(transform.position, new Vector2(transform.position.x, transform.position.y + 1), speed * Time.deltaTime);
        textTimer -=Time.deltaTime;

        if (textTimer < 0)
        {
            //可见度随时间消耗
            float alpha=myText.color.a-colordisapperSpeed*Time.deltaTime;
            myText.color=new Color(myText.color.r, myText.color.g, myText.color.b, alpha);

            if (myText.color.a < 50)
            {
                speed = disapperSpeed;
            }
            if (myText.color.a <= 0) 
            {
                Destroy(gameObject);
            }

        }
        
    }

}
