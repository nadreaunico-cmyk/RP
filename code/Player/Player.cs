using GameSystems;
using Sandbox.UI;

namespace Sandbox.GameSystems.Player;

/// <summary>
/// Represents your local player
/// </summary>
public partial class Player : Component, Component.INetworkSpawn
{
	[Property, Group( "References" )] public PlayerHUD PlayerHud { get; set; }
	[Property, Group( "References" )] public PlayerHUD PlayerTabMenu { get; set; }
	[Property, Group( "References" )] public LeaderBoard LeaderBoard { get; set; }
	private CameraComponent _camera;

	public string Name {get; set;} 
	
	protected override void OnAwake()
	{
		_camera = Scene.GetAllComponents<CameraComponent>().FirstOrDefault( x => x.IsMainCamera );

		if ( !Network.IsProxy )
		{
			// TODO: This should be moved off of the player and moved globally
			PlayerHud.Enabled = true;
			PlayerTabMenu.Enabled = true;
			LeaderBoard.Enabled = true;
		}
	}

	protected override void OnStart()
	{
		var owner = Network.Owner;
		if ( owner == null )
		{
			Log.Warning("Player started without Network.Owner");
		}
		else
		{
			GameController.Instance.AddPlayer( GameObject, owner );
			Name = owner.DisplayName;
		}

		OnStartMovement();

		if ( !Network.IsProxy )
		{
			OnStartStatus();
			OnStartInventory();
		}
	}

	protected override void OnUpdate()
	{
		OnUpdateMovement();

		if ( !IsProxy && Input.Pressed( "Menu" ) )
		{
			if ( PlayerHud != null ) PlayerHud.Enabled = !PlayerHud.Enabled;
			if ( PlayerTabMenu != null ) PlayerTabMenu.Enabled = !PlayerTabMenu.Enabled;
			if ( LeaderBoard != null ) LeaderBoard.Enabled = !LeaderBoard.Enabled;

			var menu = Scene.GetAllComponents<BasicMenu>().FirstOrDefault();
			if ( menu != null ) menu.Enabled = !menu.Enabled;
		}
	}

	protected override void OnFixedUpdate()
	{
		OnFixedUpdateMovement();

		if ( !IsProxy )
		{
			OnFixedUpdateStatus();
			OnFixedUpdateInventory();
			OnFixedUpdateInteraction();
		}
	}

	public void OnNetworkSpawn( Connection owner )
	{
		OnNetworkSpawnOutfitter( owner );
	}
}
