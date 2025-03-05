using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ActionFit.PackageInstaller
{
    internal class InstallationProcessor
    {
        private readonly List<string> _allPackages;
        private readonly InstallationStateManager _stateManager;
        private readonly PackageListManager _listManager;
        private readonly OpenUPMRegistryManager _registryManager;

        public InstallationProcessor(PackageListManager listManager, InstallationStateManager stateManager, OpenUPMRegistryManager registryManager)
        {
            _listManager = listManager;
            _stateManager = stateManager;
            _registryManager = registryManager;
            _allPackages = _listManager.GetAllPackages();
        }

        public void StartNewInstallation()
        {
            if (_allPackages.Count == 0)
            {
                Debug.Log("[패키지 설치] 설치할 패키지가 없습니다.");
                return;
            }

            _stateManager.IsInstalling = true;
            _stateManager.CurrentPackageIndex = 0;
            _stateManager.TotalPackages = _allPackages.Count;

            Debug.Log($"[패키지 설치] 설치를 시작합니다. 총 {_allPackages.Count}개 패키지");

            // OpenUPM 레지스트리 구성
            _registryManager.EnsureRegistry(_listManager.GetOpenUPMPackages());

            InstallNextPackage();
        }

        public void ResumeInstallation()
        {
            Debug.Log($"[패키지 설치] 설치를 재개합니다. ({_stateManager.CurrentPackageIndex + 1}/{_stateManager.TotalPackages})");
            InstallNextPackage();
        }

        private void InstallNextPackage()
        {
            var index = _stateManager.CurrentPackageIndex;
            if (index >= _stateManager.TotalPackages)
            {
                CompleteInstallation();
                return;
            }

            var packageIdentifier = _allPackages[index];
            EditorUtility.DisplayProgressBar("패키지 설치",
                $"({index + 1}/{_stateManager.TotalPackages}) {packageIdentifier}",
                (float)index / _stateManager.TotalPackages);

            // 유니티 레지스트리 패키지인지 확인
            bool isUnityRegistryPackage = _listManager.GetUnityRegistryPackages().Contains(packageIdentifier);
            
            var task = new PackageInstallationTask(packageIdentifier, isUnityRegistryPackage);
            task.Start(success =>
            {
                // 패키지 타입에 따라 다른 지연 시간 적용
                int delayFrames;
                if (task.IsGitPackage())
                {
                    delayFrames = 20;
                }
                else if (task.IsUnityRegistryPackage())
                {
                    delayFrames = 15; // Unity 패키지는 중간 정도의 지연
                }
                else
                {
                    delayFrames = 10;
                }
                
                DelayedCall(delayFrames, () =>
                {
                    _stateManager.CurrentPackageIndex++;
                    InstallNextPackage();
                });
            });
        }

        // EditorApplication.delayCall를 재귀적으로 호출하여 프레임 지연 효과를 줍니다.
        private void DelayedCall(int remainingFrames, Action callback)
        {
            if (remainingFrames <= 0)
            {
                callback();
            }
            else
            {
                EditorApplication.delayCall += () => DelayedCall(remainingFrames - 1, callback);
            }
        }

        private void CompleteInstallation()
        {
            _stateManager.IsInstalling = false;
            _stateManager.Reset();
            EditorUtility.ClearProgressBar();
            Debug.Log("[패키지 설치] 모든 패키지 설치가 완료되었습니다.");

            // 인스톨러 패키지 자체 제거
            InstallerPackageRemover.RemoveInstallerPackage();
        }
    }
}