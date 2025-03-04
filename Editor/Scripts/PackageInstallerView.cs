
using UnityEditor;
using UnityEngine;

namespace ActionFit.PackageInstaller
{
    public class PackageInstallerView
    {
        private const string LogPrefix = "[패키지 인스톨러] ";

        public void ShowStartMessage()
        {
            EditorUtility.DisplayProgressBar("패키지 인스톨러", "설치 준비 중...", 0f);
            Debug.Log($"{LogPrefix} 패키지 설치를 시작합니다.");
        }

        public void ShowProgressMessage(string message)
        {
            EditorUtility.DisplayProgressBar("패키지 인스톨러", message, 0.5f);
            Debug.Log($"{LogPrefix} {message}");
        }

        public void ShowSuccessMessage(string message)
        {
            EditorUtility.DisplayProgressBar("패키지 인스톨러", message, 0.8f);
            Debug.Log($"{LogPrefix} {message}");
        }

        public void ShowErrorMessage(string message)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"{LogPrefix} 오류 발생: {message}");
            EditorUtility.DisplayDialog("패키지 인스톨러 오류", $"패키지 설치 중 오류가 발생했습니다: {message}", "확인");
        }

        public void ShowCancelledMessage()
        {
            EditorUtility.ClearProgressBar();
            Debug.Log($"{LogPrefix} 패키지 설치가 취소되었습니다.");
        }

        public void ShowSelfDestructMessage()
        {
            EditorUtility.DisplayProgressBar("패키지 인스톨러", "모든 패키지 설치 완료! 인스톨러 제거 중...", 1f);
            Debug.Log($"{LogPrefix} 모든 패키지 설치가 완료되었습니다. 인스톨러를 제거합니다.");
        }
    }
}