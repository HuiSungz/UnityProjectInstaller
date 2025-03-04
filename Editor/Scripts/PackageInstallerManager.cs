
using UnityEditor;
using UnityEngine;

namespace ActionFit.PackageInstaller
{
    [InitializeOnLoad]
    public static class PackageInstallerManager
    {
        private static readonly InstallationStateManager StateManager = new();
        private static readonly PackageListManager ListManager = new();
        private static readonly OpenUPMRegistryManager RegistryManager = new();
        
        private static InstallationProcessor _installationProcessor;

        static PackageInstallerManager()
        {
            // Editor 시작 시 이전 설치 상태가 있으면 재개
            EditorApplication.delayCall += () =>
            {
                if (!StateManager.IsInstalling)
                {
                    return;
                }
                Debug.Log("[패키지 설치] 이전 설치를 재개합니다...");
                ResumeInstallation();
            };
        }

        [MenuItem("ActFit/Project Initialize")]
        public static void RunInstaller()
        {
            if (StateManager.IsInstalling)
            {
                Debug.Log("[패키지 설치] 이미 설치가 진행 중입니다.");
                return;
            }
            
            _installationProcessor = new InstallationProcessor(ListManager, StateManager, RegistryManager);
            _installationProcessor.StartNewInstallation();
        }

        private static void ResumeInstallation()
        {
            _installationProcessor = new InstallationProcessor(ListManager, StateManager, RegistryManager);
            _installationProcessor.ResumeInstallation();
        }

        [MenuItem("ActFit/Utilities/Reset Package Installation")]
        public static void ResetInstallation()
        {
            if (!EditorUtility.DisplayDialog("패키지 설치 초기화",
                    "패키지 설치 상태를 초기화하시겠습니까?\n\n이 작업은 현재 진행 중인 설치를 취소하고 처음부터 다시 시작할 수 있게 합니다.",
                    "초기화", "취소"))
            {
                return;
            }
            
            StateManager.Reset();
            Debug.Log("[패키지 설치] 패키지 설치 상태가 초기화되었습니다. ActFit/Project Initialize 메뉴를 통해 다시 시작하세요.");
        }
    }
}