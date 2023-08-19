﻿using NewBeginnings.Common.PlayerBackgrounds;
using System;
using Terraria;
using Terraria.GameContent.UI.States;

namespace NewBeginnings.Common.Edits
{
    internal class CharCreationHijackSaveDetour
    {
        private static bool FirstSave = false;

        public static void Load()
        {
            On_UICharacterCreation.FinishCreatingCharacter += UICharacterCreation_FinishCreatingCharacter;
            Terraria.IO.On_PlayerFileData.CreateAndSave += PlayerFileData_CreateAndSave;

            MrPlaguesCompat.AddCharCreationDetour();
        }

        internal static void MrPlaguesHookFinishCharacter(Action<object> orig, object self) //Thanks mrplagues
        {
            FirstSave = true;
            orig(self);
            FirstSave = false;
        }

        private static void UICharacterCreation_FinishCreatingCharacter(On_UICharacterCreation.orig_FinishCreatingCharacter orig, UICharacterCreation self)
        {
            FirstSave = true;
            orig(self);
            FirstSave = false;
        }

        private static Terraria.IO.PlayerFileData PlayerFileData_CreateAndSave(Terraria.IO.On_PlayerFileData.orig_CreateAndSave orig, Player player)
        {
            if (FirstSave)
            {
                PlayerBackgroundData data = player.GetModPlayer<PlayerBackgroundPlayer>().BackgroundData;

                data.ApplyToPlayer(player);
                data.Delegates.ModifyPlayerCreation?.Invoke(player);
            }

            return orig(player);
        }
    }
}
