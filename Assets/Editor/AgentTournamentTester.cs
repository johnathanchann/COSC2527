#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using ConnectFour;

public class AgentTournamentWindow : EditorWindow
{
    private List<GameObject> agentPrefabs = new List<GameObject>();
    private int gamesPerPair = 200;

    [MenuItem("Tools/Agent Tournament…")]
    public static void ShowWindow()
    {
        var wnd = GetWindow<AgentTournamentWindow>("Agent Tournament");
        wnd.minSize = new Vector2(350, 300);
    }

    private void OnEnable()
    {
        if (agentPrefabs == null)
            agentPrefabs = new List<GameObject>();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Agent Tournament Setup", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("Agent Prefabs", EditorStyles.label);
        for (int i = 0; i < agentPrefabs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            agentPrefabs[i] = (GameObject)EditorGUILayout.ObjectField(
                $"Prefab {i + 1}",
                agentPrefabs[i],
                typeof(GameObject),
                true
            );
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                agentPrefabs.RemoveAt(i);
                break;
            }
            EditorGUILayout.EndHorizontal();
        }
        if (GUILayout.Button("Add Prefab"))
        {
            agentPrefabs.Add(null);
        }

        GUILayout.Space(10);

        gamesPerPair = EditorGUILayout.IntField("Games Per Pair", gamesPerPair);

        GUILayout.Space(20);

        EditorGUI.BeginDisabledGroup(agentPrefabs.Count < 2);
        if (GUILayout.Button("Run Tournament"))
        {
            RunTournament(agentPrefabs, gamesPerPair);
        }
        EditorGUI.EndDisabledGroup();
    }

    private void RunTournament(List<GameObject> prefabs, int gamesPerPair)
    {
        int n = prefabs.Count;
        int pairCount = n * (n - 1) / 2;
        int totalRounds = pairCount * gamesPerPair;
        int currentRound = 0;

        var score = new float[n, n];
        string[] agentNames = new string[n];
        Func<Agent>[] factories = new Func<Agent>[n];

        for (int i = 0; i < n; i++)
        {
            var prefab = prefabs[i];
            if (prefab == null)
                throw new InvalidOperationException($"Agent prefab at index {i} is null.");

            agentNames[i] = prefab.name;
            factories[i] = () =>
            {
                GameObject go = Instantiate(prefab, Vector3.zero, Quaternion.identity);
                go.hideFlags = HideFlags.HideAndDontSave;
                return go.GetComponent<Agent>();
            };
        }

        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                for (int g = 0; g < gamesPerPair; g++)
                {
                    currentRound++;
                    float progress = (float)currentRound / totalRounds;

                    if (EditorUtility.DisplayCancelableProgressBar(
                          "Agent Tournament",
                          $"Game {currentRound}/{totalRounds} — {agentNames[i]} vs {agentNames[j]}",
                          progress
                        ))
                    {
                        goto After;
                    }

                    int swap = g % 2;
                    var state = new Connect4State();
                    int idxY = i, idxR = j;
                    if (state.GetPlayerTurn() == swap)
                        (idxY, idxR) = (j, i);

                    var yellow = factories[idxY]();
                    var red = factories[idxR]();
                    while (state.GetResult() == Connect4State.Result.Undecided)
                        state.MakeMove((state.GetPlayerTurn() == 0 ? yellow : red).GetMove(state));

                    if (state.GetResult() == Connect4State.Result.YellowWin)
                        score[idxY, idxR]++;
                    else if (state.GetResult() == Connect4State.Result.RedWin)
                        score[idxR, idxY]++;
                    else
                    {
                        score[idxY, idxR] += 0.5f;
                        score[idxR, idxY] += 0.5f;
                    }

                    DestroyImmediate(yellow.gameObject);
                    DestroyImmediate(red.gameObject);
                }
            }
        }

    After:
        EditorUtility.ClearProgressBar();

        var sb = new StringBuilder();
        sb.Append("Agent");
        foreach (var name in agentNames)
            sb.Append($",\"{name} (%)\"");
        sb.AppendLine();

        for (int i = 0; i < n; i++)
        {
            sb.Append($"\"{agentNames[i]}\"");
            for (int j = 0; j < n; j++)
            {
                if (i == j) sb.Append(",—");
                else
                {
                    double rate = (double)score[i, j] / gamesPerPair * 100.0;
                    sb.Append($",{rate:F2}");
                }
            }
            sb.AppendLine();
        }

        string outPath = Path.Combine(Application.dataPath, "Editor", "tournament_results.csv");
        Directory.CreateDirectory(Path.GetDirectoryName(outPath));
        File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);

        Debug.Log($"Tournament complete. Results written to:\n{outPath}");
        AssetDatabase.Refresh();
    }
}
#endif
