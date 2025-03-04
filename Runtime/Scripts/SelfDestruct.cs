
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
            // 1. 현재 패키지의 이름 가져오기 (package.json에서)
            var packageName = GetCurrentPackageName();
            if (string.IsNullOrEmpty(packageName))
            {
                Debug.LogError("패키지 이름을 찾을 수 없습니다.");
                return;
            }

            // 2. 패키지 제거
            EditorApplication.delayCall += () =>
            {
                UnityEditor.PackageManager.Client.Remove(packageName);
                Debug.Log($"패키지 '{packageName}'가 성공적으로 제거되었습니다.");
                
                EditorUtility.ClearProgressBar();
            };
        }

        private string GetCurrentPackageName()
        {
            var packagePath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject
                (ScriptableObject.CreateInstance<DummyScriptableObject>()));
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