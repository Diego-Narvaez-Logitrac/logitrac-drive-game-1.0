using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace LogitracDrive.Tests
{
    public class GamesNavigationTests
    {
        const string ScenePath = "Assets/Scenes/Games.unity";

        [UnityTest]
        public IEnumerator OpenCardThenBackReturnsToGames()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync("Games");
#endif
            yield return null; // let Awake/layout run

            var app = Object.FindAnyObjectByType<GamesApp>();
            Assert.IsNotNull(app, "GamesApp not found in Games scene");
            Assert.IsTrue(app.GamesPanel.activeInHierarchy, "App should open on Games");

            Click(app.GetCardButton(GameCatalog.ConvoyRush));
            yield return null;
            Assert.IsTrue(app.PreviewPanel.activeInHierarchy, "Card did not open its preview");
            Assert.IsFalse(app.GamesPanel.activeInHierarchy, "Games panel still visible");
            Assert.AreEqual("Convoy Rush", app.PreviewTitle.text);

            Click(app.BackButton);
            yield return null;
            Assert.IsTrue(app.GamesPanel.activeInHierarchy, "Back did not return to Games");
            Assert.IsFalse(app.PreviewPanel.activeInHierarchy);
        }

        /// <summary>Sends a pointer-click event through the EventSystem rather than calling onClick directly.</summary>
        static void Click(Button button)
        {
            Assert.IsNotNull(button, "Button is null");
            Assert.IsTrue(button.isActiveAndEnabled && button.IsInteractable(), $"{button.name} is not clickable");
            var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerClickHandler);
        }
    }
}
