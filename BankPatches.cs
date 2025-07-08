using FixedBankTabs;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.Button;

namespace FixedBankTabs
{
    public static class BankPatches
    {
        static int BASENUMOFBUTTONS = 3;
        public static int EXTRANUMOFTABS = 6;

        static Button[] newButtons;
        static List<ItemData>[] newItemDatas;
        static int[][] newStorageSizes;
        static ItemStorage_Profile[] newItemStorageProfiles;

        public static void init()
        {
            newButtons = new Button[EXTRANUMOFTABS];
            newItemDatas = new List<ItemData>[EXTRANUMOFTABS];
            newStorageSizes = new int[EXTRANUMOFTABS][];
            newItemStorageProfiles = new ItemStorage_Profile[EXTRANUMOFTABS];
            for(int i = 0; i < EXTRANUMOFTABS; i++)
            {
                newStorageSizes[i] = new int[3];
                newItemDatas[i] = new List<ItemData>();
                newItemStorageProfiles[i] = new ItemStorage_Profile();
                newItemStorageProfiles[i]._heldItemStorage = new ItemData[0];
            }
        }


        static void SetStorageTab(ItemStorageManager instance, int index)
        {
            if (index < 3)
            {
                if (index < 1)
                {
                    instance.SetStorageTab_00();
                }
                else if (index < 2)
                {
                    instance.SetStorageTab_01();
                }
                else
                {
                    instance.SetStorageTab_02();
                }
            }
            else
            {
                Plugin.Logger.LogInfo("index is " + index);
                instance._selectedStorageTab = index;
                instance._storageTabHighlight.transform.position = newButtons[index - BASENUMOFBUTTONS].transform.GetChild(0).position;
            }
        }

        static bool CheckOutOfBounds(int index)
        {
            if (index < BASENUMOFBUTTONS || index >= BASENUMOFBUTTONS + EXTRANUMOFTABS)
            {
                return true;
            }
            return false;
        }

        [HarmonyPatch(typeof(ItemStorageManager), "Awake")]
        [HarmonyPostfix]
        private static void StorageManagerAwakePatch(ItemStorageManager __instance)
        {
            Button[] buttons = new Button[BASENUMOFBUTTONS];
            buttons[0] = __instance._storageTabButton_00;
            buttons[1] = __instance._storageTabButton_01;
            buttons[2] = __instance._storageTabButton_02;

            float offset = buttons[1].transform.localPosition.x - buttons[0].transform.localPosition.x;

            int lastSiblingIndex = buttons[buttons.Length - 1].transform.GetSiblingIndex();

            for (int i = 0; i < EXTRANUMOFTABS; i++)
            {
                Button orig = buttons[i % 3];
                GameObject origObject = orig.gameObject;
                newButtons[i] = GameObject.Instantiate(origObject,
                                                       buttons[2].transform.position + new Vector3(offset*(i+1), 0f, 0f),
                                                       origObject.transform.rotation,
                                                       origObject.transform.parent).GetComponent<Button>();
                Button currentButton = newButtons[i];
                currentButton.transform.SetSiblingIndex(lastSiblingIndex + 1);
                lastSiblingIndex++;

                Image buttonImage = currentButton.GetComponentInChildren<Image>();
                buttonImage.color += new Color(0.35f, 0f, 0.1f);

                ButtonClickedEvent currentClick = currentButton.onClick;
                currentClick.RemoveAllListeners();
                int index = i + BASENUMOFBUTTONS;
                currentClick.AddListener(delegate { SetStorageTab(__instance, index); });
                currentClick.AddListener(__instance.Clear_StorageEntries);
                currentClick.AddListener(__instance.Init_StorageListing);
            }



        }

        [HarmonyPatch(typeof(ItemStorageManager), "Begin_StorageListing")]
        [HarmonyPrefix]
        static bool BeginStorageListingPatch(ItemStorageManager __instance)
        {
            if (CheckOutOfBounds(__instance._selectedStorageTab))
            {
                return true;
            }
            ItemData[] items = null;
            items = newItemDatas[__instance._selectedStorageTab - BASENUMOFBUTTONS].ToArray();
            for (int i = 0; i < items.Length; i++)
            {
                ItemData itemData = items[i];
                ScriptableItem scriptableItem = GameManager._current.Locate_Item(itemData._itemName);
                if (itemData._quantity > 0 && scriptableItem)
                {
                    __instance.Create_StorageEntry(itemData, scriptableItem, i, itemData._slotNumber);
                }
            }

            return false;
        }

        [HarmonyPatch(typeof(ItemStorageManager), "Create_StorageEntry")]
        [HarmonyPrefix]
        static bool CreateStorageEntryPatch(ItemStorageManager __instance, ItemData _itemData, ScriptableItem _scriptItem, int _index, int _slotNumber)
        {



            if (CheckOutOfBounds(__instance._selectedStorageTab))
            {
                return true;
            }
            int offsetIndex = __instance._selectedStorageTab - BASENUMOFBUTTONS;
            if (!GameManager._current.Locate_Item(_itemData._itemName))
            {
                return false;
            }
            if (_itemData._modifierID > 0 && !GameManager._current.Locate_StatModifier(_itemData._modifierID))
            {
                _itemData._modifierID = 0;
            }

            if (newStorageSizes[offsetIndex][(int)_scriptItem._itemType] >= 48)
            {
                return false;
            }
            newStorageSizes[offsetIndex][(int)_scriptItem._itemType]++;

            GameObject gameObject = GameObject.Instantiate<GameObject>(__instance._storageEntryPrefab);
            ItemListDataEntry itementry = gameObject.GetComponent<ItemListDataEntry>();
            switch (_scriptItem._itemType)
            {
                case ItemType.GEAR:
                    gameObject.transform.SetParent(__instance._gearTabContainer);
                    break;
                case ItemType.CONSUMABLE:
                    gameObject.transform.SetParent(__instance._consumableTabContainer);
                    break;
                case ItemType.TRADE:
                    gameObject.transform.SetParent(__instance._tradeItemTabContainer);
                    break;
            }
            itementry._dataID = _index;
            itementry._itemData = _itemData;
            itementry._scriptableItem = _scriptItem;
            itementry._entryType = ItemListEntryType.STORAGE;
            itementry.transform.localScale = Vector3.one;
            itementry._itemData._slotNumber = _slotNumber;
            itementry._parentItemSlotUI = __instance._storageItemSlots[_slotNumber];
            itementry.Apply_ItemDataInfo();
            __instance._storageListEntries.Add(itementry);
            return false;
        }

        [HarmonyPatch(typeof(ItemStorageManager), "Delete_StorageEntry")]
        [HarmonyPrefix]
        static bool DeleteStorageEntryPatch(ItemStorageManager __instance, ItemData _itemData)
        {
            if (CheckOutOfBounds(__instance._selectedStorageTab))
            {
                return true;
            }
            int offsetIndex = __instance._selectedStorageTab - BASENUMOFBUTTONS;
            ScriptableItem scriptableItem = GameManager._current.Locate_Item(_itemData._itemName);
            if (!scriptableItem)
            {
                return false;
            }

            newItemDatas[offsetIndex].Remove(_itemData);
            newStorageSizes[offsetIndex][(int)scriptableItem._itemType]--;

            for (int i = 0; i < __instance._storageListEntries.Count; i++)
            {
                if (__instance._storageListEntries[i]._itemData == _itemData)
                {
                    GameObject.Destroy(__instance._storageListEntries[i].gameObject);
                    __instance._storageListEntries.RemoveAt(i);
                    return false;
                }
            }
            return false;
        }

        [HarmonyPatch(typeof(ItemStorageManager), "Update")]
        [HarmonyPostfix]
        static void UpdatePatch(ItemStorageManager __instance)
        {
            int offsetIndex = __instance._selectedStorageTab - BASENUMOFBUTTONS;

            if (__instance._isOpen)
            {
                if (!CheckOutOfBounds(__instance._selectedStorageTab))
                {
                    __instance._counter_gearItemSize.text = string.Format("{0}/48", newStorageSizes[offsetIndex][0]);
                    __instance._counter_consumableItemSize.text = string.Format("{0}/48", newStorageSizes[offsetIndex][1]);
                    __instance._counter_tradeItemSize.text = string.Format("{0}/48", newStorageSizes[offsetIndex][2]);
                }

                for (int i = 0; i < EXTRANUMOFTABS; i++)
                {
                    newButtons[i].interactable = __instance._selectedStorageTab - BASENUMOFBUTTONS != i;
                }
                __instance.Handle_TabVisibility();
                return;
            }
        }


        [HarmonyPatch(typeof(ProfileDataManager), "Load_ItemStorageData")]
        [HarmonyPostfix]
        static void LoadItemStorageDataPatch(ProfileDataManager __instance)
        {
            for(int i = 0; i < EXTRANUMOFTABS; i++)
            {
                string path = Path.Combine(__instance._dataPath, "atl_itemBank_" + (i+BASENUMOFBUTTONS).ToString("00"));
                if (File.Exists(path))
                {
                    ItemStorage_Profile itemStorageProfile = JsonUtility.FromJson<ItemStorage_Profile>(File.ReadAllText(path));
                    newItemStorageProfiles[i] = itemStorageProfile;
                }
                else
                {

                }

            }
            ItemStorageManager current = ItemStorageManager._current;
            if (!current)
            {
                return;
            }

            for (int i = 0; i < EXTRANUMOFTABS; i++)
            {
                
                newStorageSizes[i][0] = 0;
                newStorageSizes[i][1] = 0;
                newStorageSizes[i][2] = 0;
                newItemDatas[i].Clear();
                newItemDatas[i].AddRange(newItemStorageProfiles[i]._heldItemStorage);
            }
        }

        [HarmonyPatch(typeof(ProfileDataManager), "Save_ItemStorageData")]
        [HarmonyPostfix]
        static void SaveItemStorageDataPatch(ProfileDataManager __instance)
        {

            for (int i = 0; i < EXTRANUMOFTABS; i++)
            {

                string path = Path.Combine(__instance._dataPath, "atl_itemBank_" + (i + BASENUMOFBUTTONS).ToString("00"));
                if (!File.Exists(path))
                {
                    newItemStorageProfiles[i] = new ItemStorage_Profile();
                }
                newItemStorageProfiles[i]._heldItemStorage = newItemDatas[i].ToArray();
                string contents = JsonUtility.ToJson(newItemStorageProfiles[i],true);
                File.WriteAllText(path,contents);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ItemListDataEntry), "Init_PutItemIntoStorage")]
        static bool PutItemIntoStoragePatch(ItemListDataEntry __instance, int _setItemSlot)
        {
            ItemStorageManager ism = ItemStorageManager._current;
            if (CheckOutOfBounds(ism._selectedStorageTab))
            {
                return true;
            }
            int offsetIndex = ism._selectedStorageTab - BASENUMOFBUTTONS;

            if (__instance._entryType != ItemListEntryType.INVENTORY)
            {
                return false;
            }



            if (newStorageSizes[offsetIndex][(int)__instance._scriptableItem._itemType] >= 48)
            {
                ErrorPromptTextManager.current.Init_ErrorPrompt("Storage Full");
                __instance.Relocate_ToOriginSlot();
                return false;
            }
            List<ItemListDataEntry> storageListEntries = ItemStorageManager._current._storageListEntries;

            for (int i = 0; i < storageListEntries.Count && Input.GetKey(KeyCode.LeftShift); i++)
            {
                if (storageListEntries[i]._scriptableItem == __instance._scriptableItem &&
                    storageListEntries[i]._itemData._quantity < storageListEntries[i]._itemData._maxQuantity &&
                    __instance._itemData._quantity + storageListEntries[i]._itemData._quantity <= storageListEntries[i]._itemData._maxQuantity)
                {
                    storageListEntries[i]._itemData._quantity += __instance._itemData._quantity;
                    Player._mainPlayer._pInventory.Remove_Item(__instance._itemData, 0);
                    __instance.Init_SaveProfiles();
                    ProfileDataManager._current.Save_ItemStorageData();
                    return false;
                }
            }



            ism._commandBuffer = 0.25f;
            ism.Create_StorageEntry(__instance._itemData, __instance._scriptableItem, ItemStorageManager._current._storageListEntries.Count, _setItemSlot);
            newItemDatas[offsetIndex].Add(__instance._itemData);
            Player._mainPlayer._pInventory.Remove_Item(__instance._itemData, 0);
            __instance.Init_SaveProfiles();
            ProfileDataManager._current.Save_ItemStorageData();
            return false;
        }
    }
}
