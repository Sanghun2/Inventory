using UnityEngine;

public readonly struct ItemEventArgs
{
    public readonly string itemID;
    public readonly int delta;
    public readonly int index;

    public ItemEventArgs(string itemID, int delta, int index=-1) {
        this.itemID = itemID; 
        this.delta = delta; 
        this.index = index;
    }
}
