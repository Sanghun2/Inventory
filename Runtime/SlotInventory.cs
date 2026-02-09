using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace BilliotGames
{
    [Serializable]
    public class SlotInventory : InventoryBase
    {
        public enum SlotState
        {
            Empty,
            Occupied,
        }
        public bool IsEmptySlotExist => emptyItemSlots.Count > 0;

        /// <summary>
        /// int => slot index
        /// </summary>
        public IReadOnlyDictionary<int, ItemStack> UsingSlotDict => usingItemSlotDict;

        private SortedSet<int> emptyItemSlots = new(); // 사용중이지 않은 목록
        private SortedDictionary<int, ItemStack> usingItemSlotDict = new(); // 사용중인 목록
        private Dictionary<string, int> amountDict = new();

        public SlotInventory(string inventoryID, int capacity) : base(inventoryID, capacity) {
            this.inventoryID = inventoryID;
            this.capacity = capacity;
            InitInventory();
        }

        public override void InitInventory() {
            if (isInit) return;

            isInit = true;
            ClearInventory(capacity);
        }
        public override void ClearInventory() {
            ClearInventory(capacity);
        }

        #region Item Control

        /// <summary>
        /// 인벤토리에 아이템 push하는 기능
        /// </summary>
        /// <param name="newItemStack">push할 item stack</param>
        /// <param name="overflowedStack">push하고 남은 item stack</param>
        /// <returns>true이면 itemstack이 모두 저장된 것이고 false인 경우 남은 slot이 없어 overflow된 것</returns>
        public override bool TryPushItem(ItemStack newItemStack, out ItemStack overflowedStack) {
            overflowedStack = null;
            if (TryGetAvailableStack(newItemStack.ItemData.ItemID, out ItemStack availableStack)) {
                switch (availableStack.MergeStack(newItemStack)) {
                    case ItemStack.MergeResult.Success:

                        return true;
                    case ItemStack.MergeResult.Failed_DifferentItemType:
                        Debug.LogError($"<color=red>item push에서는 나오면 안되는 로직 흐름</color>");
                        return false;
                    case ItemStack.MergeResult.Success_Overflowed:
                        return TryPushItem(newItemStack, out overflowedStack);
                    default:
                        break;
                }
            }
            else {
                if (TryGetEmptySlotIndex(out int? targetIndex)) {
                    int amount = newItemStack.Amount;

                    // 모든 아이템을 저장할 수 있는 경우
                    if (amount <= newItemStack.ItemData.MaxStackAmount) {
                        if (TryAllocateSlot(targetIndex ?? -1, newItemStack)) {

                        }
                        return true;
                    }
                    // max를 넘어가는 경우
                    else {
                        int overAmount = newItemStack.Amount - newItemStack.ItemData.MaxStackAmount;
                        newItemStack.SplitStack(overAmount, out ItemStack splitStack);
                        if (TryAllocateSlot(targetIndex ?? -1, newItemStack)) {
                            return TryPushItem(splitStack, out overflowedStack);
                        }
                    }
                }
            }


            Debug.LogAssertion($"no empty slot remain");
            overflowedStack = newItemStack;
            return false;
        }

        /// <summary>
        /// 인벤토리에서 아이템 remove하는 기능. targetamount이상 존재해야 삭제
        /// </summary>
        /// <param name="itemID">pop할 아이템 id</param>
        /// <param name="targetAmount">pop할 양</param>
        /// <returns></returns>
        public override bool TryRemoveItem(string itemID, int targetAmount) {
            if (GetItemCount(itemID) >= targetAmount) {
                List<int> deleteSlotList = new List<int>();
                int currentAmount = 0;
                foreach (var item in usingItemSlotDict) {
                    int slotIndex = item.Key;
                    ItemStack itemStack = item.Value;

                    if (itemID.Equals(itemStack.ItemData.ItemID)) {
                        int needAmount = targetAmount - currentAmount;
                        int getAmount = Mathf.Min(needAmount, itemStack.Amount);
                        currentAmount += getAmount;
                        itemStack.TryRemoveStack(getAmount);
                        if (itemStack.Amount == 0) { deleteSlotList.Add(slotIndex); }

                        if (currentAmount == targetAmount) break;
                    }
                }

                for (int i = 0; i < deleteSlotList.Count; i++) {
                    int deleteSlotIndex = deleteSlotList[i];
                    ReleaseSlot(deleteSlotIndex);
                }
                return true;
            }

            Debug.LogAssertion($"<color=orange>개수가 {targetAmount}만큼 존재하지 않음</color>");
            return false;
        }
        public bool TryRemoveItemAsPossible(string itemID, int removeAmount, out int remainAmount) {
            remainAmount = 0;
            List<int> releaseTargetSlotIndexList = new List<int>(capacity);
            foreach (var itemPair in usingItemSlotDict) {
                int slotIndex = itemPair.Key;
                ItemStack itemStack = itemPair.Value;

                if (itemStack.ItemData == null) continue;

                string targetID = itemStack.ItemData.ItemID;
                if (targetID.Equals(itemID)) {
                    int finalAmount = Mathf.Min(removeAmount, itemStack.Amount);
                    itemStack.TryRemoveStack(finalAmount);
                    removeAmount -= finalAmount;
                    if (itemStack.Amount == 0) {
                        releaseTargetSlotIndexList.Add(slotIndex);
                    }

                    if (removeAmount == 0) {
                        for (int i = 0; i < releaseTargetSlotIndexList.Count; i++) {
                            var targetIndex = releaseTargetSlotIndexList[i];
                            ReleaseSlot(targetIndex);
                        }

                        //OnItemChanged?.Invoke(ItemList);
                        return true;
                    }
                }
            }

            for (int i = 0; i < releaseTargetSlotIndexList.Count; i++) {
                ReleaseSlot(releaseTargetSlotIndexList[i]);
            }

            remainAmount = removeAmount;
            //OnItemChanged?.Invoke(ItemList);
            return remainAmount == 0;
        }

        /// <summary>
        /// 인벤토리 내의 remove target에 해당하는 item을 찾아 remove amount만큼 제거
        /// </summary>
        /// <param name="removeTarget"></param>
        /// <param name="removeAmount"></param>
        /// <returns></returns>
        public bool TryRemoveItem(ItemStack removeTarget, int removeAmount) {
            foreach (var itemPair in usingItemSlotDict) {
                if (removeTarget.Equals(itemPair.Value)) {
                    //Debug.LogAssertion($"일치하는 stack = {itemPair.Value.GetHashCode()}");
                    if (removeAmount <= removeTarget.Amount) {
                        removeTarget.TryRemoveStack(removeAmount);
                        if (removeTarget.Amount == 0) { ReleaseSlot(itemPair.Key); }
                        return true;
                    }

                    Debug.LogAssertion($"<color=orange>일치하는 아이템은 찾았으나 남은 amount가 부족</color>");
                    return false;
                }
            }

            Debug.LogAssertion($"<color=orange>일치하는 아이템이 없음</color>");
            return false;
        }


        public void DeleteItem(ItemStack targetItemStack) {
            int? targetIndex = null;
            foreach (var itemPair in usingItemSlotDict) {
                if (itemPair.Value.Equals(targetItemStack)) {
                    targetIndex = itemPair.Key;
                    break;
                }
            }

            ReleaseSlot(targetIndex);
        }

        private void ReleaseSlot(int? targetIndex) {
            if (targetIndex == null) return;
            int index = (int)targetIndex;
            if (usingItemSlotDict.TryGetValue(index, out ItemStack targetStack)) {
                targetStack.OnAmountChanged -= OnItemAmountChanged;
            }
            usingItemSlotDict.Remove(index);
            emptyItemSlots.Add(index);
        }

        private bool TryAllocateSlot(int slotIndex, ItemStack newItemStack) {
            if (!emptyItemSlots.Contains(slotIndex)) return false;

            usingItemSlotDict.Add(slotIndex, newItemStack);
            emptyItemSlots.Remove(slotIndex);
            newItemStack.OnAmountChanged += OnItemAmountChanged;
            UpdateAmountDictAdd(this, newItemStack);
            return true;
        }


        #endregion

        #region Item Check

        /// <summary>
        /// 현재 인벤토리에서 가장 처음 발견되는 item stack을 반환
        /// </summary>
        /// <param name="targetItemData"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public bool TryFindFirstItem(ItemData targetItemData, out ItemStack foundStack) {
            foreach (var itemPair in usingItemSlotDict) {
                ItemStack currentStack = itemPair.Value;
                if (currentStack.ItemData.Equals(targetItemData)) {
                    foundStack = currentStack;
                    return true;
                }
            }

            foundStack = null;
            return false;
        }

        /// <summary>
        /// 실제로 아이템을 pop하지 않고 전체 개수만 보여주는 함수
        /// </summary>
        /// <param name="itemID"></param>
        /// <param name="totalAmount"></param>
        /// <returns></returns>
        public override int GetItemCount(string itemID) {
            int totalAmount = 0;

            foreach (var item in usingItemSlotDict) {
                int slotIndex = item.Key;
                ItemStack itemStack = item.Value;

                if (itemID.Equals(itemStack.ItemData.ItemID)) {
                    totalAmount += itemStack.Amount;
                }
            }

            //Debug.LogAssertion($"{targetID} 전체 수: {totalAmount}");
            return totalAmount;
        }
        public void SwitchItemPosition(SlotInventory inventory1, int slotIndex1, int slotIndex2) {
            SlotInventory inventory2 = this;

            (SlotState slotState, ItemStack itemStack) itemInfo1 = inventory1.GetSlotState(slotIndex1);
            (SlotState slotState, ItemStack itemStack) itemInfo2 = inventory2.GetSlotState(slotIndex2);

            // 기존 사용 여부 초기화
            inventory1.ClearStateInfo(slotIndex1);
            inventory2.ClearStateInfo(slotIndex2);

            var itemStack1 = itemInfo1.itemStack;
            var itemStack2 = itemInfo2.itemStack;
            itemStack1.OnAmountChanged -= inventory1.OnItemAmountChanged;
            itemStack2.OnAmountChanged -= inventory2.OnItemAmountChanged;
            UpdateAmountDictRemove(inventory1, itemStack1);
            UpdateAmountDictRemove(inventory2, itemStack2);

            // 기존 사용 여부 업데이트
            inventory1.UpdateItemState(slotIndex1, itemStack2, itemInfo2.slotState); // inventory1의 slot1에 item2를 넣고 item state2로 slot1의 state를 동기화
            inventory2.UpdateItemState(slotIndex2, itemStack1, itemInfo1.slotState);

            itemStack2.OnAmountChanged += inventory1.OnItemAmountChanged;
            itemStack1.OnAmountChanged += inventory2.OnItemAmountChanged;
            UpdateAmountDictAdd(inventory1, itemStack2);
            UpdateAmountDictAdd(inventory2, itemStack1);

            itemStack1.InvokeAmountEvent();
            itemStack2.InvokeAmountEvent();
        }

        /// <summary>
        /// amount가 max가 아닌 itemstack을 찾아서 return 하는 기능
        /// </summary>
        /// <param name="newItemStack"></param>
        /// <param name="availableStack"></param>
        /// <returns></returns>
        public bool TryGetAvailableStack(string targetID, out ItemStack availableStack) {
            availableStack = null;
            if (usingItemSlotDict.Count == 0) return false;

            //usingItemSlotDict.PrintDict($"<color=yellow>using item dict</color>");
            foreach (var item in usingItemSlotDict) {
                int slotIndex = item.Key;
                ItemStack itemStack = item.Value;
                if (targetID.Equals(itemStack.ItemData.ItemID)) {
                    if (itemStack.Amount < itemStack.ItemData.MaxStackAmount) {
                        availableStack = itemStack;
                        //Debug.LogAssertion($"같은 아이템이 있는 slot 발견) invenID:{inventoryID}, idx:{slotIndex}, item:{removeTarget.ItemData.DisplayName}");
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion


        /// <summary>
        /// 남은 빈 slot이 있는지 체크
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private bool TryGetEmptySlotIndex(out int? index) {
            if (!IsEmptySlotExist) { index = null; return false; }

            index = emptyItemSlots.First();
            return true;
        }

        private void ClearInventory(int capacity) {
            emptyItemSlots = new SortedSet<int>(Enumerable.Range(0, capacity));
            usingItemSlotDict?.Clear();
            amountDict?.Clear();
        }

        private (SlotState, ItemStack) GetSlotState(int targetIndex) {
            if (emptyItemSlots.Contains(targetIndex)) return (SlotState.Empty, null);
            else return (SlotState.Occupied, usingItemSlotDict[targetIndex]);
        }

        /// <summary>
        /// switching 하기 전 add error 방지 reset처리용 기능
        /// </summary>
        /// <param name="targetIndex">reset할 index</param>
        private void ClearStateInfo(int targetIndex) {
            emptyItemSlots.Remove(targetIndex);
            usingItemSlotDict.Remove(targetIndex);
        }

        /// <summary>
        /// set state로 targetindex에 해당하는 item의 stateTo update
        /// </summary>
        /// <param name="slotIndex"></param>
        /// <param name="stateTo"></param>
        /// <param name="stackToChangeState"></param>
        /// <exception cref="Exception"></exception>
        private void UpdateItemState(int slotIndex, ItemStack stackToChangeState, SlotState stateTo) {
            switch (stateTo) {
                case SlotState.Empty:
                    usingItemSlotDict.Remove(slotIndex);
                    emptyItemSlots.Add(slotIndex);
                    break;
                case SlotState.Occupied:
                    emptyItemSlots.Remove(slotIndex);
                    usingItemSlotDict.Add(slotIndex, stackToChangeState);
                    break;
                default:
                    break;
            }

            //Debug.LogAssertion($"change item state as {stateTo}");
        }
        private void OnItemAmountChanged(ItemStack itemStack, int deltaAmount) {
            string itemID = itemStack.ItemData.ItemID;
            if (amountDict.ContainsKey(itemID)) {
                amountDict[itemID] += deltaAmount;
            }
            else {
                amountDict.Add(itemID, deltaAmount);
            }
        }

        private void UpdateAmountDictRemove(SlotInventory targetInventory, ItemStack newItemStack) {
            if (targetInventory == null || newItemStack == null) return; 
            string itemID = newItemStack.ItemData.ItemID;
            if (targetInventory.amountDict.ContainsKey(itemID)) {
                targetInventory.amountDict[itemID] -= newItemStack.Amount;
            }
            else {
                Debug.LogError($"<color=red>key가 없는 상태에서 삭제 시도. 이 상황에서는 key가 있다고 가정하기 때문에 오류가 있는지 확인 필요</color>");
            }
        }
        private void UpdateAmountDictAdd(SlotInventory targetInventory, ItemStack newItemStack) {
            if (targetInventory == null || newItemStack == null) return;
            string itemID = newItemStack.ItemData.ItemID;
            if (targetInventory.amountDict.ContainsKey(itemID)) {
                targetInventory.amountDict[itemID] += newItemStack.Amount;
            }
            else {
                targetInventory.amountDict[itemID] = newItemStack.Amount;
            }
        }


    }
}