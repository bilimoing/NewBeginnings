﻿using Microsoft.Xna.Framework;
using Terraria.ModLoader;
using Terraria;

namespace NewBeginnings.Content.Items
{
    internal class DeprivedLantern : ModItem
    {
        public override void SetStaticDefaults() => Tooltip.SetDefault("Lights up when placed in inventory\nInfinite duration");

        public override void SetDefaults()
        {
            Item.Size = new Vector2(18, 36);
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.value = Item.buyPrice(gold: 2);
        }

        public override void UpdateInventory(Player player) => Lighting.AddLight(player.Center + new Vector2(0, 8), new Vector3(0.4f, 0.48f, 0.48f));
    }
}