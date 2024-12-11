using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public enum LotState {
    ON_MARKET, OFF_MARKET
}

public struct LotHistory {
    public float ownerIncome;
    public float currentPrice;
    public float attractiveness;
    public int PotentialBuyers;
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
    [HideInInspector] public ColorChanger colorChanger;

    // Start is called before the first frame update
    void Start()
    {
        colorChanger = GetComponent<ColorChanger>();
        if (DataCollection.instance.saveData) SimManager.instance.nextStep.AddListener(SaveDataPoint);
        SimManager.instance.nextStep.AddListener(UpdateLot);
    }

    // void Update() {
    //      // [0,1] => [-10,10]
    // }

    void UpdateLot() 
    {
        if (owner != null && owner.overSpent >= 12) {
            // become homeless lot yahoo
            colorChanger.R = 0;
            attractiveness = -10;
            owner.quality = -2f;
        } else if (owner == null || owner.quality > -2f) { // change lot quality if owner is not homeless
            // deteriorate vs no change vs gentrify
            float changeInLot = Calculate.ChangeInLot(this)/20f;
            ChangeAttractiveness(changeInLot);

            // if u gentrified, gentrify the neighbors
            if (changeInLot > 0) {
                var (neighborLots, adjacentLots) = MyUtils.GetSurroundingLots(this);
                foreach(Lot lot in neighborLots) {
                    float change = changeInLot/2;
                    lot.ChangeAttractiveness(change);
                }
                foreach(Lot lot in adjacentLots) {
                    float change = changeInLot/4;
                    lot.ChangeAttractiveness(change);            
                }
            }
        }

        (currentPrice, F_income, F_interest) = Calculate.DynamicLotPrice(this); // update dah price
        if (state == LotState.OFF_MARKET) {
            // update the expense of the owner
            owner.costliness = currentPrice / owner.income;
        }
    }

    public void ChangeAttractiveness(float change) {
        colorChanger.R = Mathf.Clamp(change + colorChanger.R, 0,1); // whewf.. got it here
        attractiveness = Mathf.Clamp((int)Mathf.Round(colorChanger.R * 20) - 10, -10, 10);
    }

    // ========================= DATA SAVING ============================

    public void SaveDataPoint() {
        DataCollection.instance.NewDataPoint(this);
    }

}
