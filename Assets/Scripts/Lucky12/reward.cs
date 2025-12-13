using DG.Tweening;
using System;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

public class reward : MonoBehaviour
{
    [SerializeField] private GameObject CoinParents;
    [SerializeField] private TextMeshProUGUI Counter;
    [SerializeField]private Vector3[] InitialPos;
    [SerializeField]private Quaternion[] InitialRotatoin;
    [SerializeField] private int CoinNo;
    [SerializeField] private AudioSource coinSound;
    [SerializeField] private float soundDuration = 2f;
    void Start()
    {
        InitialPos = new Vector3[CoinNo];
        InitialRotatoin = new Quaternion[CoinNo];

        for (int i = 0; i < CoinParents.transform.childCount; i++)
        {
            InitialPos[i] = CoinParents.transform.GetChild(i).position;
            InitialRotatoin[i] = CoinParents.transform.GetChild(i).rotation;
        }

        // Update is called once per frame

    }
    private void Reset()
    {
        if (CoinParents == null) return; 

        for (int i = 0; i < CoinParents.transform.childCount; i++)
        {
            var child = CoinParents.transform.GetChild(i);

            // Kill any ongoing animations first
            child.DOKill();

            // Reset to initial state
            child.position = InitialPos[i];
            child.rotation = InitialRotatoin[i];
            child.localScale = Vector3.zero;
            //InitialPos[i] = CoinParents.transform.GetChild(i).position;
            // InitialRotatoin[i] = CoinParents.transform.GetChild(i).rotation;
        }
        CoinParents.SetActive(false);
    }
    public void Rewardcoin(int no_coin)
    {
        Reset();

        var delay = 0f;
        CoinParents.SetActive(true);

        for (int i = 0; i < CoinParents.transform.childCount; i++)
        {
            // Reset position, rotation, and scale immediately
            CoinParents.transform.GetChild(i).position = InitialPos[i];
            CoinParents.transform.GetChild(i).rotation = InitialRotatoin[i];
            CoinParents.transform.GetChild(i).localScale = Vector3.zero;
        }
        if (coinSound != null)
        {
            coinSound.Play();
            
            Invoke("StopCoinSound", soundDuration);
        }
        for (int i = 0; i < CoinParents.transform.childCount; i++)
        {
            CoinParents.transform.GetChild(i).DOScale(0.2f, 0.3f).SetDelay(delay).SetEase(Ease.OutBack);
            CoinParents.transform.GetChild(i).GetComponent<RectTransform>().DOAnchorPos(new Vector2 (81f , -165f ), 1f).SetDelay(delay + 0.5f).SetEase(
                Ease.OutBack);
            CoinParents.transform.GetChild(i).DORotate(Vector3.zero , 0.5f).SetDelay(delay + 0.5f).SetEase(Ease.Flash);
            CoinParents.transform.GetChild(i).DOScale(0.0f, 0.0f).SetDelay(delay +1f).SetEase(Ease.OutBack);
            delay += 0.1f;
        }
    }
    private void StopCoinSound()
    {
        if (coinSound != null && coinSound.isPlaying)
        {
            coinSound.Stop();
        }
    }

    internal void StartRewardAnimation()
    {
        throw new NotImplementedException();
    }
}

