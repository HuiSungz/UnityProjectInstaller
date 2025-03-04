using System;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace ActionFit.PackageInstaller
{
    public class NewtonJsonInstaller
    {
        private const string NewtonJsonPackage = "com.unity.nuget.newtonsoft-json";
        private const string NewtonJsonSymbol = "INSTALL_NEWTON";
        private readonly PackageInstallerView _view;
        
        public NewtonJsonInstaller(PackageInstallerView view)
        {
            _view = view;
        }
        
        public static bool IsInstalled()
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
                    if (package.name == NewtonJsonPackage)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }
        
        public static bool IsSymbolDefined()
        {
            return EditorSymbolsManager.IsSymbolDefined(NewtonJsonSymbol);
        }
        
        public async Task Install()
        {
            try
            {
                _view.ShowStartMessage();
                _view.ShowProgressMessage($"Newtonsoft.Json 패키지 설치 중...");
                
                var request = UnityEditor.PackageManager.Client.Add(NewtonJsonPackage);
                while (!request.IsCompleted)
                {
                    await Task.Delay(100);
                }
                
                if (request.Status == UnityEditor.PackageManager.StatusCode.Success)
                {
                    _view.ShowSuccessMessage($"Newtonsoft.Json 패키지 설치 완료!");
                    
                    // 패키지 설치 후 컴파일이 완료될 때까지 기다린 후 심볼 추가
                    _view.ShowProgressMessage("컴파일 완료를 기다리는 중...");
                    
                    // 컴파일러 이벤트 구독
                    bool compilationFinished = false;
                    CompilationPipeline.compilationFinished += (obj) => 
                    {
                        compilationFinished = true;
                    };
                    
                    // 컴파일 완료될 때까지 대기
                    EditorApplication.delayCall += () => {
                        WaitForCompilationAndAddSymbol();
                    };
                }
                else
                {
                    _view.ShowErrorMessage($"Newtonsoft.Json 패키지 설치 실패: {request.Error?.message}");
                }
            }
            catch (Exception ex)
            {
                _view.ShowErrorMessage($"Newtonsoft.Json 패키지 설치 중 오류 발생: {ex.Message}");
            }
        }
        
        private void WaitForCompilationAndAddSymbol()
        {
            if (EditorApplication.isCompiling)
            {
                // 아직 컴파일 중이면 다시 확인
                EditorApplication.delayCall += WaitForCompilationAndAddSymbol;
                return;
            }
            
            // 컴파일이 끝났으면 심볼 추가
            try
            {
                EditorSymbolsManager.AddSymbol(NewtonJsonSymbol);
                _view.ShowSuccessMessage("INSTALL_NEWTON 심볼 추가 완료!");
                
                // 에디터 재시작 확인 메세지
                if (EditorUtility.DisplayDialog("Unity 에디터 재시작 필요", 
                    "Newtonsoft.Json 패키지 설치 및 INSTALL_NEWTON 심볼 추가가 완료되었습니다.\n\n" +
                    "변경사항을 적용하려면 Unity 에디터를 재시작해야 합니다.\n\n" +
                    "지금 재시작하시겠습니까?", 
                    "재시작", "나중에"))
                {
                    // 에디터 재시작
                    string projectPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, ".."));
                    EditorApplication.OpenProject(projectPath);
                }
                else
                {
                    // 재시작하지 않음을 선택한 경우 메세지
                    _view.ShowProgressMessage("유니티 에디터를 재시작한 후 다시 'ActFit/Project Initialize' 메뉴를 클릭해주세요.");
                }
            }
            catch (Exception ex)
            {
                _view.ShowErrorMessage($"심볼 추가 중 오류 발생: {ex.Message}");
            }
        }
    }
}