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
        (moneyInHousingMarket, averageOwnedHousePrice, medianOwnedHousePrice) = HousingMarket();
        (averageHousePrice, medianHousePrice) = (Lots.Average(l => l.Key.currentPrice),
                                                MyUtils.Median(Lots.Select(d => d.Key.currentPrice).ToArray()));
        N_0 = BaselineInterest();
    }






    // ========================================== MATH ==========================================

    (float, float, float) HousingMarket() 
    {
        List<float> prices = new();

        foreach (Lot lot in AvailableLots) {
            prices.Add(lot.currentPrice);
        }
        if (prices.Count == 0) return (0,0,0);
        float total = prices.Sum();
        float avg = total/prices.Count();
        float median = MyUtils.Median(prices.ToArray());
        return (total, avg, median);
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
        return (float)numInterested/numAvail;

    }
    

}
