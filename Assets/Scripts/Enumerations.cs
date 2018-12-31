using UnityEngine;
using System.Collections;

public class Enumerations : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}

[System.Serializable]
public enum PlayerStates
{
	Normal,
	LedgeScanning,
	LedgeClimbing,
	PlateauClimbing,
	LedgeHanging,
    PlateauHanging,
}


[System.Serializable]
public enum LedgeType
{
    Ledge,
    Plateau,
    Invalid,
}



public enum BlockType
{
    Path,
    Trap,
    Empty,
}

public enum EdgeDirection
{
    FrontLeft,
    FrontRight,
    RearLeft,
    RearRight,
}

public enum PointFindMode
{
    FixedPoint,
    CascadePoints,
}
