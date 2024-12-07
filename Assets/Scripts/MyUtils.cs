using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Linq;
using System.Numerics;
using System;
using Unity.VisualScripting;
using System.Text.RegularExpressions;

public static class ListExtensions
{
    public static (int,T) GetRandom<T>(this List<T> list)
    {
        if (list == null || list.Count == 0)
        {
            Debug.LogWarning("List is empty or null. Returning default value.");
            return default;
        }

        // Generate a random index
        int randomIndex = UnityEngine.Random.Range(0, list.Count);

        // Get the element at the random index
        T element = list[randomIndex];

        // Return the popped element
        return (randomIndex, element);
    }

    public static T PopRandom<T>(this List<T> list)
    {
        var (idx, element) = list.GetRandom();

        // Remove the element from the list
        list.RemoveAt(idx);

        // Return the popped element
        return element;
    }

    public static List<Player> InsertInOrder(this List<Player> list, Player player)
    {
        int index = list.FindIndex(p => p.income < player.income);
        if (index == -1)
            list.Add(player); // Add at the end if no one has a smaller income
        else
            list.Insert(index, player); // Insert in the correct position
        return list;
    }

    public static List<int> GetIndicesFromValue(this List<bool> list, bool value)
    {
        List<int> indices = new List<int>();

        for (int i = 0; i < list.Count; i++)
        {
            if (value == list[i]) // Check if the value is false
            {
                indices.Add(i);
            }
        }
        return indices;
    }

    public static int GetRandomIndexFromValue(this List<bool> list, bool value)
    {
        // Filter the indices that match the value (true or false)
        List<int> matchingIndices = list.GetIndicesFromValue(value);
        // If no matching indices were found, return -1 or handle as needed
        if (matchingIndices.Count == 0)
        {
            return -1; // No match found
        }
        // Return a random index from the filtered list
        int randomIndex = UnityEngine.Random.Range(0, matchingIndices.Count);
        return matchingIndices[randomIndex];
    }

    private static System.Random rng = new System.Random();  
    public static void Shuffle<T>(this IList<T> list)  
    {  
        int n = list.Count;  
        while (n > 1) {  
            n--;  
            int k = rng.Next(n + 1);  
            T value = list[k];  
            list[k] = list[n];  
            list[n] = value;  
        }  
    }


}

public static class MyUtils {
    public static List<float> SplitPie(float pie, int numPeople)
    {
                List<float> distributions = new List<float>();
                float totalRandomValue = 0f;

                // Generate random values for each person
                for (int i = 0; i < numPeople; i++)
                {
                    float randValue = UnityEngine.Random.value;
                    distributions.Add(randValue);
                    totalRandomValue += randValue;
                }
                // Normalize the values so that they sum to 'pie'
                for (int i = 0; i < numPeople; i++) distributions[i] = distributions[i] / totalRandomValue * pie;
                return distributions;
            }
        public static float Median(float[] values)
    {
        var sortedValues = values.OrderBy(v => v).ToArray();
        int middle = sortedValues.Length / 2;
        
        // If the length is odd, return the middle value, otherwise, return the average of the two middle values
        return (sortedValues.Length % 2 == 0) ? (sortedValues[middle - 1] + sortedValues[middle]) / 2 : sortedValues[middle];
    }

    // Function to calculate the trimmed mean
    public static float TrimmedMean(float[] values, float trimPercentage)
    {
        var sortedValues = values.OrderBy(v => v).ToArray();
        int trimCount = (int)(sortedValues.Length * trimPercentage);

        // Trim the extremes based on the given percentage and calculate the mean
        var trimmedValues = sortedValues.Skip(trimCount).Take(sortedValues.Length - 2 * trimCount).ToArray();
        return trimmedValues.Average();
    }

    private static System.Random random = new System.Random();
    // Box-Muller Transform
    public static float GaussianRandomValue(float mean, float stdDev)
    {

        float u1 = 1.0f - (float)random.NextDouble(); // Uniform random variable between 0 and 1
        float u2 = 1.0f - (float)random.NextDouble();
        float z0 = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Cos(2.0f * Mathf.PI * u2); // Gaussian distribution
        return mean + z0 * stdDev;
    }

    // min and max [-10,10] hardcoded for usecase in attractiveness
    public static int[] GuassedRandom(int num, float[] means, float[] stdDevs, float[] prob) {
        // float randValue = UnityEngine.Random.value;
        if (prob.Sum() != 1) Debug.LogError($"Sum of probabilities for GuassedRandom != 1. {prob.Sum()}");

        List<int> randoms = new();
        float bit = (float)1/num;
        float counter = 0;
        int i = 0;
        float probCounter = prob[i];
        
        for (int n = 0; n < num; n++) {
            if (counter >= probCounter) {
                i++;
                probCounter += prob[i];
            } 
            
            var (mean, stdDev) = (means[i], stdDevs[i]);
            float guass = GaussianRandomValue(mean, stdDev);
            int a = (int)Mathf.Clamp(Mathf.Round(guass), -10, 10);
            randoms.Add(a);
            counter += bit;
        }

        randoms.Shuffle();
        return randoms.ToArray();
        
    }

    // Get the direct (up, down, left, right) neighbors
    public static List<int> GetDirectNeighbors(int index)
    {
        var (rows, cols) = (SimManager.instance.rows,SimManager.instance.cols);
        List<int> neighbors = new List<int>();

        // Calculate row and column from index
        int row = index / cols;
        int col = index % cols;

        // Check neighbors (clamped)
        // Up
        if (row > 0) neighbors.Add(index - cols);  // row - 1, same column
        // Down
        if (row < rows - 1) neighbors.Add(index + cols);  // row + 1, same column
        // Left
        if (col > 0) neighbors.Add(index - 1);  // same row, col - 1
        // Right
        if (col < cols - 1) neighbors.Add(index + 1);  // same row, col + 1

        return neighbors;
    }

    // Get the diagonal (top-left, top-right, bottom-left, bottom-right) neighbors
    public static List<int> GetDiagonalNeighbors(int index)
    {
        var (rows, cols) = (SimManager.instance.rows,SimManager.instance.cols);
        List<int> neighbors = new List<int>();

        // Calculate row and column from index
        int row = index / cols;
        int col = index % cols;

        // Check diagonal neighbors (clamped)
        // Top-left
        if (row > 0 && col > 0) neighbors.Add(index - cols - 1);
        // Top-right
        if (row > 0 && col < cols - 1) neighbors.Add(index - cols + 1);
        // Bottom-left
        if (row < rows - 1 && col > 0) neighbors.Add(index + cols - 1);
        // Bottom-right
        if (row < rows - 1 && col < cols - 1) neighbors.Add(index + cols + 1);

        return neighbors;
    }

    public static (List<Lot>, List<Lot>) GetSurroundingLots(Lot lot) {

        // Method to get the child's index in the parent's hierarchy
        int GetChildIndexInHierarchy(GameObject child)
        {
            // Ensure the child and its parent are valid
            if (child != null && child.transform.parent != null)
            {
                // Return the child's index in the parent's hierarchy
                return child.transform.GetSiblingIndex();
            }
            // Return -1 if there is no valid parent (indicating an error)
            return -1;
        }

        // Method to get a child GameObject by sibling index
        GameObject GetChildByIndexInHierarchy(int index)
        {
            var transform = SimManager.instance.lotManager.transform;
            // Ensure the parent has children
            if (transform.childCount > 0 && index >= 0 && index < transform.childCount)
            {
                // Return the child at the given index
                Transform childTransform = transform.GetChild(index);
                return childTransform.gameObject;
            }

            // Return null if the index is invalid or no children exist
            return null;
        }


        int idx = GetChildIndexInHierarchy(lot.transform.gameObject);
        int[] neighborIdx = GetDirectNeighbors(idx).ToArray();
        int[] adjacentIdx = GetDiagonalNeighbors(idx).ToArray();

        List<Lot> neighborLots = new();
        List<Lot> adjacentLots = new();

        foreach (int i in neighborIdx) {
            if (i >= SimManager.instance.lotManager.transform.childCount) Debug.Log(i);
            neighborLots.Add(GetChildByIndexInHierarchy(i).GetComponent<Lot>());
        }
        foreach (int i in adjacentIdx) {
            adjacentLots.Add(GetChildByIndexInHierarchy(i).GetComponent<Lot>());
        }

        return (neighborLots, adjacentLots);

    }


}