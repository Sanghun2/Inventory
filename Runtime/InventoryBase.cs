using System;
using System.Collections.Generic;
using UnityEngine;

namespace BilliotGames
{
    public abstract class InventoryBase
    {
        public int Capacity => capacity;
        public string InventoryID => inventoryID;
        public int SearchPriority => searchPriority;
        public string Tag => tag;
        public string DisplayName => displayName;

        [SerializeField] protected int capacity;
        [SerializeField] protected string inventoryID;
        [SerializeField] protected string displayName;
        [SerializeField] protected int searchPriority;
        [SerializeField] protected string tag;
        protected List<IPushCondition> pushConditions = new List<IPushCondition>();
        protected bool isInit;

        public virtual event Action<ItemEventArgs> OnItemAdded;
        public virtual event Action<ItemEventArgs> OnItemMerged;
        public virtual event Action<ItemEventArgs> OnItemRemoved;
        public virtual event Action<ItemEventArgs> OnItemChanged;

        public InventoryBase(string id, int capacitiy, string tag = "none", int searchPriority = 5, string displayName="") {
            this.inventoryID = id;
            this.capacity = capacitiy;
            this.searchPriority = searchPriority;
            this.tag = tag;
            this.displayName = displayName;
        }


        public InventoryBase SetSearchPriority(int searchPriority) {
            this.searchPriority = searchPriority;
            return this;
        }
        public InventoryBase SetTag(string tag) {
            this.tag = tag;
            return this;
        }

        #region Condition

        public InventoryBase AddCondition(IPushCondition condition) {
            pushConditions.Add(condition);
            return this;
        }
        public bool CheckConditions(ItemStack item) {
            foreach (var condition in pushConditions) {
                if (!condition.CanPush(item)) return false;
            }
            return true;
        }

        #endregion


        #region Content

        public abstract void InitInventory();
        public abstract void ClearInventory();

        public abstract bool TryPushItem(ItemStack inputStack, out ItemStack overflowedStack, bool ignoreConditions = false);
        public abstract bool TryRemoveItem(string itemID, int targetAmount);
        public abstract int RemoveItemPartial(string itemID, int requestAmount);
        public abstract int GetItemCount(string itemID);

        #endregion
    }
}