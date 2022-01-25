using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace PointSoundGame.StepMode
{
    public class StepPoint : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public StepPointDataItem itemData;

        public string HexColor = "#FFF";
        public bool IgnoreHoldZooming = false;

        private bool isPointerDown = false;
        private float pointDownTime = 0;

        private Vector2 pointDownMousePosition;
        enum DragState
        {
            None,
            Detecting,
            Confirm,
            Failed
        }
        private DragState pointerDragState = DragState.None;

        enum LongPressState
        {
            None,
            Detecting,
            Confirm,
            Failed
        }
        private LongPressState pointerLongPressDownState = LongPressState.None;
        private static float POINT_LONGPRESS_DETECT_LENGTH = 0.8f; // 秒
        private float pointLongPressDownTime = 0;
        private float pointLongPressDownOffset = 0;

        private static float POINT_WIDTH_MIN = 30.0f;
        private static float POINT_WIDTH_MAX = 200.0f;

        private static float AMPLIFY_DELAY = 1.0f; // 秒
        private static float AMPLIFY_PERIOD = 4.0f; // 秒
        // 0.3 - 3 倍
        private static float AMPLIFY_SCALE_MIN = 0.3f;
        private static float AMPLIFY_SCALE_RANGE = 2.7f;
        public static float AMPLIFY_STANDARD = (1.0f - AMPLIFY_SCALE_MIN) / AMPLIFY_SCALE_RANGE;

        // Start is called before the first frame update
        void Start()
        {
            Material material = new Material(Shader.Find("Shader Graphs/Point2DShaderGraph"));
            Color color;
            ColorUtility.TryParseHtmlString(HexColor, out color);
            material.SetColor("_color", color);
            // material.SetFloat("_color_saturation", 1.12f);
            // material.SetFloat("_dirty_range", 0.04f);
            material.SetVector("_dirty_random_xy", new Vector4(Random.Range(1.0f, 5.0f), Random.Range(1.0f, 5.0f), 0, 0));
            GetComponent<Image>().material = material;
            AmplyfyAudio(AMPLIFY_STANDARD);
        }

        // Update is called once per frame
        void Update()
        {
            if (isPointerDown)
            {
                float now = Time.time;
                if (pointerLongPressDownState == LongPressState.Detecting
                    && now - pointDownTime >= POINT_LONGPRESS_DETECT_LENGTH)
                {
                    pointLongPressDownTime = now;

                    StartPlayAudio();

                    float amp = currentAudioAmplitude;
                    float offset = (Mathf.Asin(amp * 2 - 1.0f) / (2 * Mathf.PI) + 1.0f / 4) * AMPLIFY_PERIOD;
                    pointLongPressDownOffset = offset;

                    pointerLongPressDownState = LongPressState.Confirm;
                    pointerDragState = DragState.Failed;
                }
                else if (pointerLongPressDownState == LongPressState.Confirm)
                {
                    float delta = Time.time - pointLongPressDownTime - AMPLIFY_DELAY;
                    float amplitude = delta > 0 ?
                        ((Mathf.Sin((((delta + pointLongPressDownOffset) / AMPLIFY_PERIOD) - 1.0f / 4) * 2 * Mathf.PI) + 1) / 2.0f)
                        : currentAudioAmplitude;
                    // Debug.Log("Update:amplitude: " + amplitude + " " + pointDownOffset);

                    AmplyfyAudio(amplitude);
                }

                if (pointerDragState == DragState.Detecting
                    && Vector2.Distance(pointDownMousePosition, Input.mousePosition) > 10)
                {
                    pointerDragState = DragState.Confirm;
                    pointerLongPressDownState = LongPressState.Failed;

                    TransformPointByPositionInScreen(Input.mousePosition);
                    StartPlayAudio();
                }
                else if (pointerDragState == DragState.Confirm)
                {
                    TransformPointByPositionInScreen(Input.mousePosition);
                }
            }
        }

        public void TransformPointByPositionInScreen(Vector2 positionInScreen)
        {
            transform.localPosition = ConvertScreenPositionToParent(positionInScreen);
            GetComponent<StepPoint>().UpdateAudioSettingFromBackgroundPosition(
                Background.ConvertScreenPosition(positionInScreen));
        }

        private void ResizeCreatingPointByTime(float amplitude)
        {
            if (IgnoreHoldZooming)
            {
                return;
            }

            float diff = POINT_WIDTH_MAX - POINT_WIDTH_MIN;

            float width = POINT_WIDTH_MIN + amplitude * diff;
            RectTransform transform = GetComponent<RectTransform>();
            transform.sizeDelta = new Vector2(width, width);
        }

        public Vector2 ConvertScreenPositionToParent(Vector2 positionInScreen)
        {
            Vector2 localPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform.parent, positionInScreen, null, out localPosition);
            return localPosition;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Debug.Log("Point:OnPointerDown: " + eventData.position + eventData.pointerCurrentRaycast.gameObject.name);

            if (!isPointerDown)
            {
                finishPointer();
            }

            isPointerDown = true;
            pointDownTime = Time.time;
            pointDownMousePosition = eventData.position;
            pointerDragState = DragState.Detecting;

            pointerLongPressDownState = LongPressState.Detecting;
            pointLongPressDownTime = 0;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Debug.Log("Point:OnPointerUp: " + eventData.position + eventData.pointerCurrentRaycast.gameObject.name);

            if (isPointerDown)
            {
                finishPointer();
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Debug.Log("Point:OnPointerExit: " + eventData.position);

            // AudioSourcePlay audioPlayComp = GetComponent<AudioSourcePlay>();
            // audioPlayComp.StopPlayAudio();
            // pointDownTime = 0;
            // isPointerDown = false;
        }

        private void finishPointer()
        {
            StopPlayAudio();

            pointerLongPressDownState = LongPressState.None;
            pointLongPressDownTime = 0;

            pointDownMousePosition = Vector2.zero;
            isPointerDown = false;
            pointDownTime = 0;
        }

        private float currentAudioAmplitude
        {
            get
            {
                AudioSourcePlay audioPlayComp = GetComponent<AudioSourcePlay>();
                float amplitude = (audioPlayComp.GetAmplitude() - AMPLIFY_SCALE_MIN) / AMPLIFY_SCALE_RANGE;
                return amplitude;
            }
        }

        public void AmplyfyAudio(float amplitude)
        {
            float audioAmp = AMPLIFY_SCALE_MIN + amplitude * AMPLIFY_SCALE_RANGE;
            GetComponent<AudioSourcePlay>().AmplifyPlayingAudio(audioAmp);

            ResizeCreatingPointByTime(amplitude);

            itemData.amplitude = amplitude;
        }

        public void PitchAudio(float pitchInOctave)
        {
            GetComponent<AudioSourcePlay>().PitchPlayingAudio(pitchInOctave);
            itemData.pitchInOctave = pitchInOctave;
        }

        public void StartPlayAudio()
        {
            GetComponent<AudioSourcePlay>().StartPlayAudio();
        }

        public void StopPlayAudio()
        {
            GetComponent<AudioSourcePlay>().StopPlayAudio();
        }

        public void UpdateAudioSettingFromBackgroundPosition(Vector2 positionInBackground)
        {
            itemData.numOfBeatTempo = Background.ConvertTempoFromPositionX(positionInBackground.x + Background.Size.x / 2);
            PitchAudio(Background.ConvertPitchInOctaveFromPositionY(positionInBackground.y));
        }

        public void UpdateAudioSetting()
        {
            AmplyfyAudio(itemData.amplitude);
            PitchAudio(itemData.pitchInOctave);
            transform.localPosition = new Vector2(
                Background.ConvertTempoToPositionX(itemData.numOfBeatTempo) - Background.Size.x / 2,
                Background.ConvertPitchInOctaveToPositionY(itemData.pitchInOctave)
            );
        }
    }
}