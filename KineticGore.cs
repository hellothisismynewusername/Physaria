using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.Audio;

namespace KineticGore
{
	public class KineticGore : Mod
	{
	}

	public class KineticGoreConfig : ModConfig
	{
		public override ConfigScope Mode => ConfigScope.ClientSide;

		[Header("Compatibility")]
		[DefaultValue(false)]
		public bool EnableSwingModCompatibility { get; set; }

		[Header("QualityOfLife")]
		[DefaultValue(false)]
		public bool ArrowsStickToEnemies { get; set; }

		[DefaultValue(false)]
		public bool EnableHeadBanging { get; set; }

		[Header("GeneralSettings")]
		[DefaultValue(1200)]
		[Range(60, 3600)]
		public int CorpseDuration { get; set; }

		[DefaultValue(30)]
		[Range(0, 100)]
		public int DismembermentChance { get; set; }

		[DefaultValue(false)]
		[Label("Legacy Ragdoll Physics")]
		[Tooltip("Reverts physics to the older, spinnier version.")]
		public bool LegacyRagdollPhysics { get; set; }

		[DefaultValue(true)]
		[Label("Enable Ragdoll Collision")]
		[Tooltip("Allows ragdolls to stack and collide with each other.")]
		public bool EnableRagdollCollision { get; set; }

		[Header("BloodSettings")]
		[DefaultValue(0.71f)]
		[Range(0f, 5f)]
		[Tooltip("Controls the amount of red blood particles (dust).")]
		public float BloodAmountMultiplier { get; set; }

		[DefaultValue(0.42f)]
		[Range(0f, 5f)]
		[Tooltip("Controls the amount of solid body parts (gibs) from hits.")]
		public float GibAmountMultiplier { get; set; }

		[DefaultValue(0.69f)]
		[Range(0.1f, 2.0f)]
		public float BloodScale { get; set; }

		[DefaultValue(1.31f)]
		[Range(0.1f, 3.0f)]
		public float BloodVelocity { get; set; }

		[DefaultValue(true)]
		public bool EnableStickyBlood { get; set; }

		[DefaultValue(60)]
		[Range(30, 600)]
		public int StickyBloodDuration { get; set; }

		[DefaultValue(150)]
		[Range(0, 600)]
		public int BleedDuration { get; set; }

		[Header("PhysicsSettings")]
		[DefaultValue(0.35f)]
		[Range(0.05f, 2.0f)]
		public float Gravity { get; set; }

		[DefaultValue(0.3f)]
		[Range(0f, 1.5f)]
		public float Bounciness { get; set; }

		[DefaultValue(0.78f)]
		[Range(0.1f, 1.0f)]
		public float Friction { get; set; }

		[DefaultValue(0.79f)]
		[Range(0.1f, 5.0f)]
		public float KnockbackMultiplier { get; set; }

		[Header("HeadPhysics")]
		[DefaultValue(1.0f)]
		[Range(0f, 5f)]
		public float HeadBobIntensity { get; set; }

		[DefaultValue(true)]
		public bool EnableHeadFlinching { get; set; }

		[DefaultValue(false)]
		public bool ReverseHeadFlinch { get; set; }

		[DefaultValue(0.08f)]
		[Range(0.01f, 0.5f)]
		public float HeadSpringStiffness { get; set; }

		[DefaultValue(0.85f)]
		[Range(0.1f, 0.99f)]
		public float HeadSpringDamping { get; set; }

		[Header("SlimePhysics")]
		[DefaultValue(1.0f)]
		[Range(0f, 5f)]
		public float SlimeJiggleIntensity { get; set; }

		[DefaultValue(0.1f)]
		[Range(0.01f, 0.5f)]
		public float SlimeSpringStiffness { get; set; }

		[DefaultValue(0.85f)]
		[Range(0.1f, 0.99f)]
		public float SlimeSpringDamping { get; set; }

		[Header("VisualAdjustments")]
		[DefaultValue(-4f)]
		[Range(-20f, 20f)]
		[Tooltip("Adjust this if Humanoid NPCs are floating or sinking.")]
		public float HumanoidVerticalOffset { get; set; }
	}

	public class StickyBloodSystem : ModSystem
	{
		public override void PostUpdateDusts()
		{
			var config = ModContent.GetInstance<KineticGoreConfig>();
			if (!config.EnableStickyBlood) return;

			for (int i = 0; i < Main.maxDust; i++)
			{
				if (Main.dust[i].active && Main.dust[i].type == DustID.Blood)
				{
					ref Dust dust = ref Main.dust[i];
					ProcessBlood(ref dust, config);
				}
			}
		}

		private void ProcessBlood(ref Dust dust, KineticGoreConfig config)
		{
			if (dust.customData == null) dust.customData = 150;

			int stateData = (int)dust.customData;

			if (stateData <= 150)
			{
				stateData--;
				dust.customData = stateData;

				dust.alpha = 0;

				if (dust.scale < config.BloodScale) dust.scale = config.BloodScale;
				if (dust.scale > config.BloodScale) dust.scale = config.BloodScale;

				if (stateData <= 0)
				{
					dust.active = false;
					return;
				}

				bool hitGround = false;
				if (Collision.SolidCollision(dust.position, 2, 2))
				{
					hitGround = true;
				}
				else if (Math.Abs(dust.velocity.Y) < 0.1f)
				{
					dust.velocity.X *= 0.5f;
					if (Math.Abs(dust.velocity.X) < 0.2f)
					{
						if (Collision.SolidCollision(dust.position + new Vector2(0, 4), 2, 2)) hitGround = true;
					}
				}

				if (hitGround)
				{
					dust.customData = 10000 + config.StickyBloodDuration;
					dust.velocity = Vector2.Zero;
					dust.noGravity = true;
					dust.alpha = 0;
				}
			}
			else
			{
				dust.velocity = Vector2.Zero;
				dust.noGravity = true;
				dust.rotation = 0f;

				stateData--;
				dust.customData = stateData;

				int ticksLeft = stateData - 10000;
				int fadeTime = config.StickyBloodDuration;

				if (ticksLeft > 0)
				{
					float progress = 1f - ((float)ticksLeft / (float)fadeTime);
					dust.alpha = (int)(255 * progress);
					dust.scale = config.BloodScale;
				}
				else
				{
					dust.active = false;
					dust.customData = null;
				}
			}
		}
	}

	public class StuckArrow : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_1";

		public override void SetDefaults()
		{
			Projectile.width = 10;
			Projectile.height = 10;
			Projectile.aiStyle = -1;
			Projectile.friendly = false;
			Projectile.hostile = false;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 90;
			Projectile.alpha = 0;
			Projectile.ignoreWater = true;
			Projectile.tileCollide = false;
		}

		public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
		{
			behindNPCs.Add(index);
		}

		public override void AI()
		{
			if (Projectile.localAI[0] == 0)
			{
				Projectile.localAI[0] = 1;
				Entity target = null;

				if (Projectile.ai[1] == 0)
				{
					int npcIndex = (int)Projectile.ai[0];
					if (Main.npc.IndexInRange(npcIndex) && Main.npc[npcIndex].active)
						target = Main.npc[npcIndex];
				}
				else
				{
					int projIndex = (int)Projectile.ai[0];
					if (Main.projectile.IndexInRange(projIndex) && Main.projectile[projIndex].active)
						target = Main.projectile[projIndex];
				}

				if (target != null && target.active)
				{
					Vector2 offset = Projectile.Center - target.Center;
					float targetRot = (Projectile.ai[1] == 0) ? ((NPC)target).rotation : ((Projectile)target).rotation;
					offset = offset.RotatedBy(-targetRot);
					Projectile.velocity = offset;
					Projectile.localAI[1] = Projectile.rotation - targetRot;
				}
				else
				{
					Projectile.Kill();
				}
			}
			else
			{
				Entity target = null;
				if (Projectile.ai[1] == 0)
				{
					int npcIndex = (int)Projectile.ai[0];
					if (Main.npc.IndexInRange(npcIndex)) target = Main.npc[npcIndex];
				}
				else
				{
					int projIndex = (int)Projectile.ai[0];
					if (Main.projectile.IndexInRange(projIndex)) target = Main.projectile[projIndex];
				}

				if (target != null && target.active)
				{
					float targetRot = (Projectile.ai[1] == 0) ? ((NPC)target).rotation : ((Projectile)target).rotation;
					Vector2 offset = Projectile.velocity;
					Projectile.Center = target.Center + offset.RotatedBy(targetRot);
					Projectile.rotation = targetRot + Projectile.localAI[1];

					if (Projectile.timeLeft < 20) Projectile.alpha += 12;
				}
				else
				{
					Projectile.Kill();
				}
			}
		}

		public override bool PreDraw(ref Color lightColor)
		{
			int originalType = (int)Projectile.ai[2];
			if (originalType <= 0) originalType = ProjectileID.WoodenArrowFriendly;

			Main.instance.LoadProjectile(originalType);
			Texture2D tex = TextureAssets.Projectile[originalType].Value;
			Rectangle rect = new Rectangle(0, 0, tex.Width, tex.Height);
			Vector2 origin = rect.Size() / 2f;

			Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, rect, lightColor * ((255 - Projectile.alpha) / 255f), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
			return false;
		}
	}

	public class RealisticDeathGlobalNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public float liveHeadRotation = 0f;
		public float liveHeadAngularVel = 0f;
		public float liveHeadWobble = 0f;
		public float squashCompression = 1f;

		private bool IsHumanoid(NPC npc) => npc.aiStyle == 3 || npc.townNPC;
		private bool IsSlime(NPC npc) => npc.aiStyle == 1 || npc.type == NPCID.BlueSlime || npc.type == NPCID.GreenSlime;
		private bool ShouldIgnore(NPC npc) => npc.boss || npc.lifeMax < 5 || (npc.friendly && !npc.townNPC);

		private bool IsMusicPlaying(NPC npc)
		{
			for (int i = 0; i < Main.maxPlayers; i++)
			{
				Player p = Main.player[i];
				if (p.active && !p.dead && Vector2.Distance(npc.Center, p.Center) < 600f)
				{
					if (p.itemAnimation > 0)
					{
						int type = p.HeldItem.type;
						if (type == ItemID.MagicalHarp ||
							type == ItemID.TheAxe ||
							type == ItemID.IvyGuitar)
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		private Player GetNearestMusician(NPC npc)
		{
			Player nearest = null;
			float dist = 600f;
			for (int i = 0; i < Main.maxPlayers; i++)
			{
				Player p = Main.player[i];
				if (p.active && !p.dead)
				{
					float d = Vector2.Distance(npc.Center, p.Center);
					if (d < dist)
					{
						if (p.itemAnimation > 0)
						{
							int type = p.HeldItem.type;
							if (type == ItemID.MagicalHarp ||
								type == ItemID.TheAxe ||
								type == ItemID.IvyGuitar ||
								type == ItemID.Harp)
							{
								nearest = p;
								dist = d;
							}
						}
					}
				}
			}
			return nearest;
		}

		public override void PostAI(NPC npc)
		{
			if (ShouldIgnore(npc)) return;
			var config = ModContent.GetInstance<KineticGoreConfig>();

			if (IsHumanoid(npc))
			{
				float targetRot = npc.rotation - (npc.velocity.X * 0.1f);

				if (Math.Abs(npc.velocity.X) > 0.1f)
					targetRot += (float)Math.Sin(Main.GameUpdateCount * 0.35f) * (npc.velocity.X * 0.12f * config.HeadBobIntensity);

				if (Main.netMode != NetmodeID.Server && config.EnableHeadBanging)
				{
					Player musician = GetNearestMusician(npc);
					if (musician != null)
					{
						int dir = (musician.Center.X > npc.Center.X) ? 1 : -1;
						npc.spriteDirection = dir;
						float wave = (float)Math.Sin(Main.GameUpdateCount * 0.2f);
						float bang = wave * (0.6f * config.HeadBobIntensity) * -npc.spriteDirection;
						targetRot += bang;
					}
				}

				float diff = targetRot - liveHeadRotation;
				float force = diff * config.HeadSpringStiffness;
				liveHeadAngularVel += force;
				liveHeadAngularVel *= config.HeadSpringDamping;
				liveHeadRotation += liveHeadAngularVel;
				liveHeadRotation = MathHelper.Clamp(liveHeadRotation, -0.8f, 0.8f);
			}

			if (IsSlime(npc))
			{
				float strength = 0.04f * config.SlimeJiggleIntensity;
				float targetSquash = 1.0f + (Math.Abs(npc.velocity.Y) * strength);
				if (targetSquash > 1.0f + (0.3f * config.SlimeJiggleIntensity))
					targetSquash = 1.0f + (0.3f * config.SlimeJiggleIntensity);
				squashCompression = MathHelper.Lerp(squashCompression, targetSquash, 0.1f);
				if (npc.velocity.Y == 0 && squashCompression > 1.1f) squashCompression = 0.7f;
				squashCompression = MathHelper.Lerp(squashCompression, 1f, 0.05f);
			}
		}

		public override void HitEffect(NPC npc, NPC.HitInfo hit)
		{
			// Safety: If we generated this hit, stop recursion
			if (npc.life == -999) return;

			var config = ModContent.GetInstance<KineticGoreConfig>();

			if (IsHumanoid(npc) && config.EnableHeadFlinching)
			{
				float flinchDir = config.ReverseHeadFlinch ? -0.4f : 0.4f;
				float impulse = hit.HitDirection * flinchDir * config.HeadBobIntensity;
				liveHeadAngularVel += MathHelper.Clamp(impulse, -0.5f, 0.5f);
			}

			// --- STANDARD BLOOD DUST (CUSTOM & VANILLA REPLACEMENT) ---
			// We spawm our OWN blood based on the config.
			if (npc.life > 0 || npc.life <= 0)
			{
				// Increased base amount logic for better responsiveness to config
				float damageFactor = hit.Damage;
				if (damageFactor < 5) damageFactor = 5; // Min floor for base calc

				int baseAmount = (int)(damageFactor / 2.5f);
				if (baseAmount > 50) baseAmount = 50;
				if (baseAmount < 4) baseAmount = 4;

				// Strictly controlled by BloodAmountMultiplier
				int dustAmount = (int)(baseAmount * config.BloodAmountMultiplier);
				float hitOffsetX = (npc.width / 2) * hit.HitDirection;
				Vector2 hitPoint = npc.Center + new Vector2(hitOffsetX, Main.rand.Next(-npc.height/4, npc.height/4));

				for (int i = 0; i < dustAmount; i++)
				{
					Vector2 sprayVelocity = new Vector2(hit.HitDirection * -1 * Main.rand.NextFloat(2f, 6f), Main.rand.NextFloat(-4f, 4f));
					sprayVelocity *= config.BloodVelocity;
					Dust d = Dust.NewDustPerfect(hitPoint, DustID.Blood, sprayVelocity, 100, default, config.BloodScale);
				}
			}

			// --- VANILLA GIBS ON HIT (ALIVE) ---
			if (npc.life > 0 && !ShouldIgnore(npc))
			{
				if (Main.rand.Next(100) < 30) // 30% chance
				{
					bool[] activeGoreSnapshot = new bool[Main.maxGore];
					for(int i=0; i<Main.maxGore; i++) activeGoreSnapshot[i] = Main.gore[i].active;

					// We snapshot dusts to KILL vanilla blood that spawns during this fake hit
					bool[] activeDustSnapshot = new bool[Main.maxDust];
					for(int i=0; i<Main.maxDust; i++) activeDustSnapshot[i] = Main.dust[i].active;

					int savedLife = npc.life;
					npc.life = -999;
					npc.HitEffect(hit);
					npc.life = savedLife;

					// --- KILL VANILLA BLOOD ---
					// Any blood dust spawned by the vanilla HitEffect call above is unwanted
					// because we already spawned our own custom blood based on the config above.
					for(int i=0; i<Main.maxDust; i++)
					{
						if (Main.dust[i].active && !activeDustSnapshot[i])
						{
							if (Main.dust[i].type == DustID.Blood)
							{
								Main.dust[i].active = false; // DELETE IT
							}
						}
					}

					// --- PROCESS GIBS ---
					List<int> newGores = new List<int>();
					for(int i=0; i<Main.maxGore; i++)
					{
						if (Main.gore[i].active && !activeGoreSnapshot[i])
						{
							if (Main.gore[i].timeLeft >= 599) newGores.Add(i);
						}
					}

					if (newGores.Count > 0)
					{
						// Sort by Size (Smallest first)
						newGores.Sort((a, b) => (Main.gore[a].Width * Main.gore[a].Height).CompareTo(Main.gore[b].Width * Main.gore[b].Height));

						// Use NEW GibAmountMultiplier
						int baseCount = Main.rand.Next(1, 4);
						int amountToKeep = (int)(baseCount * config.GibAmountMultiplier);
						if (amountToKeep < 1 && config.GibAmountMultiplier > 0.01f && Main.rand.NextBool()) amountToKeep = 1;

						for (int i = 0; i < newGores.Count; i++)
						{
							int goreIndex = newGores[i];
							if (i < amountToKeep)
							{
								// Dynamic Directional Spray
								Vector2 sprayDir = new Vector2(hit.HitDirection * 3f, -3f);
								Main.gore[goreIndex].velocity = sprayDir + Main.rand.NextVector2Circular(1.5f, 1.5f);
							}
							else
							{
								Main.gore[goreIndex].active = false;
							}
						}
					}
				}
			}
			// ---------------------------------------------

			if (npc.life <= 0 && !ShouldIgnore(npc))
			{
				for (int i = 0; i < Main.maxGore; i++)
				{
					Gore g = Main.gore[i];
					if (g.active && g.timeLeft >= 599)
					{
						if (Vector2.Distance(g.position, npc.position) < Math.Max(npc.width, npc.height) * 1.5f)
							g.active = false;
					}
				}
			}
		}

		public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
		{
			if (ShouldIgnore(npc)) return;

			var config = ModContent.GetInstance<KineticGoreConfig>();
			if (!config.ArrowsStickToEnemies) return;

			if (projectile.arrow && Main.netMode != NetmodeID.MultiplayerClient)
			{
				Vector2 stickPos = projectile.Center + projectile.velocity.SafeNormalize(Vector2.Zero) * 12f;
				stickPos += Main.rand.NextVector2Circular(3, 3);
				int pIndex = Projectile.NewProjectile(npc.GetSource_OnHit(npc), stickPos, Vector2.Zero, ModContent.ProjectileType<StuckArrow>(), 0, 0, Main.myPlayer, npc.whoAmI, 0, projectile.type);
				Main.projectile[pIndex].rotation = projectile.rotation + Main.rand.NextFloat(-0.1f, 0.1f);
			}
		}

		public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			if (ShouldIgnore(npc)) return true;

			if (IsSlime(npc) && npc.color != default)
				drawColor = npc.color.MultiplyRGBA(drawColor);

			var config = ModContent.GetInstance<KineticGoreConfig>();

			Vector2 drawPos = npc.Center - screenPos;
			drawPos.Y += npc.gfxOffY;

			Texture2D mainTex = TextureAssets.Npc[npc.type].Value;
			int frameH = Main.npcFrameCount[npc.type] > 0 ? mainTex.Height / Main.npcFrameCount[npc.type] : mainTex.Height;

			if (IsSlime(npc))
			{
				Rectangle fullSrc = npc.frame;
				Vector2 origin = fullSrc.Size() / 2f;

				float scaleY = squashCompression;
				float scaleX = 1f / squashCompression;

				float heightChange = (fullSrc.Height * scaleY) - fullSrc.Height;
				drawPos.Y -= heightChange * 0.5f;

				spriteBatch.Draw(mainTex, drawPos, fullSrc, drawColor, npc.rotation, origin, npc.scale * new Vector2(scaleX, scaleY), npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None, 0f);
				return false;
			}

			if (IsHumanoid(npc))
			{
				// FIXED: Only offset Town NPCs
				if (npc.townNPC)
				{
					drawPos.Y += config.HumanoidVerticalOffset;
				}

				Rectangle frame = npc.frame;
				int splitY = (int)(frameH * 0.38f);
				int overlap = 2;

				Rectangle headSrc = new Rectangle(frame.X, frame.Y, frame.Width, splitY + overlap);
				Rectangle bodySrc = new Rectangle(frame.X, frame.Y + splitY - overlap, frame.Width, frameH - splitY + overlap);

				float centerFrameY = frameH / 2f;
				float bodyTopY = splitY - overlap;
				Vector2 bodyOrigin = new Vector2(frame.Width / 2f, centerFrameY - bodyTopY);

				Vector2 headOrigin = new Vector2(frame.Width / 2f, splitY);

				SpriteEffects effects = npc.spriteDirection == 1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

				spriteBatch.Draw(mainTex, drawPos, bodySrc, drawColor, npc.rotation, bodyOrigin, npc.scale, effects, 0f);

				Vector2 neckOffset = new Vector2(0, splitY - centerFrameY);
				Vector2 neckPos = drawPos + neckOffset.RotatedBy(npc.rotation);

				spriteBatch.Draw(mainTex, neckPos, headSrc, drawColor, liveHeadRotation, headOrigin, npc.scale, effects, 0f);
				return false;
			}
			return true;
		}

		public override bool PreKill(NPC npc)
		{
			if (ShouldIgnore(npc)) return true;

			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				var config = ModContent.GetInstance<KineticGoreConfig>();
				int projType = ModContent.ProjectileType<PhysicsCorpse>();
				float isFlying = npc.noGravity ? 1f : 0f;
				int dismembermentState = 0;

				float packedColor = 0f;
				if (npc.color != default(Color))
				{
					int rgb = (npc.color.R << 16) | (npc.color.G << 8) | npc.color.B;
					packedColor = BitConverter.Int32BitsToSingle(rgb);
				}

				if (IsHumanoid(npc) && Main.rand.Next(100) < config.DismembermentChance)
				{
					// FORCE CLEAN SEPARATION (Headless Body)
					dismembermentState = 1;

					// ALWAYS spawn the head projectile
					Vector2 headVelocity = npc.velocity * config.KnockbackMultiplier + Main.rand.NextVector2Circular(3, 3);
					float headAI = (isFlying) + (30f);
					Projectile.NewProjectile(npc.GetSource_Death(), npc.Center, headVelocity, projType, 0, 0, Main.myPlayer, (float)npc.whoAmI, headAI, packedColor);

					// Only spawn blood particles, no gore chunks
					int amount = (int)(15 * config.BloodAmountMultiplier);
					for(int i=0; i<amount; i++)
					{
						Vector2 vel = (npc.velocity * 0.5f) + Main.rand.NextVector2Circular(4, 4);
						vel *= config.BloodVelocity;
						Dust d = Dust.NewDustPerfect(npc.Center, DustID.Blood, vel, 0, default, config.BloodScale);
					}
				}

				Vector2 launchVelocity = npc.velocity * config.KnockbackMultiplier;
				if (launchVelocity.Length() < 2f) launchVelocity = new Vector2(npc.direction * -2f, -2.5f);

				float packedAI = isFlying + (dismembermentState * 10f);
				Projectile.NewProjectile(npc.GetSource_Death(), npc.Center, launchVelocity, projType, 0, 0, Main.myPlayer, (float)npc.whoAmI, packedAI, packedColor);
			}
			return true;
		}
	}

	public class PhysicsCorpse : ModProjectile
	{
		public override string Texture => "Terraria/Images/Projectile_0";

		private int npcType;
		private int netID;
		private float npcScale;

		private bool isFlyingEntity => (Projectile.ai[1] % 10) == 1f;

		private int dismemberType
		{
			get => (int)(Projectile.ai[1] / 10f);
			set => Projectile.ai[1] = (isFlyingEntity ? 1f : 0f) + (value * 10f);
		}

		private float angularMomentum = 0f;
		private float headRotation = 0f;
		private float headAngularVel = 0f;

		private int integrity = 100;
		private int hitCooldown = 0;
		private float squashCompression = 1f;
		private float squashVelocity = 0f;

		private int bleedTimer
		{
			get => (int)Projectile.localAI[1];
			set => Projectile.localAI[1] = value;
		}

		private bool IsHumanoid()
		{
			NPC sample = ContentSamples.NpcsByNetId[npcType];
			return sample.aiStyle == 3 || sample.townNPC;
		}

		private bool IsSlime()
		{
			NPC sample = ContentSamples.NpcsByNetId[npcType];
			return sample.aiStyle == 1 || npcType == NPCID.BlueSlime || npcType == NPCID.GreenSlime;
		}

		public override void SetStaticDefaults()
		{
			Main.projFrames[Projectile.type] = 1;
		}

		public override void SetDefaults()
		{
			Projectile.width = 14;
			Projectile.height = 14;
			Projectile.aiStyle = -1;
			Projectile.friendly = true;
			Projectile.hostile = false;
			Projectile.penetrate = -1;
			Projectile.timeLeft = 1200;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = false;
		}

		public override void OnSpawn(IEntitySource source)
		{
            this.npcType = (int) Main.npc[(int) base.Projectile.ai[0]].type;
            this.netID = Main.npc[(int) base.Projectile.ai[0]].netID;
            this.npcScale = Main.npc[(int) base.Projectile.ai[0]].scale;
        }

		public override void AI()
		{
			var config = ModContent.GetInstance<KineticGoreConfig>();

			if (Projectile.localAI[0] == 0)
			{
				Main.instance.LoadNPC(npcType);
				Projectile.localAI[0] = 1;
				Projectile.timeLeft = config.CorpseDuration;

				if (isFlyingEntity) Projectile.velocity.Y += 4f;

				if (dismemberType == 3) {
					// Hitbox size is now handled dynamically below
					Projectile.velocity += Main.rand.NextVector2Circular(1, 1);
				}
				else if (dismemberType == 1) {
					bleedTimer = config.BleedDuration;
				}

				headRotation = Projectile.rotation;
			}

			// --- RAGDOLL COLLISION (SOFT STACKING & FLOOR CHECK) ---
			// Only run if collision is enabled
			if (config.EnableRagdollCollision)
			{
				for (int i = 0; i < Main.maxProjectiles; i++)
				{
					Projectile other = Main.projectile[i];
					if (i != Projectile.whoAmI && other.active && other.type == Projectile.type && other.timeLeft > 0)
					{
						if (Projectile.DistanceSQ(other.Center) > 3600) continue;

						Rectangle myRect = Projectile.Hitbox;
						Rectangle otherRect = other.Hitbox;

						if (myRect.Intersects(otherRect))
						{
							Rectangle overlap = Rectangle.Intersect(myRect, otherRect);

							Vector2 separation = Vector2.Zero;
							if (overlap.Width < overlap.Height)
							{
								int dir = Projectile.Center.X < other.Center.X ? -1 : 1;
								separation.X = dir * overlap.Width;
							}
							else
							{
								int dir = Projectile.Center.Y < other.Center.Y ? -1 : 1;
								separation.Y = dir * overlap.Height;
							}

							float softness = 0.2f;
							Vector2 push = separation * softness;

							if (push.Y > 0)
							{
								if (Collision.SolidCollision(Projectile.position + push, Projectile.width, Projectile.height))
								{
									push.Y = 0;
								}
							}

							if (!Collision.SolidCollision(Projectile.position + push, Projectile.width, Projectile.height))
							{
								Projectile.position += push;

								if (Math.Abs(separation.Y) > 0)
								{
									Projectile.velocity.X *= 0.9f;
									if (Projectile.velocity.Y > 0 && push.Y < 0) Projectile.velocity.Y = 0;
								}
							}
						}
					}
				}
			}
			// -----------------------------------------

			// --- DYNAMIC HITBOX RESIZING ---
			NPC sample = ContentSamples.NpcsByNetId[netID];
			int targetW = sample.width;
			int targetH = sample.height;

			if (dismemberType == 3) // Detached Head
			{
				targetW = 14;
				targetH = 14;
			}
			else if (dismemberType == 1) // Headless Body
			{
				// Reduce height by approx 35% so it's not a ghost head
				targetH = (int)(sample.height * 0.65f);
				if (targetH < 14) targetH = 14;
			}
			else if (dismemberType == 4) // Legless Body (Upper)
			{
				targetH = (int)(sample.height * 0.7f);
			}
			else if (dismemberType == 5) // Legs
			{
				targetH = (int)(sample.height * 0.35f);
			}

			if (!IsSlime() && dismemberType != 3)
			{
				float rot = Projectile.rotation % MathHelper.TwoPi;
				if (rot < 0) rot += MathHelper.TwoPi;

				// Check if body is lying sideways (approx 90 or 270 degrees)
				bool isSideways = (rot > MathHelper.PiOver4 && rot < 3 * MathHelper.PiOver4) ||
								  (rot > 5 * MathHelper.PiOver4 && rot < 7 * MathHelper.PiOver4);

				if (isSideways)
				{
					int swap = targetW;
					targetW = targetH;
					targetH = swap;
				}

				if (targetW < 14) targetW = 14;
				if (targetH < 14) targetH = 14;
			}

			if (Projectile.width != targetW || Projectile.height != targetH)
			{
				Vector2 oldCenter = Projectile.Center;
				Projectile.width = targetW;
				Projectile.height = targetH;
				Projectile.Center = oldCenter;

				if (Collision.SolidCollision(Projectile.position, (int) Math.Round(npcScale * base.Projectile.width), (int) Math.Round(npcScale * base.Projectile.height)))
					{
					Projectile.position.Y -= 16f;
					if (Collision.SolidCollision(Projectile.position, (int) Math.Round(npcScale * base.Projectile.width), (int) Math.Round(npcScale * base.Projectile.height)))
						{
						Projectile.position.Y += 16f;
					}
				}
			}
			// -------------------------------

			if (dismemberType == 1 && bleedTimer > 0)
			{
				bleedTimer--;

				if (bleedTimer % 3 == 0)
				{
					Texture2D tex = TextureAssets.Npc[npcType].Value;
					int frameCount = Main.npcFrameCount[npcType];
					if (frameCount < 1) frameCount = 1;
					int frameHeight = tex.Height / frameCount;

					float centerToNeckDist = (frameHeight * 0.38f) - (frameHeight / 2f);
					Vector2 neckPos = Projectile.Center + new Vector2(0, centerToNeckDist).RotatedBy(Projectile.rotation);

					Vector2 bloodVel = new Vector2(0, -Main.rand.NextFloat(2f, 5f)).RotatedBy(Projectile.rotation);
					bloodVel += Main.rand.NextVector2Circular(1f, 1f);
					bloodVel += Projectile.velocity * 0.5f;

					bloodVel *= config.BloodVelocity;

					Dust d = Dust.NewDustPerfect(neckPos, DustID.Blood, bloodVel, 0, default, config.BloodScale);
				}
			}

			if (Projectile.timeLeft < 60) Projectile.alpha = (int)MathHelper.Lerp(255, 0, Projectile.timeLeft / 60f);

			// --- WATER BUOYANCY ---
			if (Collision.WetCollision(Projectile.position, (int) Math.Round(npcScale * base.Projectile.width), (int) Math.Round(npcScale * base.Projectile.height)))
				{
				Projectile.velocity.Y -= 0.4f;
				Projectile.velocity *= 0.95f;
				if (Projectile.velocity.Y < -2f) Projectile.velocity.Y = -2f;
				angularMomentum *= 0.9f;
			}
			else
			{
				// --- REDUCED GRAVITY (LIGHTER FEEL) ---
				// Legacy check?
				// We removed drag in favor of LegacyRagdollPhysics check below
				// But for now keep it standard as base
				Projectile.velocity.Y += config.Gravity * 0.85f;
				if (Projectile.velocity.Y > 16f) Projectile.velocity.Y = 16f;
			}

			Tile tileBelow = Framing.GetTileSafely((int)(Projectile.Center.X / 16f), (int)((Projectile.Center.Y + 10) / 16f));
			bool onGround = Projectile.velocity.Y == 0 || (tileBelow.HasTile && Main.tileSolid[tileBelow.TileType]);

			if (onGround)
			{
				float friction = config.Friction;
				if (dismemberType == 3) friction = Math.Min(0.98f, config.Friction + 0.06f);

				if (tileBelow.Slope != SlopeType.Solid || tileBelow.IsHalfBlock)
				{
					if (tileBelow.Slope == SlopeType.SlopeDownRight) Projectile.velocity.X -= 0.5f;
					if (tileBelow.Slope == SlopeType.SlopeDownLeft) Projectile.velocity.X += 0.5f;
					angularMomentum += Projectile.velocity.X * 0.04f;
				}
				else
				{
					Projectile.velocity.X *= friction;
					// If Legacy is OFF, we use the "drag more" logic
					if (!config.LegacyRagdollPhysics)
					{
						angularMomentum *= (friction * 0.8f); // Drag stops spin faster
					}
					else
					{
						angularMomentum *= friction; // Standard
					}
				}
				if (Math.Abs(Projectile.velocity.X) < 0.1f) Projectile.velocity.X = 0;
			}
			else
			{
				if (!config.LegacyRagdollPhysics)
				{
					angularMomentum = MathHelper.Lerp(angularMomentum, Projectile.velocity.X * 0.015f, 0.2f);
				}
				else
				{
					angularMomentum = Projectile.velocity.X * 0.05f; // Old logic
				}
			}

			if (IsSlime())
			{
				Projectile.rotation = MathHelper.Lerp(Projectile.rotation, 0f, 0.05f);
				angularMomentum *= 0.9f;

				float targetSquash = 1f;
				float force = (targetSquash - squashCompression) * config.SlimeSpringStiffness;
				squashVelocity += force;
				squashVelocity *= config.SlimeSpringDamping;
				squashCompression += squashVelocity;
			}
			else
			{
				Projectile.rotation += angularMomentum;
			}

			// --- FLOPPIER PHYSICS ---
			if (IsHumanoid() && (dismemberType == 0 || dismemberType == 2 || dismemberType == 4))
			{
				float targetRotation = Projectile.rotation;
				float stiffMult = 0.6f; // Reduced stiffness base
				float force = (targetRotation - headRotation) * (config.HeadSpringStiffness * stiffMult);
				headAngularVel += force;
				headAngularVel *= config.HeadSpringDamping;
				headRotation += headAngularVel;
			}
			else if (!IsHumanoid())
			{
				headRotation = Projectile.rotation;
			}

			if (hitCooldown > 0) hitCooldown--;

			bool compatibilityMode = config.EnableSwingModCompatibility;

			for (int i = 0; i < Main.maxPlayers; i++)
			{
				Player player = Main.player[i];
				if (player.active && !player.dead && player.itemAnimation > 0 && hitCooldown <= 0)
				{
					if (!compatibilityMode)
					{
						float reach = 100f * player.GetAdjustedItemScale(player.HeldItem);
						if (Vector2.Distance(player.Center, Projectile.Center) < reach)
							OnHitByPlayer(player.direction);
					}
					else
					{
						bool isTool = player.HeldItem.pick > 0 || player.HeldItem.axe > 0 || player.HeldItem.hammer > 0;
						if (isTool)
						{
							Rectangle itemRect = new Rectangle((int)player.itemLocation.X, (int)player.itemLocation.Y, 32, 32);
							if (!player.HeldItem.IsAir)
							{
								itemRect.Width = (int)(player.HeldItem.width * player.GetAdjustedItemScale(player.HeldItem));
								itemRect.Height = (int)(player.HeldItem.height * player.GetAdjustedItemScale(player.HeldItem));
							}
							itemRect.X -= itemRect.Width / 2;
							itemRect.Y -= itemRect.Height / 2;
							if (itemRect.Intersects(Projectile.Hitbox)) OnHitByPlayer(player.direction);
						}
						else
						{
							float reach = 100f * player.GetAdjustedItemScale(player.HeldItem);
							if (Vector2.Distance(player.Center, Projectile.Center) < reach)
							{
								float dirToRagdoll = Math.Sign(Projectile.Center.X - player.Center.X);
								if (dirToRagdoll == player.direction || Vector2.Distance(player.Center, Projectile.Center) < 50f)
									OnHitByPlayer(player.direction);
							}
						}
					}
				}
			}

			for (int i = 0; i < Main.maxProjectiles; i++)
			{
				Projectile other = Main.projectile[i];
				if (other.active && other.friendly && hitCooldown <= 0)
				{
					if (other.type == Projectile.type) continue;
					if (other.damage <= 0 && other.aiStyle == 0 && other.timeLeft < 5) continue;

					if (other.arrow && config.ArrowsStickToEnemies)
					{
						if (other.Colliding(other.Hitbox, Projectile.Hitbox))
						{
							Vector2 stickPos = other.Center + other.velocity.SafeNormalize(Vector2.Zero) * 12f;
							stickPos += Main.rand.NextVector2Circular(3, 3);
							int pIndex = Projectile.NewProjectile(Projectile.GetSource_FromThis(), stickPos, Vector2.Zero, ModContent.ProjectileType<StuckArrow>(), 0, 0, Main.myPlayer, Projectile.whoAmI, 1, other.type);
							Main.projectile[pIndex].rotation = other.rotation + Main.rand.NextFloat(-0.1f, 0.1f);
							other.Kill();
						}
					}

					bool hit = false;
					if (other.Colliding(other.Hitbox, Projectile.Hitbox)) hit = true;

					if (compatibilityMode && !hit)
					{
						bool isMelee = other.DamageType == DamageClass.Melee || other.aiStyle == 19 || other.aiStyle == 0;
						if (isMelee)
						{
							float reach = 100f + (other.width/2);
							if (Vector2.Distance(other.Center, Projectile.Center) < reach) hit = true;
						}
					}

					if (hit)
					{
						int dir = other.Center.X < Projectile.Center.X ? 1 : -1;
						if (Math.Abs(other.velocity.X) > 0.1f) dir = Math.Sign(other.velocity.X);
						if (other.owner != 255 && other.aiStyle == 19) dir = Main.player[other.owner].direction;
						OnHitByPlayer(dir);
					}
				}
			}
		}

		private void OnHitByPlayer(int hitDir)
		{
			var config = ModContent.GetInstance<KineticGoreConfig>();
			hitCooldown = 45;
			Projectile.velocity.X += hitDir * Main.rand.NextFloat(2f, 5f) * config.KnockbackMultiplier;
			Projectile.velocity.Y -= Main.rand.NextFloat(2f, 4f) * config.KnockbackMultiplier;

			// LEGACY CHECK
			if (config.LegacyRagdollPhysics)
			{
				angularMomentum += hitDir * 0.2f; // Old Higher Spin
			}
			else
			{
				angularMomentum += hitDir * 0.05f; // New Low Spin
			}

			headAngularVel += hitDir * 0.3f;
			if (IsSlime()) squashVelocity += 0.2f;

			SoundEngine.PlaySound(SoundID.NPCDeath1, Projectile.Center);
			LoseLimb(true);

			if (IsHumanoid() && dismemberType == 0 && Main.netMode != NetmodeID.MultiplayerClient && Main.rand.Next(100) < config.DismembermentChance)
			{
				if (Main.rand.NextBool())
				{
					// --- OPTION A: HEAD DECAPITATION ---
					dismemberType = 1; // Headless Body
					bleedTimer = config.BleedDuration;
					int projType = ModContent.ProjectileType<PhysicsCorpse>();
					float headAI = (isFlyingEntity ? 1f : 0f) + (30f);
					Vector2 spawnPos = Projectile.Center + new Vector2(0, -10).RotatedBy(Projectile.rotation);
					Vector2 headLaunch = Projectile.velocity * 1.1f + new Vector2(hitDir * 2, -3);

					Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPos, headLaunch, projType, 0, 0, Main.myPlayer, npcType, headAI, Projectile.ai[2]);
					for(int i=0; i<10; i++) Dust.NewDust(spawnPos, 10, 10, DustID.Blood, 0, 0, 0, default, config.BloodScale);
				}
				else
				{
					// --- OPTION B: LEG SEPARATION (NEW) ---
					dismemberType = 4; // Legless Body (Upper)

					int projType = ModContent.ProjectileType<PhysicsCorpse>();
					float legsAI = (isFlyingEntity ? 1f : 0f) + (50f); // Type 5 = Legs

					Vector2 legsLaunch = Projectile.velocity * 0.8f + new Vector2(hitDir * 1.5f, -1);

					// Spawn Legs
					Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, legsLaunch, projType, 0, 0, Main.myPlayer, npcType, legsAI, Projectile.ai[2]);

					int amount = (int)(8 * config.BloodAmountMultiplier);
					for(int i=0; i<amount; i++)
					{
						Dust.NewDust(Projectile.Center, 10, 10, DustID.Blood, 0, 0, 0, default, config.BloodScale);
					}
				}
			}
		}

		public override bool OnTileCollide(Vector2 oldVelocity)
		{
			var config = ModContent.GetInstance<KineticGoreConfig>();

			if (oldVelocity.Length() > 3f)
			{
				SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
				headAngularVel += Main.rand.NextFloat(-0.1f, 0.1f);
				if (IsSlime()) squashVelocity -= 0.15f;
			}
			if (isFlyingEntity && oldVelocity.Length() > 6f)
			{
				Splatter();
				return true;
			}
			if (Projectile.velocity.X != oldVelocity.X) Projectile.velocity.X = -oldVelocity.X * config.Bounciness;
			if (Projectile.velocity.Y != oldVelocity.Y)
			{
				if (oldVelocity.Y > 8f)
				{
					LoseLimb(false);

					// LEGACY CHECK
					if (config.LegacyRagdollPhysics)
					{
						headAngularVel += Math.Sign(Projectile.velocity.X) * 0.3f; // Old Higher Spin
					}
					else
					{
						headAngularVel += Math.Sign(Projectile.velocity.X) * 0.1f; // New Low Spin
					}

					if (IsSlime()) squashVelocity -= 0.3f;
				}
				Projectile.velocity.Y = -oldVelocity.Y * config.Bounciness;
			}
			Projectile.velocity *= 0.9f;
			return false;
		}

		private void LoseLimb(bool forced)
		{
			if (integrity <= 0 && !forced) return;
			integrity -= 15;

			var config = ModContent.GetInstance<KineticGoreConfig>();

			// FIXED: Use BloodAmountMultiplier for the blood dust count
			int dustCount = (int)(5 * config.BloodAmountMultiplier);
			if (dustCount < 1 && config.BloodAmountMultiplier > 0.1f) dustCount = 1;

			for(int i=0; i<dustCount; i++)
			{
				Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Blood);
				d.velocity = -Projectile.velocity * 0.2f + Main.rand.NextVector2Circular(1.5f, 1.5f);
				d.scale = Main.rand.NextFloat(1.0f, 1.4f) * config.BloodScale;
				d.noGravity = false;
			}

			if (!IsSlime())
			{
				int amount = (int)(6 * config.BloodAmountMultiplier);
				float scale = config.BloodScale;
				for(int i=0; i<amount; i++) Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Blood, Projectile.velocity.X * 0.3f, Projectile.velocity.Y * 0.3f, 80, default, scale);
			}
		}

		private void Splatter()
		{
			var config = ModContent.GetInstance<KineticGoreConfig>();
			SoundEngine.PlaySound(SoundID.NPCDeath1, Projectile.Center);

			// --- SPLATTER: SPAWN AUTHENTIC DEATH GORE ---
			try
			{
				int type = (int)npcType; // Use explicit local var to avoid scope issues
				NPC dummy = new NPC();
				dummy.SetDefaults(type);
				dummy.Center = Projectile.Center;
				dummy.rotation = Projectile.rotation;
				dummy.velocity = Projectile.velocity;
				dummy.life = -1;
				dummy.active = true;

				NPC.HitInfo hit = new NPC.HitInfo();
				hit.Damage = 50;
				hit.Knockback = 6f;
				hit.HitDirection = Math.Sign(Projectile.velocity.X);

				dummy.HitEffect(hit);
				dummy.active = false;
			}
			catch { }

			Projectile.Kill();
		}

		public override bool PreDraw(ref Color lightColor)
		{
			Main.instance.LoadNPC(npcType);
			Texture2D texture = TextureAssets.Npc[npcType].Value;
			int frameCount = Main.npcFrameCount[npcType];
			if (frameCount < 1) frameCount = 1;
			int frameHeight = texture.Height / frameCount;
			Color drawColor = lightColor * ((255 - Projectile.alpha) / 255f);

			if (Projectile.ai[2] != 0)
			{
				int rgb = BitConverter.SingleToInt32Bits(Projectile.ai[2]);

				int r = (rgb >> 16) & 0xFF;
				int g = (rgb >> 8) & 0xFF;
				int b = rgb & 0xFF;

				Color dynamicColor = new Color(r, g, b);
				drawColor = dynamicColor.MultiplyRGBA(drawColor);
			}
			else
			{
				NPC sample = ContentSamples.NpcsByNetId[netID];
				if (sample.color != default) drawColor = sample.color.MultiplyRGBA(drawColor);
			}

			if (IsSlime())
			{
				// --- SLIME TRANSPARENCY FIX ---
				NPC sample = ContentSamples.NpcsByNetId[netID];
				float opacity = (255f - sample.alpha) / 255f;
				drawColor *= opacity;

				Rectangle fullSrc = new Rectangle(0, 0, texture.Width, frameHeight);
				Vector2 origin = fullSrc.Size() / 2f;

				float scaleY = squashCompression;
				float scaleX = 1f / squashCompression;

                Vector2 scaleVector = new Vector2(npcScale * scaleX, npcScale * scaleY);

                float heightChange = (fullSrc.Height * scaleY) - fullSrc.Height;
				Vector2 drawPos = Projectile.Center - Main.screenPosition;
				drawPos.Y -= heightChange * 0.5f;

				Main.EntitySpriteDraw(texture, drawPos, fullSrc, drawColor, Projectile.rotation, origin, scaleVector, SpriteEffects.None, 0);
				return false;
			}

			if (!IsHumanoid())
			{
				Rectangle fullSrc = new Rectangle(0, 0, texture.Width, frameHeight);
				Vector2 origin = fullSrc.Size() / 2f;
				Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, fullSrc, drawColor, Projectile.rotation, origin, 1f, SpriteEffects.None, 0);
				return false;
			}

            // --- FIXED DEFINITIONS: Defined BEFORE drawing logic ---
			int splitY = (int)(frameHeight * 0.38f);
			int overlap = 2;

			Rectangle headSrc = new Rectangle(0, 0, texture.Width, splitY + overlap);
			Rectangle bodySrc = new Rectangle(0, splitY - overlap, texture.Width, frameHeight - splitY + overlap);

			// Leg Separation Calculation
			int legSplitY = (int)(frameHeight * 0.75f);
			Rectangle upperBodySrc = new Rectangle(0, 0, texture.Width, legSplitY);
			Rectangle legsSrc = new Rectangle(0, legSplitY, texture.Width, frameHeight - legSplitY);
			Rectangle leglessBodySrc = new Rectangle(0, splitY - overlap, texture.Width, legSplitY - splitY + overlap);

			Vector2 headOrigin = new Vector2(texture.Width / 2f, splitY);
			Vector2 bodyOrigin = new Vector2(texture.Width / 2f, overlap);
			Vector2 upperBodyOrigin = new Vector2(texture.Width / 2f, legSplitY / 2f); // Approx center
			Vector2 legsOrigin = new Vector2(texture.Width / 2f, 0);

			if (dismemberType == 3) // Detached Head
			{
				Vector2 centerHeadOrigin = new Vector2(texture.Width / 2f, splitY / 2f);
				Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, headSrc, drawColor, Projectile.rotation, centerHeadOrigin, 1f, SpriteEffects.None, 0);
				return false;
			}

			if (dismemberType == 2) // Just Head (Legacy?)
			{
				Vector2 centerHeadOrigin = new Vector2(texture.Width / 2f, splitY);
				Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, headSrc, drawColor, Projectile.rotation, centerHeadOrigin, 1f, SpriteEffects.None, 0);
				return false;
			}

			if (dismemberType == 5) // Detached Legs
			{
				Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, legsSrc, drawColor, Projectile.rotation, legsOrigin, 1f, SpriteEffects.None, 0);
				return false;
			}

			if (dismemberType == 4) // Legless Body (Head + Torso)
			{
				// Draw Torso
				Vector2 torsoOrigin = new Vector2(texture.Width / 2f, splitY); // Re-align to neck
				float centerToNeck = splitY - (frameHeight * 0.4f); // Approx pos

				// Re-calc origin for drawing Torso part of Upper Body
				Vector2 leglessOrigin = new Vector2(texture.Width / 2f, overlap);

				float neckDist = splitY - (frameHeight/2f);
				Vector2 neckPosCalc = Projectile.Center + new Vector2(0, neckDist).RotatedBy(Projectile.rotation);

				// Draw Legless Torso
				Main.EntitySpriteDraw(texture, neckPosCalc - Main.screenPosition, leglessBodySrc, drawColor, Projectile.rotation, leglessOrigin, 1f, SpriteEffects.None, 0);

				// Draw Head (Wobbly)
				Main.EntitySpriteDraw(texture, neckPosCalc - Main.screenPosition, headSrc, drawColor, headRotation, headOrigin, 1f, SpriteEffects.None, 0);
				return false;
			}

            // STANDARD DRAW (Full Body or Headless Body)
			float centerToNeckDist = splitY - (frameHeight / 2f);
			Vector2 neckPos = Projectile.Center + new Vector2(0, centerToNeckDist).RotatedBy(Projectile.rotation);

			Main.EntitySpriteDraw(texture, neckPos - Main.screenPosition, bodySrc, drawColor, Projectile.rotation, bodyOrigin, 1f, SpriteEffects.None, 0);

			if (dismemberType != 1)
			{
				Main.EntitySpriteDraw(texture, neckPos - Main.screenPosition, headSrc, drawColor, headRotation, headOrigin, 1f, SpriteEffects.None, 0);
			}

			return false;
		}
	}
}