using HarmonyLib;
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace satania.harmony.hierarchy
{
    public static class HierarchyEditorPatch
    {
        public static void ApplyPatch()
        {
            var harmony = new Harmony("satania.harmony.hierarchy");

            // UnityEditor.SceneHierarchyのTypeを取得
            var sceneHierarchyType = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchy");

            // "DoTreeView"メソッドのMethodInfoを取得
            var doTreeViewMethod = sceneHierarchyType.GetMethod("DoTreeView", BindingFlags.NonPublic | BindingFlags.Instance);

            // PrefixメソッドのMethodInfoを取得
            var prefixMethod = typeof(HierarchyPrefix).GetMethod("DoTreeViewPrefix", BindingFlags.Static | BindingFlags.Public);

            // DoTreeViewメソッドにPrefixを適用
            harmony.Patch(doTreeViewMethod, new HarmonyMethod(prefixMethod));
        }

        public static void Unpatch()
        {
            var harmony = new Harmony("satania.harmony.hierarchy");
            harmony.UnpatchAll();
        }
    }

    public class InitializeHarmonyPatch
    {
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            HierarchyEditorPatch.ApplyPatch();
        }
    }

    public static class HierarchyPrefix
    {
        public static Type hierarchyEditorType;
        public static MethodInfo OnGUI;

        public static string text = "ああああ";
        public static float areaPer = 0.2f;

        public static void InvokeTreeViewOnGUI(object treeView, Rect treeViewRect, int treeViewKeyboardControlID)
        {
            // TreeViewControllerが継承されていることを確認し、OnGUIメソッドを取得
            Type treeViewControllerType = treeView.GetType();
            MethodInfo onGUIMethod = treeViewControllerType.GetMethod("OnGUI", BindingFlags.Public | BindingFlags.Instance);

            if (onGUIMethod != null)
            {
                float _80 = treeViewRect.height * (1.0f - areaPer);
                float _20 = treeViewRect.height * areaPer;

                treeViewRect.height = _80;

                // OnGUIメソッドを反映的に呼び出す
                onGUIMethod.Invoke(treeView, new object[] { treeViewRect, treeViewKeyboardControlID });

                GUIContent content = new GUIContent(text);
                var style = new GUIStyle(GUI.skin.label);
                style.fontSize = 24;
                style.alignment = TextAnchor.UpperLeft;
                style.richText = true;

                GUI.Label(new Rect(0, treeViewRect.y + treeViewRect.height, treeViewRect.width, _20), content, style);
            }
            else
            {
                Debug.LogError("OnGUI method not found on TreeViewController.");
            }
        }

        public static void GetDoTreeViewParameters(EditorWindow sceneHierarchyWindow)
        {
            if (sceneHierarchyWindow == null)
            {
                Debug.LogError("SceneHierarchy window instance is null.");
                return;
            }

            // UnityEditor.SceneHierarchyのTypeを取得
            Type sceneHierarchyType = typeof(EditorWindow).Assembly.GetType("UnityEditor.SceneHierarchy");

            // treeViewRectプロパティの値を取得
            PropertyInfo treeViewRectProperty = sceneHierarchyType.GetProperty("treeViewRect", BindingFlags.NonPublic | BindingFlags.Instance);
            Rect treeViewRect = (Rect)treeViewRectProperty.GetValue(sceneHierarchyWindow);

            // treeViewフィールドの値を取得
            PropertyInfo treeViewField = sceneHierarchyType.GetProperty("treeView", BindingFlags.NonPublic | BindingFlags.Instance);
            object treeView = treeViewField.GetValue(sceneHierarchyWindow);

            // m_TreeViewKeyboardControlIDフィールドの値を取得
            FieldInfo keyboardControlIDField = sceneHierarchyType.GetField("m_TreeViewKeyboardControlID", BindingFlags.NonPublic | BindingFlags.Instance);
            int keyboardControlID = (int)keyboardControlIDField.GetValue(sceneHierarchyWindow);

            InvokeTreeViewOnGUI(treeView, treeViewRect, keyboardControlID);
        }

        [HarmonyPrefix]
        // Prefixメソッド
        public static bool DoTreeViewPrefix(EditorWindow __instance, ref float searchPathHeight)
        {
            // インスタンスを使用してパラメータを取得
            GetDoTreeViewParameters(__instance);

            // trueを返して元のメソッドを実行する
            return false;
        }
    }

    public class HierarchyExtensionWindow : EditorWindow
    {

        [MenuItem("さたにあ/HierarchyExtension")]
        public static void Init()
        {
            var window = GetWindow<HierarchyExtensionWindow>();

            window.titleContent = new GUIContent("HierarchyExtension Setting");

            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("表示するテキスト");
            HierarchyPrefix.text = EditorGUILayout.TextArea(HierarchyPrefix.text);

            GUILayout.Space(10);

            HierarchyPrefix.areaPer = EditorGUILayout.Slider("空欄の広さ", HierarchyPrefix.areaPer, 0.0f, 1.0f);

            GUILayout.Space(10);
        }
    }
}
