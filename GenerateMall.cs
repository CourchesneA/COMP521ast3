using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateMall : MonoBehaviour {
    //We will use thick walls for the simulation, i.e. 1x1 blocks

    public Mall mall;

    public void Awake()
    {
        mall = new Mall(5,5,5);
    }


}

public class Mall
{
    //Input variables
    public int stairLength;
    public int timeDimension;
    public int mallPopulation;

    //Constants
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
    private int mallWidth
    {
        get { return floorWidth; }
    }
    private int mallLength
    {
        get { return floorLength * 2 + stairLength; }
    }

    //2D Object Array used for generating terrain initially
    private Tile[,] gridPlan;

    //3D int array used for generating the shoppers
    private int[,,] occupancyMap;

    public Mall(int stairLength, int timeDimension, int mallPopulation)
    {
        this.stairLength = stairLength;
        this.timeDimension = timeDimension;
        this.mallPopulation = mallPopulation;

        this.gridPlan = new Tile[mallWidth, mallLength];
        this.occupancyMap = new int[timeDimension, mallWidth, mallLength];

        //Populate the first layer with empty tiles
        for (int i=0; i<gridPlan.GetLength(0); i++)
        {
            for(int j=0; j<gridPlan.GetLength(1); j++)
            {
                gridPlan[i, j] = new Tile(new Position(i,j));
            }
        }

        //Add outofbounds & stair tiles
        AddBounds();

        //Add store walls
        AddStores();

        //Add Plants
        AddPlants();

        PrintGrid();
    }

    private void AddBounds()
    {
        for(int x=0; x < mallWidth; x++)
        {
            for(int y = floorLength; y < mallLength-floorLength; y++)
            {
                Tile room = gridPlan[x, y];
                if(x%(mallWidth/(stairCount))== (mallWidth / (stairCount))/2)
                {
                    room.isStair = true;
                }
                else
                {
                    room.occupant = new Abyss();
                }
            }
        }
    }

    private void AddStores()
    {
        //Lower floor
        for(int i=0 ; i<storePerFloor; i++)
        {
            GenerateStore(new Position(i * (storeWidth + 1), 0), false);
        }

        //Upper floor
        for(int i=0; i<storePerFloor; i++)
        {
            GenerateStore(new Position(i * (storeWidth + 1), mallLength-(storeLength+2)), true);
        }
    }

    private void GenerateStore(Position corner, bool inverted)
    {
        for(int i=0; i<storeLength+2; i++)
        {
            Tile room = gridPlan[corner.x, corner.y + i];
            if (room.occupant == null)
            {
                room.occupant = new Wall();
            }
            room = gridPlan[corner.x + storeWidth + 1, corner.y + i];
            if(room.occupant == null)
            {
                room.occupant = new Wall();
            }
        }

        for(int i=0; i<storeWidth; i++)
        {
            Tile room = gridPlan[corner.x + 1 + i, corner.y];
            if(room.occupant == null)
            {
                room.occupant = new Wall();
            }
            room = gridPlan[corner.x + 1 + i, corner.y + storeLength + 1];
            if(room.occupant == null)
            {
                room.occupant = new Wall();
            }
        }

        int offset = inverted ? 0:storeLength + 1;
        gridPlan[corner.x + 1 + storeWidth / 2, corner.y + offset].occupant = null;
    }

    private void AddPlants()
    {
        int plantGenerated = 0;
        while(plantGenerated < plantCount)
        {
            int x = Random.Range(0,mallWidth);
            int offset = Random.Range(0, 3);
            int side = Random.Range(0, 2);

            int y = (side == 0) ? storeLength + 3 + offset : mallLength - (storeLength + 4) - offset;

            Tile room = gridPlan[x, y];
            if (!room.isObstacle)
            {
                room.occupant = new Plant();
                plantGenerated++;
            }
        }
        
    }

    public void PrintGrid()
    {
        string str = "";
        for(int i=0; i< gridPlan.GetLength(0); i++)
        {
            for(int j=0; j<gridPlan.GetLength(1); j++)
            {
                char tileType = '_';
                if(gridPlan[i,j].occupant is Wall)
                {
                    tileType = 'X';
                }else if(gridPlan[i, j].occupant is Abyss)
                {
                    tileType = 'A';
                }else if(gridPlan[i,j].occupant is Plant)
                {
                    tileType = 'P';
                }
                str += "[" + tileType + "]";
            }
            str += System.Environment.NewLine;

        }
        Debug.Log(str);
    }

}

public class Tile
{
    public IOccupant occupant;
    public bool isObstacle
    {
        get { return (occupant is Wall || occupant is Plant || occupant is Abyss); }
    }
    public bool isStair = false;
    public Position position;

    public Tile(Position position)
    {
        this.position = position;
        this.occupant = null;
    }

}

public interface IOccupant
{

}

public class Shopper : IOccupant
{

}

public class Plant : IOccupant
{

}

public class Wall : IOccupant
{

}

public class Abyss : IOccupant
{

}


public class Position
{
    public int x;
    public int y;

    public Position(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}