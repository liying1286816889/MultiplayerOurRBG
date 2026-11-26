using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPiont : MonoBehaviour
{
    private Animator anim;
    public string id;
    public bool activationStatus;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }

    private void Start()
    {

    }
    [ContextMenu("生成存档点的ID")]
    private void GenerateId()
    {
        id=System.Guid.NewGuid().ToString();
    }


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<Player>() != null)
        {
            ActivateCheckPoint();
        }

    }

    public void ActivateCheckPoint()
    {
        if (NetGame.IsOnline)
        {
            // 联机：交给服务器统一激活，避免两个客户端状态不一致
            WorldObjectNetwork net = GetComponent<WorldObjectNetwork>();
            if (net != null && net.IsSpawned)
            {
                if (NetGame.IsServer)
                {
                    net.ServerActivate();
                }
                else
                {
                    net.ActivateServerRpc();
                }
            }

            ApplyActivatedVisual();
            return;
        }

        ApplyActivatedVisual();
    }

    /// <summary>只负责表现（音效 + 动画），不涉及状态权威。</summary>
    public void ApplyActivatedVisual()
    {
        if (activationStatus == false)
        {
            AudioManager.instance.PlaySFX(5, transform);
        }

        activationStatus = true;

        if (anim == null)
        {
            anim = GetComponent<Animator>();
        }

        if (anim != null)
        {
            anim.SetBool("Active", true);
        }
    }

    


}
