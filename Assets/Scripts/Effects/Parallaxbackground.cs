using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parallaxbackground : MonoBehaviour
{
    private GameObject cam;
    [SerializeField]private float parllaxEffect;
    private float xPosition;
    private float length;
    void Start()
    {
        cam = GameObject.Find("Main Camera");


        length=GetComponent<SpriteRenderer>().bounds.size.x;
        xPosition = transform.position.x;
    }

    void Update()
    {
        float distanceToMoved=cam.transform.position.x * (1-parllaxEffect);
        float distanceToMove=cam.transform.position.x * parllaxEffect;
        transform.position = new Vector3(xPosition + distanceToMove, transform.position.y);

        if (distanceToMoved > xPosition + length)
        {
            xPosition += length;
        }
        else if (distanceToMoved < xPosition - length)
        {
            xPosition -= length;
        }
    }
}
