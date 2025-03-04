
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
        private readonly OpenUPMInstaller _openUPMInstaller;
        private readonly GitPackageInstaller _gitInstaller;
        private readonly SelfDestruct _selfDestruct;
        private PackageInstallerView _view;
        
        private static bool _isProcessing;
        private const string NewtonJsonSymbol = "INSTALL_NEWTON";

        [MenuItem("ActFit/Project Initialize")]
        public static void RunInstaller()
        {
            if (_isProcessing)
            {
                Debug.Log("패키지 설치가 이미 진행 중입니다.");
                return;
            }
            
            _isProcessing = true;
            
            if (!NewtonJsonInstaller.IsInstalled())
            {
                Debug.Log("Newtonsoft.Json 패키지가 설치되어 있지 않습니다. 먼저 설치를 진행합니다.");
                var installer = new PackageInstaller();
                var newtonInstaller = new NewtonJsonInstaller(installer._view);
                _ = newtonInstaller.Install().ContinueWith(_ => { _isProcessing = false; });
            }
            else if (!NewtonJsonInstaller.IsSymbolDefined())
            {
                Debug.Log("Newtonsoft.Json 패키지가 설치되어 있습니다. INSTALL_NEWTON 심볼을 추가합니다.");
                EditorSymbolsManager.AddSymbol(NewtonJsonSymbol);
                AssetDatabase.Refresh();
                
                Debug.Log("심볼 추가가 완료되었습니다. Unity 에디터가 리컴파일을 완료한 후 다시 'ActFit/Project Initialize' 메뉴를 클릭해주세요.");
                _isProcessing = false;
            }
            else
            {
                _ = new PackageInstaller().StartInstallation();
            }
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