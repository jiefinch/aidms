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
    public int movingPlayers;
    public float housedRate; // % of people currently housed. movingPlayers / numPlayers
    public List<float> movingPlayersIncomeDistribution;
}

public class MovingManager : MonoBehaviour
{
    public Dictionary<string, Player> Players = new();
    public Dictionary<string, Lot> Lots = new();

    public Dictionary<string, bool> MovingPlayers = new();
    public Dictionary<string, bool> AvailableLots = new();


    [Header("INFORMATION")]
    // for public exposure
    public List<string> _AvailableLots = new();
    public List<string> _MovingPlayers = new();


    [Header("MATH")]
    public float moneyInHousingMarket;
    public float averageOwnedHousePrice; // average price of owned houses across the board
    public float medianOwnedHousePrice;
    public float averageHousePrice;
    public float medianHousePrice;

    public float N_0; // # movers / # avail lots ==> average lot interest
    public bool takeOut0s = true;



    public int toConsider = 1;


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

    void Update() {
        // marketPrice = 0f;
        _AvailableLots = AvailableLots.Where(item => item.Value == true).Select(item => item.Key).ToList();
        _MovingPlayers = MovingPlayers.Where(item => item.Value == true).Select(item => item.Key).ToList();
    }

    void UpdateLots() {
        // add wait in here
        (moneyInHousingMarket, averageOwnedHousePrice, medianOwnedHousePrice) = HousingMarket();
        (averageHousePrice, medianHousePrice) = (Lots.Average(l => l.Value.currentPrice),
                                                MyUtils.Median(Lots.Select(d => d.Value.currentPrice).ToArray()));
        N_0 = BaselineInterest();

    }

    public Lot GetRandomAvailable() {
        var values = AvailableLots.Where(item => item.Value == true).Select(item => item.Key).ToList();
        var randLot = values.PopRandom();
        return Lots[randLot];
    }

    public void BuyHouse(Player player, Lot lot) 
    {
        player.currentLot = lot;
        lot.owner = player;
        
        player.SetState(PlayerState.STATIC);
        lot.state = LotState.OFF_MARKET;
        
        player.InterestedIn = new();
        lot.PotentialBuyers = new();
        // remove self from all the housese u were interested in
        foreach(Lot l in player.InterestedIn) {
            l.PotentialBuyers.Remove(player);
        }

        AvailableLots[lot.gameObject.name] = false;
        MovingPlayers[player.gameObject.name] = false;

        player.expense = lot.currentPrice;
        (player.costliness, player.quality) =  player.CalculateStats(lot);
        if (player.quality > player.qualityGoal) {
            player.qualityGoal = player.quality; // improve ur quality standards to match this house
        }
    }

    public void MoveOut(Player player)
    {
        player.InterestedIn = new();

        // if you actually own a house right now give that up and put it onto market
        if (player.currentLot != null) {
            Lot lot = player.currentLot;
            lot.state = LotState.ON_MARKET;
            AvailableLots[lot.gameObject.name] = true;
            lot.owner = null;
            lot.PotentialBuyers = new();
        }
        

        player.SetState(PlayerState.MOVING);
        MovingPlayers[player.gameObject.name] = true;
        (player.expense, player.costliness, player.quality) = (0f,0f,Mathf.NegativeInfinity); // technically homeless i guess

        // hello! you're just freshly moving!!!
        for (int i = 0; i < toConsider; i++) {
            Lot lot = GetRandomAvailable();
            AddInterest(player, lot);
        }

        player.currentLot = null;
        player.numMoves++;
    }

    public void AddInterest(Player player, Lot lot) 
    {

        player.InterestedIn.Add(lot);
        lot.PotentialBuyers.InsertInOrder(player); // most to least money

        if (player.InterestedInBuyChance == null) player.InterestedInBuyChance = new();
        if (!player.InterestedInBuyChance.ContainsKey(lot)) {
            float _quality = player.CalculateStats(lot).Item2;
            float R = Calculate.ChanceOfBuying(_quality, player.qualityGoal);
            player.InterestedInBuyChance.Add(lot, R);
        }
    }

    (float, float, float) HousingMarket() 
    {
        List<float> prices = new();

        foreach ((string name, bool avail) in AvailableLots) {
            if (!avail) {
                prices.Add(Lots[name].currentPrice);
            }
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
        foreach ((string name, bool avail) in AvailableLots) {
            if (avail) {
                var tmp = Lots[name].PotentialBuyers.Count(); // remove 0's??
                if (takeOut0s && tmp > 0 || !takeOut0s) {
                    numAvail ++;
                    numInterested += tmp;
                }
            }
        }
        return (float)numInterested/numAvail;

    }


    // ========================================================

    // This function matches the highest income person who has a random chance less than their probability
    public Player MatchPersonToLot(Lot lot)
    {
        var randValue = UnityEngine.Random.value;
        // Loop through the list (which is already sorted by income high to low)

        for (int i = 0 ; i < lot.PotentialBuyers.Count; i++ ) {
            Player player = lot.PotentialBuyers[i];
            
            // If random value is less than their chance to buy, return this person
            // Debug.Log($"{randValue} {player.InterestedInBuyChance[lot]} | {randValue < player.InterestedInBuyChance[lot]}");
            if (randValue < player.InterestedInBuyChance[lot]) return player;
        }

        return null;
    }
    

}
