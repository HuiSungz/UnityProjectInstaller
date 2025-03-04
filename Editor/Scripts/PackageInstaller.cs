
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace ActionFit.PackageInstaller
{
    public class PackageInstaller
    {
        private readonly PackageInstallerModel _model;
        private readonly SimpleOpenUPMInstaller _openUPMInstaller;
        private readonly GitPackageInstaller _gitInstaller;
        private readonly SelfDestruct _selfDestruct;
        private PackageInstallerView _view;
        
        private static bool _isProcessing;
        
        [MenuItem("ActFit/Project Initialize Safe")]
        public static void RunInstallerSafe()
        {
            if (_isProcessing)
            {
                Debug.Log("패키지 설치가 이미 진행 중입니다.");
                return;
            }
            
            _isProcessing = true;
            
            try
            {
                var installer = new PackageInstaller();
                
                // Task를 Fire-and-forget으로 실행하지 않고 동기적으로 처리
                EditorApplication.delayCall += async () =>
                {
                    try
                    {
                        await installer.StartInstallation();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"패키지 설치 중 오류 발생: {ex.Message}");
                    }
                    finally
                    {
                        _isProcessing = false;
                    }
                };
            }
            catch (Exception ex)
            {
                Debug.LogError($"패키지 설치 초기화 중 오류 발생: {ex.Message}");
                _isProcessing = false;
            }
        }

        public PackageInstaller()
        {
            _model = new PackageInstallerModel();
            _view = new PackageInstallerView();
            _openUPMInstaller = new SimpleOpenUPMInstaller();
            _gitInstaller = new GitPackageInstaller();
            _selfDestruct = new SelfDestruct();
            
            _model.SetOpenUPMPackages(new List<string>
            {
                "jp.hadashikick.vcontainer",
                "com.cysharp.unitask",
                "com.cysharp.zstring"
            });
            
            _model.SetGitPackages(new List<string>
            {
                "https://github.com/HuiSungz/UnityProjectCore.git"
            });
        }

        public async Task StartInstallation()
        {
            try
            {
                _view.ShowStartMessage();
                
                await InstallOpenUPMPackages();
                
                await InstallGitPackages();
                
                _view.ShowSuccessMessage("모든 패키지 설치가 완료되었습니다!");
                
                _view.ShowSelfDestructMessage();
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken: _model.CancellationToken);
                
                _selfDestruct.RemovePackage();
            }
            catch (OperationCanceledException)
            {
                _view.ShowCancelledMessage();
            }
            catch (Exception ex)
            {
                _view.ShowErrorMessage(ex.Message);
            }
        }

        private async Task InstallOpenUPMPackages()
        {
            var total = _model.OpenUPMPackages.Count;
            for (var i = 0; i < total; i++)
            {
                var package = _model.OpenUPMPackages[i];
                _view.ShowProgressMessage($"OpenUPM 패키지 설치 중... ({i+1}/{total}): {package}");
                
                await _openUPMInstaller.InstallPackage(package);
                
                _view.ShowSuccessMessage($"{package} 설치 완료!");
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken: _model.CancellationToken);
            }
        }

        private async Task InstallGitPackages()
        {
            var total = _model.GitPackages.Count;
            for (var i = 0; i < total; i++)
            {
                var package = _model.GitPackages[i];
                _view.ShowProgressMessage($"Git 패키지 설치 중... ({i+1}/{total}): {package}");
                
                await _gitInstaller.InstallPackage(package);
                
                _view.ShowSuccessMessage($"{package} 설치 완료!");
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken: _model.CancellationToken);
            }
        }
    }
}