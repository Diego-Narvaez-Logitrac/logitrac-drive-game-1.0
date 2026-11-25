using System.Collections;
using LogitracDrive.TurboCircuit;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LogitracDrive.Tests
{
    public class TurboCircuit3DNavigationTests
    {
        string _savedView;

        [SetUp] public void SaveView() => _savedView = PlayerPrefs.HasKey(GamesApp.ViewPrefKey) ? PlayerPrefs.GetString(GamesApp.ViewPrefKey) : null;

        [TearDown]
        public void RestoreView()
        {
            if (_savedView == null) PlayerPrefs.DeleteKey(GamesApp.ViewPrefKey);
            else PlayerPrefs.SetString(GamesApp.ViewPrefKey, _savedView);
            PlayerPrefs.Save();
        }

        [UnityTest]
        public IEnumerator PreviewSetTo3DPlayLoads3DScene()
        {
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Scenes/Games.unity", new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;

            var app = Object.FindAnyObjectByType<GamesApp>();
            Click(app.GetCardButton(GameCatalog.TurboCircuit));
            yield return null;
            Assert.IsTrue(app.ViewSwitch.activeInHierarchy, "2D | 3D switch should show on the Turbo Circuit preview in the Editor");

            Click(app.View3DButton);
            yield return null;
            Assert.AreEqual("3D", GamesApp.TurboCircuitView);

            Click(app.PreviewPlayButton);
            for (int i = 0; i < 30 && SceneManager.GetActiveScene().name != RaceController.SceneName3D; i++) yield return null;
            yield return null;

            Assert.AreEqual(RaceController.SceneName3D, SceneManager.GetActiveScene().name, "Play did not load the 3D race");
            var race = Object.FindAnyObjectByType<RaceController>();
            Assert.IsNotNull(race);
            Assert.AreEqual(RaceViewMode.ThreeD, race.ViewMode);
        }

        static void Click(Button button)
        {
            Assert.IsNotNull(button, "Button is null");
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
        }
    }
}
