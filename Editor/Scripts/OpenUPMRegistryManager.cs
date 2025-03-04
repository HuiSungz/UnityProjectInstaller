
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ActionFit.PackageInstaller
{
    internal class OpenUPMRegistryManager
    {
        public bool EnsureRegistry(List<string> openUPMPackages)
        {
            var manifestPath = Path.Combine(Application.dataPath, "../Packages/manifest.json");

            if (!File.Exists(manifestPath))
            {
                Debug.LogError("[패키지 설치] manifest.json 파일을 찾을 수 없습니다");
                return false;
            }

            try
            {
                var json = File.ReadAllText(manifestPath);
                var needsUpdate = false;

                // OpenUPM 패키지에서 고유한 스코프 목록 추출
                var scopes = new HashSet<string>();
                foreach (var package in openUPMPackages)
                {
                    if (string.IsNullOrEmpty(package))
                    {
                        continue;
                    }
                    
                    var parts = package.Split('.');
                    scopes.Add(parts.Length >= 2 
                        ? $"{parts[0]}.{parts[1]}" 
                        : package);
                }

                if (scopes.Count == 0)
                {
                    return true;
                }

                // OpenUPM 레지스트리가 없는 경우 새로 추가
                if (!json.Contains("https://package.openupm.com"))
                {
                    var scopesList = scopes.ToList();
                    var scopesJson = "";
                    for (var i = 0; i < scopesList.Count; i++)
                    {
                        scopesJson += $"        \"{scopesList[i]}\"";
                        if (i < scopesList.Count - 1)
                            scopesJson += ",\n";
                    }

                    var registryEntry =
                        "\"scopedRegistries\": [\n" +
                        "    {\n" +
                        "      \"name\": \"OpenUPM\",\n" +
                        "      \"url\": \"https://package.openupm.com\",\n" +
                        "      \"scopes\": [\n" +
                        scopesJson + "\n" +
                        "      ]\n" +
                        "    }\n" +
                        "  ],";

                    json = json.Contains("\"dependencies\":") 
                        ? Regex.Replace(json, "(\\s*\"dependencies\":\\s*\\{)", registryEntry + "\n$1") 
                        : json.Insert(1, registryEntry + "\n");

                    needsUpdate = true;
                }
                else
                {
                    // 이미 등록된 경우 스코프 추가
                    foreach (var scope in scopes)
                    {
                        if (json.Contains($"\"{scope}\""))
                        {
                            continue;
                        }
                        
                        var pattern = "(\"scopes\":\\s*\\[[^\\]]*)(\\])";
                        var replacement = "$1" + (json.Contains("\"scopes\": [") && !json.Contains("\"scopes\": []") ? ",\n        " : "") + $"\"{scope}\"$2";
                        json = Regex.Replace(json, pattern, replacement);
                        needsUpdate = true;
                    }
                }

                if (!needsUpdate)
                {
                    return true;
                }
                
                File.WriteAllText(manifestPath, json);
                Debug.Log("[패키지 설치] OpenUPM 레지스트리 구성이 업데이트되었습니다");
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[패키지 설치] OpenUPM 레지스트리 설정 중 오류: {exception.Message}");
                return false;
            }
        }
    }
}