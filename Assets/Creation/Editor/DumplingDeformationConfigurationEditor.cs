using UnityEditor;
using UnityEngine;
using SquishyDumpling.Creation;
[CustomEditor(typeof(DumplingDeformationConfiguration))]
public sealed class DumplingDeformationConfigurationEditor : Editor
{
    bool fitting, simulation;
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox("Feel settings update live in Final Squish Play. Defaults retain Task 8 feel. Mesh fitting requires a fresh final-play object.",MessageType.Info);
        fitting=EditorGUILayout.Foldout(fitting,"Placeholder fitting (rebuild required)",true);
        if(fitting) Fields("meshBounds","columns","rows","contactOutline");
        Group("Interaction","pressRadius","pressStrength","grabRadius","grabStrength","influenceFalloff");
        Group("Material","neighborCoupling","springStiffness","damping","stretchResistance","maxStretch","maxStrain");
        Group("Recovery / wobble","recoveryStrength","settleThreshold");
        EditorGUILayout.HelpBox("For stronger wobble, lower Damping and adjust Recovery Strength. There is no separate oscillation or tween parameter.",MessageType.None);
        simulation=EditorGUILayout.Foldout(simulation,"Simulation (advanced)",true);
        if(simulation) Fields("fixedStep","maxSubsteps","velocityLimit","debugGizmos");
        serializedObject.ApplyModifiedProperties();
    }
    void Group(string title,params string[] names)
    { EditorGUILayout.Space();EditorGUILayout.LabelField(title,EditorStyles.boldLabel);Fields(names); }
    void Fields(params string[] names)
    { foreach(string name in names) EditorGUILayout.PropertyField(serializedObject.FindProperty(name),true); }
}
