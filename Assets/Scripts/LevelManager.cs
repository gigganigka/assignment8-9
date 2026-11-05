using TMPro;
using UnityEngine;

// Менеджер уровня: считает монетки и обновляет HUD. Один пустой объект на сцене.
public class LevelManager : MonoBehaviour
{
    public static LevelManager Instance;   // чтобы монетки могли обратиться к менеджеру

    public int coinsRequired = 5;          // сколько нужно собрать, чтобы открылся портал
    public TMP_Text coinText;              // UI-текст "Coins: 0/5"
    public TMP_Text hpText;                // UI-текст "HP: 3"
    public Health player;                  // перетащи Player

    int coins;

    public bool PortalOpen { get { return coins >= coinsRequired; } }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        RefreshCoins();
    }

    void Update()
    {
        if (player != null && hpText != null)
            hpText.text = "HP: " + player.CurrentHp;
    }

    public void AddCoin()
    {
        coins++;
        RefreshCoins();
    }

    void RefreshCoins()
    {
        if (coinText != null)
            coinText.text = "Coins: " + coins + "/" + coinsRequired;
    }
}
