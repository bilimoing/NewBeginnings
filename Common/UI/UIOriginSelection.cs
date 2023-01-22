﻿using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.GameContent;
using Terraria.GameContent.UI.Elements;
using Terraria.UI;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;
using NewBeginnings.Common.PlayerBackgrounds;
using System.Collections.Generic;
using NewBeginnings.Content.Items.Icons;
using System.Linq;

namespace NewBeginnings.Common.UI
{
    internal class UIOriginSelection : UIState
    {
        private const int TotalWidth = 800;
        private const int BackgroundListWidth = 220;

        private readonly Player _player;
        private readonly MouseEvent _return;

        private UIText _statsText;
        private UIText _descFlavour;
        private UIText _descText;
        private UIElement _itemContainer;

        private float BaseHeight => 1080f / 554f * Main.screenHeight;

        public UIOriginSelection(Player player, MouseEvent returnAction)
        {
			_player = player;
            _return = returnAction;

			Setup();
        }

        private void Setup()
        {
            RemoveAllChildren();

            UIElement mainElement = new UIElement
            {
                Width = StyleDimension.FromPixels(TotalWidth),
                Height = StyleDimension.FromPixels(BaseHeight),
                Top = StyleDimension.FromPixels(220f),
                HAlign = 0.5f,
                VAlign = 0f
            };
            mainElement.SetPadding(0f);
            Append(mainElement);

            Color panelColor = new Color(33, 43, 79) * 0.8f;

            UIPanel panel = new UIPanel
            {
                Width = StyleDimension.FromPercent(1f),
                Height = StyleDimension.FromPercent(1f),
                Top = StyleDimension.FromPixels(50f),
                BackgroundColor = panelColor
            };

            mainElement.Append(panel);

            UIImageButton closeButton = new UIImageButton(ModContent.Request<Texture2D>("NewBeginnings/Assets/Textures/UI/OriginBack"))
            {
                Width = StyleDimension.FromPixels(32),
                Height = StyleDimension.FromPixels(32),
                Left = StyleDimension.FromPixelsAndPercent(-40, 1f),
                Top = StyleDimension.FromPixelsAndPercent(0, 0)
            };

            closeButton.OnClick += _return;
            panel.Append(closeButton);

            AddCharacterPreview(panel);

            UIPanel descriptionPanel = new UIPanel
            {
                Width = StyleDimension.FromPixelsAndPercent(-BackgroundListWidth - 10, 1f),
                HAlign = 1f,
                Height = StyleDimension.FromPixelsAndPercent(-40, 1f),
                Top = StyleDimension.FromPixels(30),
                BackgroundColor = panelColor
            };
            panel.Append(descriptionPanel);

            BuildDescription(descriptionPanel);

            UIPanel originsListPanel = new UIPanel
            {
                Width = StyleDimension.FromPixels(BackgroundListWidth),
                HAlign = 0,
                Height = StyleDimension.FromPixelsAndPercent(-20, 1f),
                Top = StyleDimension.FromPixels(10),
                BackgroundColor = panelColor
            };
            panel.Append(originsListPanel);

            BuildBackgroundSelections(originsListPanel, descriptionPanel);

            UIText text = new UIText(GetSplashText(), 0.8f)
            {
                HAlign = 0.68f,
                Top = StyleDimension.FromPixels(4),
            };
            text.OnUpdate += (UIElement affectedElement) => { text.TextColor = Main.MouseTextColorReal; };
            panel.Append(text);
        }

        private static string GetSplashText()
        {
            int random = Main.rand.Next(12);

            return random switch
            {
                0 => "Now with more space!",
                1 => "By the same people who gave you Overseer!",
                2 => "Not to be confused with MrPlague's Authentic Races!",
                3 => "15% less sodium, 15% less sugar, 100% better characters!",
                4 => "Contains a dangerous amount of awful code!",
                5 => "Has never had and never will have any bugs!",
                6 => "Purist is only for people who want to look at the mod, not play it!",
                7 => "Surprisingly popular in Antarctica!",
                8 => "Sponsored by Team Spirit!",
                9 => "Contains a significant lack of shaders!",
                10 => "Confirmed drama-free by researchers!",
                _ => "Tremor? I hardly know her!"
            };
        }

        private void AddCharacterPreview(UIPanel panel)
        {
            UICharacter element = new UICharacter(_player, animated: true, hasBackPanel: false, 1.5f)
            {
                Width = StyleDimension.FromPixels(80f),
                Height = StyleDimension.FromPixelsAndPercent(80f, 0f),
                Top = StyleDimension.FromPixelsAndPercent(-80, 0f),
                VAlign = 0f,
                HAlign = 0.5f
            };
            panel.Append(element);
        }

        private void BuildDescription(UIPanel panel)
        {
            var list = new UIList() //List for use in the description
            {
                Width = StyleDimension.FromPercent(0.98f),
                Height = StyleDimension.FromPixelsAndPercent(0, 1f),
                PaddingLeft = 8,
                PaddingRight = 8,
                ListPadding = -20
            };
            panel.Append(list);

            var scrollBar = new UIScrollbar() //Scrollbar for above list
            {
                HAlign = 1f,
                Height = StyleDimension.FromPixelsAndPercent(-8, 1f),
                Top = StyleDimension.FromPixels(4),
            };

            list.SetScrollbar(scrollBar);
            panel.Append(scrollBar);

            var bgData = _player.GetModPlayer<PlayerBackgroundPlayer>().BackgroundData;
            _statsText = new UIText(GetStatsText(bgData))
            {
                Top = StyleDimension.FromPercent(0.1f),
                Width = StyleDimension.FromPixelsAndPercent(-8, 1f),
                Height = StyleDimension.FromPixels(44),
                IsWrapped = false,
                MarginTop = 8
            };
            list.Add(_statsText);

            _descFlavour = new UIText(bgData.Flavour)
            {
                Top = StyleDimension.FromPercent(0.1f),
                Width = StyleDimension.FromPixelsAndPercent(-8, 1f),
                Height = StyleDimension.FromPixels(10),
                IsWrapped = true,
                MarginTop = 8
            };
            list.Add(_descFlavour);

            _itemContainer = new();
            SetItemList(bgData, true);
            list.Add(_itemContainer);

            _descText = new UIText(bgData.Description, 0.9f)
            {
                TextColor = Color.Gray,
                Top = StyleDimension.FromPercent(0.1f),
                Width = StyleDimension.FromPixelsAndPercent(-8, 1f),
                Height = StyleDimension.FromPixels(10),
                IsWrapped = true,
                MarginTop = 8
            };
            list.Add(_descText);
        }

        private void SetItemList(PlayerBackgroundData data, bool resetItemContainer = false)
        {
            var descItemHeight = StyleDimension.FromPixels(data.DisplayItemCount() == 0 ? 0 : 54 + 32 * (data.DisplayItemCount() / 12f));

            if (resetItemContainer) //Creates a new item container
            {
                _itemContainer = new UIElement()
                {
                    Width = StyleDimension.FromPercent(1),
                    Height = descItemHeight,
                    Top = StyleDimension.FromPixels(-20),
                    HAlign = 0.5f,
                };
            }
            else //Adjusts container to fit new height
            {
                _itemContainer.Height = descItemHeight;
                _itemContainer.HAlign = 0.5f;
                _itemContainer.Recalculate();
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

                _itemContainer.Append(slotBG);

                offset++;
                if (10 + offset * 32 > TotalWidth - BackgroundListWidth + 20) //ew hardcoding but nothing works
                {
                    offset = 0;
                    yOffset++;
                }
                id++;
            }
        }

        private static string GetStatsText(PlayerBackgroundData bgData)
        {
            string stats = $"[i:{ItemID.Heart}]{bgData.Misc.MaxLife} [i:{ItemID.Star}]{bgData.Misc.AdditionalMana}";
            string stars = string.Empty;

            for (int i = 0; i < 5; ++i)
                stars += bgData.Misc.Stars > i ? $"[i:{ModContent.ItemType<LightStar>()}]" : $"[i:{ModContent.ItemType<DimStar>()}]";

            return stars + "   " + stats;
        }

        /// <summary>Builds the origin list and buttons.</summary>
        private void BuildBackgroundSelections(UIPanel container, UIPanel descriptionContainer)
        {
            UIList allBGButtons = new UIList() //List of all background buttons
            {
                Width = StyleDimension.FromPercent(1),
                Height = StyleDimension.FromPixelsAndPercent(0, 1f),
                ListPadding = 4,
            };

            container.Append(allBGButtons);

            UIScrollbar scroll = new UIScrollbar() //Scrollbar for above list
            {
                HAlign = 1f,
                Height = StyleDimension.FromPixelsAndPercent(-8, 1f),
                Top = StyleDimension.FromPixels(4)
            };

            allBGButtons.SetScrollbar(scroll);
            container.Append(scroll);

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
                    Left = StyleDimension.FromPixels(-64),
                    Top = StyleDimension.FromPixels(8),
                    MarginRight = 4f,
                    MarginTop = 4f,
                };
                currentBGButton.SetColor(Color.Gray);

                currentBGButton.OnMouseDown += (UIMouseEvent evt, UIElement listeningElement) => //Click event
                {
                    BackgroundButtonClick(allBGButtons, item, currentBGButton);
                };

                currentBGButton.OnMouseOver += (UIMouseEvent evt, UIElement listeningElement) =>
                {
                    item.ApplyArmor(_player);
                    item.ApplyAccessories(_player, true);

                    currentBGButton.SetColor(new Color(220, 220, 220));
                };

                currentBGButton.OnMouseOut += (UIMouseEvent evt, UIElement listeningElement) =>
                {
                    var bgData = _player.GetModPlayer<PlayerBackgroundPlayer>().BackgroundData;

                    if (!_player.GetModPlayer<PlayerBackgroundPlayer>().HasBG() || bgData.Name != item.Name)
                    {
                        bgData.ApplyArmor(_player);
                        bgData.ApplyAccessories(_player, true);
                        _player.GetModPlayer<PlayerBackgroundPlayer>().SetBackground(bgData); //Sets the player's background.

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

        private PlayerBackgroundData BackgroundButtonClick(UIList allBGButtons, PlayerBackgroundData item, UIColoredImageButton currentBGButton)
        {
            PlayerBackgroundData useData = item.Name == "Random" ? Main.rand.Next(PlayerBackgroundDatabase.playerBackgroundDatas.SkipLast(1).ToList()) : item; //Hardcoding for random, sucks but eh
            _descFlavour.SetText(item.Name != "Random" ? useData.Flavour : item.Flavour); //Changes the UIText's value to use the bg's description
            _descText.SetText(item.Name != "Random" ? useData.Description : item.Description); //Changes the UIText's value to use the bg's description

            if (item.Name != "Random")
                _statsText.SetText(GetStatsText(useData));
            else
                _statsText.SetText($"[i:{ItemID.Heart}]??? [i:{ItemID.Star}]???");

            _itemContainer.RemoveAllChildren();

            if (item.Name != "Random")
                SetItemList(useData);

            item.ApplyArmor(_player);
            item.ApplyAccessories(_player, true);
            _player.GetModPlayer<PlayerBackgroundPlayer>().SetBackground(useData); //...and sets it.

            foreach (var button in allBGButtons.Where(x => x is UIColoredImageButton))
                (button as UIColoredImageButton).SetColor(Color.Gray);

            currentBGButton.SetColor(Color.White); //"Selects" the button visually.
            return item;
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
    }
}