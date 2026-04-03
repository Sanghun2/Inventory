using UnityEngine;

public readonly struct ItemEventArgs
{
    public readonly string itemID;
    public readonly int delta;
    public readonly int index;
    public readonly int weight;

    public ItemEventArgs(string itemID, int delta) {
        this.itemID = itemID; 
        this.delta = delta; 
        this.index = -1;
        this.weight = 0;
    }

    public ItemEventArgs(string itemID, int delta, int index) : this(itemID, delta) {
        this.index = index;
    }

    public ItemEventArgs(string itemID, int delta, int index, int weight) : this (itemID, delta, index) {
        this.weight = weight;
    }
}
