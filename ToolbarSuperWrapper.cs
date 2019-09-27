using System.Collections.Generic;
using KSP.UI.Screens;
using UnityEngine;

namespace DaMichelToolbarSuperWrapper
{
	public class ToolbarInfo
	{
		public string launcherTexture;
		public string name;
		public string toolbarTexture;
		public string tooltip;
		public GameScenes[] visibleInScenes;
	}


	public abstract class PluginWithToolbarSupport : MonoBehaviour
	{
		private ApplicationLauncherButton applauncherButton;

		private IButton toolbarButton;
		private bool useAppLauncher = true;
		private bool useToolbar = true;
		private bool visibleByKspGui = true;
		private bool visibleByToolbars = true;

		protected bool isGuiVisible => visibleByKspGui && visibleByToolbars;

		protected abstract ToolbarInfo GetToolbarInfo();

		protected abstract void OnGuiVisibilityChange();

		protected void SaveImmutableToolbarSettings(ConfigNode node)
		{
			node.AddValue("useToolbar", useToolbar);
			node.AddValue("useAppLauncher", useAppLauncher);
		}

		protected void SaveMutableToolbarSettings(ConfigNode node)
		{
			node.AddValue("active", visibleByToolbars);
		}

		protected void LoadImmutableToolbarSettings(ConfigNode node)
		{
			node.TryGetValue("useToolbar", ref useToolbar);
			node.TryGetValue("useAppLauncher", ref useAppLauncher);
		}

		protected void LoadMutableToolbarSettings(ConfigNode node)
		{
			node.TryGetValue("active", ref visibleByToolbars);
		}

		private void OnHideByToolbar()
		{
			visibleByToolbars = false;
			OnGuiVisibilityChange();
		}

		private void OnShowByToolbar()
		{
			visibleByToolbars = true;
			OnGuiVisibilityChange();
		}

		private void OnHideByKspGui()
		{
			visibleByKspGui = false;
			OnGuiVisibilityChange();
		}

		private void OnShowByKspGui()
		{
			visibleByKspGui = true;
			OnGuiVisibilityChange();
		}

		protected void InitializeToolbars()
		{
			if (ToolbarManager.ToolbarAvailable && useToolbar && toolbarButton == null) {
				var tb = GetToolbarInfo();
				toolbarButton = ToolbarManager.Instance.add(tb.name, tb.name);
				toolbarButton.TexturePath = tb.toolbarTexture;
				toolbarButton.ToolTip = tb.tooltip;
				toolbarButton.Visibility = new GameScenesVisibility(tb.visibleInScenes);
				toolbarButton.Enabled = true;
				toolbarButton.OnClick += e => {
					visibleByToolbars = !visibleByToolbars;
					OnGuiVisibilityChange();
				};
			}

			GameEvents.onHideUI.Add(OnHideByKspGui);
			GameEvents.onShowUI.Add(OnShowByKspGui);
			if (useAppLauncher) {
				GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
			}
		}

		private void OnGUIAppLauncherReady()
		{
			if (applauncherButton == null) {
				var tb = GetToolbarInfo();
				var m = new Dictionary<GameScenes, ApplicationLauncher.AppScenes>();
				m.Add(GameScenes.FLIGHT, ApplicationLauncher.AppScenes.FLIGHT);
				m.Add(GameScenes.EDITOR, ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB);
				m.Add(GameScenes.SPACECENTER, ApplicationLauncher.AppScenes.SPACECENTER);
				// and so on ...
				var v = ApplicationLauncher.AppScenes.NEVER;
				foreach (var s in tb.visibleInScenes) {
					v |= m[s];
				}

				applauncherButton = ApplicationLauncher.Instance.AddModApplication(
					OnShowByToolbar,
					OnHideByToolbar,
					null,
					null,
					null,
					null,
					v,
					GameDatabase.Instance.GetTexture(tb.launcherTexture, false));
				if (visibleByToolbars) {
					applauncherButton.SetTrue(false);
				}
			}
		}

		protected void TearDownToolbars()
		{
			// unregister, or else errors occur
			GameEvents.onHideUI.Remove(OnHideByKspGui);
			GameEvents.onShowUI.Remove(OnShowByKspGui);
			GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);

			if (applauncherButton != null) {
				ApplicationLauncher.Instance.RemoveModApplication(applauncherButton);
				applauncherButton = null;
			}

			if (toolbarButton != null) {
				toolbarButton.Destroy();
				toolbarButton = null;
			}
		}
	}
}