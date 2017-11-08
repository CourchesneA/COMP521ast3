﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateMall : MonoBehaviour {
    //We will use thick walls for the simulation, i.e. 1x1 blocks

        //TODO loop & analyze

    public Mall mall;
    //public Transform wallPrefab;
    //public Transform tilePrefab;
    //public Transform plantPrefab;
    //public Transform shopperPrefab;
    public int TimeDimension;
    public int StairLength;
    public int MallPopulation;

    private int updateFrame = 40;   //Update shoppers position at each __ frames 

    private double frameCount = 0;

    public void Awake()
    {
        //Generates the mall with obstacles and terrain
        mall = new Mall(StairLength, TimeDimension, MallPopulation);

        //Display the Map without shoppers
        for(int i=0; i<mall.gridPlan.GetLength(0); i++)
        {
            for(int j=0; j<mall.gridPlan.GetLength(1); j++)
            {
                if(mall.gridPlan[i,j].occupant is Wall)
                {
                    
                    Instantiate(Resources.Load("Prefabs/Wall"), new Vector3(i, GetHeigth(j)+0.5f, j), Quaternion.identity, gameObject.transform);
                }else if(mall.gridPlan[i,j].occupant is Plant)
                {
                    Instantiate(Resources.Load("Prefabs/Plant"), new Vector3(i, GetHeigth(j), j), Quaternion.Euler(-90,0,0), gameObject.transform);
                }

                if(!(mall.gridPlan[i,j].occupant is Abyss))
                {
                    
                    Instantiate(Resources.Load("Prefabs/FloorTile"), new Vector3(i, GetHeigth(j), j), Quaternion.identity, gameObject.transform);
                }
            }
        }
        
        foreach(Shopper shopper in mall.shoppers)
        {
            GameObject go = Instantiate(Resources.Load("Prefabs/Shopper"), new Vector3(shopper.position.x, GetHeigth(shopper.position.y), shopper.position.y), Quaternion.identity, gameObject.transform) as GameObject;
            int shirtColor = Random.Range(1, 6);
            go.GetComponentInChildren<Renderer>().material = Resources.Load("Materials/shirt"+shirtColor) as Material;
            shopper.transform = go.transform;
        }
    }

    public void Update()
    {
        if(frameCount % updateFrame == 0)    //Execute once every 60 frames
        {
            foreach(Shopper shopper in mall.shoppers)
            {
                Vector3 newPos;
                try
                {
                    Position3 p = shopper.path[((int)(frameCount / updateFrame))].position;  //This might thrown null or out of bound
                    newPos = new Vector3(p.x, GetHeigth(p.y), p.y);
                    shopper.transform.position = newPos;
                }
                catch (System.Exception)
                {
                    //Either the path is done or no path found
                    // => do nothing
                }
            }

        }

        frameCount++;
    }

    public float GetHeigth(int y)
    {
        float height;
        if (y > 10 + StairLength)
        {
            height = StairLength;
        }
        else if (y < 10)
        {
            height = 0;
        }
        else
        {
            height = y - 10;
        }
        return height;
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

    public int floorWidth = (storeWidth + wallWidth) * storePerFloor + wallWidth;
    public int floorLength = floorDepth + wallLength * 2 + storeLength;
    public int MallWidth
    {
        get { return floorWidth; }
    }
    public int MallLength
    {
        get { return floorLength * 2 + stairLength; }
    }

    public List<Shopper> shoppers;


    //2D Object Array used for generating terrain initially
    public Tile[,] gridPlan;

    //3D int array used for generating the shoppers
    private int[,,] occupancyMap;

    public Mall(int stairLength, int timeDimension, int mallPopulation)
    {
        this.stairLength = stairLength;
        this.timeDimension = timeDimension;
        this.mallPopulation = mallPopulation;
        shoppers = new List<Shopper>(mallPopulation);

        this.gridPlan = new Tile[MallWidth, MallLength];
        this.occupancyMap = new int[timeDimension, MallWidth, MallLength];

        //Populate the first layer with empty tiles
        for (int i=0; i<gridPlan.GetLength(0); i++)
        {
            for(int j=0; j<gridPlan.GetLength(1); j++)
            {
                gridPlan[i, j] = new Tile(new Position2(i,j));
            }
        }

        //Add outofbounds & stair tiles
        AddBounds();

        //Add store walls
        AddStores();

        //Add Plants
        AddPlants();

        //Generate occupancy map from the 2D terrain
        PopulateOccupancyMap();

        //Add randomly positionned shoppers
        AddShoppers();

        //Generate a path for each shopper, reserving it in the occupancy grid
        foreach(Shopper shopper in shoppers)
        {
            PlanMoves(shopper);
            Debug.Log("Planning done for shopper " + shopper.ID);
            //break; //DEBUG
        }

        PrintGrid();
    }

    
    /// <summary>
    /// Add the out of bounds tiles (i.e. on each sides of teh stairs)
    /// </summary>
    private void AddBounds()
    {
        for(int x=0; x < MallWidth; x++)
        {
            for(int y = floorLength; y < MallLength-floorLength; y++)
            {
                Tile room = gridPlan[x, y];
                if(x%(MallWidth/(stairCount))== (MallWidth / (stairCount))/2)
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

    /// <summary>
    /// Generates the stores on 2D array of the terrain
    /// </summary>
    private void AddStores()
    {
        //Lower floor
        for(int i=0 ; i<storePerFloor; i++)
        {
            GenerateStore(new Position2(i * (storeWidth + 1), 0), false);
        }

        //Upper floor
        for(int i=0; i<storePerFloor; i++)
        {
            GenerateStore(new Position2(i * (storeWidth + 1), MallLength-(storeLength+2)), true);
        }
    }

    /// <summary>
    /// Generate a store given its position and orientation
    /// </summary>
    /// <param name="corner"></param>
    /// <param name="inverted"></param>
    private void GenerateStore(Position2 corner, bool inverted)
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

    /// <summary>
    /// Randomly positions plants
    /// </summary>
    private void AddPlants()
    {
        int plantGenerated = 0;
        while(plantGenerated < plantCount)
        {
            int x = Random.Range(0,MallWidth);
            int offset = Random.Range(0, 3);
            int side = Random.Range(0, 2);

            int y = (side == 0) ? storeLength + 3 + offset : MallLength - (storeLength + 4) - offset;

            Tile room = gridPlan[x, y];
            if (!room.IsObstacle)
            {
                room.occupant = new Plant();
                plantGenerated++;
            }
        }
        
    }

    /// <summary>
    /// Randomly position shoppers
    /// </summary>
    private void AddShoppers()
    {
        int shoppersGenerated = 0;
        int shopperID = 1;
        while (shoppersGenerated < mallPopulation)
        {
            int x = Random.Range(0, MallWidth);
            int y = Random.Range(0, MallLength);

            if (occupancyMap[0, x, y] == 0)
            {
                Shopper s = new Shopper() { ID = shopperID++, position = new Position2(x,y) };
                occupancyMap[0, x, y] = s.ID;
                shoppersGenerated++;
                shoppers.Add(s);
            }
        }
    }

    /// <summary>
    /// Use the 2D array of object to build a 3D array of primitive that will be used for A*
    /// </summary>
    private void PopulateOccupancyMap()
    {
        //Occupancy codes: 0=empty, -1=wall, plant or out of bounds, positive number : shopper ID
        for(int i = 0; i < occupancyMap.GetLength(0); i++)
        {
            for (int j = 0; j < occupancyMap.GetLength(1); j++)
            {
                for(int k = 0; k < occupancyMap.GetLength(2); k++)
                {
                    if (gridPlan[j, k].IsObstacle)
                    {
                        occupancyMap[i, j, k] = -1;
                    }
                }
                
            }
        }
    }

    /// <summary>
    /// Given a shopper, plan its trip to its randomly chosen destination.
    /// If no such path exists in the given timeframe, the shopper will stay idle
    /// Result will be store in shopper.path
    /// </summary>
    public void PlanMoves(Shopper shopper)
    {
        //TODO Make sure every node are explored only once (35*37*25 possibilities)

        //Choose a destination with equal probability of inside a shop or not
        shopper.destination = shopper.ChooseDestination(this);

        GridNode[,,] nodeMap = new GridNode[occupancyMap.GetLength(0), occupancyMap.GetLength(1), occupancyMap.GetLength(2)];
        for(int i=0; i< occupancyMap.GetLength(0); i++)
        {
            for(int j=0; j< occupancyMap.GetLength(1); j++)
            {
                for(int k=0; k<occupancyMap.GetLength(2); k++)
                {
                    Position3 nodePos = new Position3(j, k, i);
                    nodeMap[i, j, k] = new GridNode(nodePos) { gCost= int.MaxValue , hCost = CostToGo(nodePos.p2, shopper.destination) };
                }
            }
        }

        //A*: Now we have what we need to search a path for 
        List<GridNode> openSet = new List<GridNode>();
        HashSet<GridNode> closedSet = new HashSet<GridNode>();

        //openSet.Add(new GridNode(new Position3(shopper.position.x, shopper.position.y, 0)) { gCost = 0, hCost = CostToGo(shopper.position,shopper.destination) });
        nodeMap[0, shopper.position.x, shopper.position.y].gCost = 0;
        openSet.Add(nodeMap[0, shopper.position.x, shopper.position.y]);

        while(openSet.Count > 0)    //While there are node to explore
        {
            if(closedSet.Count > timeDimension * MallLength * MallWidth * 0.35)     //Could not find a path by exploring the first 35% of the 3D grid, give up to speed up process
            {
                break;
            }

            //TODO We should implement this as a heap for optimization
            GridNode currentNode = openSet[0];      //Select one with lowest fCost
            for(int i=1; i<openSet.Count; i++)
            {
                if(openSet[i].fCost < currentNode.fCost || openSet[i].fCost == currentNode.fCost && openSet[i].hCost < currentNode.hCost)
                {
                    currentNode = openSet[i];
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode.position.Compare(shopper.destination))
            {
                //Retrace path and register it in the occupancy map
                shopper.path = RetracePath(shopper.position, currentNode, shopper.ID);
                return;
            }

            foreach(GridNode node in GetFreeNeighbours(currentNode,shopper.destination,nodeMap))
            {
                //non traversable nodes are already removed from this list

                //Check if node is in already explored*******
                if (closedSet.Contains(node))
                {
                    continue;
                }

                //If path is shorter update gcost & parent node
                //int newNeighbourCost = currentNode.position.Compare(node.position.p2) ? currentNode.gCost : currentNode.gCost + 1;
                int newNeighbourCost = currentNode.gCost + 1;

                bool isInOpen = openSet.Contains(node);
                if (newNeighbourCost < node.gCost || !isInOpen)
                {
                    node.gCost = newNeighbourCost;
                    node.parent = currentNode;

                    if (!isInOpen)
                    {
                        openSet.Add(node);
                    }
                }
            }

        }

        //null path => we should not move
        shopper.path = null;
    }

    public List<GridNode> RetracePath(Position2 start, GridNode endNode, int id)
    {
        List<GridNode> path = new List<GridNode>();
        GridNode currentNode = endNode;
        //Also reserve the endNode until the end of the cycle
        for(int i= endNode.position.t; i<timeDimension; i++)
        {
            occupancyMap[i, endNode.position.x, endNode.position.y] = id;
        }

        while(!currentNode.position.Compare(start))
        {
            path.Add(currentNode);
            occupancyMap[currentNode.position.t, currentNode.position.x, currentNode.position.y] = id;
            try
            {
                occupancyMap[currentNode.position.t + 1, currentNode.position.x, currentNode.position.y] = id; //To prevent face to face collision
            } catch (System.Exception) { }
            currentNode = currentNode.parent;
        }
        path.Reverse();
        return path;
    }

    /// <summary>
    /// Return a list of adjacent nodes that are not out of bounds and are not obstacles
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    public List<GridNode> GetFreeNeighbours(GridNode node, Position2 destination, GridNode[,,] nodeMap)
    {
        List<GridNode> list = new List<GridNode>();
        Position3 pos = node.position;
        if(pos.t >= timeDimension)
        {
            return list;    //No neighbour available, return empty list
        }
        int time = pos.t + 1;


        for (int i = 0; i < 4; i++)
        {
            try
            {
                Position3 otherPos = new Position3(pos.x + (int)Mathf.Sin(i * Mathf.PI / 2), pos.y + (int)Mathf.Cos(i * Mathf.PI / 2), time);
                //Add this adjacent cell if its not a room and its not out of bound
                if (occupancyMap[time,otherPos.x,otherPos.y] == 0)
                {
                    //GridNode newNode = new GridNode(otherPos);
                    GridNode newNode = nodeMap[otherPos.t, otherPos.x, otherPos.y];
                    //newNode.gCost = node.gCost + 1;
                    //newNode.hCost = CostToGo(newNode.position.p2, destination);
                    list.Add(newNode);
                }
            }
            catch (System.Exception)
            {
                //Tile was out of bound or obstacle, continue
                continue;
            }
        }

        //Not moving may also be a choice
        list.Add(new GridNode(new Position3(pos.x, pos.y, time)) { gCost = node.gCost, hCost = CostToGo(pos.p2,destination) }); //gCost + 1?

        return list;
    }

    /// <summary>
    /// Debugging : Print a representation of the grid in ASCII in the console
    /// </summary>
    public void PrintGrid()
    {
        try
        {
            string str = "";
            for (int i = 0; i < gridPlan.GetLength(0); i++)
            {
                for (int j = 0; j < gridPlan.GetLength(1); j++)
                {
                    char tileType = '_';
                    if (gridPlan[i, j].occupant is Wall)
                    {
                        tileType = 'X';
                    }
                    else if (gridPlan[i, j].occupant is Abyss)
                    {
                        tileType = 'A';
                    }
                    else if (gridPlan[i, j].occupant is Plant)
                    {
                        tileType = 'P';
                    }
                    else if (occupancyMap[0, i, j] != 0)
                    {
                        tileType = occupancyMap[0, i, j].ToString()[0];
                    }
                    str += "[" + tileType + "]";
                }
                str += System.Environment.NewLine;

            }
            Debug.Log(str);
        }
        catch (System.Exception)
        {
            //We dont want an error thrown in a debug function
            Debug.Log("Error Occured while trying to print grid to console");
        }
        
    }

    /// <summary>
    /// Check if the given position is in a store (or wall around them)
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public bool IsInShop(Position2 position)
    {
        if(position.y <= storeLength+2 || position.y >= MallLength - (storeLength + 2))
        {
            return true;
        }
        return false;
    }

    private int ManhattanDist(Position2 p1, Position2 p2)
    {
        return System.Math.Abs(p2.x - p1.x) + System.Math.Abs(p2.y - p1.y);
    }

    private int CostToGo(Position2 current, Position2 dest)
    {
        return ManhattanDist(current, dest);
    }
}

public class Tile
{
    public IOccupant occupant;
    public bool IsObstacle
    {
        get { return (occupant is Wall || occupant is Plant || occupant is Abyss); }
    }
    public bool isStair = false;
    public Position2 position;

    public Tile(Position2 position)
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
    public Position2 position;
    public int ID;
    public Position2 destination;
    public List<GridNode> path;
    public Transform transform;

    public Position2 ChooseDestination(Mall mall)
    {
        bool inShop = Random.Range(0, 2) == 1;

        while (true)
        {
            int x = Random.Range(0, mall.MallWidth);
            int y = Random.Range(0, mall.MallLength);
            Position2 position = new Position2(x, y);

            if(mall.IsInShop(position) == inShop && !mall.gridPlan[x, y].IsObstacle)
            {
                return position;
            }
            
        }

    }

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

public class GridNode
{
    public Position3 position;
    public int gCost;
    public int hCost;
    public GridNode parent;

    public GridNode(Position3 position)
    {
        this.position = position;
    }
    public int fCost
    {
        get {
            if (gCost.Equals(int.MaxValue)) return int.MaxValue;
            return gCost + hCost;
        }
    }
    /*
    bool System.IEquatable<GridNode>.Equals(GridNode other)
    {
        if(this.position.x == other.position.x && this.position.y == other.position.y && this.position.t == other.position.t)
        {
            return true;
        }
        return false;
    }*/
}

public class Position2
{
    public int x;
    public int y;

    public Position2(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

public class Position3
{
    public int x;
    public int y;
    public int t;

    public Position2 p2
    {
        get{ return new Position2(this.x, this.y); }
    }

    public Position3(int x, int y, int t)
    {
        this.x = x;
        this.y = y;
        this.t = t;
    }

    


    public bool Compare(Position2 other)
    {
        return x == other.x && y == other.y;
    }

}