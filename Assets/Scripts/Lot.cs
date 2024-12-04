using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum LotState {
    ON_MARKET, OFF_MARKET
}
public class Lot : MonoBehaviour
{
    [Header("INFORMATION")]
    public Player owner;
    [Range(-10,10)]
    public int attractiveness;
    public float currentPrice;


    [Header("GAME")]
    public LotState state;
    public List<Player> PotentialBuyers; // list of players. sort by paymore : payless

    public float deteriorationChance;
    // ========
    private ColorChanger colorChanger;

    // Start is called before the first frame update
    void Start()
    {
        colorChanger = GetComponent<ColorChanger>();
        SimManager.instance.nextStep.AddListener(UpdateLot);
    }

    

    // Update is called once per frame
    void Update()
    {
        colorChanger.R = (attractiveness + 10) / 20.0f;
    }

    void UpdateLot() 
    {
        if (state == LotState.OFF_MARKET) PotentialBuyers = new();

        if (owner != null) currentPrice = owner.expense;
        else currentPrice = 0;

        // chance to deteriorate
        float randValue = Random.value;
        if (randValue < deteriorationChance)
        {
            attractiveness--;
            // Clamp the attractiveness value between -10 and 10
            Mathf.Clamp(attractiveness, -10, 10);
        }

    }
    
    // public float CalculateExpense(Player player) {
    //     if (SimManager.instance.pricingStyle == PricingStyle.STATIC) {
    //         return Calculate.StaticLotPrice(attractiveness, player);
    //     } else {
    //         // SimManager.instance.pricingStyle == PricingStyle.DYNAMIC
    //         return Calculate.DynamicLotPrice(attractiveness, player);
    //     }
    // }

}
