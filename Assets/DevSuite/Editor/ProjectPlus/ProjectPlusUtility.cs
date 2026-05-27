using System.IO;
using UnityEditor;
using UnityEngine;

namespace DevSuite.ProjectPlus
{
    public static class ProjectPlusUtility
    {
        [MenuItem("Tools/DevSuite/Clean Empty Folders")]
        public static void CleanEmptyFolders()
        {
            int deletedCount = DeleteEmptyFoldersRecursive(Application.dataPath);
            
            if (deletedCount > 0)
            {
                AssetDatabase.Refresh();
                EditorUtility.DisplayDialog("DevSuite Clean Up", $"Successfully deleted {deletedCount} empty folders.", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("DevSuite Clean Up", "No empty folders found in the project.", "OK");
            }
        }

        private static int DeleteEmptyFoldersRecursive(string path)
        {
            int count = 0;
            
            // Recurse first so child folders get deleted before parents
            foreach (var directory in Directory.GetDirectories(path))
            {
                count += DeleteEmptyFoldersRecursive(directory);
            }

            string[] files = Directory.GetFiles(path);
            string[] subdirs = Directory.GetDirectories(path);

            // A folder is empty if it contains no subdirs and either no files or only .meta files
            bool isEmpty = subdirs.Length == 0;
            if (isEmpty)
            {
                foreach (var file in files)
                {
                    if (!file.EndsWith(".meta"))
                    {
                        isEmpty = false;
                        break;
                    }
                }
            }

            // Do not delete the root Assets folder
            if (isEmpty && path != Application.dataPath)
            {
                // Delete directory and its meta file
                string metaFile = path + ".meta";
                if (File.Exists(metaFile))
                {
                    File.Delete(metaFile);
                }

                try
                {
                    Directory.Delete(path, true);
                    count++;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[DevSuite] Failed to delete folder {path}: {e.Message}");
                }
            }

            return count;
        }
    }
}
