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
            RemovePackageInternal();
        }
        
        private void RemovePackageInternal()
        {
            var packageName = GetCurrentPackageName();
            if (string.IsNullOrEmpty(packageName))
            {
                Debug.LogError("패키지 이름을 찾을 수 없습니다.");
                return;
            }

            EditorApplication.delayCall += () =>
            {
                EditorUtility.DisplayProgressBar("패키지 제거 중", $"패키지 '{packageName}'를 제거하는 중...", 0.5f);
                
                var request = UnityEditor.PackageManager.Client.Remove(packageName);
                
                bool isCompleted = false;
                
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
        
        private class DummyScriptableObject : ScriptableObject { }
    }
}