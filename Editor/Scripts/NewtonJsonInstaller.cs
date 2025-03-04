
using System;
using System.Threading.Tasks;

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
                    
                    EditorSymbolsManager.AddSymbol(NewtonJsonSymbol);
                    
                    _view.ShowSuccessMessage("INSTALL_NEWTON 심볼 추가 완료!");
                    
                    _view.ShowProgressMessage("유니티 에디터가 리컴파일을 완료한 후 다시 'ActFit/Project Initialize' 메뉴를 클릭해주세요.");
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
    }
}