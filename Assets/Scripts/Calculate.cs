using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Numerics;
using System;

public class Calculate
{
    public static float alpha = 5f; // highqualiy attraction
    public static float beta = 2f; // lowquality hate
    public static float kappa = 5f; // degree of motivation seperation | cost vs attractiveness | high kappa = sharper care for attractiveness at higher incomes
    public static float sigma = 9f; // cost satisfaction sigmoid

    // chat gptfied
    public static float StaticLotPrice(Lot lot)
    {
        // Normalize attractiveness ([-10, 10] to [0, 1]) : F(A)
        float attractivenessMultiplier = (1+(lot.attractiveness / 10))*2f;

        // see below for G(I): median income
        var incomeDistribution = SimManager.instance.incomeDistribution.ToArray();
        float incomeMultiplier = IncomeManagement.CalculateIncomeMultiplier(incomeDistribution);

        // Calculate price
        float price = attractivenessMultiplier * incomeMultiplier;

        // Debug.Log($"static price: {price} | A{lot.attractiveness} Inc{incomeMultiplier}");
        return price;
    }

    public static float DynamicLotPrice(Lot lot) {
        // sigma: 1: totally dynamic price 2: totally static price
        float staticPrice = StaticLotPrice(lot);
        var (median, sigma) = (SimManager.instance.medianIncome, SimManager.instance.dynamicPricingPercent);
        
        int N = lot.PotentialBuyers.Count(); // num ppl interested
        if (N > 0) {
            float F_interest = (float)N / MovingManager.instance.N_0;
            float F_income = lot.PotentialBuyers.Average(player => player.income) / median;
            float dynamicPrice = staticPrice * F_interest * F_income;

            // Debug.Log($"dynamic price: {dynamicPrice} | static price: {staticPrice} | int{F_interest} inc{F_income}");
            return (1-sigma)*staticPrice + sigma*dynamicPrice;
        }
        return staticPrice;
    }

    // ================================================================================================

    public static float ChanceOfBuying(float quality, float qualityGoal) {
        float d = Math.Abs(quality-qualityGoal);

        if (quality >= qualityGoal) {
            return 1 - (float)Math.Exp(-(alpha*d+1.3f));
        } else {
            return (float)Math.Exp(-(beta*d+0.3f));
        }
    }

    public static float ChanceOfMoving(Player player) 
    {
        var (S_a, S_c) = LotSatisfaction(player);
        float P = 1 - (player.WeightAttr*S_a + player.WeightCost*S_c);
        // chance of movign out: how satisfied w/ attraction you are * how much do u care abt attractiveness
        return P;

        // Debug.Log($"{player.gameObject.name} A{player.currentLot.attractiveness} C{player.costliness} | S_a{S_a} S_c{S_c} | move?{P}");
    }


    // ================================================================================================

    public static float QualityOfLot(Lot lot, Player player) {
        // cost quality [-1, 1] : W * costliness mapped to [-1,1]
        // attractive quality [-1, 1] : W * lot.attractiveness/10f;


        float costliness = lot.currentPrice / player.income; // [0,1]
        if (costliness > 1) return 0; // cant even afford it bro
        float goodPrice = 1 - costliness;  // [0,1] >> reverse it so 1 = good

        float costScore =  2 * goodPrice - 1;  // Shift to [-1, 1] range
        float attractiveScore = lot.attractiveness/10f; // shift to [-1, 1] range
        // Debug.Log($"C{player.WeightCost} A{player.WeightAttr} | q_c{costliness} q_a{attractiveScore} | quality{Q}");

        float Q = player.WeightCost * costScore + player.WeightAttr * attractiveScore;
        return Q; // [0,1]*[-1,1] + [0,1]*[-1,1] => [-1,1]
    }

    public static (float, float) LotSatisfaction(Player player)
    {

        // satisfaction attractiveness
        float S_a = (player.currentLot.attractiveness+10f)/20f;
        // satisfaction cost
        float S_c = 1 / (1+(float)Math.Exp(sigma*(player.costliness-0.5f))); // sigmoid satisfaction
        // costliness is in [0,1]

        // Debug.Log($"{player.gameObject.name} | attr{S_a} cost{S_c} | costiness{player.costliness}");

        return (S_a, S_c);
    }

    public static (float, float) QualityOnMarket(Player player) {
        float maxQuality = 0f;
        List<float> qualities = new();
        foreach ((Lot lot, bool avail) in MovingManager.instance.AvailableLots) {
            if (avail) {
                var q = QualityOfLot(lot, player);
                qualities.Add(q);

                if (q > maxQuality) maxQuality = q;
            }
        }

        return qualities.Count > 0 ? (maxQuality, MyUtils.Median(qualities.ToArray())) : (maxQuality, 0f);
    }
}

public class IncomeManagement // G(i)
{
        // Function to calculate the median of an array

    // Function to calculate g(I) with a more robust income measure (median, trimmed mean, or log-transformed)
    public static float CalculateIncomeMultiplier(float[] incomeDistribution)
    {
        float medianIncome = MyUtils.Median(incomeDistribution); // median
        // float medianIncome = TrimmedMean(incomeDistribution, 0.1f); // 10% trimmed // trimmed mean
        // float medianIncome = Math.Log(incomeDistribution.Average()); // Log-transformation // log-transformed income
        return medianIncome;
    }

    public static (float, float) Weights(Player player) {

        float econStanding = player.income/SimManager.instance.medianIncome;
        float WeightCost = 1/(1 + (float)Math.Exp(Calculate.kappa * (player.income-SimManager.instance.medianIncome)));
        float WeightAttr = 1 - WeightCost;

        // Debug.Log($"{player.socioClass} diff{player.income-SimManager.instance.medianIncome} |wcost{WeightCost} wattr{WeightAttr}");

        return (WeightCost, WeightAttr);
    }
    public static float ScaleToRange(float x)
    {
        var (min, max) = (SimManager.instance.lowestIncome, SimManager.instance.highestIncome);
        // Ensure the value is within bounds, to avoid division by zero or out-of-bounds scaling
        if (min == max)
        {
            throw new InvalidOperationException("Min and Max values must be different.");
        }
        // Scale to the [-1, 1] range
        return 2 * (x - min) / (max - min) - 1;
    }

                

}
