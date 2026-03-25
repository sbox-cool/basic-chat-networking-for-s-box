namespace Sandbox;

/// <summary>
/// Network Storage credentials are loaded automatically — no manual setup needed in code.
///
/// How it works:
///   1. Open the s&box editor
///   2. Go to Editor → Network Storage → Setup
///   3. Enter your Project ID and API keys from sboxcool.com
///   4. Click "Save Configuration"
///   5. The Setup window writes Assets/network-storage.credentials.json
///   6. At runtime, the first API call triggers AutoConfigure() which reads that file
///
/// The credentials file (Assets/network-storage.credentials.json) looks like:
///   {
///     "projectId": "your_project_id_from_dashboard",
///     "publicKey": "sbox_ns_your_public_key",
///     "baseUrl": "https://api.sboxcool.com",
///     "apiVersion": "v3"
///   }
///
/// If you need to override credentials manually (not recommended):
///   NetworkStorage.Configure( "projectId", "sbox_ns_publicKey" );
///
/// Troubleshooting:
///   - 401 Unauthorized → Credentials are wrong or not loaded. Check the file exists.
///   - 400 Bad Request  → Endpoint or collection not pushed. Use Editor → Network Storage → Sync Tool.
///   - After changing credentials, do a full restart of s&box (not hot-reload)
///     because the static auto-config flag persists across hot-reloads.
/// </summary>
public static class NetworkStorageConfig
{
	// Intentionally empty — auto-configuration handles everything.
	// See class summary above for setup instructions.
}
