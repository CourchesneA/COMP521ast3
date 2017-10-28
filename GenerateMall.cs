using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateMall : MonoBehaviour {
    //We will use thick walls for the simulation, i.e. 1x1 blocks

    public int stairLength;
    public int timeDimension;
    public int mallPopulation;

    public const int plantCount = 4;
    public const int storePerFloor = 6;
    public const int floorDepth = 5;
    public const int stairCount = 4;
    public const int storeWidth = 5;    //Does not include walls
    public const int storeLength = 3;    //Does not include walls

    public const int wallWidth = 1;
    public const int wallLength = 1;

    private int floorWidth = (storeWidth + wallWidth) * storePerFloor + wallWidth;
    private int floorLength = floorDepth + wallLength * 2 + storeLength;

    public void Awake()
    {
        
    }


}

public class Tile
{
    public bool isWall;
    public bool isPlant;
    public bool isObstacle
    {
        get { return (isWall || isPlant); }
    }
    public int x;
    public int y;
    public int z;
}
