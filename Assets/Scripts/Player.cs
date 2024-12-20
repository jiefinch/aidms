using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;

public enum SocioClass {
    LOW, MID, HIGH
}

public struct PlayerInformation {
    public float income;
    public float relativeIncome;
    public float econRank;
}

public struct PlayerHistory {
    public float expense;
    public float costliness;
    public int attractiveness; // r u housed
    public int numMoves;
    public float qualityGoal;
    public float quality; 
}

public class Player : MonoBehaviour
{
    [Header("INFORMATION")]
    public SocioClass socioClass;
    public float income;
    public float expense;
    public float econRank; // [-1,1] in terms of econmoic standing
    public Lot currentLot = null;

    [Header("HISTORY")]
    public int numMoves;
    public int attemptsToMove;
    public int overSpent;


    [Header("GAME")]
    public float costliness; // expense : income => want to minimize [0-1] => must be less than 1
    [HideInInspector] public float maxQuality = 0f;
    public float qualityGoal;
    public float quality; // attractiveness : costliness => want to maximize [0/[0-1] : 20/[0:1]]
    public Dictionary<Lot, int> InterestedIn = new();
    public Lot[] _InterestedIn; // list of lots
    public float WeightCost = 0f;
    public float WeightAttr = 0f;

    // Start is called before the first frame update
    void Start()
    {
        if (DataCollection.instance.saveData) SimManager.instance.nextStep.AddListener(SaveDataPoint);
        SimManager.instance.nextStep.AddListener(UpdatePlayer);
        // SimManager.instance.nextStep.AddListener(SaveDataPoint);
    }

    void Update() {
        _InterestedIn = InterestedIn.Keys.ToArray();
        if (currentLot!=null) expense = currentLot.currentPrice;
    }

    void UpdatePlayer() 
    {
        if (costliness > 1) overSpent++;
        if (costliness <= 0.75 && quality > -2) overSpent = overSpent-1<0? 0 : overSpent-1; // pay back ur debt if youre not homeless

        ConsiderMoving();
        (costliness, quality) =  Calculate.LotStats(currentLot, this);
    }

    public void UpdatePosition() {
        transform.position = currentLot.transform.position;
    }

    // ========================= SIM FUNCTIONS ============================
    
    void ConsiderMoving() {
        float randValue = UnityEngine.Random.value;        
        if (randValue < Calculate.ChanceOfMoving(this)) {
            Lot lot = ConsiderOptions(); // is there a lot i wanna buy?
            if (lot != null) MoveToLot(lot); // yes! let's move there.
            else Wait(); // no, add another place i'm looking to move.
        }  
    }

    Lot ConsiderOptions()
    {
        Lot buyLot = null;
        float _prevChance = 0f;

        var randValue = UnityEngine.Random.value;
        // highest chance to buy lot
        if (_InterestedIn.Length > 0) {
            foreach (Lot l in InterestedIn.Keys.ToArray()) {
                float lotQuality = Calculate.QualityOfLot(l, this);
                float costli = l.currentPrice / income;
                float chance = Calculate.ChanceOfBuying(quality, lotQuality, qualityGoal, costli);
                if (chance > _prevChance && chance > randValue) {
                    buyLot = l;
                    _prevChance = chance;
                } else {
                    InterestedIn[l] += 1; // didin't but this time tiger
                }
            }
        }

        if (buyLot != null) {
            foreach (Player player in buyLot.PotentialBuyers.ToArray()) {
                player.InterestedIn.Remove(buyLot);
            } // STOP LOOKING the lots u were looking at >____< ... im gonna buy something
        }

        return buyLot;
    }

    void Wait()
    {
        attemptsToMove++;
        // PLAYER POV
        // remove interest chances
        // 1. interested in
        // 2. potential buyers
        int num = _InterestedIn.Length;
        // float randValue = UnityEngine.Random.value; => generate new random value per
        Lot[] lotsToRemove = InterestedIn.Where(item => UnityEngine.Random.value < Calculate.ChanceOfDropping(InterestedIn, item.Key)).
                Select(item => item.Key)
                .ToArray();

        foreach(Lot lot in lotsToRemove.ToArray()) {
            InterestedIn.Remove(lot);
            lot.PotentialBuyers.Remove(this);
        }

        // 0.5 if interested in is new, then add howevermuch u need to
        int toAdd = _InterestedIn.Length > 0 ? 1 : MovingManager.instance.toConsider;

        // 1. add contender(s)
        for (int i = 0; i < toAdd; i ++) {
            // get random avail
            Lot lot = MovingManager.instance.AvailableLots.GetRandom().Item2;
            if (!InterestedIn.ContainsKey(lot)) {
                InterestedIn.Add(lot, 0);
                lot.PotentialBuyers.Add(this); 
            }
        }
        if (qualityGoal>-1f) qualityGoal = Mathf.Clamp(qualityGoal-Calculate.DropInQualityGoal(this), quality, 1);
    }


    public void MoveToLot(Lot lot) {
        attemptsToMove = 0;
        if (lot.currentPrice / income<= 0.75) overSpent = overSpent-1<0? 0 : overSpent-1;

        // put in dah new guys
        MovingManager.instance.Lots[lot] = this;
        MovingManager.instance.Players[this] = lot;
        if (MovingManager.instance.AvailableLots.Contains(lot)) MovingManager.instance.AvailableLots.Remove(lot);
        if (currentLot != null) {
            MovingManager.instance.AvailableLots.Add(currentLot);
            currentLot.owner = null;
            currentLot.state = LotState.ON_MARKET;
            currentLot.PotentialBuyers = new();
        }

        var N = lot.PotentialBuyers.Count; // num ppl who were interested in this lot
        // no longer interested in other lots
        foreach(Player player in lot.PotentialBuyers.ToArray()) {
            player.InterestedIn.Remove(lot);
        }
        // no longer a potential buyer of other lots
        foreach(Lot l in InterestedIn.Keys.ToArray()) {
            l.PotentialBuyers.Remove(this);
        }
        
        InterestedIn = new();
        lot.PotentialBuyers = Enumerable.Repeat<Player>(null, N).ToList();  // save the null state

        currentLot = lot;
        lot.owner = this;
        lot.state = LotState.OFF_MARKET;

        (costliness, quality) =  Calculate.LotStats(lot, this);
        if (quality > qualityGoal) {
            qualityGoal = quality; // improve ur quality standards to match this house
        }
        

        numMoves++;    
        UpdatePosition();
    }

    // ========================= DATA SAVING ============================
    public void SaveDataPoint() {
        DataCollection.instance.NewDataPoint(this);
    }

}
