﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using NewBeginnings.Common.PlayerBackgrounds;
using NewBeginnings.Content.Items.Icons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.GameContent.UI.States;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace NewBeginnings.Common.Edits;

/// <summary>
/// Handles most of the mod's UI modifications and detours.<br/>
/// This is an incredibly intricate class and is unlikely to be useful in any situation but its own.
/// </summary>
internal class CharCreationEdit
{
    public static bool _bgToggled = false;
    public static UICharacterCreation _self;
    public static UIElement _difficultyContainer;
    public static UISlicedImage _difficultyDescriptionContainer;
    public static IEnumerable<UIElement> _originalChildren;

    //All of these are for the description changes
    public static UIText _originalDifficultyDescription;
    public static UIText _backgroundFlavour;
    public static UIText _backgroundDescription;
    public static UIText _backgroundStats;
    public static UIList _descriptionList;
    public static UIScrollbar _descScrollBar;
    public static UIElement _descItemContainer;

    public static FieldInfo InternalPlayerField;

    internal static void Load()
    {
        InternalPlayerField = typeof(UICharacterCreation).GetField("_player", BindingFlags.Instance | BindingFlags.NonPublic);

        IL.Terraria.GameContent.UI.States.UICharacterCreation.MakeInfoMenu += UICharacterCreation_MakeInfoMenu;
        On.Terraria.GameContent.UI.States.UICharacterCreation.Click_GoBack += UICharacterCreation_Click_GoBack;
        On.Terraria.GameContent.UI.States.UICharacterCreation.Click_NamingAndCreating += UICharacterCreation_Click_NamingAndCreating;

        CharCreationHijackSaveDetour.Load();
        CharNameEdit.Load();
    }

    /// <summary>Just used to reset origin toggle if the player goes to a different screen.</summary>
    private static void UICharacterCreation_Click_NamingAndCreating(On.Terraria.GameContent.UI.States.UICharacterCreation.orig_Click_NamingAndCreating orig, UICharacterCreation self, UIMouseEvent evt, UIElement listeningElement)
    {
        _bgToggled = false;
        orig(self, evt, listeningElement);
    }

    /// <summary>Just used to reset origin toggle if the player goes to a different screen.</summary>
    private static void UICharacterCreation_Click_GoBack(On.Terraria.GameContent.UI.States.UICharacterCreation.orig_Click_GoBack orig, UICharacterCreation self, UIMouseEvent evt, UIElement listeningElement)
    {
        _bgToggled = false;
        orig(self, evt, listeningElement);
    }

    /// <summary>IL edit that shoves everything we want into the vanilla UI.</summary>
    private static void UICharacterCreation_MakeInfoMenu(ILContext il)
    {
        ILCursor c = new(il);

        if (!c.TryGotoNext(x => x.MatchLdarg(1)))
            return;

        c.Index += 3;

        c.Emit(OpCodes.Ldloc_0);
        c.EmitDelegate(AddNewButton); //Add player bg button

        if (!c.TryGotoNext(x => x.MatchLdstr("UI.PlayerEmptyName")))
            return;

        if (!c.TryGotoNext(x => x.MatchLdcR4(0.5f)))
            return;

        if (!c.TryGotoNext(x => x.MatchLdloc(0)))
            return;

        c.Emit(OpCodes.Ldarg_0);
        c.EmitDelegate((UICharacterCreation self) =>
        {
            _self = self;

            var plr = InternalPlayerField.GetValue(_self) as Player;
            if (!plr.GetModPlayer<PlayerBackgroundPlayer>().HasBG()) //Set background data if it's null
                plr.GetModPlayer<PlayerBackgroundPlayer>().SetBackground(PlayerBackgroundDatabase.playerBackgroundDatas.First());
        });

        c.Emit(OpCodes.Ldloc_1);
        c.EmitDelegate((UICharacterNameButton characterNameButton) => //Adjust character name button to give space for the player bg button
        {
            characterNameButton.HAlign = 0f;
            characterNameButton.Width = StyleDimension.FromPixelsAndPercent(0f, 0.9f);
        });

        if (!c.TryGotoNext(MoveType.After, x => x.MatchStloc(5)))
            return;

        c.Emit(OpCodes.Ldloc_S, (byte)5);
        c.EmitDelegate((UIElement elem) => //Store uiElement2 / _difficultyContainer
        {
            _difficultyContainer = elem;
        });

        if (!c.TryGotoNext(MoveType.After, x => x.MatchStloc(6)))
            return;

        c.Emit(OpCodes.Ldloc_S, (byte)6);
        c.EmitDelegate((UISlicedImage elem) => //Store uiText
        {
            _difficultyDescriptionContainer = elem;
        });
    }

    /// <summary>Adds the background icon button to the UI.</summary>
    private static void AddNewButton(UIElement parent)
    {
        UIImageButton backgroundButton = new(ModContent.Request<Texture2D>("NewBeginnings/Assets/Textures/BackgroundIcon"))
        {
            HAlign = 0.99f,
            VAlign = 0.0f,
            Width = StyleDimension.FromPixels(40f),
            Height = StyleDimension.FromPixels(40f)
        };

        backgroundButton.SetPadding(0f);
        backgroundButton.OnMouseDown += BackgroundButton_OnMouseDown;
        //backgroundButton.OnUpdate += BackgroundButton_OnUpdate;

        parent.Append(backgroundButton);
    }

    //private static void BackgroundButton_OnUpdate(UIElement affectedElement)
    //{
    //    int i = 0;
    //    i++;
    //}

    /// <summary>OnMouseDown event for the background icon button. Sets the background list, changes the description and sets defaults where necessary.</summary>
    /// <param name="evt"></param>
    /// <param name="listeningElement"></param>
    private static void BackgroundButton_OnMouseDown(UIMouseEvent evt, UIElement listeningElement)
    {
        if (!_bgToggled) //If we're switching to the bg section
        {
            _bgToggled = true;

            var children = new List<UIElement>(); //copy children
            foreach (var item in _difficultyContainer.Children)
                children.Add(item);
            _originalChildren = children;

            _difficultyContainer.RemoveAllChildren(); //kill original children

            var uiText = _difficultyDescriptionContainer.Children.FirstOrDefault(x => x is UIText text); //And set the description
            if (uiText is UIText tex)
                _originalDifficultyDescription = tex;

            var plr = InternalPlayerField.GetValue(_self) as Player;
            BuildDescriptionScrollbar(plr);
            BuildBackgroundSelections();

            _difficultyDescriptionContainer.RemoveChild(_originalDifficultyDescription);
        }
        else //If we're switching out of the bg section
        {
            _bgToggled = false;

            _difficultyContainer.RemoveAllChildren();

            foreach (var item in _originalChildren) //replace children with clones
                _difficultyContainer.Append(item);

            _originalChildren = new List<UIElement>();

            int dif = (InternalPlayerField.GetValue(_self) as Player).difficulty;
            string text = Lang.menu[31].Value;

            if (dif == 1)
                text = Lang.menu[30].Value;
            else if (dif == 2)
                text = Lang.menu[29].Value;
            else if (dif == 3)
                text = Language.GetText("UI.CreativeDescriptionPlayer").Value;

            _difficultyDescriptionContainer.RemoveChild(_descScrollBar);
            _difficultyDescriptionContainer.RemoveChild(_descriptionList);
            _difficultyDescriptionContainer.RemoveChild(_descItemContainer);
            _originalDifficultyDescription.SetText(text);
            _difficultyDescriptionContainer.Append(_originalDifficultyDescription);
        }
    }

    private static void BuildDescriptionScrollbar(Player plr)
    {
        _descriptionList = new UIList() //List for use in the description
        {
            Width = StyleDimension.FromPixels(270),
            Height = StyleDimension.FromPixels(200),
            PaddingLeft = 8,
            PaddingRight = 8,
            ListPadding = -20
        };
        _difficultyDescriptionContainer.Append(_descriptionList);

        _descScrollBar = new UIScrollbar() //Scrollbar for above list
        {
            HAlign = 1f,
            Height = StyleDimension.FromPixelsAndPercent(-8, 1f),
            Top = StyleDimension.FromPixels(4),
        };

        _descriptionList.SetScrollbar(_descScrollBar);
        _difficultyDescriptionContainer.Append(_descScrollBar);

        var bgData = plr.GetModPlayer<PlayerBackgroundPlayer>().BackgroundData;
        _backgroundStats = new UIText(GetStatsText(bgData))
        {
            Top = StyleDimension.FromPercent(0.1f),
            Width = StyleDimension.FromPixelsAndPercent(-8, 1f),
            Height = StyleDimension.FromPixels(44),
            IsWrapped = false,
            MarginTop = 8
        };
        _descriptionList.Add(_backgroundStats);

        _backgroundFlavour = new UIText(bgData.Flavour)
        {
            Top = StyleDimension.FromPercent(0.1f),
            Width = StyleDimension.FromPixelsAndPercent(-8, 1f),
            Height = StyleDimension.FromPixels(10),
            IsWrapped = true,
            MarginTop = 8
        };
        _descriptionList.Add(_backgroundFlavour);

        SetItemList(bgData, true);
        _descriptionList.Add(_descItemContainer);

        _backgroundDescription = new UIText(bgData.Description, 0.9f)
        {
            TextColor = Color.Gray,
            Top = StyleDimension.FromPercent(0.1f),
            Width = StyleDimension.FromPixelsAndPercent(-8, 1f),
            Height = StyleDimension.FromPixels(10),
            IsWrapped = true,
            MarginTop = 8
        };
        _descriptionList.Add(_backgroundDescription);
    }

    private static void SetItemList(PlayerBackgroundData data, bool resetItemContainer = false)
    {
        var descItemHeight = StyleDimension.FromPixels(data.DisplayItemCount() == 0 ? 0 : 58 + 38 * (data.DisplayItemCount() / 6f));

        if (resetItemContainer) //Creates a new item container
        {
            _descItemContainer = new UIElement()
            {
                Width = StyleDimension.FromPercent(1),
                Height = descItemHeight,
                Top = StyleDimension.FromPixels(-20)
            };
        }
        else //Adjusts container to fit new height
        {
            _descItemContainer.Height = descItemHeight;
            _descItemContainer.Recalculate();
        }

        int offset = 0;
        float yOffset = 0;
        int id = 0;
        List<(int type, int stack)> items = new();
        (int sword, int pick, int axe) = (data.Misc.CopperShortswordReplacement, data.Misc.CopperPickaxeReplacement, data.Misc.CopperAxeReplacement);

        if (sword != -1)
            items.Add((sword, 1));

        if (pick != -1)
            items.Add((pick, 1));

        if (axe != -1)
            items.Add((axe, 1));

        (int head, int body, int legs) = (data.Equip.Head, data.Equip.Body, data.Equip.Legs);

        if (head > ItemID.None)
            items.Add((head, 1));

        if (body > ItemID.None)
            items.Add((body, 1));

        if (legs > ItemID.None)
            items.Add((legs, 1));

        foreach (var item in data.Equip.Accessories)
            items.Add((item, 1));

        items.AddRange(data.Inventory.ToList());

        foreach (var (type, stack) in items)
        {
            Item item = new(type) { stack = stack, scale = 0.85f };

            UIImage slotBG = new UIImage(TextureAssets.InventoryBack) //Slot background
            {
                Left = StyleDimension.FromPixels(-6 + (offset * 42)),
                Top = StyleDimension.FromPixels(-2 + (yOffset * 38)),
                ImageScale = 0.77f,
            };

            UIItemIcon icon = new UIItemIcon(item, false) //Item itself
            {
                Left = StyleDimension.FromPixels(10),
                Top = StyleDimension.FromPixels(9),
            };

            if (stack > 1)
            {
                UIText stackUI = new UIText(stack.ToString(), 0.65f, false)
                {
                    Left = StyleDimension.FromPixels(4),
                    Top = StyleDimension.FromPixels(18)
                };

                icon.Append(stackUI);
            }
            slotBG.Append(icon);

            float width = FontAssets.ItemStack.Value.MeasureString(item.HoverName).X * 0.8f;

            UIText name = new(item.HoverName, 0.8f)
            {
                Width = StyleDimension.FromPixels(42),
                Top = StyleDimension.FromPixels(-10),
                HAlign = 0.5f,
                DynamicallyScaleDownToWidth = true,
            };

            if (id == 0 || id % 5 == 1)
            {
                name.Top = StyleDimension.FromPixels(-10);
                name.HAlign = 0f;
                name.Left = StyleDimension.FromPixels(6);
            }
            else if (id % 5 == 0)
            {
                name.Top = StyleDimension.FromPixels(-10);
                name.HAlign = 1f;
            }

            name.DynamicallyScaleDownToWidth = true;

            icon.OnMouseOver += (UIMouseEvent evt, UIElement listeningElement) => slotBG.Append(name);
            icon.OnMouseOut += (UIMouseEvent evt, UIElement listeningElement) => slotBG.RemoveChild(name);

            _descItemContainer.Append(slotBG);

            offset++;
            if (10 + offset * 32 > 200) //ew hardcoding but nothing works
            {
                offset = 0;
                yOffset++;
            }
            id++;
        }
    }

    /// <summary>Builds the origin list and buttons.</summary>
    private static void BuildBackgroundSelections()
    {
        UIList allBGButtons = new UIList() //List of all background buttons
        {
            Width = StyleDimension.FromPixels(180),
            Height = StyleDimension.FromPixels(120),
            ListPadding = 4,
        };

        _difficultyContainer.Append(allBGButtons);

        UIScrollbar scroll = new UIScrollbar() //Scrollbar for above list
        {
            HAlign = 1f,
            Height = StyleDimension.FromPixelsAndPercent(-8, 1f),
            Top = StyleDimension.FromPixels(4)
        };

        allBGButtons.SetScrollbar(scroll);
        _difficultyContainer.Append(scroll);

        List<(int, UIColoredImageButton)> buttons = new();

        foreach (var item in PlayerBackgroundDatabase.playerBackgroundDatas) //Adds every background into the list as a button
        {
            if (!item.Delegates.ClearCondition())
                continue;

            var asset = PlayerBackgroundDatabase.backgroundIcons[PlayerBackgroundDatabase.backgroundIcons.ContainsKey(item.Texture) ? item.Texture : "Default"];
            UIColoredImageButton currentBGButton = new(asset)
            {
                Width = StyleDimension.FromPercent(0.9f),
                Height = StyleDimension.FromPixels(36),
                Left = StyleDimension.FromPixels(-60),
            };
            currentBGButton.SetColor(Color.Gray);

            currentBGButton.OnMouseDown += (UIMouseEvent evt, UIElement listeningElement) => //Click event
            {
                PlayerBackgroundData useData = item.Name == "Random" ? Main.rand.Next(PlayerBackgroundDatabase.playerBackgroundDatas.SkipLast(1).ToList()) : item; //Hardcoding for random, sucks but eh
                _backgroundFlavour.SetText(item.Name != "Random" ? useData.Flavour : item.Flavour); //Changes the UIText's value to use the bg's description
                _backgroundDescription.SetText(item.Name != "Random" ? useData.Description : item.Description); //Changes the UIText's value to use the bg's description

                if (item.Name != "Random")
                    _backgroundStats.SetText(GetStatsText(useData));
                else
                    _backgroundStats.SetText($"[i:{ItemID.Heart}]??? [i:{ItemID.Star}]???");

                _descItemContainer.RemoveAllChildren();

                Player plr = InternalPlayerField.GetValue(_self) as Player; //Applies the visuals for the background...

                if (item.Name != "Random")
                    SetItemList(useData);

                item.ApplyArmor(plr);
                item.ApplyAccessories(plr);
                plr.GetModPlayer<PlayerBackgroundPlayer>().SetBackground(useData); //...and sets it.

                foreach (var item in allBGButtons.Where(x => x is UIColoredImageButton))
                    (item as UIColoredImageButton).SetColor(Color.Gray);

                currentBGButton.SetColor(Color.White); //"Selects" the button visually.
            };

            currentBGButton.OnMouseOver += (UIMouseEvent evt, UIElement listeningElement) =>
            {
                Player plr = InternalPlayerField.GetValue(_self) as Player;
                item.ApplyArmor(plr);
                item.ApplyAccessories(plr);

                currentBGButton.SetColor(new Color(220, 220, 220));
            };

            currentBGButton.OnMouseOut += (UIMouseEvent evt, UIElement listeningElement) =>
            {
                Player plr = InternalPlayerField.GetValue(_self) as Player;
                var bgData = plr.GetModPlayer<PlayerBackgroundPlayer>().BackgroundData;

                if (!plr.GetModPlayer<PlayerBackgroundPlayer>().HasBG() || bgData.Name != item.Name)
                {
                    bgData.ApplyArmor(plr);
                    bgData.ApplyAccessories(plr);
                    plr.GetModPlayer<PlayerBackgroundPlayer>().SetBackground(bgData); //Sets the player's background.

                    currentBGButton.SetColor(Color.Gray);
                }
                else
                    currentBGButton.SetColor(Color.White);
            };

            UIText bgName = new(item.Name, 1.2f) //Background's name
            {
                HAlign = 0f,
                VAlign = 0.5f,
                Left = StyleDimension.FromPixels(108)
            };

            currentBGButton.Append(bgName);
            buttons.Add((item.Misc.SortPriority, currentBGButton));
        }

        foreach (var item in buttons)
            allBGButtons.Add(item.Item2);

        SetSort(allBGButtons, buttons);
    }

    private static string GetStatsText(PlayerBackgroundData bgData)
    {
        string stats = $"[i:{ItemID.Heart}]{bgData.Misc.MaxLife} [i:{ItemID.Star}]{bgData.Misc.AdditionalMana}";
        string stars = string.Empty;

        for (int i = 0; i < 5; ++i)
            stars += bgData.Misc.Stars > i ? $"[i:{ModContent.ItemType<LightStar>()}]" : $"[i:{ModContent.ItemType<DimStar>()}]";

        return stars + "   " + stats;
    }

    private static void SetSort(UIList allBGButtons, List<(int, UIColoredImageButton)> buttons)
    {
        //This is some of the ugliest nonsense I've ever written
        allBGButtons.ManualSortMethod = (list) => list.Sort((self, other) =>
        {
            int mySortPriority = buttons.Find(x => x.Item2 == self).Item1; //Find priority by finding the value that has the given button as Item2
            int otherSortPriority = buttons.Find(x => x.Item2 == other).Item1; //for both the current and next button

            return otherSortPriority.CompareTo(mySortPriority);
        });

        allBGButtons.UpdateOrder(); //Reorder the list according to the above manual sort method
    }

    private class UIItemDummy : UIItemSlot
    {
        public UIItemDummy(Item[] itemArray, int itemIndex, int itemSlotContext) : base(itemArray, itemIndex, itemSlotContext)
        {
        }
    }
}
