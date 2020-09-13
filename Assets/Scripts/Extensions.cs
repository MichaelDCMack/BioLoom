using UnityEngine;

public class Extensions
{
    public static void Shuffle<T>(T[] array)
    {
        int n = array.Length;
        while (n > 1)
        {
            int k = Random.Range(0, n);
            --n;
            T temp = array[n];
            array[n] = array[k];
            array[k] = temp;
        }
    }
    
    public static int Clamp(int value, int min, int max)
    {
        if(value < min)
        {
            return min;
        }
        if(value > max)
        {
            return max;
        }
        return value;
    }

    public static int Mod(int a, int b)
    {
        return ((a %= b) < 0) ? a + b : a;
    }
}