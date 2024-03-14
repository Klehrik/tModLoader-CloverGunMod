using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CloverGunMod.Projectiles
{
	public class CloverInvisBullet : ModProjectile
	{
		public override void SetStaticDefaults() {
			ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5; // The length of old position to be recorded
			ProjectileID.Sets.TrailingMode[Projectile.type] = 0; // The recording mode
		}

		public override void SetDefaults() {
            Projectile.width = 0; // The width of projectile hitbox
            Projectile.height = 0;
            Projectile.friendly = false; // Can the projectile deal damage to enemies?
			Projectile.hostile = false; // Can the projectile deal damage to the player?
			Projectile.penetrate = -1; // How many monsters the projectile can penetrate. (OnTileCollide below also decrements penetrate for bounces as well)
			Projectile.timeLeft = 1000; // The live time for the projectile (60 = 1 second, so 600 is 10 seconds)
			Projectile.alpha = 255; // The transparency of the projectile, 255 for completely transparent. (aiStyle 1 quickly fades the projectile in) Make sure to delete this if you aren't using an aiStyle that fades in. You'll wonder why your projectile is invisible.
			Projectile.ignoreWater = true; // Does the projectile's speed be influenced by water?
			Projectile.tileCollide = false; // Can the projectile collide with tiles?
			Projectile.extraUpdates = 1; // Set to above 0 if you want the projectile to update multiple time in a frame
        }

		private float ChargeTime {
			get => Projectile.ai[0];
			set => Projectile.ai[0] = value;
		}

		public override void AI() {
			Player owner = Main.player[Projectile.owner];
            Vector2 ownerCenter = new Vector2(owner.position.X + owner.width / 2, owner.position.Y + owner.height / 2);
			bool playerHasDaawnlight = false;

			// Check for Daawnlight Spirit Origin
			// If so, prevent guaranteed crits to non-Weak Point areas
			if (ModLoader.TryGetMod("CalamityMod", out Mod mod))
			{
				for (int i = 3; i <= 9; i++)
				{
					if (owner.armor[i].type == mod.Find<ModItem>("DaawnlightSpiritOrigin").Type)
					{
						playerHasDaawnlight = true;
						break;
                    }
				}
			}

            // Charge
            if (Projectile.ai[1] == 0f) {
				if (!owner.channel || ChargeTime >= 120) {
					Projectile.ai[1] = 1f;
				}
				ChargeTime++;

                return;
			}

			if (ChargeTime < 120)
			{
				int damage = Projectile.damage;
				if (ChargeTime < 50) damage = (int) Math.Round(damage * 0.7);

				// Create actual projectile moving towards the mouse
				float shootSpeed = 28f;

				Vector2 dir = Vector2.Normalize(Main.MouseWorld - ownerCenter) * shootSpeed;
				int j = Projectile.NewProjectile(Projectile.GetSource_NaturalSpawn(), ownerCenter.X, ownerCenter.Y, dir.X, dir.Y, ModContent.ProjectileType<CloverBullet>(), damage, Projectile.knockBack, owner.whoAmI);
				Projectile proj = Main.projectile[j];

                // Set crit and penetration values
                if (ChargeTime >= 91)
				{
					proj.penetrate = 4;
					if (!playerHasDaawnlight) proj.CritChance = 1000;
				}
				else if (ChargeTime >= 50)
				{
					proj.penetrate = 2;
				}

                // Play shoot sfx
                SoundStyle sound = new SoundStyle("CloverGunMod/Projectiles/CloverBulletShoot") {
					Volume = 0.27f,
					PitchVariance = 0.27f,
					MaxInstances = 0,
				};
				SoundEngine.PlaySound(sound, owner.Center);
            }

			else
			{
                Projectile.NewProjectile(Projectile.GetSource_NaturalSpawn(), ownerCenter.X, ownerCenter.Y - 80, 0, -0.7f, ModContent.ProjectileType<CloverMiss>(), 0, 0, owner.whoAmI);

                // Play miss sfx
                SoundStyle sound = new SoundStyle("CloverGunMod/Projectiles/CloverBulletMiss")
                {
                    Volume = 0.5f,
                    MaxInstances = 0,
                };
                SoundEngine.PlaySound(sound, owner.Center);
            }

            Projectile.Kill();
		}
        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            Vector2 ownerCenter = new Vector2(owner.position.X + owner.width / 2, owner.position.Y + owner.height / 2);

			float scale = 1.75f;
			int offset = 100;

            // Draw Crosshair
            Texture2D texture = ModContent.Request<Texture2D>("CloverGunMod/Items/Crosshair").Value;
            Vector2 drawOrigin = new Vector2(texture.Width * 0.5f, texture.Height * 0.5f);
            Vector2 drawPos = (ownerCenter - Main.screenPosition);
            drawPos.Y -= offset;
            Main.EntitySpriteDraw(texture, drawPos, null, Color.White, Projectile.rotation, drawOrigin, scale, SpriteEffects.None, 0);

            // Draw charge (circle primitive)
            Texture2D texture2 = ModContent.Request<Texture2D>("CloverGunMod/Items/CrosshairCharge").Value;
            Vector2 drawOrigin2 = new Vector2(texture2.Width * 0.5f, texture2.Height * 0.5f);
            Vector2 drawPos2 = (ownerCenter - Main.screenPosition);
            drawPos2.Y -= offset;
            Main.EntitySpriteDraw(texture2, drawPos2, null, Color.White, Projectile.rotation, drawOrigin2, (120 - ChargeTime)/120 * scale, SpriteEffects.None, 0);

            return false;
        }
    }
}