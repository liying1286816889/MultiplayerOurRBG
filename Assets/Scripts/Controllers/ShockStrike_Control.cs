using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

public class ShockStrike_Control : MonoBehaviour
{
    [SerializeField] private CharacterStats targetStats;
    [SerializeField] private float speed;
    private int damage;



    private Animator anim;
    private bool triggeted;


    public void Start()
    {
        anim = GetComponentInChildren<Animator>();


    }

    public void Setup(int _damage,CharacterStats _targetStats)
    {

        damage = _damage;
        targetStats = _targetStats;

    }



    public void Update()
    {
        if(!targetStats || targetStats.currentHealth <= 0)
        {
            Destroy(gameObject);
            return;
        }


        if (triggeted)
        {
            return;
        }


        transform.position=Vector2.MoveTowards(transform.position, targetStats.transform.position, speed * Time.deltaTime);

        transform.localRotation = Quaternion.Euler(0, 0, 180);

        transform.right = (transform.position-targetStats.transform.position).normalized;



        if (Vector2.Distance(transform.position, targetStats.transform.position) < 0.1f)
        {


            anim.transform.localPosition= new Vector3(0, 0.5f);
            anim.transform.localRotation=Quaternion.identity;

            transform.localRotation = Quaternion.identity;
            transform.localScale=new Vector3(3, 3);

            Invoke("DamageAndSelfDestroy",0.2f);

            triggeted = true;
            anim.SetTrigger("Hit");


        }
    }

    void DamageAndSelfDestroy()
    {
        targetStats.TakeDamage(1);
        Destroy(gameObject, 0.4f);

    }
}
