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
    public class TurboCircuitNavigationTests
    {
        [UnityTest]
        public IEnumerator GamesToTurboCircuitPlayLoadsRaceScene()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Scenes/Games.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync("Games");
#endif
            yield return null;

            var app = Object.FindAnyObjectByType<GamesApp>();
            Assert.IsNotNull(app, "GamesApp not found in Games scene");

            Click(app.GetCardButton(GameCatalog.TurboCircuit));
            yield return null;
            Assert.AreEqual(GameCatalog.TurboCircuit, app.CurrentGame?.id, "Turbo Circuit preview did not open");

            Click(app.PreviewPlayButton);
            for (int i = 0; i < 30 && SceneManager.GetActiveScene().name != RaceController.SceneName; i++) yield return null;
            yield return null;

            Assert.AreEqual(RaceController.SceneName, SceneManager.GetActiveScene().name, "Play did not load the race scene");
            var race = Object.FindAnyObjectByType<RaceController>();
            Assert.IsNotNull(race, "RaceController not found in the race scene");
            Assert.AreEqual(RacePhase.Countdown, race.Race.phase, "Race should start with the countdown");
        }

        static void Click(Button button)
        {
            Assert.IsNotNull(button, "Button is null");
            var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
        }
    }
}
