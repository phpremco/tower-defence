using Game.Drones;
using Game.Levels;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Energy gauge and the drone buttons, bottom left. Energy gathers over time; each button launches one drone
    /// of its type and is greyed out when energy is short or that type is at its limit. Reads LevelContext only.
    /// </summary>
    public sealed class DronePanelView : MonoBehaviour
    {
        private const float Margin = 24f;
        private const float BottomBarHeight = 190f;
        private const float PanelWidth = 280f;
        private const float ButtonHeight = 78f;
        private const float BarHeight = 22f;
        private const float TextHeight = 40f;
        private const int FontSize = 28;
        private static readonly Color TextColor = new Color(0.9f, 0.95f, 1f);
        private static readonly Color Unaffordable = new Color(1f, 0.55f, 0.5f);
        private static readonly Color EnergyColor = new Color(0.35f, 0.9f, 1f);

        private LevelContext _context;
        private Text _energyText;
        private RectTransform _fill;
        private DroneButton _repair;
        private DroneButton _collector;

        private struct DroneButton
        {
            public DroneDefinition Drone;
            public Button Button;
            public Text Label;
        }

        public void Initialize(LevelContext context, RectTransform safe)
        {
            _context = context;

            var panel = new GameObject("DronePanel", typeof(RectTransform));
            var rect = panel.GetComponent<RectTransform>();
            rect.SetParent(safe, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(Margin, BottomBarHeight + Margin);

            var layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.LowerLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            var fitter = panel.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _energyText = UiKit.CreateText(rect, "Energy", FontSize, TextAnchor.LowerLeft);
            _energyText.color = TextColor;
            _energyText.horizontalOverflow = HorizontalWrapMode.Overflow;
            Size(_energyText.gameObject, PanelWidth, TextHeight);

            RectTransform bar = UiKit.CreateBar(rect, UiKit.Skin != null ? UiKit.Skin.BarBlue : null, 0f);
            Size(bar.gameObject, PanelWidth, BarHeight);
            bar.sizeDelta = new Vector2(PanelWidth, BarHeight);
            _fill = bar.Find("Fill") as RectTransform;
            if (UiKit.Skin == null || UiKit.Skin.BarBlue == null)
            {
                bar.Find("Fill").GetComponent<Image>().color = EnergyColor;
            }

            _repair = AddButton(rect, context.Level.RepairDrone);
            _collector = AddButton(rect, context.Level.CollectorDrone);

            context.Energy.Changed += Refresh;
            context.DronesChanged.Subscribe(Refresh);
            Refresh();
        }

        private void OnDestroy()
        {
            if (_context == null)
            {
                return;
            }

            _context.Energy.Changed -= Refresh;
            _context.DronesChanged.Unsubscribe(Refresh);
        }

        private DroneButton AddButton(RectTransform parent, DroneDefinition definition)
        {
            var entry = new DroneButton { Drone = definition };
            if (definition == null)
            {
                return entry;
            }

            Button button = UiKit.CreateButton(parent, definition.DisplayName, () => OnClicked(definition));
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var element = button.GetComponent<LayoutElement>();
            element.preferredWidth = PanelWidth;
            element.preferredHeight = ButtonHeight;
            entry.Button = button;
            entry.Label = button.GetComponentInChildren<Text>();
            entry.Label.resizeTextMaxSize = FontSize;
            return entry;
        }

        private void OnClicked(DroneDefinition definition)
        {
            _context.DroneRequested.Raise(definition);
            EventSystem.current.SetSelectedGameObject(null);
        }

        private static void Size(GameObject go, float width, float height)
        {
            var element = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
        }

        private void Refresh()
        {
            var energy = _context.Energy;
            _energyText.text = $"Energy {Mathf.FloorToInt(energy.Current)}/{Mathf.RoundToInt(energy.Maximum)}";
            _fill.anchorMax = new Vector2(Mathf.Clamp01(energy.Current / energy.Maximum), 1f);
            Refresh(_repair);
            Refresh(_collector);
        }

        private void Refresh(DroneButton entry)
        {
            if (entry.Drone == null)
            {
                return;
            }

            int active = _context.ActiveDrones(entry.Drone);
            bool full = active >= entry.Drone.MaxActive;
            bool affordable = _context.Energy.CanAfford(entry.Drone.EnergyCost);

            entry.Button.interactable = affordable && !full;
            entry.Label.color = affordable || full ? TextColor : Unaffordable;
            entry.Label.text = full
                ? $"{entry.Drone.DisplayName}\nmax {active}/{entry.Drone.MaxActive}"
                : $"{entry.Drone.DisplayName}\n{Mathf.RoundToInt(entry.Drone.EnergyCost)} energy  {active}/{entry.Drone.MaxActive}";
        }
    }
}
