using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CloverGunMod.Projectiles
{
	public class CloverBulletHoming : ModProjectile
	{
		//private float maxDetectRadius = 100 * 16f;
		private NPC closestNPC;

        public override void SetStaticDefaults()
		{
			ProjectileID.Sets.TrailCacheLength[Projectile.type] = 5; // The length of old position to be recorded
			ProjectileID.Sets.TrailingMode[Projectile.type] = 0; // The recording mode
		}

		public override void SetDefaults()
		{
			Projectile.width = 8; // The width of projectile hitbox
			Projectile.height = 8; // The height of projectile hitbox
			Projectile.aiStyle = 1; // The ai style of the projectile, please reference the source code of Terraria
			Projectile.friendly = true; // Can the projectile deal damage to enemies?
			Projectile.hostile = false; // Can the projectile deal damage to the player?
			Projectile.DamageType = DamageClass.Ranged; // Is the projectile shoot by a ranged weapon?
			Projectile.penetrate = 1; // How many monsters the projectile can penetrate. (OnTileCollide below also decrements penetrate for bounces as well)
			Projectile.timeLeft = 600; // The live time for the projectile (60 = 1 second, so 600 is 10 seconds)
			Projectile.alpha = 255; // The transparency of the projectile, 255 for completely transparent. (aiStyle 1 quickly fades the projectile in) Make sure to delete this if you aren't using an aiStyle that fades in. You'll wonder why your projectile is invisible.
			Projectile.light = 0.7f; // How much light emit around the projectile
			Projectile.ignoreWater = true; // Does the projectile's speed be influenced by water?
			Projectile.tileCollide = true; // Can the projectile collide with tiles?
			Projectile.extraUpdates = 1; // Set to above 0 if you want the projectile to update multiple time in a frame

			AIType = ProjectileID.Bullet; // Act exactly like default Bullet

			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = -1;
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			Projectile.Kill();
			return false;
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Main.instance.LoadProjectile(Projectile.type);
			Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;

			// Redraw the projectile with the color not influenced by light
			Vector2 drawOrigin = new Vector2(texture.Width * 0.5f, Projectile.height * 0.5f);
			for (int k = 0; k < Projectile.oldPos.Length; k++)
			{
				Vector2 drawPos = (Projectile.oldPos[k] - Main.screenPosition) + drawOrigin + new Vector2(0f, Projectile.gfxOffY);
				Color color = Projectile.GetAlpha(lightColor) * ((Projectile.oldPos.Length - k) / (float)Projectile.oldPos.Length);
				Main.EntitySpriteDraw(texture, drawPos, null, color, Projectile.rotation, drawOrigin, Projectile.scale, SpriteEffects.None, 0);
			}

			return true;
		}

		public override void OnKill(int timeLeft)
		{
			// This code and the similar code above in OnTileCollide spawn dust from the tiles collided with. SoundID.Item10 is the bounce sound you hear.
			Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
			SoundEngine.PlaySound(SoundID.Item10, Projectile.position);
		}

		public override void AI()
		{
			float rotationalSpeed = 0.09f;

			// Trying to find strongest NPC closest to the projectile
			if (closestNPC == null || !closestNPC.active)
			{
				closestNPC = FindNearest();
				return;
			}

			// Rotate current velocity towards the direction of the target
			// Use the SafeNormalize extension method to avoid NaNs returned by Vector2.Normalize when the vector is zero
			Vector2 current = Projectile.velocity.SafeNormalize(Vector2.Zero);
			Vector2 target = (closestNPC.Center - Projectile.Center).SafeNormalize(Vector2.Zero);
			int dir = GetRotationDirection(current.ToRotation(), target.ToRotation());
            Projectile.velocity = Projectile.velocity.RotatedBy(dir * rotationalSpeed);
            Projectile.rotation = Projectile.velocity.ToRotation() + (new Vector2(0, 1)).ToRotation();
        }

        public NPC FindNearest()
		{
			NPC closestNPC = null;

			// Using squared values in distance checks will let us skip square root calculations, drastically improving this method's speed.
			//int life = 0;
			//float sqrMaxDetectDistance = maxDetectDistance * maxDetectDistance;
			float savedAngle = 10f;
            //float sqrSavedDist = sqrMaxDetectDistance;

            //float angleFacing = Projectile.velocity.SafeNormalize(Vector2.Zero).ToRotation();

            // Loop through all NPCs
            for (int k = 0; k < Main.maxNPCs; k++)
			{
				NPC target = Main.npc[k];
				// Check if NPC able to be targeted. It means that NPC is
				// 1. active (alive)
				// 2. chaseable (e.g. not a cultist archer)
				// 3. max life bigger than 5 (e.g. not a critter)
				// 4. can take damage (e.g. moonlord core after all it's parts are downed)
				// 5. hostile (!friendly)
				// 6. not immortal (e.g. not a target dummy)
				if (target.CanBeChasedBy())
				{
					//float sqrDistanceToTarget = Vector2.DistanceSquared(target.Center, Projectile.Center);
					//float angleToTarget = (target.Center - Projectile.Center).SafeNormalize(Vector2.Zero).ToRotation();

					Vector2 toTarget = (target.Center - Projectile.Center);
					float length = Projectile.velocity.Length() * toTarget.Length();
                    float dAngle = (float) Math.Acos(Vector2.Dot(Projectile.velocity, toTarget) / length);

                    if (dAngle < savedAngle)
                    {
                        //sqrMaxDetectDistance = sqrDistanceToTarget;
                        savedAngle = dAngle;
                        closestNPC = target;
                    }

                    //if (sqrDistanceToTarget < sqrMaxDetectDistance)
                    //{
                    //}
                }
            }

            return closestNPC;

            //if (target.lifeMax > life)
            //{
            //	life = target.life;
            //	sqrSavedDist = sqrMaxDetectDistance;
            //	closestNPC = target;
            //}

            //else if (target.lifeMax == life)
            //{
            //                   // The DistanceSquared function returns a squared distance between 2 points, skipping relatively expensive square root calculations
            //                   float sqrDistanceToTarget = Vector2.DistanceSquared(target.Center, Projectile.Center);

            //                   // Check if it is within the radius
            //                   if (sqrDistanceToTarget < sqrSavedDist)
            //                   {
            //                       sqrSavedDist = sqrDistanceToTarget;
            //                       closestNPC = target;
            //                   }
            //               }
        }

		public int GetRotationDirection(float angle, float angleTo)
		{
			int dir = 1;

            if (angle < angleTo)
            {
				if (Math.Abs(angle - angleTo) < Math.PI) dir = 1;
                else dir = -1;
			}

            else
            {
                if (Math.Abs(angle - angleTo) < Math.PI) dir = -1;
                else dir = 1;
			}

			return dir;
        }
	}
}