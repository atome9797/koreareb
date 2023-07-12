using System.Diagnostics;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    void Start()
    {
        Stopwatch watch = new Stopwatch();
        watch.Start();

        watch.Stop();
        UnityEngine.Debug.Log(watch.ElapsedMilliseconds + " ms");
    }
}