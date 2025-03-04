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
        private const string NewtonJsonPackage = "com.unity.nuget.newtonsoft-json";

        [MenuItem("ActFit/Project Initialize")]
        public static void RunInstaller()
        {
            if (_isProcessing)
            {
                Debug.Log("패키지 설치가 이미 진행 중입니다.");
                return;
            }
            
            _isProcessing = true;
            
            // 뉴턴 JSON 설치가 필요한지 확인
            if (!IsPackageInstalled(NewtonJsonPackage))
            {
                // 아직 설치되지 않았으면 설치 진행
                Debug.Log("Newtonsoft.Json 패키지가 설치되어 있지 않습니다. 먼저 설치를 진행합니다.");
                _ = new PackageInstaller().InstallNewtonsoftJson();
            }
            else if (!IsDefineSymbolAdded("INSTALL_NEWTON"))
            {
                // 패키지는 설치되어 있지만 심볼이 없으면 심볼만 추가
                Debug.Log("Newtonsoft.Json 패키지가 설치되어 있습니다. INSTALL_NEWTON 심볼을 추가합니다.");
                AddDefineSymbol("INSTALL_NEWTON");
                AssetDatabase.Refresh();
                
                // 심볼 추가 후 프로젝트 초기화 진행
                EditorApplication.delayCall += () =>
                {
                    _ = new PackageInstaller().StartInstallation();
                };
            }
            else
            {
                // 뉴턴 JSON이 설치되어 있고 심볼도 있으면 바로 초기화 진행
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

        public async Task InstallNewtonsoftJson()
        {
            try
            {
                _view = new PackageInstallerView();
                _view.ShowStartMessage();
                _view.ShowProgressMessage($"Newtonsoft.Json 패키지 설치 중...");
                
                // 패키지 설치
                var request = UnityEditor.PackageManager.Client.Add(NewtonJsonPackage);
                while (!request.IsCompleted)
                {
                    await Task.Delay(100);
                }
                
                if (request.Status == UnityEditor.PackageManager.StatusCode.Success)
                {
                    _view.ShowSuccessMessage($"Newtonsoft.Json 패키지 설치 완료!");
                    
                    // INSTALL_NEWTON 심볼 추가
                    AddDefineSymbol("INSTALL_NEWTON");
                    
                    _view.ShowSuccessMessage("INSTALL_NEWTON 심볼 추가 완료!");
                    await Task.Delay(1000);
                    
                    // 스크립트가 다시 컴파일될 때까지 대기
                    _view.ShowProgressMessage("스크립트 리컴파일 중... 잠시 기다려 주세요.");
                    AssetDatabase.Refresh();
                    
                    // 리컴파일 후 프로젝트 초기화 계속 진행
                    EditorApplication.delayCall += () =>
                    {
                        _isProcessing = false;
                        _ = new PackageInstaller().StartInstallation();
                    };
                }
                else
                {
                    _view.ShowErrorMessage($"Newtonsoft.Json 패키지 설치 실패: {request.Error?.message}");
                    _isProcessing = false;
                }
            }
            catch (Exception ex)
            {
                if (_view != null)
                {
                    _view.ShowErrorMessage($"Newtonsoft.Json 패키지 설치 중 오류 발생: {ex.Message}");
                }
                else
                {
                    Debug.LogError($"Newtonsoft.Json 패키지 설치 중 오류 발생: {ex.Message}");
                }
                _isProcessing = false;
            }
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
                
                // 설치 완료 표시
                _view.ShowSuccessMessage("모든 패키지 설치가 완료되었습니다!");
                
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
        
        private static bool IsPackageInstalled(string packageName)
        {
            var request = UnityEditor.PackageManager.Client.List(true);
            while (!request.IsCompleted)
            {
                System.Threading.Thread.Sleep(100);
            }
            
            if (request.Status == UnityEditor.PackageManager.StatusCode.Success)
            {
                foreach (var package in request.Result)
                {
                    if (package.name == packageName)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        private static bool IsDefineSymbolAdded(string symbol)
        {
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup);
            
            return defines.Contains(symbol);
        }
        
        private static void AddDefineSymbol(string symbol)
        {
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup);
            
            if (!defines.Contains(symbol))
            {
                if (defines.Length > 0)
                {
                    defines += ";";
                }
                
                defines += symbol;
                
                PlayerSettings.SetScriptingDefineSymbolsForGroup(
                    EditorUserBuildSettings.selectedBuildTargetGroup, defines);
            }
        }
    }
}