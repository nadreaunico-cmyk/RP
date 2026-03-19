
using Sandbox.Entity;
using Sandbox.GameSystems.Player;
using Entity.Interactable.Door;
using Sandbox.UI;
using System;


namespace Entity.Interactable.Door
{
	[Library]
	public sealed class DoorLogic : BaseEntity, Component.INetworkListener
	{

		[Property] public GameObject Door { get; set; }
		[Property] public bool IsUnlocked { get; set; } = true;
		[Property] public bool IsOpen { get; set; } = false;
		[Property] public bool IsOwnable {get; set;} = true;

		Rotation originalRotation;

		[Property, Sync] public NetList<Player> DoorOwners {get; set;} = new();
		[Property, Sync] public NetList<Player> CanOwn {get; set;} = new();

		[Property, Sync] public int Price { get; set; } = 100;

		[Property] public DoorMenu DoorMenu {get; set;}
		[Sync] public string DoorTitle {get; set;} = "";

		public bool ShowTextIfOwner {get; set;} = false;
		public bool ShowTextIfCanOwn {get; set;} = false;

		protected override void OnAwake()
		{
			originalRotation = Door.Transform.World.Rotation;
		}

		public override void InteractUse( SceneTraceResult tr, GameObject player )
		{
			// Dont interact with the door if it is locked
			if ( IsUnlocked == false ) { return; }

			// Open / Close door
			OpenCloseDoor( player );
		}
		public override void InteractSpecial( SceneTraceResult tr, GameObject playerobject )
		{
			if (!IsOwnable) return;

			Player player = playerobject.Components.Get<Player>();

			if ( DoorOwners.Count == 0 || CanOwn.Contains(player))
			{
				PurchaseDoor(player);
				return;
			}

			if (IsDoorOwner(player))
			{
				DoorMenu.OpenDoorMenu(this, player);
			}
		}

		public override void InteractAttack1( SceneTraceResult tr, GameObject player )
		{
			// TODO The user should have a "keys" weapon select to do the following interactions to avoid input conflicts
			if (IsDoorOwner(player.Components.Get<Player>())) { LockDoor(); } else { KnockOnDoor(); }
		}

		public override void InteractAttack2( SceneTraceResult tr, GameObject player )
		{
			// TODO The user should have a "keys" weapon select to do the following interactions to avoid input conflicts
			if (IsDoorOwner(player.Components.Get<Player>())) { UnlockDoor(); } else { KnockOnDoor(); }
		}
		
		[Rpc.Owner]
		public void PurchaseDoor(Player player)
		{
			if (CanOwn.Contains(player)) CanOwn.Remove(player);
			player.UpdateBalance(CanOwn.Contains(player) ? -Price/4 : -Price);
			DoorOwners.Add(player);
			player.Doors.Add(Door);
			player.CanOwnDoors.Remove(Door);

			using(Rpc.FilterInclude(c => c.Id == player.Network.OwnerId))
			{
				ShowIfOwner(true);
				ShowCanOwn(false);
			}

		}

		[Rpc.Owner]
		public void SellDoor(Player player)
		{
			if (player == DoorOwners[0]) 
			{
				CanOwn.Clear();
				ShowCanOwn(false);
			}
			
			if (DoorOwners.Count == 1)
			{
				UnlockDoor();
				SetDoorTitle("");
				ShowIfOwner(false);
			}

			player.UpdateBalance(player == DoorOwners[0] ? Price / 4 / 2 : Price/2);
			player.Doors.Remove(Door);
			DoorOwners.Remove(player);
	
			using(Rpc.FilterInclude(c => c.Id == player.Network.OwnerId))
			{
				ShowIfOwner(false);
			}

		}

		[Rpc.Owner]
		public void SetDoorTitle(string title)
		{
			DoorTitle = title;
		}

		[Rpc.Broadcast]
		public void AddDoorOwner(Player player)
		{
			if (!CanOwn.Contains(player))
			{
				CanOwn.Add(player);
				player.CanOwnDoors.Add(Door);
			} 
			using(Rpc.FilterInclude( c => c.Id == player.Network.OwnerId))
			{
				ShowCanOwn(true);
			}
		}
		
		[Rpc.Broadcast]
		public void RemoveDoorOwner(Player player)
		{
			if (CanOwn.Contains(player))
			{
				CanOwn.Remove(player);
				ShowTextIfCanOwn = false;
				return;
			}
			SellDoor(player);
			player?.SendMessage( $"Your ownership of {DoorOwners[0].Name}'s door was revoked." );
		}

		[Rpc.Broadcast]
		private void OpenCloseDoor(GameObject player)
		{
			if ( Door == null ) { return; }
			float yaw = Door.Transform.Rotation.Yaw();
			Rotation rotationIncrement = Rotation.From( 0, 3, 0 );

			Vector3 directionToDoor = (Door.Transform.Position - player.Transform.Position).Normal;

			Vector3 forward = Door.Transform.Rotation.Forward;
			float dotProduct = Vector3.Dot( forward, directionToDoor );

			var shouldOpenForward = dotProduct > 0;

			if ( IsOpen )
			{
				_rotationIncrement = yaw > originalRotation.Yaw() ? rotationIncrement.Inverse : rotationIncrement;
				close = true;
			}
			else
			{
				_rotationIncrement = shouldOpenForward ? rotationIncrement : rotationIncrement.Inverse;
				open = true;
			}

			Sound.Play( "audio/door.sound", Door.Transform.World.Position );
		}

		bool open = false;
		bool close = false;

		Rotation _rotationIncrement;

		protected override void OnFixedUpdate()
		{
			base.OnFixedUpdate();
			
			if (open)
			{
				float yaw = Door.Transform.Rotation.Yaw();
				
				if (yaw < originalRotation.Yaw() + 90 && yaw > originalRotation.Yaw() - 90)
				{
					Door.Transform.Rotation *= _rotationIncrement;
				}
				else
				{
					IsOpen = true;
					open = false;
				}

				
			}

			if (close)
			{
				float yaw = Door.Transform.Rotation.Yaw();

				if (yaw < originalRotation.Yaw() + 3 && yaw > originalRotation.Yaw() - 3)
				{
					Door.Transform.Rotation = originalRotation;
					IsOpen = false;
					close = false;
				}
				else
				{
					Door.Transform.Rotation *= _rotationIncrement;
				}
			}

		}


		public bool IsDoorOwner(Player player)
		{
			return DoorOwners.Contains(player);
		}

		public bool IsDoorOwned()
		{
			return DoorOwners.Count > 0;
		}

		[Rpc.Broadcast]
		public void LockDoor()
		{
			if (IsUnlocked)
			{
				IsUnlocked = false;
				Sound.Play( "audio/lock.sound", Door.Transform.World.Position );
			}
		}

		[Rpc.Broadcast]
		public void UnlockDoor()
		{
			if (!IsUnlocked)
			{
				IsUnlocked = true;
				Sound.Play( "audio/lock.sound", Door.Transform.World.Position );
			}
			
		}

		[Rpc.Broadcast]
		private void KnockOnDoor()
		{
			Sound.Play( "audio/knock.sound", Door.Transform.World.Position );
		}

		[Rpc.Broadcast]
		void ShowIfOwner(bool show)
		{
			ShowTextIfOwner = show;
		}

		[Rpc.Broadcast]
		void ShowCanOwn(bool show)
		{
			ShowTextIfCanOwn = show;
		}
	}
}
