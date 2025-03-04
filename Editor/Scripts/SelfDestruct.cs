using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ActionFit.PackageInstaller
{
    public class SelfDestruct
    {
        public void RemovePackage()
        {
            // 심볼 제거 (필요하다면)
            if (ShouldRemoveSymbol("INSTALL_NEWTON"))
            {
                RemoveDefineSymbol("INSTALL_NEWTON");
                Debug.Log("INSTALL_NEWTON 심볼이 제거되었습니다.");
            }
            
            // 패키지 제거
            RemovePackageInternal();
        }
        
        private void RemovePackageInternal()
        {
            // 현재 패키지의 이름 가져오기 (package.json에서)
            var packageName = GetCurrentPackageName();
            if (string.IsNullOrEmpty(packageName))
            {
                Debug.LogError("패키지 이름을 찾을 수 없습니다.");
                return;
            }

            // 패키지 제거
            EditorApplication.delayCall += () =>
            {
                // 진행 상태 표시
                EditorUtility.DisplayProgressBar("패키지 제거 중", $"패키지 '{packageName}'를 제거하는 중...", 0.5f);
                
                // 패키지 제거 요청
                var request = UnityEditor.PackageManager.Client.Remove(packageName);
                
                // 완료 여부 확인을 위한 변수 초기화
                bool isCompleted = false;
                
                // 결과 확인을 위한 콜백 등록
                EditorApplication.update += CheckRemovalProgress;
                
                void CheckRemovalProgress()
                {
                    if (isCompleted) return;
                    
                    if (request.IsCompleted)
                    {
                        isCompleted = true;
                        EditorApplication.update -= CheckRemovalProgress;
                        
                        if (request.Status == UnityEditor.PackageManager.StatusCode.Success)
                        {
                            Debug.Log($"패키지 '{packageName}'가 성공적으로 제거되었습니다.");
                        }
                        else
                        {
                            Debug.LogError($"패키지 '{packageName}' 제거 실패: {request.Error?.message}");
                        }
                        
                        EditorUtility.ClearProgressBar();
                    }
                }
            };
        }

        private string GetCurrentPackageName()
        {
            var packagePath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject
                (ScriptableObject.CreateInstance<DummyScriptableObject>()));
                
            if (string.IsNullOrEmpty(packagePath))
            {
                return null;
            }
            
            var scriptDirectory = Path.GetDirectoryName(packagePath);

            if (string.IsNullOrEmpty(scriptDirectory))
            {
                return null;
            }

            var directory = new DirectoryInfo(Path.Combine(Application.dataPath, ".."));
            var packageJsonFiles = directory.GetFiles("package.json", SearchOption.AllDirectories);

            var packageJsonPath = (from file in packageJsonFiles 
                let json = File.ReadAllText(file.FullName) 
                where json.Contains("\"name\"") && file.FullName.Contains(scriptDirectory.Replace("/", "\\")) 
                select file.FullName).FirstOrDefault();

            if (string.IsNullOrEmpty(packageJsonPath))
            {
                return null;
            }
            
            var jsonContent = File.ReadAllText(packageJsonPath);
            var namePattern = "\"name\"\\s*:\\s*\"([^\"]+)\"";
            var match = System.Text.RegularExpressions.Regex.Match(jsonContent, namePattern);
            
            return match.Success ? match.Groups[1].Value : null;
        }
        
        private bool ShouldRemoveSymbol(string symbol)
        {
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup);
            
            return defines.Contains(symbol);
        }
        
        private void RemoveDefineSymbol(string symbol)
        {
            string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(
                EditorUserBuildSettings.selectedBuildTargetGroup);
            
            // 심볼이 포함되어 있는지 확인
            if (defines.Contains(symbol))
            {
                // 정확한 심볼 제거를 위해 정규식 사용
                // symbol만 제거하고, symbol;, ;symbol, ;symbol; 등의 모든 경우 처리
                string pattern = $"(^{symbol};?)|(;{symbol};?)|(;{symbol}$)";
                string newDefines = System.Text.RegularExpressions.Regex.Replace(defines, pattern, "");
                
                // 심볼 제거 후 설정
                PlayerSettings.SetScriptingDefineSymbolsForGroup(
                    EditorUserBuildSettings.selectedBuildTargetGroup, newDefines);
                
                // 설정 적용을 위해 AssetDatabase 리프레시
                AssetDatabase.Refresh();
            }
        }
        
        private class DummyScriptableObject : ScriptableObject { }
    }
}