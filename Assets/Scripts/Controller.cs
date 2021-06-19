using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour
{
    //GameObjects
    public GameObject board;
    public GameObject[] cops = new GameObject[2];
    public GameObject robber;
    public Text rounds;
    public Text finalMessage;
    public Button playAgainButton;

    //Otras variables
    Tile[] tiles = new Tile[Constants.NumTiles];
    private int roundCount = 0;
    private int state;
    private int clickedTile = -1;
    private int clickedCop = 0;
    void Start()
    {        
        InitTiles();
        InitAdjacencyLists();
        state = Constants.Init;
    }
        
    //Rellenamos el array de casillas y posicionamos las fichas
    void InitTiles()
    {
        for (int fil = 0; fil < Constants.TilesPerRow; fil++)
        {
            GameObject rowchild = board.transform.GetChild(fil).gameObject;            

            for (int col = 0; col < Constants.TilesPerRow; col++)
            {
                GameObject tilechild = rowchild.transform.GetChild(col).gameObject;                
                tiles[fil * Constants.TilesPerRow + col] = tilechild.GetComponent<Tile>();                         
            }
        }
                
        cops[0].GetComponent<CopMove>().currentTile=Constants.InitialCop0;
        cops[1].GetComponent<CopMove>().currentTile=Constants.InitialCop1;
        robber.GetComponent<RobberMove>().currentTile=Constants.InitialRobber;           
    }

    public void InitAdjacencyLists()
    {
        //Matriz de adyacencia
        int[,] matriu = new int[Constants.NumTiles, Constants.NumTiles];

        //DONE: Inicializar matriz a 0's
        // Miramos cada fila
        for (int x = 0; x < Constants.NumTiles; x++)
        {
            // Miramos cada columna
            for (int y = 0; y < Constants.NumTiles; y++)
            {
                // Ponemos el valor de la casilla actual a 0
                matriu[x, y] = 0;
            }
        }

        // Cada tile
        for (int tile = 0; tile < Constants.NumTiles; tile++)
        {
            //DONE: Para cada posición, rellenar con 1's las casillas adyacentes (arriba, abajo, izquierda y derecha)
            // Fila y columna del tile actual 
            int columna = tile % 8;
            int fila = Mathf.FloorToInt(tile / 8);
            // Arriba
            if(fila + 1 < 8)
            {
                matriu[tile, tile + 8] = 1;
            }
            // Abajo
            if (tile - 8 >= 0)
            {
                matriu[tile, tile - 8] = 1;
            }
            // Izquierda
            if (columna - 1 >= 0)
            {
                matriu[tile, tile - 1] = 1;
            }
            // Derecha
            if (columna + 1 < 8)
            {
                matriu[tile, tile + 1] = 1;
            }
        } // for

        //DONE: Rellenar la lista "adjacency" de cada casilla con los índices de sus casillas adyacentes
        for (int x = 0; x < Constants.NumTiles; x++)
        {
            // Miramos cada columna
            for (int y = 0; y < Constants.NumTiles; y++)
            {
                if (matriu[x, y] == 1)
                {
                    // Ponemos en numero de la casilla en la lista de adyacentes
                    tiles[x].adjacency.Add(y);
                }
            }
        }
    }  

    //Reseteamos cada casilla: color, padre, distancia y visitada
    public void ResetTiles()
    {        
        foreach (Tile tile in tiles)
        {
            tile.Reset();
        }
    }

    public void ClickOnCop(int cop_id)
    {
        switch (state)
        {
            case Constants.Init:
            case Constants.CopSelected:                
                clickedCop = cop_id;
                clickedTile = cops[cop_id].GetComponent<CopMove>().currentTile;
                tiles[clickedTile].current = true;

                ResetTiles();
                FindSelectableTiles(true);

                state = Constants.CopSelected;                
                break;            
        }
    }

    public void ClickOnTile(int t)
    {                     
        clickedTile = t;

        switch (state)
        {            
            case Constants.CopSelected:
                //Si es una casilla roja, nos movemos
                if (tiles[clickedTile].selectable)
                {                  
                    cops[clickedCop].GetComponent<CopMove>().MoveToTile(tiles[clickedTile]);
                    cops[clickedCop].GetComponent<CopMove>().currentTile=tiles[clickedTile].numTile;
                    tiles[clickedTile].current = true;   
                    
                    state = Constants.TileSelected;
                }                
                break;
            case Constants.TileSelected:
                state = Constants.Init;
                break;
            case Constants.RobberTurn:
                state = Constants.Init;
                break;
        }
    }

    public void FinishTurn()
    {
        switch (state)
        {            
            case Constants.TileSelected:
                ResetTiles();

                state = Constants.RobberTurn;
                RobberTurn();
                break;
            case Constants.RobberTurn:                
                ResetTiles();
                IncreaseRoundCount();
                if (roundCount <= Constants.MaxRounds)
                    state = Constants.Init;
                else
                    EndGame(false);
                break;
        }

    }

    public void RobberTurn()
    {
        clickedTile = robber.GetComponent<RobberMove>().currentTile;
        tiles[clickedTile].current = true;
        FindSelectableTiles(false);

        /*TODO: Cambia el código de abajo para hacer lo siguiente
        - Elegimos una casilla aleatoria entre las seleccionables que puede ir el caco
        - Movemos al caco a esa casilla
        - Actualizamos la variable currentTile del caco a la nueva casilla
        */

        List<int> tilesInRange = new List<int>();

        BfsTiles(clickedTile);

        for (int i = 0; i < Constants.NumTiles; i++)
        {
            if (tiles[i].distance <= 2 && tiles[i].distance > 0)
            {
                tilesInRange.Add(i);
            }
        }

        int masLejana = tilesInRange[0];
        int distanciaMasLejana = 0;
        foreach (var tile in tilesInRange)
        {
            BfsTiles(tile);

            int distancia = 0;
            foreach (var policia in cops)
            {
                distancia += tiles[policia.GetComponent<CopMove>().currentTile].distance;
            }
            Debug.Log(distancia);

            if (distancia > distanciaMasLejana)
            {
                masLejana = tile;
                distanciaMasLejana = distancia;
                Debug.Log(masLejana + ", " + distanciaMasLejana);
            }
        }

        BfsTiles(clickedTile);

        robber.GetComponent<RobberMove>().currentTile = masLejana;

        robber.GetComponent<RobberMove>().MoveToTile(tiles[masLejana]);
    }

    public void EndGame(bool end)
    {
        if(end)
            finalMessage.text = "You Win!";
        else
            finalMessage.text = "You Lose!";
        playAgainButton.interactable = true;
        state = Constants.End;
    }

    public void PlayAgain()
    {
        cops[0].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop0]);
        cops[1].GetComponent<CopMove>().Restart(tiles[Constants.InitialCop1]);
        robber.GetComponent<RobberMove>().Restart(tiles[Constants.InitialRobber]);
                
        ResetTiles();

        playAgainButton.interactable = false;
        finalMessage.text = "";
        roundCount = 0;
        rounds.text = "Rounds: ";

        state = Constants.Restarting;
    }

    public void InitGame()
    {
        state = Constants.Init;
         
    }

    public void IncreaseRoundCount()
    {
        roundCount++;
        rounds.text = "Rounds: " + roundCount;
    }

    public void FindSelectableTiles(bool cop)
    {
                 
        int indexcurrentTile;        

        if (cop==true)
            indexcurrentTile = cops[clickedCop].GetComponent<CopMove>().currentTile;
        else
            indexcurrentTile = robber.GetComponent<RobberMove>().currentTile;

        //La ponemos rosa porque acabamos de hacer un reset
        tiles[indexcurrentTile].current = true;

        BfsTiles(indexcurrentTile);

        for (int i = 0; i < Constants.NumTiles; i++)
        {
            if(tiles[i].distance <= 2 && tiles[i].distance > 0)
            {
                bool ocupada = false;
                foreach (var policia in cops)
                {
                    if (policia.GetComponent<CopMove>().currentTile == tiles[i].numTile)
                    {
                        ocupada = true;
                    }
                }
                if (!ocupada)
                {
                    tiles[i].selectable = true;
                }
            }
        }
    }

    public void BfsTiles(int indexcurrentTile)
    {
        //Cola para el BFS
        Queue<Tile> nodes = new Queue<Tile>();


        foreach (var tile in tiles)
        {
            tile.distance = 999;
            tile.parent = null;
            tile.visited = false;
            tile.selectable = false;
        }
        tiles[indexcurrentTile].distance = 0;
        tiles[indexcurrentTile].parent = null;
        tiles[indexcurrentTile].visited = true;

        Queue<Tile> cola = new Queue<Tile>();
        cola.Enqueue(tiles[indexcurrentTile]);
        //DONE: Implementar BFS. Los nodos seleccionables los ponemos como selectable=true
        //Tendrás que cambiar este código por el BFS
        while (cola.Count > 0)
        {
            Tile tile = cola.Dequeue();

            foreach (var adyacente in tile.adjacency)
            {
                if (!tiles[adyacente].visited)
                {
                    bool ocupada = false;
                    foreach (var policia in cops)
                    {
                        if (policia.GetComponent<CopMove>().currentTile == adyacente)
                        {
                            ocupada = true;
                        }
                    }
                    if (ocupada)
                    {
                        tiles[adyacente].distance = tile.distance + 1;
                        tiles[adyacente].visited = true;
                        tiles[adyacente].parent = tile;
                    }
                    else
                    {
                        tiles[adyacente].distance = tile.distance + 1;
                        tiles[adyacente].visited = true;
                        tiles[adyacente].parent = tile;
                        cola.Enqueue(tiles[adyacente]);
                    }
                }
            }
        }
    }
}
