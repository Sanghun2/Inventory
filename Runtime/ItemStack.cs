using System;
using System.Collections.Generic;
using UnityEngine;


namespace BilliotGames
{
    [Serializable]
    public partial class ItemData : IEquatable<ItemData>
    {
        public string ItemID => itemID;
        public int MaxStackAmount => maxStackAmount;

        [SerializeField] string itemID;
        [SerializeField] int maxStackAmount;

        public ItemData(string itemID, int maxStackAmount) {
            this.itemID = itemID;
            this.maxStackAmount = maxStackAmount;
        }

        public bool Equals(ItemData other) {
            if (other == null) return false;
            return ItemID.Equals(other.ItemID);
        }
    }

    [Serializable]
    public class ItemStack : IEquatable<ItemStack>
    {
        public enum MergeResult {
            Success,
            Success_Overflowed,
            Failed_DifferentItemType,
            Failed_InvalidIStack,
        }


        public bool IsNull => _itemData == null;
        public ItemData ItemData => _itemData;
        public int Amount
        {
            get
            {
                return _amount;
            }
            set
            {
                int prevAmount = _amount;

                _amount = value < 0 ? 0 : value;
                OnAmountChanged?.Invoke(this, _amount-prevAmount);
                if (_amount <= 0) {
                    OnItemRemoved?.Invoke();
                    ReleaseItem(); 
                }
            }
        }


        private ItemData _itemData;
        private int _amount;

        public delegate void ItemHandler(ItemStack itemStack, int deltaAmount);
        public event ItemHandler OnAmountChanged;
        public virtual event Action OnItemRemoved;

        public ItemStack(ItemData itemData, int amount) {
            this._itemData = itemData;
            this._amount = amount;
            OnAmountChanged = null;
        }

        public MergeResult MergeStack(ItemStack inputStack) {
            if (inputStack == null || inputStack.IsNull) return MergeResult.Failed_InvalidIStack;

            if (IsNull) {
                _itemData = inputStack.ItemData;
                Amount = inputStack.Amount; 
                inputStack.Amount = 0;
                return MergeResult.Success;
            }

            if (!ItemData.Equals(inputStack.ItemData)) {
                return MergeResult.Failed_DifferentItemType;
            }

            int mergeAmount = inputStack.Amount;
            var totalAmount = Amount + mergeAmount;
            if (totalAmount <= ItemData.MaxStackAmount) {
                Amount = totalAmount;
                inputStack.Amount = 0;
                return MergeResult.Success;
            }
            else {
                int leftAmount = totalAmount - ItemData.MaxStackAmount;
                Amount = ItemData.MaxStackAmount;
                inputStack.Amount = leftAmount;
                return MergeResult.Success_Overflowed;
            }
        }

        public bool SplitStack(int splitAmount, out ItemStack splitStack) {
            splitStack = null;

            if (IsNull || splitAmount <= 0) {
                return false;
            }

            var itemData = _itemData;
            if (TryRemoveStack(splitAmount)) {
                splitStack = new ItemStack(itemData, splitAmount);
                return true;
            }
            return false;
        }

        public bool TryRemoveStack(int removeAmount, bool allowOverflow=false) {
            if (removeAmount < 0) return false;

            if (!allowOverflow) {
                if (removeAmount > Amount) {
                    Debug.LogAssertion($"remove amount가 현재 amount 보다 많음");
                    return false;
                }
            }

            Amount -= removeAmount;
            return true;
        }

        public bool Equals(ItemStack other) {
            if (other == null) return false;
            if (IsNull || other.IsNull) return false;

            return ReferenceEquals(this, other);
        }

        public ItemStack Clone() {
            return new ItemStack(_itemData, _amount);
        }

        public void InvokeAmountEvent() {
            OnAmountChanged?.Invoke(this, 0);
        }

        private void ReleaseItem() {
            _itemData = null;
            _amount = 0;
            OnAmountChanged = null;
        }
    }
}