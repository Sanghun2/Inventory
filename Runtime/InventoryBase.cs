using System;
using UnityEngine;

namespace BilliotGames
{
    public abstract class InventoryBase
    {
        public int Capacity => capacity;
        public string InventoryID => inventoryID;
        public int SearchPriority => searchPriority;

        [SerializeField] protected int capacity;
        [SerializeField] protected string inventoryID;
        [SerializeField] protected int searchPriority;
        protected bool isInit;

        public virtual event Action<ItemStack, int> OnItemAdded;
        public virtual event Action<ItemStack, int> OnItemMerged;
        public virtual event Action<ItemStack, int> OnItemRemoved;

        public InventoryBase(string id, int capacitiy, int searchPriority=5) {
            this.inventoryID = id;
            this.capacity = capacitiy;
            this.searchPriority = searchPriority;
        }

        public void SetSearchPriority(int searchPriority) {
            this.searchPriority = searchPriority;
        }

        public abstract void InitInventory();
        public abstract void ClearInventory();

        public abstract bool TryPushItem(ItemStack inputStack, out ItemStack overflowedStack);
        public abstract bool TryRemoveItem(string itemID, int targetAmount);
        public abstract int GetItemCount(string itemID);
    }
}