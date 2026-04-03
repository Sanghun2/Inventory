using System;
using UnityEngine;

namespace BilliotGames
{
    public abstract class InventoryBase
    {
        public int Capacity => capacity;
        public string InventoryID => inventoryID;
        public int SearchPriority => searchPriority;
        public string Tag => tag;

        [SerializeField] protected int capacity;
        [SerializeField] protected string inventoryID;
        [SerializeField] protected int searchPriority;
        [SerializeField] protected string tag;
        protected bool isInit;

        public virtual event Action<ItemEventArgs> OnItemAdded;
        public virtual event Action<ItemEventArgs> OnItemMerged;
        public virtual event Action<ItemEventArgs> OnItemRemoved;
        public virtual event Action<ItemEventArgs> OnItemChanged;

        public InventoryBase(string id, int capacitiy, string tag="none", int searchPriority=5) {
            this.inventoryID = id;
            this.capacity = capacitiy;
            this.searchPriority = searchPriority;
            this.tag = tag;
        }

        public InventoryBase SetSearchPriority(int searchPriority) {
            this.searchPriority = searchPriority;
            return this;
        }
        public InventoryBase SetTag(string tag) {
            this.tag = tag;
            return this;
        }

        public abstract void InitInventory();
        public abstract void ClearInventory();

        public abstract bool TryPushItem(ItemStack inputStack, out ItemStack overflowedStack);
        public abstract bool TryRemoveItem(string itemID, int targetAmount);
        public abstract int GetItemCount(string itemID);
        public abstract int RemoveItemPartial(string itemID, int requestAmount);
    }
}