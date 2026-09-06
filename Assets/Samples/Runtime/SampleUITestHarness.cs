using UnityEngine;
using UnityEngine.SceneManagement;

namespace UISystem.Samples
{
    /// <summary>
    /// 씬 전환과 겹치기를 눌러보는 테스트 하네스.
    /// UI 를 따로 배선하지 않으려고 IMGUI 를 쓴다. 샘플 전용이다.
    /// </summary>
    public sealed class SampleUITestHarness : MonoBehaviour
    {
        [SerializeField] private string _otherScene = "SampleBattle";

        private void OnGUI()
        {
            const int width = 300;
            const int height = 32;
            var y = 10;

            var manager = UIManager.Instance;

            GUI.Label(new Rect(10, y, 520, height),
                $"활성 씬 {SceneManager.GetActiveScene().name}   로드된 씬 {SceneManager.sceneCount}   " +
                (manager == null ? "UIManager 없음" : $"스택 {manager.OpenCount}"));
            y += height + 6;

            if (GUI.Button(new Rect(10, y, width, height), $"{_otherScene} 으로 전환 (Single)"))
                SceneManager.LoadScene(_otherScene, LoadSceneMode.Single);
            y += height + 4;

            if (GUI.Button(new Rect(10, y, width, height), $"{_otherScene} 겹치기 (Additive)"))
                SceneManager.LoadScene(_otherScene, LoadSceneMode.Additive);
            y += height + 4;

            if (GUI.Button(new Rect(10, y, width, height), "겹친 씬 내리기"))
                UnloadTopScene();
            y += height + 4;

            if (GUI.Button(new Rect(10, y, width, height), "확인 팝업 열기"))
                OpenPopup();
            y += height + 4;

            if (GUI.Button(new Rect(10, y, width, height), "최상단 닫기 (뒤로가기)"))
                manager?.OnBackPressed();
        }

        private static void UnloadTopScene()
        {
            if (SceneManager.sceneCount <= 1)
                return;

            SceneManager.UnloadSceneAsync(SceneManager.GetSceneAt(SceneManager.sceneCount - 1));
        }

        private static async void OpenPopup()
        {
            var manager = UIManager.Instance;
            if (manager == null)
                return;

            await manager.OpenAsync<SampleConfirmPopup>();
        }
    }
}
