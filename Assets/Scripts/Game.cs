using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Game : MonoBehaviour
{
    [SerializeField] int WIDTH = 4;
    [SerializeField] int HEIGHT = 4;
    [SerializeField] GameObject tileHolderPrefab;
    [SerializeField] SpriteRenderer boardPrefab;
    [SerializeField] GameObject tilePrefab;


    [InspectorButton("PrintBoardIns", ButtonWidth = 150)]
    [SerializeField] bool ShowBoard;
    [InspectorButton("Shift", ButtonWidth = 150)]
    [SerializeField] bool ShiftBoard;
    [TextArea(4, 6)]
    [SerializeField] string boardString;


    int[,] Board = new int[4, 4];

    // Start is called before the first frame update
    void Start()
    {
        GenerateGrid();
        Board[0, 2] = 2;
        Board[1, 2] = 2;
        PrintBoard(Board);
        Board = Up(Board);
        PrintBoard(Board);
    }

    // Update is called once per frame
    void Update()
    {

    }

    void GenerateGrid()
    {
        for (int x = 0; x < WIDTH; x++)
        {
            for (int y = 0; y < HEIGHT; y++)
            {
                GameObject tileHolder = Instantiate(tileHolderPrefab, new Vector2(x, y), Quaternion.identity);
                tileHolder.gameObject.name = x.ToString() + "|" + y.ToString();
            }
        }

        var center = new Vector2((float)WIDTH / 2 - 0.5f, (float)HEIGHT / 2 - 0.5f);

        var board = Instantiate(boardPrefab, center, Quaternion.identity);
        board.size = new Vector2(WIDTH, HEIGHT);

        Camera.main.transform.position = new Vector3(center.x, center.y, -10);

    }

    int[,] MergeOneRowL(int[,] b, int row)
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 3; j > 0; j--)
            {
                if (b[row, j - 1] == 0)
                {
                    b[row, j - 1] = b[row, j];
                    b[row, j] = 0;
                }
            }
        }

        for (int j = 0; j < 3; j++)
        {
            if (b[row, j] == b[row, j + 1])
            {
                b[row, j] *= 2;
                b[row, j + 1] = 0;
            }
        }

        for (int j = 3; j > 0; j--)
        {
            if (b[row, j - 1] == 0)
            {
                b[row, j - 1] = b[row, j];
                b[row, j] = 0;
            }
        }

        return b;
    }

    int[,] Reverse(int[,] b)
    {
        for (int i = 0; i < WIDTH; i++)
        {
            int start = 0;
            int end = WIDTH - 1;

            while (start < end)
            {
                int temp = b[i, start];
                b[i, start] = b[i, end];
                b[i, end] = temp;

                start++;
                end--;
            }
        }

        return b;
    }

    int[,] Transpose(int[,] b)
    {
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (i != j)
                {
                    int temp = b[j, i];
                    b[j, i] = b[i, j];
                    b[i, j] = temp;
                }
            }
        }

        return b;
    }

    int[,] Left(int[,] b)
    {
        for (int i = 0; i < 4; i++)
        {
            b = MergeOneRowL(b, i);
        }

        return b;
    }

    int[,] Right(int[,] b)
    {
        b = Reverse(b);
        b = Left(b);
        b = Reverse(b);

        return b;
    }

    int[,] Up(int[,] b)
    {
        b = Transpose(b);
        //b = Left(b);
        //b = Transpose(b);

        return b;
    }

    void Shift()
    {
        Left(Board);
    }

    void PrintBoardIns()
    {
        PrintBoard(Board);
    }

    void PrintBoard(int[,] b)
    {
        string col = "";
        for (int i = 0; i <= 3; i++)
        {
            string row = "";
            for (int j = 0; j <= 3; j++)
            {
                row += " " + b[i, j] + " ";
            }
            col += row + "\n";
        }
        boardString = col;
        Debug.Log(boardString);
    }
}
