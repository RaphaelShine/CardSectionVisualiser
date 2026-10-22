using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

// CardChild.fbx의 클립으로 ChildAnimator.controller를 만들고 SummonManager.childAnimator에 연결한다
// Controller가 없으면 에디터가 로드될 때 자동으로 만든다. 다시 만들려면 Tools > Build Child Animator
[InitializeOnLoad]
public static class ChildAnimatorBuilder
{
    private const string ModelPath = "Assets/Masses/Models/CardChild.fbx";
    private const string ControllerPath = "Assets/Masses/Animators/ChildAnimator.controller";

    // ChildBehavior의 상태 이름과 같다. Child_Rig|Card_Pose_*는 Blender 내보내기 중복이라 쓰지 않는다
    private static readonly string[] Poses = { "CardFace", "Kneel", "LongSit" };

    static ChildAnimatorBuilder()
    {
        EditorApplication.delayCall += () =>
        {
            FixClipRanges();
            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath) == null)
                Build();
        };
    }

    // FBX를 새로 덮어쓰면 clipAnimations가 비워지므로, 임포트될 때마다 다시 고친다
    private class ModelPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (importedAssets.Contains(ModelPath))
                EditorApplication.delayCall += FixClipRanges;
        }
    }

    // Blender는 포즈를 1프레임짜리 take로 내보내는데, Unity 기본 클립 범위는 0-0프레임이라 커브가 없는 빈 클립이 된다
    // 클립 범위를 0-1프레임으로 늘려 다시 임포트한다. 클립 이름은 그대로라 Controller의 참조도 유지된다
    private static void FixClipRanges()
    {
        ModelImporter importer = AssetImporter.GetAtPath(ModelPath) as ModelImporter;
        if (importer == null || importer.clipAnimations.Length > 0) return;

        ModelImporterClipAnimation[] clipAnimations = importer.defaultClipAnimations;
        if (!clipAnimations.Any(clip => clip.lastFrame <= clip.firstFrame)) return;

        foreach (ModelImporterClipAnimation clip in clipAnimations)
        {
            if (clip.lastFrame <= clip.firstFrame)
                clip.lastFrame = clip.firstFrame + 1f;
        }
        importer.clipAnimations = clipAnimations;
        importer.SaveAndReimport();
        Debug.Log($"[ChildAnimatorBuilder] Extended zero-length clips in {ModelPath}");
    }

    [MenuItem("Tools/Build Child Animator")]
    public static void Build()
    {
        FixClipRanges();
        AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(ModelPath)
            .OfType<AnimationClip>()
            .Where(clip => !clip.name.StartsWith("__preview__"))
            .ToArray();

        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

        // Base 레이어: Pose_* (Child_Rig|Pose_*)
        AnimatorControllerLayer[] layers = controller.layers;
        layers[0].name = "Body";
        controller.layers = layers;
        AddStates(controller.layers[0].stateMachine, clips, "Pose_", "Child_Rig|Pose_");

        // Card 레이어: Card_Pose_* (Card|Card_Pose_*). 새 레이어의 기본 Weight는 0이므로 1로 둔다
        AnimatorControllerLayer cardLayer = new AnimatorControllerLayer
        {
            name = "Card",
            defaultWeight = 1f,
            stateMachine = new AnimatorStateMachine { name = "Card", hideFlags = HideFlags.HideInHierarchy },
        };
        AssetDatabase.AddObjectToAsset(cardLayer.stateMachine, controller);
        controller.AddLayer(cardLayer);
        AddStates(cardLayer.stateMachine, clips, "Card_Pose_", "Card|Card_Pose_");

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        AssignToSummonManagers(controller);
        Debug.Log($"[ChildAnimatorBuilder] Built {ControllerPath}");
    }

    private static void AddStates(AnimatorStateMachine stateMachine, AnimationClip[] clips, string statePrefix, string clipPrefix)
    {
        for (int i = 0; i < Poses.Length; i++)
        {
            string clipName = clipPrefix + Poses[i];
            AnimationClip clip = clips.FirstOrDefault(c => c.name == clipName);
            if (clip == null)
                Debug.LogError($"[ChildAnimatorBuilder] No clip '{clipName}' in {ModelPath}.");

            AnimatorState state = stateMachine.AddState(statePrefix + Poses[i], new Vector3(300f, 60f * i, 0f));
            state.motion = clip;
            if (i == 0)
                stateMachine.defaultState = state;
        }
    }

    // 열린 씬의 SummonManager에 연결하고 씬을 dirty로 표시한다
    private static void AssignToSummonManagers(AnimatorController controller)
    {
        foreach (SummonManager manager in Object.FindObjectsByType<SummonManager>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            SerializedObject serialized = new SerializedObject(manager);
            serialized.FindProperty("childAnimator").objectReferenceValue = controller;
            if (serialized.ApplyModifiedProperties())
                EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }
    }
}
