using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace HydrateOrDiedrate.WateringTrough
{
	public class BlockEntityWateringTrough : BlockEntityContainer, ITexPositionSource, IAnimalFoodSource, IAnimalDrinkSource, IPointOfInterest
	{
		
		internal InventoryGeneric inventory;
		private ITexPositionSource blockTexPosSource;
		private MeshData currentMesh;
		private string contentCode = "";
		private DoubleWateringTroughPoiDummy dummypoi;
		
		public float WaterLitres;
		public float MaxWaterLitres = 40f;
		public float LitresPerPortion = 1f;
		
		public override InventoryBase Inventory
		{
			get
			{
				return this.inventory;
			}
		}
		public override string InventoryClassName
		{
			get
			{
				return "trough";
			}
		}

		public Size2i AtlasSize
		{
			get
			{
				return (this.Api as ICoreClientAPI).BlockTextureAtlas.Size;
			}
		}

		public Vec3d Position
		{
			get
			{
				return this.Pos.ToVec3d().Add(0.5, 0.5, 0.5);
			}
		}

		public string Type
		{
			get
			{
				return "food";
			}
		}

		public ContentConfig[] contentConfigs
		{
			get
			{
				return this.Api.ObjectCache["troughContentConfigs-" + base.Block.Code] as ContentConfig[];
			}
		}

		public bool IsFull
		{
			get
			{
				ItemStack[] stacks = base.GetNonEmptyContentStacks(true);
				ContentConfig config = this.contentConfigs.FirstOrDefault((ContentConfig c) => c.Code == this.contentCode);
				return config != null && stacks.Length != 0 && stacks[0].StackSize >= config.QuantityPerFillLevel * config.MaxFillLevels;
			}
		}

		public TextureAtlasPosition this[string textureCode]
		{
			get
			{
				if (textureCode != "contents")
				{
					return this.blockTexPosSource[textureCode];
				}
				ContentConfig contentConfig = this.contentConfigs.FirstOrDefault((ContentConfig c) => c.Code == this.contentCode);
				string configTextureCode = (contentConfig != null) ? contentConfig.TextureCode : null;
				if (configTextureCode != null && configTextureCode.Equals("*"))
				{
					configTextureCode = "contents-" + this.Inventory.FirstNonEmptySlot.Itemstack.Collectible.Code.ToShortString();
				}
				if (configTextureCode == null)
				{
					return this.blockTexPosSource[textureCode];
				}
				return this.blockTexPosSource[configTextureCode];
			}
		}

		public BlockEntityWateringTrough()
		{
			this.inventory = new InventoryGeneric(4, null, null, (int id, InventoryGeneric inv) => new ItemSlotWateringTrough(this, inv));
			this.inventory.OnGetAutoPushIntoSlot = delegate(BlockFacing face, ItemSlot slot)
			{
				if (this.IsFull)
				{
					return null;
				}
				return this.inventory.GetBestSuitedSlot(slot, null, null).slot;
			};
		}
		
		public bool IsSuitableFor(Entity entity, CreatureDiet diet)
		{
			if (this.inventory.Empty || diet == null)
			{
				return false;
			}
			ContentConfig config = this.contentConfigs.FirstOrDefault((ContentConfig c) => c.Code == this.contentCode);
			ItemStack itemStack;
			if (config == null)
			{
				itemStack = null;
			}
			else
			{
				JsonItemStack content = config.Content;
				itemStack = ((content != null) ? content.ResolvedItemstack : null);
			}
			ItemStack contentResolvedItemstack = itemStack ?? this.ResolveWildcardContent(config, entity.World);
			if (contentResolvedItemstack == null)
			{
				return false;
			}
			if (diet.Matches(contentResolvedItemstack, true, 0f) && this.inventory[0].StackSize >= config.QuantityPerFillLevel)
			{
				BlockTroughBase trough = base.Block as BlockTroughBase;
				if (trough != null)
				{
					return !trough.UnsuitableForEntity(entity.Code.Path);
				}
			}
			return false;
		}

		private ItemStack ResolveWildcardContent(ContentConfig config, IWorldAccessor worldAccessor)
		{
			AssetLocation left;
			if (config == null)
			{
				left = null;
			}
			else
			{
				JsonItemStack content = config.Content;
				left = ((content != null) ? content.Code : null);
			}
			if (left == null)
			{
				return null;
			}
			List<CollectibleObject> searchObjects = new List<CollectibleObject>();
			EnumItemClass type = config.Content.Type;
			if (type != EnumItemClass.Block)
			{
				if (type != EnumItemClass.Item)
				{
					throw new ArgumentOutOfRangeException("Type");
				}
				searchObjects.AddRange(worldAccessor.SearchItems(config.Content.Code));
			}
			else
			{
				searchObjects.AddRange(worldAccessor.SearchBlocks(config.Content.Code));
			}
			foreach (CollectibleObject item in searchObjects)
			{
				AssetLocation code = item.Code;
				ItemSlot firstNonEmptySlot = this.Inventory.FirstNonEmptySlot;
				AssetLocation other;
				if (firstNonEmptySlot == null)
				{
					other = null;
				}
				else
				{
					ItemStack itemstack = firstNonEmptySlot.Itemstack;
					if (itemstack == null)
					{
						other = null;
					}
					else
					{
						Item item2 = itemstack.Item;
						other = ((item2 != null) ? item2.Code : null);
					}
				}
				if (code.Equals(other))
				{
					return new ItemStack(item, 1);
				}
			}
			return null;
		}
		
		public float ConsumeOnePortion(Entity entity)
		{
			ContentConfig config = this.contentConfigs.FirstOrDefault((ContentConfig c) => c.Code == this.contentCode);
			if (config == null || this.inventory.Empty)
			{
				return 0f;
			}
			this.inventory[0].TakeOut(config.QuantityPerFillLevel);
			if (this.inventory[0].Empty)
			{
				this.contentCode = "";
			}
			this.inventory[0].MarkDirty();
			this.MarkDirty(true, null);
			return 1f;
		}
		
		public float ConsumeOneLiquidPortion(Entity entity)
		{
			if (WaterLitres < LitresPerPortion)
			{
				return 0f;
			}

			WaterLitres -= LitresPerPortion;
			MarkDirty(true, null);

			return LitresPerPortion;
		}
		
		internal bool OnInteractWithLiquid(IPlayer byPlayer, BlockSelection blockSel)
		{
			ItemSlot handSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
			if (handSlot.Empty) return false;
			ItemStack stack = handSlot.Itemstack;
			var liquidSource = stack.Collectible as ILiquidSource;
			if (liquidSource == null) return false;
			int canAccept = (int)Math.Floor(MaxWaterLitres - WaterLitres);
			if (canAccept <= 0) return false;
			ItemStack taken = liquidSource.TryTakeContent(stack, canAccept);
			if (taken == null || taken.StackSize <= 0) return false;
			WaterLitres += taken.StackSize;

			MarkDirty(true, null);
			handSlot.MarkDirty();

			return true;
		}

		
		public override void Initialize(ICoreAPI api)
		{
			base.Initialize(api);
			if (this.Api.Side == EnumAppSide.Client)
			{
				ICoreClientAPI coreClientAPI = (ICoreClientAPI)api;
				if (this.currentMesh == null)
				{
					this.currentMesh = this.GenMesh();
				}
			}
			else
			{
				this.Api.ModLoader.GetModSystem<POIRegistry>(true).AddPOI(this);
				BlockWateringTroughDoubleBlock doubleblock = base.Block as BlockWateringTroughDoubleBlock;
				if (doubleblock != null)
				{
					this.dummypoi = new DoubleWateringTroughPoiDummy(this)
					{
						Position = doubleblock.OtherPartPos(this.Pos).ToVec3d().Add(0.5, 0.5, 0.5)
					};
					this.Api.ModLoader.GetModSystem<POIRegistry>(true).AddPOI(this.dummypoi);
				}
			}
			this.inventory.SlotModified += this.Inventory_SlotModified;
		}
		private void Inventory_SlotModified(int id)
		{
			ContentConfig config = ItemSlotTrough.getContentConfig(this.Api.World, this.contentConfigs, this.inventory[id]);
			this.contentCode = ((config != null) ? config.Code : null);
			if (this.Api.Side == EnumAppSide.Client)
			{
				this.currentMesh = this.GenMesh();
			}
			this.MarkDirty(true, null);
		}
		public override void OnBlockPlaced(ItemStack byItemStack = null)
		{
			base.OnBlockPlaced(byItemStack);
			if (this.Api.Side == EnumAppSide.Client)
			{
				this.currentMesh = this.GenMesh();
				this.MarkDirty(true, null);
			}
		}

		public override void OnBlockRemoved()
		{
			base.OnBlockRemoved();
			if (this.Api.Side == EnumAppSide.Server)
			{
				this.Api.ModLoader.GetModSystem<POIRegistry>(true).RemovePOI(this);
				if (this.dummypoi != null)
				{
					this.Api.ModLoader.GetModSystem<POIRegistry>(true).RemovePOI(this.dummypoi);
				}
			}
		}

		public override void OnBlockUnloaded()
		{
			base.OnBlockUnloaded();
			ICoreAPI api = this.Api;
			if (api != null && api.Side == EnumAppSide.Server)
			{
				this.Api.ModLoader.GetModSystem<POIRegistry>(true).RemovePOI(this);
				if (this.dummypoi != null)
				{
					this.Api.ModLoader.GetModSystem<POIRegistry>(true).RemovePOI(this.dummypoi);
				}
			}
		}
		internal MeshData GenMesh()
		{
			if (base.Block == null)
			{
				return null;
			}
			ItemStack firstStack = this.inventory[0].Itemstack;
			if (firstStack == null)
			{
				return null;
			}
			ICoreClientAPI capi = this.Api as ICoreClientAPI;
			string shapeLoc;
			if (this.contentCode == "" || this.contentConfigs == null)
			{
				if (!(firstStack.Collectible.Code.Path == "rot"))
				{
					return null;
				}
				shapeLoc = "block/wood/trough/" + ((base.Block.Variant["part"] == "small") ? "small" : "large") + "/rotfill" + GameMath.Clamp(firstStack.StackSize / 4, 1, 4).ToString();
			}
			else
			{
				ContentConfig config = this.contentConfigs.FirstOrDefault((ContentConfig c) => c.Code == this.contentCode);
				if (config == null)
				{
					return null;
				}
				int fillLevel = Math.Max(0, firstStack.StackSize / config.QuantityPerFillLevel - 1);
				shapeLoc = config.ShapesPerFillLevel[Math.Min(config.ShapesPerFillLevel.Length - 1, fillLevel)];
			}
			Vec3f rotation = new Vec3f(base.Block.Shape.rotateX, base.Block.Shape.rotateY, base.Block.Shape.rotateZ);
			this.blockTexPosSource = capi.Tesselator.GetTextureSource(base.Block, 0, false);
			Shape shape = Shape.TryGet(this.Api, "shapes/" + shapeLoc + ".json");
			MeshData meshbase;
			capi.Tesselator.TesselateShape("betroughcontentsleft", shape, out meshbase, this, rotation, 0, 0, 0, null, null);
			BlockWateringTroughDoubleBlock doubleblock = base.Block as BlockWateringTroughDoubleBlock;
			if (doubleblock != null)
			{
				MeshData meshadd;
				capi.Tesselator.TesselateShape("betroughcontentsright", shape, out meshadd, this, rotation.Add(0f, 180f, 0f), 0, 0, 0, null, null);
				BlockFacing facing = doubleblock.OtherPartFacing();
				meshadd.Translate(facing.Normalf);
				meshbase.AddMeshData(meshadd);
			}
			return meshbase;
		}

		public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
		{
			mesher.AddMeshData(this.currentMesh, 1);
			return false;
		}

		internal bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
		{
			ItemSlot handSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
			if (handSlot.Empty)
			{
				return false;
			}
			ItemStack[] stacks = base.GetNonEmptyContentStacks(true);
			ContentConfig contentConf = ItemSlotTrough.getContentConfig(this.Api.World, this.contentConfigs, handSlot);
			if (contentConf == null)
			{
				return false;
			}
			if (stacks.Length == 0)
			{
				if (handSlot.StackSize >= contentConf.QuantityPerFillLevel)
				{
					this.inventory[0].Itemstack = handSlot.TakeOut(contentConf.QuantityPerFillLevel);
					this.inventory[0].MarkDirty();
					return true;
				}
				return false;
			}
			else
			{
				if (handSlot.Itemstack.Equals(this.Api.World, stacks[0], GlobalConstants.IgnoredStackAttributes) && handSlot.StackSize >= contentConf.QuantityPerFillLevel && stacks[0].StackSize < contentConf.QuantityPerFillLevel * contentConf.MaxFillLevels)
				{
					handSlot.TakeOut(contentConf.QuantityPerFillLevel);
					this.inventory[0].Itemstack.StackSize += contentConf.QuantityPerFillLevel;
					this.inventory[0].MarkDirty();
					return true;
				}
				return false;
			}
		}
		public override void ToTreeAttributes(ITreeAttribute tree)
		{
			base.ToTreeAttributes(tree);
			tree.SetString("contentCode", this.contentCode);
			tree.SetFloat("waterLitres", WaterLitres);
		}

		public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
		{
			base.FromTreeAttributes(tree, worldForResolving);
			this.contentCode = tree.GetString("contentCode", null);
			WaterLitres = tree.GetFloat("waterLitres", 0f);

			ICoreAPI api = this.Api;
			if (api != null && api.Side == EnumAppSide.Client)
			{
				this.currentMesh = this.GenMesh();
				this.MarkDirty(true, null);
			}
		}


		public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
		{
			dsc.AppendLine(string.Format("Water: {0:0.#}/{1:0.#} L", WaterLitres, MaxWaterLitres));
			ItemStack firstStack = this.inventory[0].Itemstack;
			if (this.contentConfigs == null) return;
			ContentConfig config = this.contentConfigs.FirstOrDefault((ContentConfig c) => c.Code == this.contentCode);
			if (config == null && firstStack != null)
			{
				dsc.AppendLine(firstStack.StackSize + "x " + firstStack.GetName());
			}
			if (config == null || firstStack == null) return;
			{
				return;
			}
			int fillLevel = firstStack.StackSize / config.QuantityPerFillLevel;
			dsc.AppendLine(Lang.Get("Portions: {0}", new object[]
			{
				fillLevel
			}));
			ItemStack contentsStack = config.Content.ResolvedItemstack ?? this.ResolveWildcardContent(config, forPlayer.Entity.World);
			if (contentsStack == null)
			{
				return;
			}
			dsc.AppendLine(Lang.Get(contentsStack.GetName(), Array.Empty<object>()));
			HashSet<string> creatureNames = new HashSet<string>();
			foreach (EntityProperties entityType in this.Api.World.EntityTypes)
			{
				JsonObject attr = entityType.Attributes;
				bool flag;
				if (attr == null)
				{
					flag = true;
				}
				else
				{
					CreatureDiet creatureDiet = attr["creatureDiet"].AsObject<CreatureDiet>(null);
					flag = !((creatureDiet != null) ? new bool?(creatureDiet.Matches(contentsStack, true, 0f)) : null).GetValueOrDefault();
				}
				if (!flag)
				{
					BlockTroughBase blockTroughBase = base.Block as BlockTroughBase;
					if (blockTroughBase == null || !blockTroughBase.UnsuitableForEntity(entityType.Code.Path))
					{
						string text;
						if ((text = ((attr != null) ? attr["creatureDietGroup"].AsString(null) : null)) == null)
						{
							text = (((attr != null) ? attr["handbook"]["groupcode"].AsString(null) : null) ?? (entityType.Code.Domain + ":item-creature-" + entityType.Code.Path));
						}
						string code = text;
						creatureNames.Add(Lang.Get(code, Array.Empty<object>()));
					}
				}
			}
			if (creatureNames.Count <= 0)
			{
				dsc.AppendLine(Lang.Get("trough-unsuitable", Array.Empty<object>()));
				return;
			}
			dsc.AppendLine(Lang.Get("trough-suitable", new object[]
			{
				string.Join(", ", creatureNames)
			}));
		}
	}
}
