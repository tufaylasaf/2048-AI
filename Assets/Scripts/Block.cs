using UnityEngine;

public class Block
{
    public int Value;
    public Node Node;
    public Block MergingBlock;
    public bool Merging;
    public Vector2 Pos;
    public Tile tile;

    public void SetBlock(Node node)
    {
        if (Node != null) Node.OccupiedBlock = null;
        Node = node;
        Node.OccupiedBlock = this;
        Pos = node.Pos;
    }

    public void MergeBlock(Block blockToMergeWith)
    {
        MergingBlock = blockToMergeWith;

        Node.OccupiedBlock = null;

        blockToMergeWith.Merging = true;
    }

    public void SetBlock(Node node, int value)
    {
        if (Node != null) Node.OccupiedBlock = null;
        Node = node;
        Node.OccupiedBlock = this;
        Pos = node.Pos;
        Value = value;
        MergingBlock = null;
        Merging = false;
    }

    public bool CanMerge(int value) => value == Value && !Merging && MergingBlock == null;
}
