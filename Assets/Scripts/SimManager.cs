using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;
using UnityEngine.Events;
using UnityEditor;
using System.Linq;

[Serializable] public struct LotAttractiveness {
    public float[] means;
    public float[] stdDevs;
    public float[] prob;
    public float gentrification; // = 0.25f;
    public float decay; // = 0.1f;
}
[Serializable] public struct PlayerSettings 
{
    public float initInterestDropChance; // = 0.01f; // initial P(dropping)
    public float interestDeterioration; // 0.5f; || INFORMAL NAME: IMPATIENCE || longer u've looked at, the more likely to drop it || compounding on P(dropping)
    public float initQualityGoalDrop; // = 0.01f; // initial qualitydrop
    public float qualityGoalDeterioration; // = 0.1f; || INFORMAL NAME: IMPATIENCE || longer u wanted to move for, reduce your standards for a house || exponential
}

public struct SimParams {
    public int numLots;
    public int numPeople;
    public List<float> incomeDistribution;
    public int timeUnits;
    public LotAttractiveness lotAttractiveness;
    public PlayerSettings playerSettings;
    public float dynamicPricingPercent;
}

public class SimManager : MonoBehaviour
{

    [Header("LOT PARAMS")]
    // lots
    public GameObject lotManager;
    public int rows;
    public int cols;
    public GameObject lot;
    public float spacing = 1.0f;
    public float lotSize = 2.0f; 

    [Header("PLAYER PARAMS")]
    // players
    public GameObject playerManager;
    public int numLow = 10;
    public int numMid = 4;
    public int numHigh = 1;
    [HideInInspector] public int numPeople;
    public GameObject player;

    [Header("CLASS PARAMS")]
    public float MoneyInCirculation = 1000; // per time unit, generated from the nether
    [Serializable] public struct PercentsOwned {
        public float Low;
        public float Mid;
        public float High;
    }
    public PercentsOwned percentsOwned;

    public float highestIncome = 0f;
    public float medianIncome = 0f;
    public float lowestIncome = 0f;
    public List<float> incomeDistribution = new();

    [Header("GAME")]
    public float timeUnit;
    public float secsPerUnit;
    public TMP_Text displayText;
    private float timer;
    DateTime startTime;
    public UnityEvent nextStep;
    // public UnityEvent simulationEnd;


    [Header("MATH")]
    public bool controlAttractiveness = true;
    public LotAttractiveness lotAttractiveness;
    public PlayerSettings playerSettings;
    public float dynamicPricingPercent = 0.5f;  // 0: does not factor individual income | 1: max factoring
        // λ: This parameter controls how much the individual's income influences the price. If λ = 1 λ=1, the price is adjusted directly
        // by the percentage difference in income. If λ = 0 λ=0, the income difference has no effect on the price.

    //====================
    [HideInInspector] public static SimManager instance;
    private bool initialized = false;

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

    // Start is called before the first frame update
    void Start()
    {
        numPeople = numLow + numHigh + numMid;
        Debug.Assert(numPeople <= cols*rows, $"NOT ENOUGH LOTS. #PEOPLE: {numPeople}, #LOTS{cols*rows}");
        InitializeSimulation();
    }

    // Update is called once per frame
    void Update()
    {
        if (initialized) {
            timer += Time.deltaTime;
            if (timer >= secsPerUnit) {
                timer = 0;
                nextStep.Invoke();
                timeUnit++;
            }   

            displayText.text =  $"Time Unit: {timeUnit}" + 
                            $"\nTime Elapsed: {(DateTime.Now - startTime).ToString("hh\\:mm\\:ss\\.fff")}" +
                            $"\nSecs per Unit: {secsPerUnit}" ; 
        }
    }


    void InstantiateGrid()
    {
        MovingManager.instance.Lots = new();
        MovingManager.instance.AvailableLots = new();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < cols; col++)
            {
                // Calculate the position for each prefab
                // Vector3 position = new Vector3(col * spacing, row * spacing, 0);
                // centered positioning
                Vector3 position = new Vector3(
                    col * spacing - (cols - 1) * spacing / 2,
                    row * spacing - (rows - 1) * spacing / 2,
                    0
                );
                // position += lotManager.transform.position; // centered on lot manager

                // Instantiate the prefab at the calculated position
                GameObject instance = Instantiate(lot, position, Quaternion.identity);
                instance.transform.parent = lotManager.transform; // move to lot's child
                instance.transform.localScale = Vector3.one * lotSize;
                instance.name = $"lot({row},{col})";

                // intialize the lot
                Lot settings = instance.GetComponent<Lot>();

                MovingManager.instance.Lots.Add(settings, null); // no one in dah houseee
                MovingManager.instance.AvailableLots.Add(settings);
                // settings.deteriorationChance = deteriorationChance;
            }
        }

        int num = MovingManager.instance.Lots.Count;
        int[] a = MyUtils.GuassedRandom(num, lotAttractiveness.means, lotAttractiveness.stdDevs, lotAttractiveness.prob);
        
        for (int i = 0; i < num; i++) {
            var item = MovingManager.instance.Lots.ElementAt(i);
            Lot lot = item.Key;
            if (controlAttractiveness) {
                lot.attractiveness = a[i];
            }  else {
                lot.attractiveness = UnityEngine.Random.Range(-10,11);
            }
            lot.GetComponent<ColorChanger>().R = (lot.attractiveness + 10) / 20.0f;
        
        }

        

    }

    void InstatiatePlayers() 
    {
        void instantiateClass(int num, SocioClass socio, float percentOwned) {
            // List<float> incomes = new();
            float pie = percentOwned*MoneyInCirculation;
            List<float> pieDistribution = MyUtils.SplitPie(pie, num);
            incomeDistribution.AddRange(pieDistribution);

            for (int i = 0; i < num; i++) {
                GameObject instance = Instantiate(player);
                instance.transform.parent = playerManager.transform; // move to lot's child
                instance.name = $"player({socio}{i})";

                Player settings = instance.GetComponent<Player>();
                MovingManager.instance.Players.Add(settings, null); // no house yet!!!

                settings.socioClass = socio;
                settings.income = pieDistribution[i];
                if (pieDistribution[i] > highestIncome) highestIncome = pieDistribution[i];
            }
        }
        instantiateClass(numLow, SocioClass.LOW, percentsOwned.Low);
        instantiateClass(numMid, SocioClass.MID, percentsOwned.Mid);
        instantiateClass(numHigh, SocioClass.HIGH, percentsOwned.High);
    }
    void InitializePlayers() {
        for (int i = 0; i < MovingManager.instance.Players.Count; i++) {
            var item = MovingManager.instance.Players.ElementAt(i);
            Player player = item.Key;
            
            // give em a random house
            Lot lot = MovingManager.instance.AvailableLots.PopRandom();
            (player.WeightCost, player.WeightAttr) = IncomeManagement.Weights(player);
            player.quality = Calculate.QualityOfLot(lot, player);
            player.MoveToLot(lot);

            player.econRank = IncomeManagement.ScaleToRange(player.income);
            // float color = (player.econRank+1f)/2f; // chance [-1,1] => [0,1]
            float color = player.income / (medianIncome*2f); // more than double median income = higher.. 
            player.GetComponent<ColorChanger>().R = color;

            player.UpdatePosition();

        }

        // second iteration
        for (int i = 0; i < MovingManager.instance.Players.Count; i++) {
            var item = MovingManager.instance.Players.ElementAt(i);
            Player player = item.Key;

            player.maxQuality = Calculate.MaxQualityOnMarket(player);
            player.qualityGoal = player.quality > player.maxQuality ? player.quality : player.maxQuality;

        }
    }


    // ========================= SIMULATION RUNNERS ============================

    public void InitializeSimulation() {
        InstantiateGrid();
        InstatiatePlayers();
        medianIncome = MyUtils.Median(incomeDistribution.ToArray());
        lowestIncome = incomeDistribution.Min();
        foreach(Lot lot in MovingManager.instance.Lots.Keys) lot.currentPrice = Calculate.StaticLotPrice(lot);
        InitializePlayers();

        initialized = true;
        startTime = DateTime.Now;
        Debug.Log("intialized done");
    }

    public void DestroySimulation() {
        initialized = false;
        timer = 0;
        timeUnit = 0;
        nextStep.RemoveAllListeners();
        foreach(Transform child in lotManager.transform) Destroy(child.gameObject);
        foreach(Transform child in playerManager.transform) Destroy(child.gameObject);
    }

}
