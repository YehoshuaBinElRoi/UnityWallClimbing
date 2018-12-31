using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

public struct FindPoints : IJobParallelFor
{
    NativeArray<RaycastHit> pointList;

    public void Execute(int i)
    {

    }
}

public class ParallelJobs : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
