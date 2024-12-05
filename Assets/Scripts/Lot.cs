using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public enum LotState {
    ON_MARKET, OFF_MARKET
}

public struct LotHistory {
    float ownerIncome;
    float currentPrice;
    float attractiveness;
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

    public float F_income = 1f;
    public float F_interest = 1f;

    // ========
    private ColorChanger colorChanger;

    // Start is called before the first frame update
    void Start()
    {
        colorChanger = GetComponent<ColorChanger>();
        SimManager.instance.nextStep.AddListener(UpdateLot);
    }

    void Update() {
        // erm what te fuck
        if (currentPrice < 0 && attractiveness == -10) currentPrice = 0; // reset
        if (currentPrice < 0 ) Debug.Log($"{name} youre fucked {attractiveness}");
    }



    void UpdateLot() 
    {

        // deteriorate vs no change vs gentrify
        colorChanger.R += Calculate.ChangeInLot(this)/20;
        colorChanger.R = Mathf.Clamp(colorChanger.R, 0, 1);
        attractiveness = (int)Mathf.Round(colorChanger.R * 20) - 10; // [0,1] => [-10,10]
        attractiveness = Mathf.Clamp(attractiveness, -10, 10);

        if (state == LotState.OFF_MARKET) {
            PotentialBuyers = new();
            currentPrice = Calculate.DynamicLotPrice(this).Item1; // update dah price
            // update the expense
            owner.costliness = currentPrice / owner.income;

        }
        if (state == LotState.ON_MARKET) {
            (currentPrice, F_income, F_interest) = Calculate.DynamicLotPrice(this); // update dah price
        }


    }
    

}
