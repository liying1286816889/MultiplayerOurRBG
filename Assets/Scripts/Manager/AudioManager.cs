using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.VisualScripting.Member;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [SerializeField] private float sfxMinimumDistance;
    [SerializeField] private AudioSource[] sfx;
    [SerializeField] private AudioSource[] bgm;
    public bool playBgm;
    public int bgmIndex;
    private bool canPlaySFX;




    public void Awake()
    {
        if (instance != null)
        {
            Destroy(instance.gameObject);
        }
        else
        {
            instance = this;
        }

        Invoke("AllowSFX", 1f);
    }

    private void Update()
    {
        if (!playBgm)
        {
            StopAllBGM();
        }
        else
        {
            if (!bgm[bgmIndex].isPlaying)
            {
                PlayBGM(bgmIndex);   
            }

        }

    }



    public void PlaySFX(int _sfxIndex,Transform _source=null)
    {
        if (!canPlaySFX)
        {
            return;
        }

        if (sfx[_sfxIndex].isPlaying)
        {
            return;
        }

        Transform listener = NetGame.GetReferencePlayerTransform();
        if (_source != null && listener != null &&
            Vector2.Distance(listener.position, _source.position) > sfxMinimumDistance)
        {
            return;
        }
        sfx[_sfxIndex].volume = 1;

        if (_sfxIndex < sfx.Length)
        {
            sfx[_sfxIndex].Play();
        }
    }

    public void PlayRandomPitchSFX(int _sfxIndex, Transform _source=null)
    {
        if (!canPlaySFX)
        {
            return;
        }

        if (sfx[_sfxIndex].isPlaying)
        {
            return;
        }

        Transform listener = NetGame.GetReferencePlayerTransform();
        if (_source != null && listener != null &&
            Vector2.Distance(listener.position, _source.position) > sfxMinimumDistance)
        {
            return;
        }

        if (_sfxIndex < sfx.Length)
        {
            sfx[_sfxIndex].pitch = Random.Range(1f, 2f);
            sfx[_sfxIndex].Play();
        }
    }

    public void StopSFX(int _sfxIndex)
    {
        if (_sfxIndex < sfx.Length)
        {
            sfx[_sfxIndex].Stop();
        }
    }

    public void StopSFXWithTime(int _index)
    {
        StartCoroutine(DecreaseVolume(sfx[_index]));

    }

    private IEnumerator DecreaseVolume(AudioSource _audio)
    {
        float defaultVolume=_audio.volume;
        while (_audio.volume > 0.1)
        {
            _audio.volume -= _audio.volume * 0.2f;
            yield return new WaitForSeconds(.25f);
            if(_audio.volume <= 0.1)
            {
                _audio.Stop();
                _audio.volume =defaultVolume;
                break;
            }
        }
    }


    public void PlayRandomBGM()
    {
        bgmIndex=Random.Range(0, bgm.Length);
        PlayBGM(bgmIndex);

    }

    public void PlayBGM(int _bgmIndex)
    {
        bgmIndex=_bgmIndex;
        StopAllBGM();
        if (_bgmIndex < bgm.Length)
        {
            bgm[bgmIndex].Play();
        }
    }

    public void StopAllBGM()
    {
        for (int i = 0; i < bgm.Length; i++)
        {
            bgm[i].Stop();
        }
    }

    public IEnumerator WaitSecond(float _second, int _sfxIndex)
    {
        if (_sfxIndex < sfx.Length)
        {
            yield return new WaitForSeconds(_second);
            AudioManager.instance.StopSFX(_sfxIndex);
        }
    }

    private void AllowSFX()
    {
        canPlaySFX = true;
    }
}
