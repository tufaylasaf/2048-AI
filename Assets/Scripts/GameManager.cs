using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int _width = 4;
    [SerializeField] private int _height = 4;
    [SerializeField] private TileHolder tileHolderPrefab;
    [SerializeField] private Tile tilePrefab;
    [SerializeField] private SpriteRenderer _boardPrefab;
    [SerializeField] private List<BlockType> types;
    [SerializeField] private float travelTime = 0.2f;
    [SerializeField] private int winCondition = 2048;
    [SerializeField] private GameState state;
    [SerializeField] private int Score;
    [SerializeField] private int Depth;

    [TextArea(4, 6)]
    [SerializeField] string boardString;

    Block[,] board = new Block[4, 4];

    private List<Node> nodes;
    private List<Block> blocks;
    [SerializeField] private List<Tile> tiles = new List<Tile>();

    [InspectorButton("AllPossibleMoves", ButtonWidth = 150)]
    [SerializeField] bool PossibleMoves;
    [InspectorButton("CheckGameOver", ButtonWidth = 150)]
    [SerializeField] bool GameOver;
    [InspectorButton("PauseAI", ButtonWidth = 150)]
    [SerializeField] bool stopAI;
    bool pause = false;

    [Header("UI")]
    [SerializeField] Text scoreTxt;
    [SerializeField] Text moveTxt;
    [SerializeField] Text timeTxt;
    int moves;
    [SerializeField] double time;
    private int _round;
    private int moveCount = 1;

    Vector2[] directions = new Vector2[] { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
    bool calculating = false;

    private BlockType GetBlockTypeByValue(int value) => types.First(t => t.Value == value);

    void Start()
    {
        ChangeState(GameState.GenerateLevel);
    }

    private void ChangeState(GameState newState)
    {
        state = newState;

        switch (newState)
        {
            case GameState.GenerateLevel:
                GenerateGrid();
                break;
            case GameState.SpawningBlocks:
                if (moveCount > 0) SpawnBlocks(_round++ == 0 ? 2 : 1);
                else ChangeState(GameState.WaitingInput);
                break;
            case GameState.WaitingInput:
                if (!calculating && !pause)
                {
                    calculating = true;
                    double x = Time.realtimeSinceStartupAsDouble;
                    Vector2 dir = GetBestMove(blocks, Depth);
                    time = SetSigFig(Time.realtimeSinceStartupAsDouble - x, 3);
                    // Debug.Log(dir);
                    if (dir == Vector2.zero)
                    {
                        ChangeState(CheckLoss(SetBoard(blocks)) ? GameState.Lose : GameState.WaitingInput);
                    }
                    else
                    {
                        Shift(dir, blocks);
                        moves++;
                    }
                }
                break;
            case GameState.Moving:
                break;
            case GameState.Win:
                break;
            case GameState.Lose:
                Debug.Log("Lost");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
        }
    }

    void Update()
    {
        UpdateUI();
        if (state != GameState.WaitingInput) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow)) { Shift(Vector2.left, blocks); }
        if (Input.GetKeyDown(KeyCode.RightArrow)) { Shift(Vector2.right, blocks); }
        if (Input.GetKeyDown(KeyCode.UpArrow)) { Shift(Vector2.up, blocks); }
        if (Input.GetKeyDown(KeyCode.DownArrow)) { Shift(Vector2.down, blocks); }
    }

    void UpdateUI()
    {
        scoreTxt.text = Score.ToString();
        timeTxt.text = time.ToString() + "s"; ;
        moveTxt.text = moves.ToString();
    }

    double SetSigFig(double d, int digits)
    {
        if (d == 0) return 0;
        decimal scale = (decimal)Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(d))) + 1);

        return (double)(scale * Math.Round((decimal)d / scale, digits));
    }

    void PauseAI()
    {
        if (pause)
        {
            pause = false;
            calculating = false;
            ChangeState(GameState.WaitingInput);
        }
        else
        {
            pause = true;
        }
    }

    void GenerateGrid()
    {
        _round = 0;
        nodes = new List<Node>();
        blocks = new List<Block>();


        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                var TileHolder = Instantiate(tileHolderPrefab, new Vector2(x, y), Quaternion.identity);
                TileHolder.node.Pos = TileHolder.transform.position;
                TileHolder.gameObject.name = x.ToString() + "|" + y.ToString();
                nodes.Add(TileHolder.node);
            }
        }

        var center = new Vector2((float)_width / 2 - 0.5f, (float)_height / 2 - 0.5f);

        var board = Instantiate(_boardPrefab, center, Quaternion.identity);
        board.size = new Vector2(_width, _height);

        Camera.main.transform.position = new Vector3(center.x, center.y, -10);

        ChangeState(GameState.SpawningBlocks);
    }

    void SpawnBlocks(int amount)
    {
        var freeNodes = nodes.Where(n => n.OccupiedBlock == null).OrderBy(b => Random.value).ToList();

        foreach (var node in freeNodes.Take(amount))
        {
            var value = Random.value >= 0.9f ? 4 : 2;
            SpawnBlock(node, value, blocks, false);
        }

        ChangeState(GameState.WaitingInput);
    }

    void SpawnBlock(Node node, int value, List<Block> b, bool Merge = false, bool sim = false)
    {
        var block = new Block();
        block.SetBlock(node, value);
        b.Add(block);
        if (sim) return;
        // PrintBoard(SetBoard(b));

        var tile = Instantiate(tilePrefab, block.Pos, Quaternion.identity);
        tile.GetComponent<Animator>().SetBool("Merge", Merge);
        tile.Init(GetBlockTypeByValue(value), block);
        tiles.Add(tile);
        block.tile = tile;
    }

    Block SpawnBlock(Node n, int value)
    {
        Block b = new Block();
        b.Pos = n.Pos;
        b.Node = n;
        b.Value = value;
        n.OccupiedBlock = b;

        return b;
    }

    void Shift(Vector2 dir, List<Block> b)
    {
        ChangeState(GameState.Moving);

        List<Block> orderedBlocks = b.OrderBy(b => b.Pos.y).ThenBy(b => b.Pos.x).ToList();
        if (dir == Vector2.right || dir == Vector2.up) orderedBlocks.Reverse();
        moveCount = 0;

        foreach (var block in orderedBlocks)
        {
            var next = block.Node;
            do
            {
                if (block.Node != next) moveCount++;
                block.SetBlock(next);

                var possibleNode = GetNodeAtPosition(next.Pos + dir, nodes);
                if (possibleNode != null)
                {
                    if (possibleNode.OccupiedBlock != null && possibleNode.OccupiedBlock.CanMerge(block.Value))
                    {
                        block.MergeBlock(possibleNode.OccupiedBlock);
                        moveCount++;
                    }
                    else if (possibleNode.OccupiedBlock == null) next = possibleNode;
                }
            } while (next != block.Node);
        }

        var sequence = DOTween.Sequence();

        foreach (var block in orderedBlocks)
        {
            block.Pos = block.MergingBlock != null ? block.MergingBlock.Node.Pos : block.Node.Pos;
            sequence.Insert(0, block.tile.transform.DOMove(block.Pos, travelTime).SetEase(Ease.InQuad));
        }

        sequence.OnComplete(() =>
        {
            var mergeBlocks = orderedBlocks.Where(b => b.MergingBlock != null).ToList();
            foreach (var block in mergeBlocks)
            {
                MergeBlocks(block.MergingBlock, block, b);
            }

            // PrintBoard(SetBoard(b));
            calculating = false;

            if (b.Count == 16) ChangeState(CheckLoss(SetBoard(b)) ? GameState.Lose : GameState.WaitingInput);
            else { ChangeState(GameState.SpawningBlocks); }
        });
    }

    (List<Block>, bool) SimulateMove(Vector2 dir, List<Block> bl)
    {
        (List<Block> nbl, List<Node> nnd) = CopyList(bl);
        int moveCounter = 0;

        List<Block> orderedBlocks = nbl.OrderBy(b => b.Pos.y).ThenBy(b => b.Pos.x).ToList();
        if (dir == Vector2.right || dir == Vector2.up) orderedBlocks.Reverse();

        foreach (var block in orderedBlocks)
        {
            var next = block.Node;
            do
            {
                if (block.Node != next) moveCounter++;
                block.SetBlock(next);

                var possibleNode = GetNodeAtPosition(next.Pos + dir, nnd);
                if (possibleNode != null)
                {
                    if (possibleNode.OccupiedBlock != null && possibleNode.OccupiedBlock.CanMerge(block.Value))
                    {
                        block.MergeBlock(possibleNode.OccupiedBlock);
                        moveCounter++;
                    }
                    else if (possibleNode.OccupiedBlock == null) next = possibleNode;
                }
            } while (next != block.Node);
        }

        foreach (var block in orderedBlocks)
        {
            Vector2 movePoint = block.MergingBlock != null ? block.MergingBlock.Node.Pos : block.Node.Pos;
            block.Pos = movePoint;
        }

        var mergeBlocks = orderedBlocks.Where(bl => bl.MergingBlock != null).ToList();
        foreach (var block in mergeBlocks)
        {
            MergeBlocks(block.MergingBlock, block, nbl, true);
        }

        bool x = moveCounter > 0 ? false : true;

        return (nbl, x);
    }

    void MergeBlocks(Block baseBlock, Block mergingBlock, List<Block> b, bool sim = false)
    {
        var newValue = baseBlock.Value * 2;

        SpawnBlock(baseBlock.Node, newValue, b, true, sim);

        RemoveBlock(baseBlock, b, sim);
        RemoveBlock(mergingBlock, b, sim);

        if (sim) return;

        Score += newValue;
    }

    void RemoveBlock(Block block, List<Block> b, bool sim = false)
    {
        b.Remove(block);
        if (sim) return;
        Tile tile = tiles.Where(t => t.block == block).First();
        tiles.Remove(tile);
        Destroy(tile.gameObject);
    }

    Node GetNodeAtPosition(Vector2 pos, List<Node> _nodes)
    {
        return _nodes.FirstOrDefault(n => n.Pos == pos);
    }

    bool CheckLoss(Block[,] blocks)
    {
        bool gameOver = true;
        Vector2[] directions = new Vector2[] { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                if (blocks[i, j] == null) break;
                foreach (var dir in directions)
                {
                    Vector2 checkPos = blocks[i, j].Pos + dir;

                    if (checkPos.x >= 0 && checkPos.x <= 3 && checkPos.y >= 0 && checkPos.y <= 3)
                    {
                        Block b = blocks[((int)checkPos.x), ((int)checkPos.y)];
                        if (b == null) break;
                        if (blocks[i, j].Value == b.Value) gameOver = false;
                    }
                }
            }
        }

        return gameOver;
    }

    void CheckGameOver()
    {
        Debug.Log(CheckLoss(SetBoard(blocks)));
    }

    // float[,] weights = new float[4, 4]{
    //     {0, 1, 2, 3},
    //     { 7, 6, 5, 4},
    //     { 8, 9, 10, 11},
    //     { 15, 14, 13, 12}
    // };

    // float[,] weights = new float[4, 4]{
    //     {4, 3, 2, 1},
    //     { 5, 4, 3, 2},
    //     { 6, 5, 4, 3},
    //     { 7, 6, 5, 4}
    // };




    // float[,] weights = new float[4, 4]{
    //     {1, 0, -1, -2},
    //     {4, 1, 0, -1},
    //     {5, 4, 1, 0},
    //     {6, 5, 4, 1}
    // };

    // {6, 5, 4, 1},
    //     { 5, 4, 1, 0},
    //     { 4, 1, 0, -1},
    //     { 1, 0, -1, -2}

    //      {1, 0, -1, -2},
    //     {4, 1, 0, -1},
    //     {5, 4, 1, 0},
    //     {6, 5, 4, 1}

    // float[,] weights = new float[4, 4]{
    //     {0.0125498f, 0.00992495f, 0.00575871f, 0.00335193f},
    //     {0.06054f, 0.0562579f, 0.037116f, 0.0161889f},
    //     {0.0997992f, 0.0888405f, 0.07611f, 0.0724143f},
    //     {0.135759f, 0.121925f, 0.102812f, 0.099937f}
    // };

    // {0.135759f, 0.121925f, 0.102812f, 0.099937f},
    //     {0.0997992f, 0.0888405f, 0.07611f, 0.0724143f},
    //     {0.06054f, 0.0562579f, 0.037116f, 0.0161889f},
    //     {0.0125498f, 0.00992495f, 0.00575871f, 0.00335193f}

    float Evaluate(List<Block> bl)
    {
        float total = 0;
        float penalty = 0;

        (List<Block> nbl, List<Node> nnd) = CopyList(bl);

        foreach (var block in nbl)
        {
            total += block.Value * block.Value * block.Value * block.Value;
            // total += Mathf.Pow(4, weights[((int)block.Pos.y), ((int)block.Pos.x)]) * block.Value;
            // total += weights[((int)block.Pos.y), ((int)block.Pos.x)] * block.Value;

            // foreach (var dir in directions)
            // {
            //     Vector2 pos = block.Pos + dir;
            //     if (pos.x >= 0 && pos.x <= 3 && pos.y >= 0 && pos.y <= 3)
            //     {
            //         Block b = nnd.Where(n => n.Pos == block.Pos + dir).FirstOrDefault().OccupiedBlock;
            //         if (b != null)
            //         {
            //             float x = Mathf.Abs(b.Value - block.Value);
            //             penalty += Mathf.Pow(x, 1);
            //         }
            //     }
            // }
        }

        return (total - penalty) * Score;
    }

    // total += Mathf.Pow(4, weights[((int)block.Pos.y), ((int)block.Pos.x)]) * block.Value * block.Value;
    // total += weights[((int)block.Pos.y), ((int)block.Pos.x)] * block.Value;

    List<(List<Block>, Vector2 dir)> GetMoves(List<Block> bl, bool print = false)
    {
        List<(List<Block>, Vector2 dir)> moves = new List<(List<Block>, Vector2 dir)>();

        foreach (var dir in directions)
        {
            (List<Block> nbl, bool isEqual) = SimulateMove(dir, bl);
            if (isEqual) continue;

            moves.Add((nbl, dir));
            if (print) PrintBoard(SetBoard(nbl), dir);
        }

        return (moves);
    }

    Vector2 GetBestMove(List<Block> bl, int depth)
    {
        float bestScore = -Mathf.Infinity;
        Vector2 bestMove = Vector2.zero;

        foreach (var dir in directions)
        {
            (List<Block> nbl, bool isEqual) = SimulateMove(dir, bl);

            if (isEqual) continue;

            float newScore = ExpectiMax(nbl, depth - 1, false);

            if (newScore > bestScore)
            {
                bestMove = dir;
                bestScore = newScore;
            }
        }
        return bestMove;
    }

    float ExpectiMax(List<Block> bl, int depth, bool isMax)
    {
        if (depth == 0 || CheckLoss(SetBoard(bl)) == true)
        {
            return Evaluate(bl);
        }

        if (isMax)
        {
            float score = -Mathf.Infinity;

            foreach (var dir in directions)
            {
                List<Block> nbl = SimulateMove(dir, bl).Item1;

                float newScore = ExpectiMax(nbl, depth - 1, false);

                score = Mathf.Max(score, newScore);
            }

            return score;
        }
        else
        {
            float score = 0;
            (List<Block> nbl, List<Node> nnd) = CopyList(bl);

            List<Node> freeNodes = nnd.Where(n => n.OccupiedBlock == null).OrderBy(n => n.Pos.y).ThenBy(n => n.Pos.x).ToList();
            int total = (freeNodes.Count < depth + 1) ? freeNodes.Count : depth;
            // int total = freeNodes.Count;

            for (int i = 0; i < total; i++)
            {
                List<Block> bl2 = CopyBlock(nbl);
                List<Block> bl4 = CopyBlock(nbl);

                Block b2 = SpawnBlock(freeNodes[i], 4);
                Block b4 = SpawnBlock(freeNodes[i], 2);
                bl2.Add(b2);
                bl4.Add(b4);

                score += (9 * ExpectiMax(bl2, depth - 1, true) / total) + (1 * ExpectiMax(bl4, depth - 1, true) / total);
            }

            return score;
        }

    }

    Block[,] SetBoard(List<Block> blocks)
    {
        Block[,] tempBoard = new Block[4, 4];

        foreach (var block in blocks)
        {
            tempBoard[(int)block.Pos.x, (int)block.Pos.y] = block;
        }

        return tempBoard;
    }

    void PrintBoard(Block[,] _board)
    {
        string col = "";
        for (int i = 3; i >= 0; i--)
        {
            string row = "";
            for (int j = 0; j <= 3; j++)
            {
                if (_board[j, i] != null)
                {
                    row += " " + _board[j, i].Value + " ";
                }
                else
                {
                    row += " X ";
                }
            }
            col += row + "\n";
        }
        boardString = col;
    }
    void AllPossibleMoves()
    {
        List<(List<Block>, Vector2 dir)> moves = GetMoves(blocks, true);
    }
    void PrintBoard(Block[,] _board, Vector2 dir)
    {
        string col = "";
        for (int i = 3; i >= 0; i--)
        {
            string row = "";
            for (int j = 0; j <= 3; j++)
            {
                if (_board[j, i] != null)
                {
                    row += " " + _board[j, i].Value + " ";
                }
                else
                {
                    row += " X ";
                }
            }
            col += row + "\n";
        }
        Debug.Log(dir + "\n" + col);
    }

    (List<Block>, List<Node>) CopyList(List<Block> og)
    {
        List<Node> newNodes = new List<Node>();

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
            {
                Node n = new Node();
                n.Pos = new Vector2(i, j);
                newNodes.Add(n);
            }
        }

        List<Block> newBlocks = new List<Block>();

        foreach (var block in og)
        {
            Block b = new Block();
            Node n = newNodes.Where(node => node.Pos == block.Pos).FirstOrDefault();
            b.Node = n;
            b.Pos = n.Pos;
            b.Value = block.Value;
            n.OccupiedBlock = b;

            newBlocks.Add(b);
        }

        return (newBlocks, newNodes);
    }

    List<Block> CopyBlock(List<Block> og)
    {

        List<Block> newBlocks = new List<Block>();

        foreach (var block in og)
        {
            Block b = new Block();
            b.Node = block.Node;
            b.Pos = block.Pos;
            b.Value = block.Value;

            newBlocks.Add(b);
        }

        return newBlocks;
    }
}
[Serializable]
public struct BlockType
{
    public int Value;
    public Color Color;
    public int FontSize;
    public Color FontColor;
}

public enum GameState
{
    GenerateLevel,
    SpawningBlocks,
    WaitingInput,
    Moving,
    Win,
    Lose
}