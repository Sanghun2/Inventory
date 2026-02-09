using System.Data;
using UnityEngine;

namespace BilliotGames
{
    public abstract class InventoryBase
    {
        public int Capacity => capacity;
        public string InventoryID => inventoryID;

        [SerializeField] protected int capacity;
        [SerializeField] protected string inventoryID;
        protected bool isInit;

        public InventoryBase(string id, int capacitiy) {
            inventoryID = id;
            capacity = capacitiy;
        }

        public abstract void InitInventory();
        public abstract void ClearInventory();

        public abstract bool TryPushItem(ItemStack inputStack, out ItemStack overflowedStack);
        public abstract bool TryRemoveItem(string itemID, int targetAmount);
        public abstract int GetItemCount(string itemID);
    }
}