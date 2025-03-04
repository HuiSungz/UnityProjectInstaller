
using System;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace ActionFit.PackageInstaller
{
    internal static class InstallerPackageRemover
    {
        public static void RemoveInstallerPackage()
        {
            Debug.Log("[패키지 설치] 인스톨러 패키지를 제거합니다...");
            
            try
            {
                var packageName = "com.actionfit.projectinstaller";
                Debug.Log($"[패키지 설치] 패키지 {packageName} 제거 중...");

                var request = Client.Remove(packageName);
                var startTime = DateTime.Now;

                EditorApplication.update += MonitorRemoval;

                void MonitorRemoval()
                {
                    if ((DateTime.Now - startTime).TotalSeconds > 30)
                    {
                        Debug.LogWarning("[패키지 설치] 패키지 제거 시간 초과");
                        EditorApplication.update -= MonitorRemoval;
                        return;
                    }

                    if (!request.IsCompleted)
                    {
                        return;
                    }
                    EditorApplication.update -= MonitorRemoval;
                    if (request.Status == StatusCode.Success)
                    {
                        Debug.Log("[패키지 설치] 인스톨러 패키지 제거 완료");
                    }
                    else
                    {
                        Debug.LogError($"[패키지 설치] 인스톨러 패키지 제거 실패: {request.Error?.message}");
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"[패키지 설치] 인스톨러 패키지 제거 중 오류: {exception.Message}");
            }
        }
    }
}