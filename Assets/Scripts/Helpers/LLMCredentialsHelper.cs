using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Scripts.Helpers
{
    /// <summary>
    /// LLMCREDENTIALSHELPER - Cross-app credential store for MindAttic LLM integrations.
    /// <para>PURPOSE: Every MindAttic app reads LLM API keys from one place so a single
    /// credential change applies everywhere. This file is intentionally dependency-light
    /// (Newtonsoft.Json + System.IO) so any MindAttic C# app can paste it in as-is.</para>
    /// <para>RESOLUTION ORDER (first match wins):
    /// 1) %APPDATA%\MindAttic\LLM\{provider}.json with a non-empty apiKey
    /// 2) %APPDATA%\MindAttic\LLM\providers.json entry for {provider} with a non-empty apiKey
    /// 3) Environment variable {PROVIDER}_API_KEY (apiKey only)
    /// An entry with an empty apiKey is treated as "no match" so the next tier is tried.</para>
    /// <para>RELATED FILES: %APPDATA%\MindAttic\LLM\README.md (cross-app convention doc).</para>
    /// </summary>
    public static class LLMCredentialsHelper
    {
        private const string IndexFileName = "providers.json";

        private static readonly Dictionary<string, LLMCredential> cache =
            new Dictionary<string, LLMCredential>(StringComparer.OrdinalIgnoreCase);

        private static Dictionary<string, LLMCredential> indexCache;

        /// <summary>Absolute path to the MindAttic LLM credential directory on this machine.</summary>
        public static string Directory =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MindAttic", "LLM");

        /// <summary>Load credentials for a provider; throws if nothing is found.</summary>
        public static LLMCredential Get(string provider)
        {
            if (TryGet(provider, out var cred)) return cred;
            throw new FileNotFoundException(
                $"No credentials for provider '{provider}'. " +
                $"Add it to {Path.Combine(Directory, IndexFileName)}, " +
                $"create {Path.Combine(Directory, provider + ".json")}, " +
                $"or set {provider.ToUpperInvariant()}_API_KEY.");
        }

        /// <summary>Non-throwing lookup. Returns true only if an apiKey was resolved.</summary>
        public static bool TryGet(string provider, out LLMCredential credential)
        {
            if (string.IsNullOrWhiteSpace(provider))
            {
                credential = null;
                return false;
            }

            if (cache.TryGetValue(provider, out credential))
                return credential != null;

            credential =
                LoadFromFile(provider) ??
                LoadFromIndex(provider) ??
                LoadFromEnv(provider);

            cache[provider] = credential;
            return credential != null && !string.IsNullOrEmpty(credential.ApiKey);
        }

        /// <summary>Shortcut for the common case — just the raw API key.</summary>
        public static string GetApiKey(string provider) => Get(provider).ApiKey;

        /// <summary>Enumerate every provider present in providers.json (regardless of apiKey).</summary>
        public static IEnumerable<string> ListProvidersFromIndex()
        {
            var idx = EnsureIndex();
            return idx != null ? idx.Keys : Array.Empty<string>();
        }

        /// <summary>Clear the in-process cache; call after editing a credential file at runtime.</summary>
        public static void Invalidate()
        {
            cache.Clear();
            indexCache = null;
        }

        private static LLMCredential LoadFromFile(string provider)
        {
            var path = Path.Combine(Directory, provider + ".json");
            if (!File.Exists(path)) return null;

            try
            {
                var json = File.ReadAllText(path);
                var cred = JsonConvert.DeserializeObject<LLMCredential>(json);
                if (cred == null || string.IsNullOrEmpty(cred.ApiKey)) return null;
                cred.Provider = provider;
                cred.Source = path;
                return cred;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LLMCredentials] Failed to parse {path}: {ex.Message}");
                return null;
            }
        }

        private static LLMCredential LoadFromIndex(string provider)
        {
            var idx = EnsureIndex();
            if (idx == null) return null;
            if (!idx.TryGetValue(provider, out var cred) || cred == null) return null;
            if (string.IsNullOrEmpty(cred.ApiKey)) return null;
            cred.Provider = provider;
            cred.Source = Path.Combine(Directory, IndexFileName);
            return cred;
        }

        private static LLMCredential LoadFromEnv(string provider)
        {
            var envVar = provider.ToUpperInvariant() + "_API_KEY";
            var key = Environment.GetEnvironmentVariable(envVar);
            if (string.IsNullOrEmpty(key)) return null;
            return new LLMCredential
            {
                Provider = provider,
                ApiKey = key,
                Source = "env:" + envVar,
            };
        }

        private static Dictionary<string, LLMCredential> EnsureIndex()
        {
            if (indexCache != null) return indexCache;

            var path = Path.Combine(Directory, IndexFileName);
            if (!File.Exists(path)) { indexCache = new Dictionary<string, LLMCredential>(StringComparer.OrdinalIgnoreCase); return indexCache; }

            try
            {
                var json = File.ReadAllText(path);
                var parsed = JsonConvert.DeserializeObject<Dictionary<string, LLMCredential>>(json);
                indexCache = parsed != null
                    ? new Dictionary<string, LLMCredential>(parsed, StringComparer.OrdinalIgnoreCase)
                    : new Dictionary<string, LLMCredential>(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LLMCredentials] Failed to parse {path}: {ex.Message}");
                indexCache = new Dictionary<string, LLMCredential>(StringComparer.OrdinalIgnoreCase);
            }

            return indexCache;
        }
    }

    /// <summary>
    /// LLMCREDENTIAL - One provider's credential bundle. Serialized shape matches both
    /// %APPDATA%\MindAttic\LLM\{provider}.json and the per-entry value inside providers.json.
    /// Unknown JSON fields land in <see cref="Extra"/> so apps can attach provider-specific
    /// settings without schema changes.
    /// </summary>
    public sealed class LLMCredential
    {
        [JsonIgnore] public string Provider { get; set; }
        [JsonIgnore] public string Source { get; set; }

        [JsonProperty("apiKey")]    public string ApiKey    { get; set; }
        [JsonProperty("baseUrl")]   public string BaseUrl   { get; set; }
        [JsonProperty("model")]     public string Model     { get; set; }
        [JsonProperty("org")]       public string Org       { get; set; }
        [JsonProperty("type")]      public string Type      { get; set; }
        [JsonProperty("maxTokens")] public int?   MaxTokens { get; set; }
        [JsonProperty("extra")]     public JObject Extra    { get; set; }
    }
}
