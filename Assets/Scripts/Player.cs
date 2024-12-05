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

public struct PlayerHistory {
    public float expense;
    public float costliness;
    public int attractiveness;
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
    public int unitsSpentMoving;
    private int timeUnitsMoving;

    [Header("GAME")]
    public float costliness; // expense : income => want to minimize [0-1] => must be less than 1
    [HideInInspector] public float maxQuality = 0f;
    public float qualityGoal;
    public float quality; // attractiveness : costliness => want to maximize [0/[0-1] : 20/[0:1]]
    public List<Lot> InterestedIn; // list of lots
    [HideInInspector] public float WeightCost = 0f;
    [HideInInspector] public float WeightAttr = 0f;

    // Start is called before the first frame update
    void Start()
    {
        SimManager.instance.nextStep.AddListener(UpdatePlayer);
    }

    
    void UpdatePlayer() 
    {
        ConsiderMoving();
    }

    public void UpdatePosition() {
        transform.position = currentLot.transform.position;
    }

    /// ==================== SIM TIME
    
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
        if (InterestedIn.Count > 0) {
            foreach (Lot l in InterestedIn) {
                float Q = Calculate.QualityOfLot(l, this);
                float chance = Calculate.ChanceOfBuying(Q, qualityGoal);
                if (chance > _prevChance && chance > randValue) {
                    buyLot = l;
                    _prevChance = chance;
                }
            }
        }

        return buyLot;
    }

    void Wait()
    {
        // PLAYER POV
        // 0.5 if interested in is new, then add howevermuch u need to
        int toAdd = InterestedIn.Count > 0 ? 1 : MovingManager.instance.toConsider;

        // 1. add contender(s)
        for (int i = 0; i < toAdd; i ++) {
            // get random avail
            Lot lot = MovingManager.instance.AvailableLots.GetRandom().Item2;
            MovingManager.instance.AddInterest(this, lot);
        }
        
        // 2. remove any unavailable lots
        List<Lot> temp = new();
        for (int i = 0; i < InterestedIn.Count(); i++) {
            bool avail = MovingManager.instance.Lots[InterestedIn[i]] == null;
            if (avail) temp.Add(InterestedIn[i]);
        }
        InterestedIn = temp;
            
        // more time you've spent wanting to move, the less you care about quality of the place u moving to
        if (qualityGoal>-1f) qualityGoal-=SimManager.instance.qualityGoalDeterioration;
    }

    public void MoveToLot(Lot lot) {
        if (currentLot != null) MovingManager.instance.Lots[currentLot] = null;
        // put in dah new guys
        MovingManager.instance.Lots[lot] = this;
        MovingManager.instance.Players[this] = lot;

        InterestedIn = new();
        lot.PotentialBuyers = new();


        // do mutual plot-lotting
        currentLot = lot;
        lot.owner = this;

        lot.state = LotState.OFF_MARKET;
        expense = lot.currentPrice;

        (costliness, quality) =  CalculateStats(lot);
        if (quality > qualityGoal) {
            qualityGoal = quality; // improve ur quality standards to match this house
        }

        // remove self from all the housese u were interested in
        foreach(Lot l in InterestedIn) {
            l.PotentialBuyers.Remove(this);
        }


        numMoves++;    
        UpdatePosition();
    }


    // HELPER =================================================================

    public (float, float) CalculateStats(Lot lot) 
    {
        float _costliness = lot.currentPrice / income;
        float _quality = Calculate.QualityOfLot(lot, this);
        return (_costliness, _quality);
    }


}
