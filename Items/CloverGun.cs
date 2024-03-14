using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CloverGunMod.Projectiles;
using Terraria.Utilities;
using System.Collections.Generic;

namespace CloverGunMod.Items
{
    public class CloverGun : ModItem
    {
		private int timer = 0;
        private int minUseTime = 40;
        private int level = 0;
        private int damage = 0;

		public override void SetDefaults() {
			// Common Properties
			Item.width = 42; // Hitbox width of the item.
			Item.height = 24; // Hitbox height of the item.
			Item.rare = ItemRarityID.Yellow; // The color that the item's name will be in-game.
			
			// Use Properties
			Item.useTime = 45; // The item's use time in ticks (60 ticks == 1 second.)
			Item.useAnimation = 45; // The length of the item's use animation in ticks (60 ticks == 1 second.)
			Item.useStyle = ItemUseStyleID.Shoot; // How you use the item (swinging, holding out, etc.)
			Item.autoReuse = true; // Whether or not you can hold click to automatically use it again.

			// Weapon Properties
			Item.DamageType = DamageClass.Ranged; // Sets the damage type to ranged.
			Item.knockBack = 8f; // Sets the item's knockback. Note that projectiles shot by this weapon will use its and the used ammunition's knockback added together.
            Item.noMelee = true; // So the item's animation doesn't do damage.

            // Gun Properties
			Item.shoot = ModContent.ProjectileType<CloverInvisBullet>(); // ProjectileID.Bullet; // For some reason, all the guns in the vanilla source have this.
			Item.shootSpeed = 0.001f; // The speed of the projectile (measured in pixels per frame.)
				// Actual shoot speed is set in CloverInvisBullet.cs

			// Other
			Item.channel = true;
			Item.value = 250000;	// sell price of 5 gold
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true; // Allow autouse for right-click
        }

        public override Vector2? HoldoutOffset() {
			return new Vector2(2f, 0f);
		}

        public override void HoldItem(Player player)
        {
            base.HoldItem(player);
            player.scope = true;
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            base.UseStyle(player, heldItemFrame);
            player.ChangeDir(Math.Sign(Main.MouseWorld.X - player.Center.X));
            player.itemRotation = (Main.MouseWorld - player.Center).ToRotation();
            if (player.direction == -1) player.itemRotation = (float) (player.itemRotation - Math.PI);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            //if (player.altFunctionUse == 2)
            //{
            //    FireHoming(player, source, damage);
            //}
            //else
            //{
            //    if (timer <= 0)
            //    {
            //        timer = 120;
            //        player.channel = true;
            //        return true;
            //    }
            //}

            if (timer <= 0)
            {
                timer = 120;
                player.channel = true;
                return true;
            }

            player.itemAnimation = timer;
            player.itemTime = timer;
            return false;
        }

        //public void FireHoming(Player player, EntitySource_ItemUse_WithAmmo source, int damage)
        //{
        //    float multiplier = 0.4f;
        //    float kbMultiplier = 0.75f;
        //    float shootSpeed = 16f;

        //    Vector2 dir = Vector2.Normalize(Main.MouseWorld - player.Center) * shootSpeed;
        //    int i = Projectile.NewProjectile(source, player.Center.X, player.Center.Y, dir.X, dir.Y, ModContent.ProjectileType<CloverBulletHoming>(), (int) Math.Round(damage * multiplier), (int) Math.Round(Item.knockBack * kbMultiplier), player.whoAmI);
        //    Projectile proj = Main.projectile[i];
        //    proj.CritChance = 0;

        //    // Play shoot sfx
        //    SoundStyle sound = new SoundStyle("CloverGunMod/Projectiles/CloverBulletShoot")
        //    {
        //        Volume = 0.27f,
        //        PitchVariance = 0.27f,
        //        MaxInstances = 0,
        //    };
        //    SoundEngine.PlaySound(sound, player.Center);
        //}

        //public override bool CanUseItem(Player player)
        //{
        //    if (player.altFunctionUse == 2)
        //    {
        //        Item.useTime = 30; // The item's use time in ticks (60 ticks == 1 second.)
        //        Item.useAnimation = 30; // The length of the item's use animation in ticks (60 ticks == 1 second.)
        //    }
        //    else
        //    {
        //        Item.useTime = 1; // The item's use time in ticks (60 ticks == 1 second.)
        //        Item.useAnimation = 1; // The length of the item's use animation in ticks (60 ticks == 1 second.)
        //    }

        //    return base.CanUseItem(player);
        //}

        public override void UpdateInventory(Player player)
        {
            // Manage timer
            timer = Math.Max(timer - 1, 0);

            if (!player.channel && timer >= 60)
            {
                timer = Math.Max(timer - (120 - minUseTime), 0);  // Remaining time
                player.itemAnimation = timer;
                player.itemTime = timer;
            }


            // Scale base damage with boss progression
            // This is balanced around Calamity damage values

            // Divide average DPS by 2.5 (1.25 shots/sec. * 2 crit)
            float divideBy = 2.5f;
            float goodModifiers = 1.3225f;	// Unreal gives +15% avg. for Damage and Speed (1.15 * 1.15)

            // Average DPS values with no equipment or modifiers
            // Not exact; some have been adjusted at various stages
            // Needs more testing with specific loadouts later
            int[] avgDps =	{	100, 120, 145, 170, 220,						// Pre-HM
								290, 330, 410, 500, 900, 1100, 1260, 1400,		// HM
								2000, 3500, 4600, 5800, 7200, 8000, 20000		// Post-ML
							};

			// Check next level requirements
			if (level < 20 && BossDownedTier(level))
			{
                damage = (int) (avgDps[level] * goodModifiers/divideBy);
                level++;
            }
            Item.damage = damage;

            // Apply Shroomite Mask 15% damage bonus
            if (player.armor[0].type == ItemID.ShroomiteMask)
            {
                Item.damage = (int) (Item.damage * 1.15f);
            }
        }

		public bool BossDownedTier(int tier)
		{
			switch (tier)
			{
                // Pre-HM
                case 0:     return true;
				case 1:		return NPC.downedBoss1 || BossDowned("crabulon");
                case 2:		return NPC.downedBoss2;
                case 3:		return NPC.downedQueenBee || BossDowned("hivemind") || BossDowned("perforator");
                case 4:		return NPC.downedBoss3 || NPC.downedDeerclops || BossDowned("slimegod");

                // HM
                case 5:		return Main.hardMode;
                case 6:		return NPC.downedQueenSlime || BossDowned("cryogen") || BossDowned("aquaticscourge");
                case 7:		return NPC.downedMechBossAny || BossDowned("brimstoneelemental");
                case 8:		return NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3;
				case 9:		return NPC.downedPlantBoss || BossDowned("calamitasclone") || BossDowned("anahitaleviathan") || BossDowned("astrumaureus");
				case 10:	return NPC.downedGolemBoss;
				case 11:	return NPC.downedFishron || NPC.downedEmpressOfLight || BossDowned("plaguebringergoliath") || BossDowned("ravager");
				case 12:	return NPC.downedAncientCultist || BossDowned("astrumdeus");

				// Post-ML
				case 13:	return NPC.downedMoonlord;
                case 14:	return BossDowned("dragonfolly") || BossDowned("guardians");
                case 15:	return BossDowned("providence");
                case 16:	return BossDowned("polterghast") || BossDowned("oldduke");
                case 17:	return BossDowned("devourerofgods");
                case 18:	return BossDowned("yharon");
				case 19:	return BossDowned("exomechs") && BossDowned("calamitas");
            }

			return false;
		}

        public string BossesToKill(int level)
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod mod))
            {
                switch (level)
                {
                    // Pre-HM
                    case 1:     return "Eye of Cthulhu, Crabulon";
                    case 2:     return "Eater of Worlds, Brain of Cthulhu";
                    case 3:     return "Queen Bee, The Hive Mind, The Perforators";
                    case 4:     return "Skeletron, Deerclops, The Slime God";
                    case 5:     return "Wall of Flesh";

                    // HM
                    case 6:     return "Queen Slime, Cryogen, Aquatic Scourge";
                    case 7:     return "Any Mech, Brimstone Elemental";
                    case 8:     return "All Mechs";
                    case 9:     return "Plantera, Calamitas Clone, Leviathan and Anahita, Astrum Aureus";
                    case 10:    return "Golem, Empress of Light";
                    case 11:    return "Duke Fishron, Plaguebringer Goliath, Ravager";
                    case 12:    return "Lunatic Cultist, Astrum Deus";
                    case 13:    return "Moon Lord";

                    // Post-ML
                    case 14:    return "Dragonfolly, Profaned Guardians";
                    case 15:    return "Providence";
                    case 16:    return "Polterghast, The Old Duke";
                    case 17:    return "The Devourer of Gods";
                    case 18:    return "Yharon";
                    case 19:    return "Exo Mechs AND Supreme Calamitas";
                }
            }

            else
            {
                switch (level)
                {
                    // Pre-HM
                    case 1:     return "Eye of Cthulhu";
                    case 2:     return "Eater of Worlds, Brain of Cthulhu";
                    case 3:     return "Queen Bee";
                    case 4:     return "Skeletron, Deerclops";
                    case 5:     return "Wall of Flesh";

                    // HM
                    case 6:     return "Queen Slime";
                    case 7:     return "Any Mech";
                    case 8:     return "All Mechs";
                    case 9:     return "Plantera";
                    case 10:    return "Golem, Empress of Light";
                    case 11:    return "Duke Fishron";
                    case 12:    return "Lunatic Cultist";
                    case 13:    return "Moon Lord";
                }
            }

            return "--";
        }

        public bool BossDowned(string name)
        {
            if (ModLoader.TryGetMod("CalamityMod", out Mod mod))
            {
                object call = mod.Call("GetBossDowned", name);
                if (call is bool check) return check;
            }
            return false;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // Level
			tooltips.Insert(1, new TooltipLine(Mod, "Tooltip0", $"LV {level}") { OverrideColor = Color.Red });

            int line = 2;

            // Boss rush flavor text
			if (BossDowned("bossrush")) {
				tooltips.Insert(line, new TooltipLine(Mod, "Tooltip1", "Determination."));
                line++;
			}

            // Next bosses to level up
            tooltips.Insert(line, new TooltipLine(Mod, "Tooltip2", $"Next LV (kill any): {BossesToKill(level)}") { OverrideColor = Color.Yellow });
        }
		
		public override void ModifyWeaponCrit(Player player, ref float crit)
		{
			crit = 0;
		}

        public override bool? PrefixChance(int pre, UnifiedRandom rand)
        {
            return false;
        }

        public override void AddRecipes()
        {
            short[] guns = { ItemID.Revolver, ItemID.TheUndertaker, ItemID.Handgun };
            short[] bars = { ItemID.GoldBar, ItemID.PlatinumBar };

            for (int g = 0; g < 3; g++)
            {
                for (int b = 0; b < 2; b++)
                {
                    Recipe recipe = Recipe.Create(ModContent.ItemType<CloverGun>());
                    recipe.AddIngredient(guns[g]);
                    recipe.AddIngredient(bars[b], 6);
                    recipe.AddIngredient(ItemID.LifeCrystal);
                    recipe.AddTile(TileID.Anvils);
                    recipe.Register();
                }
            }
        }

		//public override bool AltFunctionUse(Player player)
		//{
		//	return true;
		//}
	}
}