using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFX : EntityFX
{
    [Header("延迟图像特效")]
    [SerializeField] private GameObject afterImagePrefabe;
    [SerializeField] private float colorLooseRate;
    [SerializeField] private float afterImageCoolDown;
    private float afterImageCoolDownTimer;

    [Header("屏幕抖动特效")]
    private CinemachineImpulseSource screenShake;
    [SerializeField] private float shakeMultiplier;
    public Vector3 shakeSwordImpact;
    public Vector3 shakeHighDamage;

    [Space]
    [SerializeField] private ParticleSystem dustFX;

    private void Update()
    {
        afterImageCoolDownTimer -= Time.deltaTime;
    }

    protected override void Start()
    {
        base.Start();
        screenShake = GetComponent<CinemachineImpulseSource>();

    }
    

    public void CreateAfterImage()
    {
        if (afterImageCoolDownTimer < 0)
        {
            afterImageCoolDownTimer = afterImageCoolDown;
            GameObject newAfterImage = Instantiate(afterImagePrefabe, transform.position, transform.rotation);
            newAfterImage.GetComponent<AfterImageFX>().SetUpAfterImage(colorLooseRate, sr.sprite);
        }

    }
    public void ScreenShake(Vector3 _shakePower)
    {
        screenShake.m_DefaultVelocity = new Vector3(_shakePower.x * player.facingDir, _shakePower.y, _shakePower.z) * shakeMultiplier;
        screenShake.GenerateImpulse();
    }

    public void PlayDustFX()
    {
        if (dustFX != null)
        {
            dustFX.Play();
        }
    }

}
