using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Text.RegularExpressions;

public class BCFilingNeedy : MonoBehaviour {

    private List<string> defaultModuleNames = new List<string>();
    private int sortingType;
    private List<List<string>> finalNames = new List<List<string>>();
    private List<int> sortedIndexes = new List<int>();
    private KMSelectable[] holdableButtons;
    private List<int> filedIndexes = new List<int>();
    private int waitingFor = 0;
    private List<KMGameInfo.KMModuleInfo> moduleInfo;
    private List<AnimatingFolder> foldersAnimating = new List<AnimatingFolder>();
    private bool isEnded = true;
    private static int _moduleIdCounter = 1;
    private int _moduleId = 0;

    public KMNeedyModule NeedyModule;
    public KMSelectable Selectable;
    public KMAudio Audio;
    public KMBombInfo BombInfo;
    public KMGameInfo GameInfo;
    public TextMesh instructionsText;
    public GameObject[] folders;
    public Material buttonDefaultMat;
    public Material buttonCorrectMat;
    public Material buttonWrongMat;

    class AnimatingFolder
    {
        public GameObject folder;
        public int Frame { get; set; }
        public Vector3 OriginalPosition { get; set; }
        public bool PassedF2 { get; set; }
        public bool isNone = false;
        public string AwaitingText { get; set; }
        public AnimatingFolder(GameObject f, string txt)
        {
            folder = f;
            AwaitingText = txt;
            Frame = 0;
            PassedF2 = false;
        }
        public AnimatingFolder()
        {
            isNone = true;
        }
    }

    void Start() {
        _moduleId = _moduleIdCounter++;
        NeedyModule.OnNeedyActivation += OnNeedyActivation;
        NeedyModule.OnNeedyDeactivation += OnNeedyDeactivation;
        NeedyModule.OnTimerExpired += OnTimerExpired;
        moduleInfo = GameInfo.GetAvailableModuleInfo();
        holdableButtons = Selectable.Children;
        for (var x = 0; x < 6; x++)
        {
            int y = x;
            holdableButtons[y].OnInteract += delegate
            {
                ButtonPressed(y);
                return false;
            };
        }
        Reset();
    }

    void Solve()
    {
        isEnded = true;
        Reset();
        NeedyModule.OnPass();
    }

    void OnNeedyActivation()
    {
        isEnded = false;
        Init();
    }

    void OnNeedyDeactivation()
    {
        isEnded = true;
        Reset();
    }

    void OnTimerExpired()
    {
        Strike();
        Reset();
        isEnded = true;
    }

    void Strike()
    {
        NeedyModule.OnStrike();
    }

    void Reset()
    {
        foreach (AnimatingFolder folder in foldersAnimating)
        {
            if (folder.isNone) continue;
            folder.isNone = true;
            folder.folder.transform.localPosition = folder.OriginalPosition;
        }
        foreach (GameObject folder in folders)
        {
            folder.SetActive(false);
        }
        instructionsText.text = "";
        foreach (KMSelectable button in holdableButtons)
        {
            button.GetComponent<MeshRenderer>().material = buttonDefaultMat;
            button.transform.GetChild(1).GetComponent<TextMesh>().text = "";
        }
    }

    void Init()
    {
        defaultModuleNames = new List<string> { "The Button", "Keypad", "Maze", "Memory", "Morse Code", "Password", "Simon Says", "Complicated Wires", "Who's on First", "Wires", "Wire Sequence", "Needy Capacitor", "Needy Knob", "Needy Vent Gas" };
        finalNames = new List<List<string>>();
        sortedIndexes = new List<int>();
        filedIndexes = new List<int>();
        waitingFor = 0;

        sortingType = UnityEngine.Random.Range(0, 2);
        string sortingInstructionsString = new List<string> { "Sort module names\nfrom A to Z.", "Sort module names\nfrom Z to A." }[sortingType];

        instructionsText.text = sortingInstructionsString;

        for (var x = 0; x < 6; x++)
        {
            foldersAnimating.Add(new AnimatingFolder());
            sortedIndexes.Add(x);
            finalNames.Add(new List<string> { GetRandomModuleName() });
        }

        sortedIndexes.Sort(SortFunc);

        Debug.LogFormat("[Bomb Corp. Filing #{0}] Initializing with sorting type {1} (Module names: {2}) (Sorting indexes: {3})", _moduleId, sortingType, string.Join(", ", finalNames.Select(n => n[0]).ToArray()), string.Join(", ", sortedIndexes.Select(n => n.ToString()).ToArray()));

        for (var x = 0; x < 6; x++)
        {
            int y = x;
            GameObject text = holdableButtons[y].transform.GetChild(1).gameObject;
            string textText = finalNames[y][0];
            text.GetComponent<TextMesh>().text = textText;
            float xScale = text.transform.localScale.x * Math.Min(1f, 1f / (textText.Length * 0.054f));
            text.transform.localScale = new Vector3(xScale, xScale * 10f, text.transform.localScale.z);
        }
    }

    private int SortFunc(int x, int y)
    {
        List<string> name1 = finalNames[x];
        List<string> name2 = finalNames[y];
        if (name2 == null) return 1;
        switch (sortingType)
        {
            case 0:
                return string.Compare(name1[0], name2[0]);
            case 1:
                return string.Compare(name2[0], name1[0]);
            case 2:
                return string.Compare(name1[0], name2[0]);
            case 3:
            default:
                return string.Compare(name2[0], name1[0]);
        }
    }

    bool IsModuleModdedValid(KMGameInfo.KMModuleInfo mod)
    {
        return mod.IsMod && !(mod.DisplayName.Contains('$') || mod.DisplayName.Contains('.') || mod.DisplayName.Contains('#'));
    }

    string GetRandomModuleName(bool useDefault = false)
    {
        if (useDefault || (moduleInfo == null))
        {
            int indD = UnityEngine.Random.Range(0, defaultModuleNames.Count);
            string nameD = defaultModuleNames[indD];
            defaultModuleNames.RemoveAt(indD);
            return nameD;
        }
        if (moduleInfo.FindAll(m => IsModuleModdedValid(m)).Count == 0)
        {
            return GetRandomModuleName(true);
        }
        int ind = UnityEngine.Random.Range(0, moduleInfo.Count);
        if (!IsModuleModdedValid(moduleInfo[ind])) return GetRandomModuleName();
        string name = moduleInfo[ind].DisplayName;
        moduleInfo.RemoveAt(ind);
        return name;
    }

    string GetRandomName(Dictionary<string, List<string>> nameList, List<string> lookup)
    {
        string letter = lookup[UnityEngine.Random.Range(0, lookup.Count)];
        List<string> names = nameList[letter];
        if (names.Count == 0) return GetRandomName(nameList, lookup);
        int nameIndex = UnityEngine.Random.Range(0, names.Count);
        string name = names[nameIndex];
        names.RemoveAt(nameIndex);
        return name;
    }

    IEnumerator RemoveWrongMat(MeshRenderer renderer)
    {
        yield return new WaitForSeconds(0.3f);
        if (renderer.sharedMaterial == buttonWrongMat) renderer.material = buttonDefaultMat;
    }

    void ButtonPressed(int buttonIndex)
    {
        KMSelectable button = holdableButtons[buttonIndex];
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
        button.AddInteractionPunch();
        if (isEnded || filedIndexes.Contains(buttonIndex)) return;
        int expectedIndex = sortedIndexes[waitingFor];
        Audio.PlaySoundAtTransform("FilingSFX", transform);
        if (!foldersAnimating[waitingFor].isNone && foldersAnimating[waitingFor].OriginalPosition.z != 0f)
        {
            foldersAnimating[waitingFor].folder.transform.localPosition = foldersAnimating[waitingFor].OriginalPosition;
        }
        AnimatingFolder aFold = new AnimatingFolder(folders[waitingFor], finalNames[buttonIndex][0]);
        foldersAnimating[waitingFor] = aFold;
        bool isCorrect = buttonIndex == expectedIndex;
        Debug.LogFormat("[Bomb Corp. Filing #{0}] {1} filed, {2} expected. Correct: {3}, Index: {4}", _moduleId, finalNames[buttonIndex][0], finalNames[expectedIndex][0], isCorrect ? "y" : "n", buttonIndex);
        MeshRenderer renderer = button.GetComponent<MeshRenderer>();
        if (isCorrect)
        {
            float time = NeedyModule.GetNeedyTimeRemaining();
            if (time > 0)
            {
                NeedyModule.SetNeedyTimeRemaining(time + 2f);
            }
            renderer.material = buttonCorrectMat;
            filedIndexes.Add(buttonIndex);
            waitingFor++;
        }
        else
        {
            renderer.material = buttonWrongMat;
            StartCoroutine(RemoveWrongMat(renderer));
            Strike();
        }
        if (waitingFor == 6)
        {
            Solve();
        }
    }

    void Update() {
        foreach (AnimatingFolder folder in foldersAnimating)
        {
            if (folder.isNone) continue;
            GameObject f = folder.folder;
            int frame = folder.Frame;
            if (frame == 0)
            {
                GameObject text = f.transform.GetChild(0).gameObject;
                text.GetComponent<TextMesh>().text = folder.AwaitingText;
                float xScale = text.transform.localScale.x * Math.Min(1f, 1f / (folder.AwaitingText.Length * 0.06f));
                text.transform.localScale = new Vector3(xScale, text.transform.localScale.y, text.transform.localScale.z);
                folder.OriginalPosition = f.transform.localPosition;
                f.transform.localPosition += new Vector3(0f, 0f, 0.799999f);
                f.SetActive(true);
                folder.Frame++;
            }
            else if (f.transform.localPosition.z >= folder.OriginalPosition.z - 0.007f && !folder.PassedF2)
            {
                f.transform.localPosition += new Vector3(0f, 0f, Time.deltaTime * (12f * 2f) * -0.1622f);
            }
            else if (f.transform.localPosition.z <= folder.OriginalPosition.z)
            {
                if (!folder.PassedF2)
                {
                    folder.PassedF2 = true;
                    f.transform.localPosition = new Vector3(f.transform.localPosition.x, f.transform.localPosition.y, folder.OriginalPosition.z - 0.007f);
                }
                f.transform.localPosition += new Vector3(0f, 0f, Time.deltaTime * (50f * 2f) * 0.0022f);
                folder.Frame++;
            }
            else
            {
                f.transform.localPosition = folder.OriginalPosition;
                folder.isNone = true;
            }
        }
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} 123456 [Selects the module names from top to bottom]";
    #pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Replace(" ", "");
        for (int i = 0; i < command.Length; i++)
        {
            if (!command[i].EqualsAny('1', '2', '3', '4', '5', '6'))
            {
                yield return "sendtochaterror!f What the heck is '" + command[i] + "' supposed to mean?";
                yield break;
            }
        }
        if (isEnded)
        {
            yield return "sendtochaterror The module is not currently active!";
            yield break;
        }
        yield return null;
        for (int i = 0; i < command.Length; i++)
        {
            Selectable.Children[int.Parse(command[i].ToString()) - 1].OnInteract();
            yield return new WaitForSeconds(.1f);
        }
    }

    void TwitchHandleForcedSolve()
    {
        StartCoroutine(HandleAutosolve());
    }

    IEnumerator HandleAutosolve()
    {
        while (true)
        {
            while (isEnded) yield return null;
            int start = waitingFor;
            for (int i = start; i < 6; i++)
            {
                Selectable.Children[sortedIndexes[i]].OnInteract();
                yield return new WaitForSeconds(.1f);
            }
        }
    }
}
