using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.Events;

public class MovingManager : MonoBehaviour
{


    public Dictionary<string, Player> Players = new();
    public Dictionary<string, Lot> Lots = new();

    public Dictionary<string, bool> MovingPlayers = new();
    public Dictionary<string, bool> AvailableLots = new();


    // for public exposure
    public List<string> _AvailableLots = new();
    public List<string> _MovingPlayers = new();

    public float moneyInHousingMarket;
    public float averageHousePrice; // average price of owned houses across the board

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

        // foreach((string name, bool avail) in AvailableLots) {
        //     if (avail) {
        //         Lot lot = Lots[name];
        //         Player buyer = MatchPersonToLot(lot);
        //         if (buyer != null) BuyHouse(buyer, lot);
        //     }
        // }

        // for (int index = 0; index < AvailableLots.Count; index++) {
        //     var item = AvailableLots.ElementAt(index);
        //     var name = item.Key;
        //     var avail = item.Value;
        //     if (avail) {
        //     Lot lot = Lots[name];
        //     Player buyer = MatchPersonToLot(lot);
        //     if (buyer != null) BuyHouse(buyer, lot);
        //     }
        // }


        // add wait in here
        (moneyInHousingMarket, averageHousePrice) = HousingMarket();

    }

    public Lot GetRandomAvailable() {
        var values = AvailableLots.Where(item => item.Value == true).Select(item => item.Key).ToList();
        var randLot = values.PopRandom();
        return Lots[randLot];
    }

    public List<string> GetUnavailableIdx() {
        return AvailableLots.Where(item => item.Value == false).Select(item => item.Key).ToList();
    }

    public void BuyHouse(Player player, Lot lot) 
    {
        player.currentLot = lot;
        player.SetState(PlayerState.STATIC);
        player.numMoves++;

        lot.owner = player;
        lot.state = LotState.OFF_MARKET;

        AvailableLots[lot.gameObject.name] = false;
        MovingPlayers[player.gameObject.name] = false;

        player.expense = CalculateExpense(lot, player);
        (player.costliness, player.quality) =  player.CalculateStats(lot);
        if (player.quality > player.qualityGoal) {
            player.qualityGoal = player.quality;
        }


    }

    public void MoveOut(Player player)
    {
        if (player.InterestedIn == null) player.InterestedIn = new();

        if (player.currentLot != null) {
            Lot lot = player.currentLot;
            AvailableLots[lot.gameObject.name] = true;
            lot.state = LotState.ON_MARKET;
            player.currentLot = null;
        }

        MovingPlayers[player.gameObject.name] = true;
        player.SetState(PlayerState.MOVING);
        (player.expense, player.costliness, player.quality) = (0f,0f,Mathf.NegativeInfinity); // technically homeless i guess

        // hello! you're just freshly moving!!!
        for (int i = 0; i < toConsider; i++) {
            Lot lot = GetRandomAvailable();
            AddInterest(player, lot);
        }

    }

    public void AddInterest(Player player, Lot lot) 
    {
        if (player.InterestedInBuyChance == null) player.InterestedInBuyChance = new();

        player.InterestedIn.Add(lot);
        lot.PotentialBuyers.InsertInOrder(player); // most to least money

        if (!player.InterestedInBuyChance.ContainsKey(lot)) {
            float _quality = player.CalculateStats(lot).Item2;
            float R = Calculate.ChanceOfBuying(_quality, player.qualityGoal);
            player.InterestedInBuyChance.Add(lot, R);
        }
    }

    (float, float) HousingMarket() 
    {
        float total = 0f;
        int count = 0;

        foreach ((string name, bool avail) in AvailableLots) {
            if (!avail) {
                total += Lots[name].currentPrice;
                count++;
            }
        }
        if (count == 0) return (0,0);
        return (total, total/count);
    }

    public float CalculateExpense(Lot lot, Player player) {
        return Calculate.DynamicLotPrice(lot, player);
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
