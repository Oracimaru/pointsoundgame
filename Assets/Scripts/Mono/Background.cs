using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PointSoundGame
{
    public class Background : MonoBehaviour
    {
        public static Background Instance = null;
        public static Vector2 Size = new Vector2(1920, 1080 - 120);
        public static Vector2 ConvertScreenPosition(Vector2 positionInScreen)
        {
            RectTransform backgroundTransform = Instance.gameObject.GetComponent<RectTransform>();
            Vector2 backgroundPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(backgroundTransform, positionInScreen, null, out backgroundPosition);
            return backgroundPosition;
        }

        public static float DISTANCE_OF_SEMITONE_STEP = 50.0f;
        public static float SEMITONE_STEP_LINE_HEIGHT = 2.0f;

        public static float TEMPO_START_MARGIN_LEFT = 80.0f;
        public static float TEMPO_INTERVAL_WIDTH = 200.0f;
        public static float TEMPO_LINE_WIDTH = 4.0f;

        private GameObject stepBackground = null;
        private GameObject freeBackground = null;

        private PointDataManager.PlayMode currentBackgroundMode;

        // Start is called before the first frame update
        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            stepBackground = transform.Find("StepBackground").gameObject;
            freeBackground = transform.Find("FreeBackground").gameObject;

            RectTransform rectTransform = GetComponent<RectTransform>();
            Vector2 size = rectTransform.rect.size;

            Debug.Log("size: " + size);

            float y = 0;
            CreateStepLine(size, y, SEMITONE_STEP_LINE_HEIGHT * 2);
            while (y + DISTANCE_OF_SEMITONE_STEP < size.y / 2)
            {
                y += DISTANCE_OF_SEMITONE_STEP;
                CreateStepLine(size, y, SEMITONE_STEP_LINE_HEIGHT);
            }
            y = 0;
            while (y - DISTANCE_OF_SEMITONE_STEP > -size.y / 2)
            {
                y -= DISTANCE_OF_SEMITONE_STEP;
                CreateStepLine(size, y, SEMITONE_STEP_LINE_HEIGHT);
            }
        }

        private void CreateStepLine(Vector2 psize, float y, float h)
        {
            Debug.Log("CreateStepLine: " + y);

            // step line
            GameObject stepLine = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/SemitoneStepLine"));
            RectTransform stepTransform = stepLine.GetComponent<RectTransform>();
            stepTransform.SetParent(stepBackground.transform);
            stepTransform.offsetMin = new Vector2(0, y - h / 2);
            stepTransform.offsetMax = new Vector2(0, y + h / 2);

            // tempo line
            float x = TEMPO_START_MARGIN_LEFT;
            while (x < psize.x)
            {
                GameObject tempoLine = GameObject.Instantiate(Resources.Load<GameObject>("Prefabs/TempoLine"));
                RectTransform tempoTransform = tempoLine.GetComponent<RectTransform>();
                tempoTransform.SetParent(stepTransform);
                tempoTransform.localPosition = new Vector3(x - psize.x / 2, 0, 0);
                tempoTransform.sizeDelta = new Vector2(TEMPO_LINE_WIDTH, h * 2);

                x += TEMPO_INTERVAL_WIDTH;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (currentBackgroundMode != PointDataManager.Singleton.playMode) {
                currentBackgroundMode = PointDataManager.Singleton.playMode;
                
                switch (currentBackgroundMode)
                {    
                    case PointDataManager.PlayMode.Step:
                        stepBackground.SetActive(true);
                        freeBackground.SetActive(false);
                        break;
                    case PointDataManager.PlayMode.Free:
                        stepBackground.SetActive(false);
                        freeBackground.SetActive(true);
                        break;
                }
            }
        }

        public static float ConvertTempoFromPositionX(float positionX)
        {
            return (positionX - TEMPO_START_MARGIN_LEFT) / TEMPO_INTERVAL_WIDTH;
        }

        public static float ConvertTempoToPositionX(float tempo)
        {
            return TEMPO_START_MARGIN_LEFT + tempo * TEMPO_INTERVAL_WIDTH;
        }

        public static float ConvertPitchInOctaveFromPositionY(float positionY, bool round=true)
        {
            float pitch = positionY / Background.DISTANCE_OF_SEMITONE_STEP;
            return (round ? Mathf.Round(pitch) : pitch) / 12;
        }

        public static float ConvertPitchInOctaveToPositionY(float pitchInOctave)
        {
            return pitchInOctave * 12 * Background.DISTANCE_OF_SEMITONE_STEP;
        }
    }
}
