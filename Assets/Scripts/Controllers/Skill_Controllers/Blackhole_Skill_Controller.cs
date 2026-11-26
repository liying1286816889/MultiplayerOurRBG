using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class Blackhole_Skill_Controller : MonoBehaviour
{
    [SerializeField] private GameObject hotKeyPrefab;
    [SerializeField] private List<KeyCode> keycodList;

    private float maxSize;
    private float growSpeed;
    private float shrinkSpeed;
    private float blackholeTimer;

    private bool canGrow=true;
    private bool canShrink;
    private bool canCreateBlackhole=true;
    private bool cloneAttrackReleased;
    private bool playerCanDisapear= true;
    

    private int amountOfAttacks;
    private float cloneCooldown;
    private float cloneAttackTimer;

    private List<Transform> targets = new List<Transform>();
    private List<GameObject> createHotKey = new List<GameObject>();

    public bool playerCanExitState { get; private set; } 

    public void SetupBlackhole(
        float _maxSize,
        float _growSpeed,
        float _shrinkSpeed,
        int _amountOfAttacks,
        float _cloneAttackCooldown,
        float _blackholeDuration
        )
    {
        maxSize = _maxSize;
        growSpeed = _growSpeed;
        shrinkSpeed=_shrinkSpeed;
        amountOfAttacks = _amountOfAttacks;
        cloneCooldown = _cloneAttackCooldown;
        blackholeTimer = _blackholeDuration;
        if(SkillManager.instance.clone.crystalInstedOfClone)
        {
            playerCanDisapear = false;
        }

    } 




    private void Update()
    {
        cloneAttackTimer -= Time.deltaTime;
        blackholeTimer -= Time.deltaTime;
        if (blackholeTimer < 0)
        {
            blackholeTimer = Mathf.Infinity;
            if (targets.Count > 0)
            {
                ReleaseCloneAtrrack();
            }
            else
            {
                FinishBlackHoleAbility();
            }
        }



        // 只有施法者本人能按 R 释放；队友那边的黑洞只是画面
        if (!NetSkillFx.IsVisualOnly(gameObject) && Input.GetKeyDown(KeyCode.R))
        {
            ReleaseCloneAtrrack();
        }
        CloneAttrackLogic();

        if (canGrow && !canShrink)
        {
            transform.localScale = Vector2.Lerp(transform.localScale, new Vector2(maxSize, maxSize), growSpeed * Time.deltaTime);
        }
        if (canShrink)
        {
            transform.localScale = Vector2.Lerp(transform.localScale, new Vector2(-1, -1), shrinkSpeed * Time.deltaTime);
            if (transform.localScale.x <= 0)
            {
                Destroy(gameObject);
                canShrink = false;
            }
        }


    }

    private void ReleaseCloneAtrrack()
    {
        if(targets.Count <= 0) return;


        DestroyHotKeys();
        cloneAttrackReleased = true;
        canCreateBlackhole = false;
        if (playerCanDisapear)
        {
            playerCanDisapear = false;
            PlayerManager.instance.player.fx.MakerTransprent(true);
        }


    }

    private void CloneAttrackLogic()
    {
        
        if (cloneAttackTimer < 0 && cloneAttrackReleased&&amountOfAttacks>0)
        {
            cloneAttackTimer = cloneCooldown;
            int randomIndex = Random.Range(0, targets.Count);
            float xOffset;
            if (Random.Range(0, 100) > 50)
            {
                xOffset = 2;

            }
            else
            {
                xOffset = -2;
            }
            if(SkillManager.instance.clone.crystalInstedOfClone)
            {
                SkillManager.instance.crystal.CreateCrystal();
                SkillManager.instance.crystal.CurrentCrystalChooseRandomTarget();
            }
            else
            {
                SkillManager.instance.clone.CreateClone(targets[randomIndex], new Vector3(xOffset, 0));
            }
            amountOfAttacks--;
            if (amountOfAttacks <= 0)
            {
                Invoke("FinishBlackHoleAbility", 1f);  
            }
        }

    }

    private void FinishBlackHoleAbility()
    {
        DestroyHotKeys();
        playerCanExitState =true;
        canShrink = true;
        cloneAttrackReleased = false;
         
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 队友那边只是表现副本：不重复冻结、不生成按键提示
        if (NetSkillFx.IsVisualOnly(gameObject)) { return; }
        if (collision.GetComponent<Enemy>() != null)
        {
            //按一些键使用技能解决敌人
            collision.GetComponent<Enemy>().FreezeTime(true);

            // 让服务器也真的冻住（否则客户端施法时服务器上的敌人照样动）
            if (NetGame.IsOnline)
            {
                NetworkCharacterStats frozenNet = collision.GetComponent<NetworkCharacterStats>();
                if (frozenNet != null && frozenNet.IsSpawned) frozenNet.SetFrozenServerRpc(true);
            }            CreateHotKey(collision);

        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (NetSkillFx.IsVisualOnly(gameObject)) { return; }
        if (collision.GetComponent<Enemy>() != null)
        {
            collision.GetComponent<Enemy>().FreezeTime(false);

            if (NetGame.IsOnline)
            {
                NetworkCharacterStats frozenNet = collision.GetComponent<NetworkCharacterStats>();
                if (frozenNet != null && frozenNet.IsSpawned) frozenNet.SetFrozenServerRpc(false);
            }        }
    }


    private void DestroyHotKeys()
    {
        if (createHotKey.Count <= 0) return;

        for (int i = 0; i < createHotKey.Count; i++)
        {
            Destroy(createHotKey[i]);
        }
    }



    private void CreateHotKey(Collider2D collision)
    {
        if (keycodList.Count <= 0)
        {
            return;
        } 

        if (!canCreateBlackhole) return;


        GameObject newHotKey = Instantiate(hotKeyPrefab, collision.transform.position + new Vector3(0, 2), Quaternion.identity);
        createHotKey.Add(newHotKey);
        KeyCode choosenKey = keycodList[Random.Range(0, keycodList.Count)];
        keycodList.Remove(choosenKey);
        Blackhole_HotKey_Controller newHotKeyScript = newHotKey.GetComponent<Blackhole_HotKey_Controller>();
        newHotKeyScript.SetupHotKey(choosenKey, collision.transform, this);
    }

    public void AddEnemyToList(Transform _enemyTransform)
    {
        if (!targets.Contains(_enemyTransform))
        {
            targets.Add(_enemyTransform);
        }
    }




}
