using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace Sandbox;

/// <summary>
/// Manages chat state — periodically syncs messages from Network Storage,
/// sends new messages via the "send-message" endpoint, and keeps
/// a local list (last 20) for the UI to read.
/// </summary>
public sealed class ChatManager : Component
{
	public static ChatManager Instance { get; private set; }

	public List<ChatMessage> Messages { get; private set; } = new();
	public bool IsLoaded { get; private set; }
	public bool IsSending { get; private set; }

	/// <summary>How often (in seconds) to sync messages from the server.</summary>
	[Property] public float SyncInterval { get; set; } = 10f;

	/// <summary>Seconds remaining until the next server sync.</summary>
	public float SecondsUntilSync => MathF.Max( 0, _nextSyncTime - RealTime.Now );

	private float _nextSyncTime;
	private bool _isSyncing;

	private const int MaxMessages = 20;

	protected override void OnStart()
	{
		Instance = this;
		_ = LoadMessagesAsync();
	}

	protected override void OnUpdate()
	{
		if ( !IsLoaded ) return;
		if ( _isSyncing ) return;

		if ( RealTime.Now >= _nextSyncTime )
		{
			_ = SyncMessagesAsync();
		}
	}

	private void ScheduleNextSync()
	{
		_nextSyncTime = RealTime.Now + SyncInterval;
	}

	private async Task LoadMessagesAsync()
	{
		await FetchMessages();
		IsLoaded = true;
		ScheduleNextSync();
	}

	private async Task SyncMessagesAsync()
	{
		_isSyncing = true;
		await FetchMessages();
		_isSyncing = false;
		ScheduleNextSync();
	}

	private async Task FetchMessages()
	{
		var result = await NetworkStorage.CallEndpoint( "load-messages" );

		if ( !result.HasValue )
			return;

		var data = result.Value;

		if ( !data.TryGetProperty( "messages", out var arr ) || arr.ValueKind != JsonValueKind.Array )
			return;

		var all = new List<ChatMessage>();

		foreach ( var msg in arr.EnumerateArray() )
		{
			var text = JsonHelpers.GetString( msg, "text", "" );
			var name = JsonHelpers.GetString( msg, "displayName", "Unknown" );
			var tsRaw = JsonHelpers.GetString( msg, "timestamp", "" );
			long ts = DateTimeOffset.TryParse( tsRaw, out var dto )
				? dto.ToUnixTimeSeconds()
				: (long)JsonHelpers.GetFloat( msg, "timestamp", 0 );
			all.Add( new ChatMessage( name, text, ts ) );
		}

		// Keep only the last 20 messages
		if ( all.Count > MaxMessages )
			all = all.GetRange( all.Count - MaxMessages, MaxMessages );

		Messages = all;
		Log.Info( $"[Chat] Synced {Messages.Count} messages" );
	}

	public async Task SendMessage( string text )
	{
		if ( string.IsNullOrWhiteSpace( text ) )
			return;

		if ( IsSending )
			return;

		IsSending = true;

		var displayName = Connection.Local?.DisplayName ?? "Player";
		var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

		// Optimistic — add locally right away
		var msg = new ChatMessage( displayName, text, timestamp );
		Messages.Add( msg );

		// Trim to last 20 locally too
		if ( Messages.Count > MaxMessages )
			Messages = Messages.GetRange( Messages.Count - MaxMessages, MaxMessages );

		var result = await NetworkStorage.CallEndpoint( "send-message", new
		{
			text,
			displayName
		} );

		if ( !result.HasValue )
		{
			Messages.Remove( msg );
			Log.Warning( "[Chat] Failed to send message" );
		}

		IsSending = false;
	}
}
