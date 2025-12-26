#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Assets.EditorTools
{
	/// <summary>
	/// Editor window that displays the runtime CombatLog.
	/// Open via Tools -> Combat Log. Works while the game is running in the Editor.
	/// </summary>
	public class CombatLogWindow : EditorWindow
	{
		private Vector2 _scroll;
		private bool _autoScroll = true;
		private bool _wrap = true;

		/// <summary>
		/// Create or focus the Combat Log window.
		/// </summary>
		[MenuItem("Window/Combat Log")]
		public static void ShowWindow()
		{
			var window = GetWindow<CombatLogWindow>("Combat Log");
			window.minSize = new Vector2(400, 200);
			window.Show();
		}

		/// <summary>
		/// Subscribe to CombatLog events when the window is enabled.
		/// </summary>
		private void OnEnable()
		{
			Assets.Helpers.CombatLogHelper.OnWrite += HandleWrite;
			EditorApplication.playModeStateChanged += HandlePlayModeChanged;
		}

		/// <summary>
		/// Unsubscribe from events when the window is disabled.
		/// </summary>
		private void OnDisable()
		{
			Assets.Helpers.CombatLogHelper.OnWrite -= HandleWrite;
			EditorApplication.playModeStateChanged -= HandlePlayModeChanged;
		}

		/// <summary>
		/// Repaints on incoming messages to keep the view live.
		/// </summary>
		private void HandleWrite(string _)
		{
			Repaint();
		}

		/// <summary>
		/// Clears scroll on domain reloads and mode switches to avoid stale positions.
		/// </summary>
		private void HandlePlayModeChanged(PlayModeStateChange change)
		{
			_scroll = Vector2.zero;
			Repaint();
		}

		/// <summary>
		/// Draws toolbar and the scrollable log area.
		/// </summary>
		private void OnGUI()
		{
			DrawToolbar();
			DrawLogArea(Assets.Helpers.CombatLogHelper.Messages);
		}

		/// <summary>
		/// Renders the top toolbar with actions.
		/// </summary>
		private void DrawToolbar()
		{
			using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
			{
				if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
				{
					Assets.Helpers.CombatLogHelper.Clear();
				}

				if (GUILayout.Button("Copy", EditorStyles.toolbarButton))
				{
					CopyAllToClipboard();
				}

				GUILayout.FlexibleSpace();

				_wrap = GUILayout.Toggle(_wrap, "Wrap", EditorStyles.toolbarButton);
				_autoScroll = GUILayout.Toggle(_autoScroll, "Auto Scroll", EditorStyles.toolbarButton);
			}
		}

		/// <summary>
		/// Copies the entire combat log into the system clipboard as plain text.
		/// </summary>
		private void CopyAllToClipboard()
		{
			var messages = Assets.Helpers.CombatLogHelper.Messages;
			if (messages == null || messages.Count == 0)
			{
				EditorGUIUtility.systemCopyBuffer = string.Empty;
				ShowNotification(new GUIContent("Combat Log is empty"));
				return;
			}

			var sb = new StringBuilder(4096);
			for (int i = 0; i < messages.Count; i++)
			{
				var msg = messages[i];
				if (!string.IsNullOrEmpty(msg)) sb.AppendLine(msg);
			}

			EditorGUIUtility.systemCopyBuffer = sb.ToString();
			ShowNotification(new GUIContent("Combat Log copied"));
		}

		/// <summary>
		/// Renders the message list with optional wrapping and auto scroll behavior.
		/// </summary>
		private void DrawLogArea(IReadOnlyList<string> messages)
		{
			var prevWordWrap = EditorStyles.label.wordWrap;
			EditorStyles.label.wordWrap = _wrap;

			_scroll = EditorGUILayout.BeginScrollView(_scroll);
			{
				if (messages == null || messages.Count == 0)
				{
					GUILayout.Label("No combat messages yet.");
				}
				else
				{
					for (int i = 0; i < messages.Count; i++)
					{
						GUILayout.Label(messages[i]);
					}

					if (_autoScroll && Event.current.type == EventType.Repaint)
					{
						_scroll.y = float.MaxValue;
					}
				}
			}
			EditorGUILayout.EndScrollView();

			EditorStyles.label.wordWrap = prevWordWrap;
		}
	}
}
#endif
