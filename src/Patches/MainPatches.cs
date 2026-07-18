using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using HarmonyLib;
using UnityEngine;

namespace MMMedical.Patches
{
    internal static class MMMLogger
    {
        private static readonly string LogPath =
            @"C:\Users\Administrator\AppData\LocalLow\Orsoniks\CasualtiesUnknown\MMMedical.log";
        internal static void Log(string msg)
        {
            try { File.AppendAllText(LogPath, DateTime.Now.ToString("HH:mm:ss.fff") + " " + msg + "\n"); } catch { }
        }
    }

    internal static class MMMState
    {
        internal static bool IsActive;
    }

    [HarmonyPatch(typeof(Locale), "GetOther")]
    internal static class Locale_GetOther_Patch
    {
        private static bool Prefix(string str, ref string __result)
        {
            if (str == "MMM") { __result = "MMM"; return false; }
            if (str == "MMMunlock") { __result = "无需解锁"; return false; }
            return true;
        }
    }

    [HarmonyPatch(typeof(TutorialHandler), "Awake")]
    internal static class TutorialHandler_Awake_Patch
    {
        private static void Postfix()
        {
            MMMLogger.Log("TutorialHandler.Awake fired");
            var list = TutorialHandler.availableCourses;
            if (list == null) return;
            foreach (var c in list)
                if (c.Item1 == "MMM") return;
            list.Add(new ValueTuple<string, int, Type>("MMM", 0, typeof(MMMedicalCourse)));
            MMMLogger.Log("Injected. Count=" + list.Count);
        }
    }

    [HarmonyPatch(typeof(TutorialHandler), "Start")]
    internal static class TutorialHandler_Start_Patch
    {
        private static void Postfix(TutorialHandler __instance)
        {
            MMMLogger.Log("TutorialHandler.Start postfix");
            try
            {
                __instance.checkForEscape = false;
                var f = typeof(TutorialHandler).GetField("tutorialBounds",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (f != null)
                {
                    var big = new Rect(-10000f, -10000f, 20000f, 20000f);
                    f.SetValue(__instance, big);
                    MMMLogger.Log("tutorialBounds set to massive in Start postfix");
                }
            }
            catch (Exception ex) { MMMLogger.Log("Start postfix err: " + ex.Message); }
        }
    }

    [HarmonyPatch(typeof(TutorialHandler), "Update")]
    internal static class TutorialHandler_Update_Patch
    {
        private static void Postfix(TutorialHandler __instance)
        {
            if (!MMMState.IsActive) return;
            try
            {
                __instance.checkForEscape = false;
                var f = typeof(TutorialHandler).GetField("escapeSequenceActive",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (f != null) f.SetValue(__instance, false);
            }
            catch { }
        }
    }

    [HarmonyPatch(typeof(TutorialHandler), "HandleCourseSelect")]
    internal static class TutorialHandler_CourseSelect_Patch
    {
        private static void Postfix(TutorialHandler __instance)
        {
            var list = TutorialHandler.availableCourses;
            if (list == null) return;
            if (__instance.selectedCourse < 0 || __instance.selectedCourse >= list.Count) return;
            if (list[__instance.selectedCourse].Item1 == "MMM" && __instance.unlockRequirement != null)
            {
                __instance.unlockRequirement.text = "无需解锁";
                __instance.unlockRequirement.color = Color.green;
            }
        }
    }

    internal class MMMedicalCourse : TutorialCourse
    {
        public override string LocaleName() { return "MMM"; }

        public override IEnumerator Sequence()
        {
            MMMLogger.Log("=== Sequence START ===");
            MMMState.IsActive = true;
            var tut = tutorial;
            var b = body;
            if (tut != null)
            {
                tut.checkForEscape = false;
                var screenField = typeof(TutorialHandler).GetField("courseSelectScreen",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (screenField != null)
                {
                    var screen = screenField.GetValue(tut) as GameObject;
                    if (screen != null) { screen.SetActive(false); MMMLogger.Log("courseSelectScreen hidden"); }
                }
            }
            if (b != null) { b.forceWalk = false; MMMLogger.Log("forceWalk=false"); }
            TeleportToOpenArea(tut);
            var wg = WorldGeneration.world;
            if (wg != null) { wg.chunkWidth = 4; wg.chunkHeight = 4; wg.width = 256; wg.height = 256; }
            yield return null; yield return null;
            if (tut != null) tut.Speak("按 B 吠叫开始医疗训练。随机创伤会施加在随机部位，地上会生成5件装备供你使用。治疗完毕后按B吠叫检验。");
            var go = new GameObject("MMMedicalController");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<MMMedicalController>();
            MMMLogger.Log("Controller created");
            yield return null;
            MMMLogger.Log("=== Sequence END ===");
        }

        private static void TeleportToOpenArea(TutorialHandler tut)
        {
            var wg = WorldGeneration.world;
            if (wg == null) return;
            var b = body;
            if (b == null) return;
            float sx = 0f, sy = -106f;
            b.transform.position = new Vector3(sx, sy, 0f);
            var rb = b.GetComponent<Rigidbody2D>();
            if (rb != null) rb.velocity = Vector2.zero;
            var cam = PlayerCamera.main;
            if (cam != null) cam.transform.position = new Vector3(sx, sy, cam.transform.position.z);
            if (tut != null)
            {
                tut.checkForEscape = false;
                var f = typeof(TutorialHandler).GetField("tutorialBounds",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (f != null) f.SetValue(tut, new Rect(sx - 500f, sy - 500f, 1000f, 1000f));
            }
        }
    }

    internal class MMMedicalController : MonoBehaviour
    {
        private enum State { Idle, Wishing, Playing, Verifying, Result }
        private static readonly string[] ALL_ITEMS = {
            "bandage", "sterilizedbandage", "tourniquet", "woundglue",
            "disinfectant", "ceftriaxone", "antibiotics",
            "splint", "morphine", "painkillers",
            "aed", "manualdefibrillator", "amiodarone",
            "bloodcoagulant", "procoagulant", "streptokinase",
            "chestdrain", "braingrow", "epinephrine",
            "antidepressants", "naloxone", "saline", "bloodbag"
        };
        private static readonly Dictionary<string, string> ITEM_CN = new Dictionary<string, string>
        {
            {"bandage", "绷带"}, {"sterilizedbandage", "无菌绷带"},
            {"tourniquet", "止血带"}, {"woundglue", "伤口胶水"},
            {"disinfectant", "消毒剂"}, {"ceftriaxone", "头孢曲松"},
            {"antibiotics", "抗生素"}, {"splint", "夹板"},
            {"morphine", "吗啡"}, {"painkillers", "止痛药"},
            {"aed", "自动除颤器"}, {"manualdefibrillator", "手动除颤器"},
            {"amiodarone", "胺碘酮"}, {"bloodcoagulant", "止血剂"},
            {"procoagulant", "促凝剂"}, {"streptokinase", "链激酶"},
            {"chestdrain", "胸腔引流"}, {"braingrow", "脑部生长液"},
            {"epinephrine", "肾上腺素"}, {"antidepressants", "抗抑郁药"},
            {"naloxone", "纳洛酮"}, {"saline", "生理盐水"},
            {"bloodbag", "血袋"}
        };
        private static string CN(string id) { return ITEM_CN.TryGetValue(id, out var n) ? n : id; }
        private State _state = State.Idle;
        private int _N = 1, _roundNum = 0;
        private float _verifyTimer, _barkCooldown;
        private Body _body;
        private string _wishedItem;
        private string[] _wishOptions;
        private Texture2D _texWhite, _texBlack;
        private GUIStyle _wishTitle, _wishBtn;
        private bool _guiInit;

        private void Start()
        {
            _body = FindBody();
            if (_body?.skills != null) { _body.skills.INT = 20; MMMLogger.Log("INT=20"); }
            MMMLogger.Log("Controller started N=" + _N);
        }

        private void LateUpdate()
        {
            if (!MMMState.IsActive) { Destroy(gameObject); return; }
            if (_body == null) _body = FindBody();
            if (_body == null) return;
            if (_body.hunger < 80f) _body.hunger = 97f;
            if (_body.thirst < 75f) _body.thirst = 87f;
            if (_body.energy < 80f) _body.energy = 97f;
            _barkCooldown -= Time.unscaledDeltaTime;
            if (_state == State.Verifying)
            {
                _verifyTimer -= Time.unscaledDeltaTime;
                if (_verifyTimer <= 0f)
                {
                    Time.timeScale = 1f;
                    _state = State.Result;
                    CalculateScore();
                    CleanupGroundItems();
                }
                return;
            }
            if (_state == State.Wishing) return;
            if (Input.GetKeyDown(KeyCode.B) && _barkCooldown <= 0f)
            {
                _barkCooldown = 1f;
                switch (_state)
                {
                    case State.Idle: BeginRound(); break;
                    case State.Playing: StartVerify(); break;
                    case State.Result: _state = State.Idle; SpeakNow("按 B 吠叫开始下一轮。N=" + _N); break;
                }
            }
        }

        private void InitGUI()
        {
            if (_guiInit) return;
            _guiInit = true;
            _texWhite = new Texture2D(1, 1); _texWhite.SetPixel(0, 0, Color.white); _texWhite.Apply();
            _texBlack = new Texture2D(1, 1); _texBlack.SetPixel(0, 0, Color.black); _texBlack.Apply();
            _wishTitle = new GUIStyle { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _wishTitle.normal.textColor = Color.white; _wishTitle.normal.background = _texBlack;
            _wishBtn = new GUIStyle { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _wishBtn.normal.textColor = Color.white; _wishBtn.normal.background = _texBlack;
            _wishBtn.hover.textColor = Color.black; _wishBtn.hover.background = _texWhite;
            _wishBtn.active.textColor = Color.black; _wishBtn.active.background = _texWhite;
            _wishBtn.padding = new RectOffset(0, 0, 8, 8);
        }

        private void DrawBorder(Rect r, int t)
        {
            GUI.DrawTexture(new Rect(r.x - t, r.y - t, r.width + t * 2, t), _texWhite);
            GUI.DrawTexture(new Rect(r.x - t, r.y + r.height, r.width + t * 2, t), _texWhite);
            GUI.DrawTexture(new Rect(r.x - t, r.y, t, r.height), _texWhite);
            GUI.DrawTexture(new Rect(r.x + r.width, r.y, t, r.height), _texWhite);
        }

        private void OnGUI()
        {
            if (!MMMState.IsActive || _state != State.Wishing || _wishOptions == null) return;
            InitGUI();
            int cols = 4, rows = (_wishOptions.Length + cols - 1) / cols;
            float cellW = 180f, cellH = 60f, pad = 6f, titleH = 44f;
            int border = 2;
            float gw = cols * (cellW + pad) + pad, gh = titleH + rows * (cellH + pad) + pad;
            float px = (Screen.width - gw) / 2f, py = (Screen.height - gh) / 2f;
            DrawBorder(new Rect(px - 4, py - 4, gw + 8, gh + 8), border);
            GUI.DrawTexture(new Rect(px, py, gw, gh), _texBlack);
            GUI.Label(new Rect(px, py + pad, gw, titleH), "许愿 — 选择一件装备", _wishTitle);
            float sy = py + titleH + pad;
            for (int i = 0; i < _wishOptions.Length; i++)
            {
                int c = i % cols, r = i / cols;
                Rect br = new Rect(px + pad + c * (cellW + pad), sy + r * (cellH + pad), cellW, cellH);
                DrawBorder(br, border);
                if (GUI.Button(br, CN(_wishOptions[i]), _wishBtn))
                {
                    _wishedItem = _wishOptions[i];
                    _wishOptions = null;
                    ExecuteRound();
                }
            }
        }

        private void BeginRound()
        {
            _roundNum++;
            if (_roundNum % 2 == 0) ShowWishScreen(); else ExecuteRound();
        }

        private void ShowWishScreen()
        {
            var tmp = new List<string>(ALL_ITEMS);
            _wishOptions = new string[7];
            for (int i = 0; i < 7 && tmp.Count > 0; i++)
            { int idx = UnityEngine.Random.Range(0, tmp.Count); _wishOptions[i] = tmp[idx]; tmp.RemoveAt(idx); }
            string msg = "许愿！点击选择一件：";
            for (int i = 0; i < _wishOptions.Length; i++) msg += (i + 1) + "." + CN(_wishOptions[i]) + " ";
            _state = State.Wishing;
            SpeakNow(msg);
        }

        private void ExecuteRound()
        {
            _state = State.Playing;
            CleanupGroundItems();
            if (_body == null) { _state = State.Idle; return; }
            GenerateTraumas(_body, _N);
            SpawnEquipment(_body, _wishedItem);
            _wishedItem = null;
            SpeakNow("N=" + _N + " 个创伤已施加。地上有5件装备，按 B 吠叫检验。");
        }

        private void StartVerify()
        {
            if (_body == null) return;
            LogBodyState("BEFORE_VERIFY");
            _state = State.Verifying;
            _verifyTimer = 5f;
            Time.timeScale = 100f;
            SpeakNow("检验中...");
        }

        private void SpeakNow(string text)
        {
            var tut = TutorialHandler.main;
            if (tut != null) tut.Speak(text);
        }

        private void GenerateTraumas(Body body, int n)
        {
            string[] lt = { "bleed", "shrapnel", "dislocated", "broken", "infection" };
            string[] bt = { "internal", "hemothorax", "stroke" };
            float sev = n * UnityEngine.Random.Range(0.6f, 1.4f);
            for (int i = 0; i < n; i++)
            {
                if (UnityEngine.Random.Range(0, 3) == 0)
                {
                    string t = bt[UnityEngine.Random.Range(0, bt.Length)];
                    if (t == "internal") body.internalBleeding = Mathf.Max(body.internalBleeding, sev * 3f);
                    else if (t == "hemothorax") body.hemothorax = Mathf.Max(body.hemothorax, sev * 5f);
                    else { body.strokeAmount = Mathf.Max(body.strokeAmount, sev * 3f); foreach (var l in body.limbs) if (l.isHead) l.strokeAffected = true; }
                }
                else
                {
                    var l = body.limbs[UnityEngine.Random.Range(0, body.limbs.Length)];
                    string t = lt[UnityEngine.Random.Range(0, lt.Length)];
                    if (t == "bleed") l.bleedAmount = Mathf.Max(l.bleedAmount, sev * 12f);
                    else if (t == "shrapnel") l.shrapnel = Mathf.Max(l.shrapnel, (int)(sev * 2));
                    else if (t == "dislocated") { if (!l.dislocated) l.Dislocate(); }
                    else if (t == "broken") { if (!l.broken) l.BreakBone(); }
                    else { l.infected = true; l.infectionAmount = Mathf.Max(l.infectionAmount, sev * 5f); }
                    l.pain = Mathf.Min(l.pain + sev * 0.4f, 1f);
                }
            }
        }

        private void SpawnEquipment(Body body, string guaranteed = null)
        {
            Vector2 pos = (Vector2)body.transform.position + Vector2.up * 0.5f;
            var tmp = new List<string>(ALL_ITEMS);
            var sel = new List<string>();
            if (!string.IsNullOrEmpty(guaranteed)) { sel.Add(guaranteed); tmp.Remove(guaranteed); }
            for (int i = sel.Count; i < 5 && tmp.Count > 0; i++)
            { int idx = UnityEngine.Random.Range(0, tmp.Count); sel.Add(tmp[idx]); tmp.RemoveAt(idx); }
            float off = 0f;
            foreach (var item in sel)
            {
                try { Utils.Create(item, pos + new Vector2(off, 0f), 0f); }
                catch (Exception ex) { MMMLogger.Log("Spawn FAIL: " + item + " " + ex.Message); }
                off += 0.8f;
            }
        }

        private void CleanupGroundItems()
        {
            var all = FindObjectsOfType<Item>();
            var held = new HashSet<Item>();
            try { var pi = _body?.GetAllItems(); if (pi != null) foreach (var it in pi) held.Add(it); } catch { }
            foreach (var item in all) if (item != null && !held.Contains(item)) Destroy(item.gameObject);
        }

        private void CalculateScore()
        {
            if (_body == null) { SpeakNow("身体丢失！评级0"); return; }
            LogBodyState("AFTER_VERIFY");
            if (!_body.alive) { SpeakNow("死亡！评级0。按B开始下一轮。"); return; }
            float score = 0; int pass = 0, fail = 0;
            if (_body.bloodPressure >= 60f && _body.bloodPressure <= 140f) { score += 3f; pass++; } else fail++;
            if (_body.internalBleeding <= 0.01f) { score += 0.5f; pass++; } else fail++;
            float tb = 0; foreach (var l in _body.limbs) tb += l.bleedAmount;
            if (tb <= 0.01f) { score += 0.5f; pass++; } else fail++;
            if (_body.fibrillationProgress <= 0.01f) { score += 1f; pass++; } else fail++;
            if (_body.strokeAmount <= 0.01f && _body.hemothorax <= 0.01f) { score += 1f; pass++; } else fail++;
            if (_body.consciousness > 0.5f) { score += 1f; pass++; } else fail++;
            if (_body.happiness > 0.3f) { score += 0.7f; pass++; } else fail++;
            if (_body.sicknessAmount < 0.2f) { score += 0.3f; pass++; } else fail++;
            float mb = 0; foreach (var l in _body.limbs) mb += l.muscleHealth; score += mb / 10f;
            string msg = "评级 " + score.ToString("F1") + "。通过" + pass + "项，失败" + fail + "项。";
            if (score > 5f && UnityEngine.Random.Range(0, 2) == 0) { _N++; msg += " 难度提升！N=" + _N; }
            msg += " 按B开始下一轮。";
            SpeakNow(msg);
        }

        private void LogBodyState(string tag)
        {
            if (_body == null) return;
            MMMLogger.Log("[" + tag + "] alive=" + _body.alive + " BP=" + _body.bloodPressure.ToString("F1")
                + " intBleed=" + _body.internalBleeding.ToString("F3") + " hemothorax=" + _body.hemothorax.ToString("F3")
                + " stroke=" + _body.strokeAmount.ToString("F3") + " fib=" + _body.fibrillationProgress.ToString("F3")
                + " con=" + _body.consciousness.ToString("F3") + " happy=" + _body.happiness.ToString("F3")
                + " sick=" + _body.sicknessAmount.ToString("F3"));
            foreach (var l in _body.limbs)
                MMMLogger.Log("[" + tag + "] " + l.shortName + " bleed=" + l.bleedAmount.ToString("F3")
                    + " shrapnel=" + l.shrapnel + " broken=" + l.broken + " dislocated=" + l.dislocated
                    + " infected=" + l.infected + " infAmt=" + l.infectionAmount.ToString("F3")
                    + " pain=" + l.pain.ToString("F3") + " muscle=" + l.muscleHealth.ToString("F1"));
        }

        private Body FindBody()
        {
            var tp = typeof(TutorialCourse).GetProperty("body",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (tp != null) { var b = tp.GetValue(null) as Body; if (b != null) return b; }
            var cam = FindObjectOfType<PlayerCamera>();
            if (cam != null) return cam.body;
            var all = FindObjectsOfType<Body>();
            return all != null && all.Length > 0 ? all[0] : null;
        }

        private void OnDestroy() { if (Time.timeScale != 1f) Time.timeScale = 1f; }
    }
}
