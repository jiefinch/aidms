using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;

public enum PlayerState {
    STATIC, MOVING, INIT
}
public enum SocioClass {
    LOW, MID, HIGH
}

public class Player : MonoBehaviour
{
    [Header("INFORMATION")]
    public SocioClass socioClass;
    public float income;
    public float expense;
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
    public PlayerState state;
    public PlayerState prevState = PlayerState.INIT;
    public List<Lot> InterestedIn; // list of lots
    public Dictionary<Lot, float> InterestedInBuyChance;
    [HideInInspector] public float WeightCost = 0f;
    [HideInInspector] public float WeightAttr = 0f;


    // [Header("MATH")]
    // // 𝑅 = chance of buying
    // public float alpha = 0.1f; //α is a constant that controls the rate at which 𝑅 increases with distance.
    // public float beta = 0.1f; //β is a constant that controls the rate at which 𝑅 R decreases with distance.
    // public float kappa = 0.5f; // how sharp the sigmoid for care of cost vs attractiveness | high kappa = sharper care for attractiveness at higher incomes
    // public float gamma = 0.1f; // controlls the getting rid of sigmoid | low: more linear | high: more sharp / doesn't need to be completely satisfactory square, will keep.
    // >> make self adjusting?


    // Start is called before the first frame update
    void Start()
    {
        SimManager.instance.nextStep.AddListener(UpdatePlayer);
        InterestedIn = new();
        InterestedInBuyChance = new();

        float color = income / SimManager.instance.highestIncome;
        GetComponent<ColorChanger>().R = color;
    }

    // Update is called once per frame
    void Update()
    {
    } 

    void UpdatePlayer() 
    {
        
        if (state == PlayerState.MOVING) {
            unitsSpentMoving++;
            timeUnitsMoving++;
            Moving();

            if (prevState == PlayerState.STATIC) prevState = PlayerState.MOVING;
        } else if (state == PlayerState.STATIC) {
            timeUnitsMoving = 0;
            ConsiderMoving(); 

            if (prevState == PlayerState.MOVING) prevState = PlayerState.STATIC;
        }

        if (prevState != state) UpdatePosition();
    }

    public void UpdatePosition() {
        if (state == PlayerState.STATIC) {
            transform.position = currentLot.transform.position;
        } else {
            // get sent to moving son
            var randPos = new UnityEngine.Vector3(UnityEngine.Random.Range(-13f, 13f), UnityEngine.Random.Range(-1.6f, 1.6f), 0 ); // y: +/-1.6
            transform.position = MovingManager.instance.gameObject.transform.position + randPos;
        }
    }

    /// ==================== SIM TIME
    
    void Moving()
    {
        Lot buyLot = null;

        var randValue = UnityEngine.Random.value;
        // highest chance to buy lot
        if (InterestedInBuyChance.Count > 0) {
            Lot lot = InterestedInBuyChance.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
            if (randValue < InterestedInBuyChance[lot]) {
                buyLot = lot;
            }
        }
        
        // figure out if you can buy house here
        // compare interestedbuyChance & lot.potentialbuyers
        // mode 1: select most buy chance
        // mode 2:  

        if (buyLot != null) {
            // BUY THE HOUSE
            BuyHouse(buyLot);
        } else {
            // WAIT
            Wait();
        }
    }

    void BuyHouse(Lot lot) 
    {
        numMoves++;
        MovingManager.instance.BuyHouse(this, lot);        
    }

    void Wait()
    {
        // PLAYER POV
        // 1. add one more contender
        Lot lot = MovingManager.instance.GetRandomAvailable();
        MovingManager.instance.AddInterest(this, lot);

        // 2. remove any unavailable lots
        List<string> unavailableLots = MovingManager.instance.GetUnavailableIdx();
        InterestedIn.RemoveAll(item => unavailableLots.Contains(item.gameObject.name)); // remove based on same name
            
        // more time you've spent moving, the less you care about quality
        qualityGoal*=1-SimManager.instance.qualityGoalDeterioration;           
    }


    void ConsiderMoving() {
        float randValue = UnityEngine.Random.value;        
        if (randValue < Calculate.ChanceOfMoving(this)) {
            MovingManager.instance.MoveOut(this);
        }  
    }



    // HELPER =================================================================

    public (float, float) CalculateStats(Lot lot) 
    {
        float _costliness = MovingManager.instance.CalculateExpense(lot, this) / income;
        float _quality = Calculate.QualityOfLot(lot, this);
        return (_costliness, _quality);
    }

    public void SetState(PlayerState _state) {
        if (prevState == PlayerState.INIT) prevState = _state;
        else prevState = state;
        state = _state;
    }


}
