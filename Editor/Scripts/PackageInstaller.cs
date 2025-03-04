
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
        private readonly PackageInstallerView _view;
        private readonly OpenUPMInstaller _openUPMInstaller;
        private readonly GitPackageInstaller _gitInstaller;
        private readonly SelfDestruct _selfDestruct;
        
        private static bool _isProcessing;

        [MenuItem("ActFit/Project Initialize")]
        public static void RunInstaller()
        {
            if (_isProcessing)
            {
                Debug.Log("패키지 설치가 이미 진행 중입니다.");
                return;
            }
            
            _isProcessing = true;
            _ = new PackageInstaller().StartInstallation();
        }

        public PackageInstaller()
        {
            _model = new PackageInstallerModel();
            _view = new PackageInstallerView();
            _openUPMInstaller = new OpenUPMInstaller();
            _gitInstaller = new GitPackageInstaller();
            _selfDestruct = new SelfDestruct();
            
            _model.SetOpenUPMPackages(new List<string>
            {
                "jp.hadashikick.vcontainer",
                "com.cysharp.unitask",
                "com.google.external-dependency-manager"
            });
            
            _model.SetGitPackages(new List<string>
            {
                "https://github.com/HuiSungz/Unity2D-ProjectSetupProcessor.git"
            });
        }

        public async Task StartInstallation()
        {
            try
            {
                _view.ShowStartMessage();
                
                // OpenUPM 패키지 설치
                await InstallOpenUPMPackages();
                
                // Git 패키지 설치
                await InstallGitPackages();
                
                // 설치 완료 후 자기 자신 제거
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
            finally
            {
                _isProcessing = false;
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