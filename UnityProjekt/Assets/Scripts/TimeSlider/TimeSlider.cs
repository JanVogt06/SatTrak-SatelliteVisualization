using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TimeSlider
{
    public class TimeSlider : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("UI References")] public Slider dateSlider;
        public TMP_Text minDateText;
        public TMP_Text maxDateText;
        public TMP_Text currentDateText;
        public TMP_Text zoomLevelText;
        public CanvasGroup Panel;
        public GameObject OpenSettings;

        [Header("Simulation Time Settings")] public float timeMultiplier = 1f;

        public DateTime CurrentSimulatedTime { get; private set; }
        private DateTime _simulationStartTime;
        private float _simulationTimeSeconds = 0f;

        private List<SliderStep> _sliderSteps = new List<SliderStep>
        {
            new(1, 12, SliderStep.Types.Month),
            new(1, 31, SliderStep.Types.Day),
            new(0, 23, SliderStep.Types.Hour),
            new(0, 59, SliderStep.Types.Minute),
            new(0, 59, SliderStep.Types.Second)
        };

        private SliderStep _currentZoom;
        private bool _isHovered;
        private bool _isDragging;


        private void Start()
        {
            _currentZoom = _sliderSteps[0];
            var eventTrigger = dateSlider.gameObject.AddComponent<EventTrigger>();
            
            // Begin Drag
            var beginDragEntry = new EventTrigger.Entry();
            beginDragEntry.eventID = EventTriggerType.BeginDrag;
            beginDragEntry.callback.AddListener((data) => { OnBeginDrag(); });
            eventTrigger.triggers.Add(beginDragEntry);
            
            // End Drag
            var endDragEntry = new EventTrigger.Entry();
            endDragEntry.eventID = EventTriggerType.EndDrag;
            endDragEntry.callback.AddListener((data) => { OnEndDrag(); });
            eventTrigger.triggers.Add(endDragEntry);
            _simulationStartTime = DateTime.Now;
            CurrentSimulatedTime = _simulationStartTime;

            UpdateVisuals();
        }

        public void HideOrShow()
        {
            var visible = !Panel.interactable;
            Panel.alpha = visible ? 1f : 0f;
            Panel.interactable = visible;     // Benutzerinteraktionen aktiv
            Panel.blocksRaycasts = visible;   // Blockiert weiterhin Klicks, wenn sichtbar
            OpenSettings.SetActive(!visible);
        }

        private void OnBeginDrag()
        {
            _isDragging = true;
        }

        private void OnEndDrag()
        {
            CurrentSimulatedTime = _currentZoom.SetDate(CurrentSimulatedTime, (int)dateSlider.value);
            _simulationStartTime = CurrentSimulatedTime;
            _simulationTimeSeconds = 0.0f;
            _isDragging = false;
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
        }

        public void OnTimeMultiplierValueChanged(float value)
        {
            timeMultiplier = value;
        }

        private void Update()
        {
            float scroll = Input.mouseScrollDelta.y;

            if (_isHovered && scroll != 0f)
            {
                OnScroll(scroll);
            }
            
            // Update simulation time if not dragging
            _simulationTimeSeconds += Time.deltaTime * timeMultiplier;
            CurrentSimulatedTime = _simulationStartTime.AddSeconds(_simulationTimeSeconds);
            // if (CurrentSimulatedTime > _currentZoom.ToMaxDate(CurrentSimulatedTime))
            // {
            //     if ((int)_currentZoom.Type < (int)_sliderSteps[0].Type)
            //         _currentZoom
            //     _simulationTimeSeconds = 0;
            //     _simulationStartTime = _absoluteMinDate;
            //     CurrentSimulatedTime = _absoluteMinDate;
            // }

            if (!_isDragging)
                dateSlider.value = _currentZoom.ToSliderValue(CurrentSimulatedTime);

            UpdateCurrentDateText();
            UpdateVisuals();
        }

        private void OnScroll(float scrollDelta)
        {
            int currentIndex = _sliderSteps.IndexOf(_currentZoom);
            currentIndex -= (int)Mathf.Sign(scrollDelta); // Scroll up -> kleinerer Index -> gröber

            currentIndex = Mathf.Clamp(currentIndex, 0, _sliderSteps.Count - 1);
            _currentZoom = _sliderSteps[currentIndex];
            dateSlider.value = _currentZoom.ToSliderValue(CurrentSimulatedTime);
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            dateSlider.minValue = _currentZoom.Min;
            dateSlider.maxValue = _currentZoom.Max;
            UpdateDateTexts();
            zoomLevelText.text = _currentZoom.Name + ": " + dateSlider.value;
        }

        private void UpdateDateTexts()
        {
            minDateText.text = _currentZoom.ToMinDateString(CurrentSimulatedTime);
            maxDateText.text = _currentZoom.ToMaxDateString(CurrentSimulatedTime);
        }

        private void UpdateCurrentDateText()
        {
            currentDateText.text = CurrentSimulatedTime.ToString("HH:mm:ss dd.MM.yyyy");
        }
    }
}