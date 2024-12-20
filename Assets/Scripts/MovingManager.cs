using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.Events;

public struct MarketHistory {
    public float moneyInHousingMarket;
    public float averageHousePrice;
    public float medianHousePrice;
    public float averageOwnedHousePrice;
    public float medianOwnedHousePrice;
    public float N_0;
    public int N_homeless;
}

public class MovingManager : MonoBehaviour
{

    public Dictionary<Lot, Player> Lots = new();
    public Dictionary<Player, Lot> Players = new();



    [Header("INFORMATION")]
    // for public exposure
    public List<Lot> AvailableLots = new();


    [Header("MATH")]
    public float moneyInHousingMarket;
    public float medianHousePrice;
    public float medianOwnedHousePrice;
    public float averageHousePrice;
    public float averageOwnedHousePrice; // average price of owned houses across the board

    public float N_0; // # movers / # avail lots ==> average lot interest
    public int N_homeless;
    public bool takeOut0s = true;

    public int toConsider = 3;


    [HideInInspector] public static MovingManager instance;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject); // Destroy duplicate
        }
    }

    void Start() {
        SimManager.instance.nextStep.AddListener(UpdateLots);
        if (DataCollection.instance.saveData) SimManager.instance.nextStep.AddListener(SaveDataPoint);
    }

    void Update() 
    {
        // instance.UpdateAvailLots();
    }


    void UpdateLots() {
        // add wait in here
        (moneyInHousingMarket,
            averageOwnedHousePrice,
            medianOwnedHousePrice, 
            averageHousePrice, 
            medianHousePrice,
            N_homeless) = HousingMarket();
        N_0 = BaselineInterest();
    }






    // ========================================== MATH ==========================================

    (float, float, float, float, float, int) HousingMarket() 
    {
        int N_homeless = 0;

        Dictionary<bool, List<float>> prices = new()
        {
            { true, new List<float>() },
            { false, new List<float>() }
        };


        for (int i = 0; i < Lots.Count; i++) {
            var item = Lots.ElementAt(i);
            Lot lot = item.Key;
            if (lot.owner == null) Lots[lot] = null;
            bool avail = lot.owner == null;
            if (lot.attractiveness != 0) { // if its not a homeless spot
                prices[avail].Add(lot.currentPrice);
            }
            if (lot.attractiveness == -10 && lot.owner != null) N_homeless++;
        }

        // foreach ((Lot lot, Player owner) in Lots) {
        //     if (lot.owner == null) Lots[lot] = null;
        //     bool avail = owner == null;
        //     if (lot.attractiveness != 0) { // if its not a homeless spot
        //         prices[avail].Add(lot.currentPrice);
        //     }
        //     if (lot.attractiveness == -10 && lot.owner != null) N_homeless++;
        // }

        List<float> combinedList = new List<float>(prices[true]);
        combinedList.AddRange(prices[false]);


        float moneyInHousingMarket = prices[false].Sum() / SimManager.instance.MoneyInCirculation;
        
        float medianHousePrice = MyUtils.Median(combinedList.ToArray()) / SimManager.instance.medianIncome;
        float medianOwnedHousePrice = MyUtils.Median(prices[false].ToArray()) / SimManager.instance.medianIncome;

        float averageHousePrice = combinedList.Average() / SimManager.instance.medianIncome;
        float averageOwnedHousePrice = prices[false].Average() / SimManager.instance.medianIncome;

        return (moneyInHousingMarket,
                averageOwnedHousePrice,
                medianOwnedHousePrice, 
                averageHousePrice, 
                medianHousePrice,
                N_homeless);


    }

    float BaselineInterest () 
    {
        int numInterested = 0;
        int numAvail = 0;
        foreach (Lot lot in AvailableLots) {
            var tmp = lot.PotentialBuyers.Count(); // remove 0's??
                if (takeOut0s && tmp > 0 || !takeOut0s) {
                    numAvail ++;
                    numInterested += tmp;
            }
        }

        // 
        return (float)numInterested/numAvail;

    }

    // ========================= DATA SAVING ============================
    public void SaveDataPoint() {
        DataCollection.instance.NewDataPoint(this);
    }

}
