using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "IceAndFireEffect", menuName = "Data/ItemEffect/IceAndFire")]
public class IceAndFire_Effect : ItemEffect
{
    [SerializeField] private GameObject iceAndFirePrefab;
    [SerializeField] private float xVelocity;

    public override void ExecuteEffect(Transform _respondPosition)
    {


        Player player = PlayerManager.instance.player;

        bool isThirdAttack = player.primaryAttack.comboCounter == 2;

        if(isThirdAttack)
        {
            GameObject newIceAndFire = Instantiate(iceAndFirePrefab, _respondPosition.position, player.transform.rotation);

            newIceAndFire.GetComponent<Rigidbody2D>().velocity = new Vector2(xVelocity*player.facingDir,0);
            
            Destroy(newIceAndFire, 8f);
        }

       
    }
}