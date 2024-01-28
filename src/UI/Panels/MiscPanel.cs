﻿using UnityExplorer.CacheObject;
using UnityExplorer.Inspectors;
using UnityExplorer.ObjectExplorer;
using UniverseLib.UI;
using UniverseLib.UI.Models;
using UniverseLib.UI.ObjectPool;
#if UNHOLLOWER
using UnhollowerRuntimeLib;
#endif
#if INTEROP
using Il2CppInterop.Runtime.Injection;
#endif

namespace UnityExplorer.UI.Panels
{
    public class Misc : UEPanel
    {
        public enum ScreenshotState
        {
            DoNothing,
            TurnOffUI,
            TakeScreenshot,
            TurnOnUI,
        }

        public class UIElement
        {
            public UIElement(object obj)
            {
                Object = obj;

                ReflectionInspector inspector = Pool<ReflectionInspector>.Borrow();
                inspector.Target = obj.TryCast();
                Type objType = obj is Type type ? type : obj.GetActualType();
                List<CacheMember> members = CacheMemberFactory.GetCacheMembers(objType, inspector);

                foreach (CacheMember member in members){
                    if (member.NameForFiltering.EndsWith(".enabled")){
                        member.Evaluate();
                        Enabled = member;
                    }

                    if (member.NameForFiltering.EndsWith(".name")){
                        member.Evaluate();
                        Name = member;
                    }

                    if (Enabled != null && Name != null) break;
                }
            }

            public object Object { get; }
            public CacheMember Enabled { get; }
            public CacheMember Name { get; }

            public override string ToString() => $"{Object}";
        }

        public Misc(UIBase owner) : base(owner)
        {
            captureScreenshotFunction = null;
            FindCaptureScreenshotFunction();
            superSizeValue = 2;
            hud = new List<UIElement>();

            screenshotStatus = ScreenshotState.DoNothing;
        }

        public override string Name => "Misc";
        public override UIManager.Panels PanelType => UIManager.Panels.Misc;
        public override int MinWidth => 275;
        public override int MinHeight => 100;
        public override Vector2 DefaultAnchorMin => new(0.4f, 0.4f);
        public override Vector2 DefaultAnchorMax => new(0.6f, 0.6f);
        public override bool NavButtonWanted => true;
        public override bool ShouldSaveActiveState => true;

        List<UIElement> hud;
        Toggle HUDToggle;

        CacheMethod captureScreenshotFunction;
        int superSizeValue;
        public ScreenshotState screenshotStatus;

        Toggle HighLodToggle;
        object qualitySettings = null;
        CacheProperty lodBias = null;

        private void LoadHUDElements(){
            List<object> currentResults = SearchProvider.UnityObjectSearch("", "UnityEngine.Canvas", ChildFilter.Any, SceneFilter.Any);
            if (hud.Count > 0){
                SetValueHUDElements(true);
                HUDToggle.isOn = true;
                hud.Clear();
            }

            foreach (object obj in currentResults){
                UIElement entry = new UIElement(obj);
                // Ignore non-enabled objects and objects without a name, since those tend to be irrelevant
                if ((bool) entry.Enabled.Value && entry.Name.Value.ToString() != ""){
                    hud.Add(entry);
                }
            }
        }

        private void SetValueHUDElements(bool value){
            // If its the first time changing the HUD, we look up for UI elements.
            if (hud.Count == 0){
                LoadHUDElements();
            }

            foreach (UIElement elem in hud){
                elem.Enabled.TrySetUserValue(value);
            }
        }

        public void ToggleHUDElements(){
            HUDToggle.isOn = !HUDToggle.isOn;
            SetValueHUDElements(HUDToggle.isOn);
        }

        private void TakeScreenshot(){
            MethodInfo methodInfo = captureScreenshotFunction.MethodInfo;
            string filename = DateTime.Now.ToString("yyyy-M-d HH-mm-ss");

            ExplorerCore.LogWarning(filename);

            string screenshotsPath = Path.Combine(ExplorerCore.ExplorerFolder, "Screenshots");
            System.IO.Directory.CreateDirectory(screenshotsPath);
            
            object[] args = {$"{screenshotsPath}\\{filename}.png", superSizeValue};

            methodInfo.Invoke(captureScreenshotFunction.DeclaringInstance, args);
        }
        
        private void FindCaptureScreenshotFunction(){
            List<object> results = SearchProvider.ClassSearch("UnityEngine.ScreenCapture");
            // We assume it's the first for now. Come back if we need to do something else to get it.
            object obj = results[0];
            Type objType = obj is Type type ? type : obj.GetActualType();
            ReflectionInspector inspector = Pool<ReflectionInspector>.Borrow();
            List<CacheMember> members = CacheMemberFactory.GetCacheMembers(objType, inspector);
            foreach (CacheMember member in members){
                if (member is CacheMethod methodMember && methodMember.NameForFiltering == "ScreenCapture.CaptureScreenshot(string, int)"){
                    captureScreenshotFunction = methodMember;
                    break;
                }
            }
        }

        private void FindQualitySettings(){
            List<object> results = SearchProvider.ClassSearch("UnityEngine.QualitySettings");
            // We assume it's the first for now. Come back if we need to do something else to get it.
            object obj = results[0];
            qualitySettings = obj;
        }

        private void FindLodBias(){
            Type objType = qualitySettings is Type type ? type : qualitySettings.GetActualType();
            ReflectionInspector inspector = Pool<ReflectionInspector>.Borrow();
            List<CacheMember> members = CacheMemberFactory.GetCacheMembers(objType, inspector);
            foreach (CacheMember member in members){
                if (member is CacheProperty propertyMember && propertyMember.NameForFiltering == "QualitySettings.lodBias"){
                    lodBias = propertyMember;
                    break;
                }
            }
        }

        private void ToogleHighLods(bool areHighLodsOn){
            if (qualitySettings == null) FindQualitySettings();
            if (lodBias == null) FindLodBias();

            lodBias.TrySetUserValue(areHighLodsOn ? 1000 : 1);
        }

        // We use an enum to walk a series of steps in each frame, so we can take the screenshot without UnityExplorer UI.
        public void MaybeTakeScreenshot(){
            switch (screenshotStatus){
                case ScreenshotState.TurnOffUI:
                    screenshotStatus = ScreenshotState.TakeScreenshot;
                    UIManager.ShowMenu = false;
                    break;
                case ScreenshotState.TakeScreenshot:
                    TakeScreenshot();
                    screenshotStatus = ScreenshotState.TurnOnUI;
                    break;
                case ScreenshotState.TurnOnUI:
                    screenshotStatus = ScreenshotState.DoNothing;
                    UIManager.ShowMenu = true;
                    break;
                case ScreenshotState.DoNothing:
                default:
                    break;
            }
        }

        // ~~~~~~~~ UI construction / callbacks ~~~~~~~~

        protected override void ConstructPanelContent()
        {
            // HUD toggle
            GameObject HUDhoriGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "HUDhoriGroup", false, false, true, true, 3,
            default, new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(HUDhoriGroup, minHeight: 25, flexibleWidth: 9999);

            ButtonRef loadHUDElements = UIFactory.CreateButton(HUDhoriGroup, "HideUI", "Load HUD elements");
            UIFactory.SetLayoutElement(loadHUDElements.GameObject, minWidth: 150, minHeight: 25);
            loadHUDElements.OnClick += LoadHUDElements;

            HUDToggle = new Toggle();
            GameObject HUDToggleObj = UIFactory.CreateToggle(HUDhoriGroup, "Toggle HUD", out HUDToggle, out Text HUDToggleText);
            UIFactory.SetLayoutElement(HUDToggleObj, minHeight: 25);
            HUDToggle.onValueChanged.AddListener(SetValueHUDElements);
            HUDToggle.isOn = true; // we picked up only the active UI elements
            HUDToggleText.text = "Toggle HUD";

            HighLodToggle = new Toggle();
            GameObject HighLodToggleObj = UIFactory.CreateToggle(ContentRoot, "HighLOD", out HighLodToggle, out Text HighLodToggleText);
            UIFactory.SetLayoutElement(HighLodToggleObj, minHeight: 25);
            HighLodToggle.onValueChanged.AddListener(ToogleHighLods);
            HighLodToggle.isOn = false;
            HighLodToggleText.text = "High LODs Toggle";

            // Screenshot function
            GameObject TakeScreenshotHoriGroup = UIFactory.CreateHorizontalGroup(ContentRoot, "Take screenshot", false, false, true, true, 3,
            default, new Color(1, 1, 1, 0), TextAnchor.MiddleLeft);
            UIFactory.SetLayoutElement(TakeScreenshotHoriGroup, minHeight: 25, flexibleWidth: 9999);

            ButtonRef takeScreenshot = UIFactory.CreateButton(TakeScreenshotHoriGroup, "TakeScreenshot", "Take screenshot");
            UIFactory.SetLayoutElement(takeScreenshot.GameObject, minWidth: 150, minHeight: 25);
            takeScreenshot.OnClick += () => screenshotStatus = ScreenshotState.TurnOffUI;

            AddInputField(TakeScreenshotHoriGroup, "Supersize", "Supersize:", $"{2}", SuperSize_OnEndEdit);
        }

        GameObject AddInputField(GameObject parent, string name, string labelText, string placeHolder, Action<string> onInputEndEdit)
        {
            Text posLabel = UIFactory.CreateLabel(parent, $"{name}_Label", labelText);
            UIFactory.SetLayoutElement(posLabel.gameObject, minWidth: 75, minHeight: 25);

            InputFieldRef inputField = UIFactory.CreateInputField(parent, $"{name}_Input", placeHolder);
            UIFactory.SetLayoutElement(inputField.GameObject, minWidth: 25, minHeight: 25);
            inputField.Component.GetOnEndEdit().AddListener(onInputEndEdit);

            return parent;
        }

        void SuperSize_OnEndEdit(string input)
        {
            if (!ParseUtility.TryParse(input, out int parsed, out Exception parseEx))
            {
                ExplorerCore.LogWarning($"Could not parse value: {parseEx.ReflectionExToString()}");
                return;
            }

            superSizeValue = parsed;
        }
    }
}