using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.Events;

public struct MovingHistory {
    public float moneyInHousingMarket;
    public float averageHousePrice;
    public float medianHousePrice;
    public float averageOwnedHousePrice;
    public float medianOwnedHousePrice;
    public float housedRate; // % of people currently housed. movingPlayers / numPlayers
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
    }

    void Update() 
    {
        // instance.UpdateAvailLots();
    }


    void UpdateLots() {
        // add wait in here
        (moneyInHousingMarket, averageOwnedHousePrice, medianOwnedHousePrice, averageHousePrice, medianHousePrice) = HousingMarket();
        N_0 = BaselineInterest();
    }






    // ========================================== MATH ==========================================

    (float, float, float, float, float) HousingMarket() 
    {

        Dictionary<bool, List<float>> prices = new()
        {
            { true, new List<float>() },
            { false, new List<float>() }
        };

        foreach ((Lot lot, Player owner) in Lots) {
            bool avail = owner == null;
            if (lot.currentPrice != 0) {
                // this isn't even a house, it's a homeless spot
                prices[avail].Add(lot.currentPrice);
            }            
        }

        float moneyInHousingMarket = prices[true].Sum();
        float averageOwnedHousePrice = prices[true].Average();
        float medianOwnedHousePrice = MyUtils.Median(prices[true].ToArray());
        float averageHousePrice = prices[true].Concat(prices[false]).ToList().Average();
        float medianHousePrice = MyUtils.Median(prices[true].Concat(prices[false]).ToArray());

        return (moneyInHousingMarket, averageOwnedHousePrice, medianOwnedHousePrice, averageHousePrice, medianHousePrice);


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
    

}
