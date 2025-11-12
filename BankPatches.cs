using FixedBankTabs;
using HarmonyLib;
using JetBrains.Annotations;
using MoreBankTabs;
using SnivysUI;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using static UnityEngine.UI.Button;

namespace FixedBankTabs
{
    public static class BankPatches
    {
        static int BASENUMOFBUTTONS = 3;
        static int MAXPAGES = 100;

        class BankTabData
        {
            public BankTabData()
            {
                ItemDatas = new List<ItemData>();
                StorageSizes = new int[3];
                ItemStorageProfile = new ItemStorage_Profile();
            }
            public List<ItemData> ItemDatas;
            public int[] StorageSizes;
            public ItemStorage_Profile ItemStorageProfile;
        }


        static Dictionary<int,BankTabData> BankTabs;

        public static void init()
        {
            BankTabs = new Dictionary<int,BankTabData>();
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
            }
        }

        static bool CheckOutOfBounds(int index)
        {
            if (index < BASENUMOFBUTTONS)
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

            GameObject PageSelector = GameObject.Instantiate(AssetHandler.FetchFromBundle<GameObject>("morebanktabs", "LeftRightTextbox"), buttons[0].transform.parent);
            
            RectTransform PSRect = PageSelector.GetComponent<RectTransform>();

            LeftRightTextbox handler = PageSelector.GetComponent<LeftRightTextbox>();

            PageSelector.transform.SetSiblingIndex(0);

            __instance._storageTabHighlight.gameObject.SetActive(false);

            PSRect.anchoredPosition = new Vector2(0, -150);


            AudioMixerGroup mixer = buttons[0].GetComponent<AudioSource>().outputAudioMixerGroup;
            AudioSource[] auds = PageSelector.GetComponentsInChildren<AudioSource>();

            foreach (AudioSource aud in auds)
            {
                aud.outputAudioMixerGroup = mixer;
            }

            foreach (Button b in buttons)
            {
                b.gameObject.SetActive(false);
            }


            ButtonClickedEvent left =  handler.GetOnLeftClick();
            ButtonClickedEvent right =  handler.GetOnRightClick();

            handler.SetText(String.Format("Page {0} / {1}", __instance._selectedStorageTab + 1, MAXPAGES));

            left.RemoveAllListeners();
            right.RemoveAllListeners();
            
            left.AddListener(delegate
            {
                if (__instance._selectedStorageTab > 0)
                {
                    SetStorageTab(__instance, __instance._selectedStorageTab - 1);
                }
                handler.SetText(String.Format("Page {0} / {1}", __instance._selectedStorageTab+1, MAXPAGES));
            });
            right.AddListener(delegate
            {
                if(__instance._selectedStorageTab < MAXPAGES)
                {
                    SetStorageTab(__instance, __instance._selectedStorageTab + 1);
                }
                handler.SetText(String.Format("Page {0} / {1}",__instance._selectedStorageTab+1, MAXPAGES));
            });

            left.AddListener(__instance.Clear_StorageEntries);
            right.AddListener(__instance.Clear_StorageEntries);

            left.AddListener(__instance.Init_StorageListing);
            right.AddListener(__instance.Init_StorageListing);




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
            items = BankTabs[__instance._selectedStorageTab].ItemDatas.ToArray();
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
            if(CheckOutOfBounds(__instance._selectedStorageTab))
            {
                return true;
            }
            
            if (!GameManager._current.Locate_Item(_itemData._itemName))
            {
                return false;
            }
            if (_itemData._modifierID > 0 && !GameManager._current.Locate_StatModifier(_itemData._modifierID))
            {
                _itemData._modifierID = 0;
            }

            BankTabData currentTab = BankTabs[__instance._selectedStorageTab];

            if (currentTab.StorageSizes[(int)_scriptItem._itemType] >= 48)
            {
                return false;
            }
            currentTab.StorageSizes[(int)_scriptItem._itemType]++;

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
            ScriptableItem scriptableItem = GameManager._current.Locate_Item(_itemData._itemName);
            if (!scriptableItem)
            {
                return false;
            }
            BankTabData currentTab = BankTabs[__instance._selectedStorageTab];
            currentTab.ItemDatas.Remove(_itemData);
            currentTab.StorageSizes[(int)scriptableItem._itemType]--;

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

            if (__instance._isOpen)
            {
                if (!CheckOutOfBounds(__instance._selectedStorageTab) && BankTabs.ContainsKey(__instance._selectedStorageTab))
                {
                    BankTabData currentTab = BankTabs[__instance._selectedStorageTab];
                    __instance._counter_gearItemSize.text = string.Format("{0}/48", currentTab.StorageSizes[0]);
                    __instance._counter_consumableItemSize.text = string.Format("{0}/48", currentTab.StorageSizes[1]);
                    __instance._counter_tradeItemSize.text = string.Format("{0}/48", currentTab.StorageSizes[2]);
                }
                /*
                for (int i = 0; i < EXTRANUMOFTABS; i++)
                {
                    newButtons[i].interactable = __instance._selectedStorageTab - BASENUMOFBUTTONS != i;
                }
                */
                __instance.Handle_TabVisibility();
                return;
            }
        }


        [HarmonyPatch(typeof(ProfileDataManager), "Load_ItemStorageData")]
        [HarmonyPostfix]
        static void LoadItemStorageDataPatch(ProfileDataManager __instance)
        {
            foreach(string path in Directory.GetFiles(__instance._dataPath, "atl_itemBank_*"))
            {
                /*
                Plugin.Logger.LogInfo(Path.GetFileName(path).Substring(13));
                Plugin.Logger.LogInfo(Path.GetFileName(path));
                Plugin.Logger.LogInfo(int.TryParse(Path.GetFileName(path).Substring(13), out int test));
                Plugin.Logger.LogInfo(test);
                */

                

                if (int.TryParse(Path.GetFileName(path).Substring(13),out int n))
                {
                    if(n < BASENUMOFBUTTONS)
                    {
                        continue;
                    }
                    BankTabs[n] = new BankTabData();
                    BankTabData currentTab = BankTabs[n];

                    ItemStorage_Profile itemStorageProfile = JsonUtility.FromJson<ItemStorage_Profile>(File.ReadAllText(path));
                    currentTab.ItemStorageProfile = itemStorageProfile;
                    currentTab.ItemDatas = [.. currentTab.ItemStorageProfile._heldItemStorage];
                }
            }

            if(ItemStorageManager._current)
            {
                ItemStorageManager ism = ItemStorageManager._current;
                if(!(CheckOutOfBounds(ism._selectedStorageTab) || BankTabs.ContainsKey(ism._selectedStorageTab)))
                {
                    BankTabs[ism._selectedStorageTab] = new BankTabData();
                }
            }
            /*
            for(int i = 0; i < EXTRANUMOFTABS; i++)
            {
                string path = Path.Combine(__instance._dataPath, "atl_itemBank_" + (i+BASENUMOFBUTTONS).ToString("00"));
                if (File.Exists(path))
                {
                    ItemStorage_Profile itemStorageProfile = JsonUtility.FromJson<ItemStorage_Profile>(File.ReadAllText(path));
                    ItemStorageProfiles[i] = itemStorageProfile;
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
            }*/
        }

        [HarmonyPatch(typeof(ProfileDataManager), "Save_ItemStorageData")]
        [HarmonyPostfix]
        static void SaveItemStorageDataPatch(ProfileDataManager __instance)
        {
            foreach(KeyValuePair<int,BankTabData> kv in BankTabs)
            {
                string path = Path.Combine(__instance._dataPath, "atl_itemBank_" + (kv.Key).ToString("00"));
                BankTabData currentTab = kv.Value;
                currentTab.ItemStorageProfile._heldItemStorage = currentTab.ItemDatas.ToArray();
                
                if(currentTab.ItemStorageProfile._heldItemStorage.Length == 0)
                {
                    
                    if(File.Exists(path))
                    {
                        File.Delete(path);
                    }
                }
                else
                {
                    string contents = JsonUtility.ToJson(currentTab.ItemStorageProfile, true);
                    File.WriteAllText(path, contents);
                }
                    
            }

            /*
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
            */
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

            if (__instance._entryType != ItemListEntryType.INVENTORY)
            {
                return false;
            }

            BankTabData currentTab = BankTabs[ism._selectedStorageTab];

            if (currentTab.StorageSizes[(int)__instance._scriptableItem._itemType] >= 48)
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
            currentTab.ItemDatas.Add(__instance._itemData);
            Player._mainPlayer._pInventory.Remove_Item(__instance._itemData, 0);
            __instance.Init_SaveProfiles();
            ProfileDataManager._current.Save_ItemStorageData();
            return false;
        }
    }
}
