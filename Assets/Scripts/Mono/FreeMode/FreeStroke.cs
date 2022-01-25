using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI.Extensions;

namespace PointSoundGame.FreeMode
{
    public class FreeStroke : MonoBehaviour
    {
        // public FreePointDataItem itemData;

        // public string HexColor = "#FFF";
        internal delegate void PlayFinishedCallback();
        private PlayFinishedCallback mPlayFinishedCallback = null;

        // 0.3 - 3 倍
        private static float AMPLIFY_SCALE_MIN = 0.3f;
        private static float AMPLIFY_SCALE_RANGE = 2.7f;
        public static float AMPLIFY_STANDARD = (1.0f - AMPLIFY_SCALE_MIN) / AMPLIFY_SCALE_RANGE;

        internal UIStroke strokeLine = null;

        internal bool isDrawing;
        internal bool isPlaying
        {
            get { return isPlayingPoints; }
        }
        internal bool hasDrawnData {
            get { return drawingPositions.Count > 0; }
        }

        public float StrokeWidth = 20;
        public int SectionalSmooth = 100;
        public float MinDistanceBetweenMovement = 5;

        private List<Vector2> drawingPositions = new List<Vector2>();
        private List<float> drawingPositionTimes = new List<float>();
        private List<GameObject> pointObjects = new List<GameObject>();

        private int nextPlayingIndex = 0;
        private bool playingSequence = true;
        private bool isPlayingPoints = false;
        private float playingStartTime = 0;

        private int playingPointIndex = 0;
        private float playingPointTime = 0;
        private float playingPointDuration = 0;

        private static float TEMPO_LENGTH = 60.0f / 120.0f; // 一分钟60拍
        public float TempoDrawingDistancePerSecond = 20; // 20pt/s

        private void Awake() {
            strokeLine = gameObject.GetComponent<UIStroke>();
        }

        // Start is called before the first frame update
        void Start()
        {
            AmplyfyAudio(AMPLIFY_STANDARD);
        }

        // Update is called once per frame
        void Update()
        {
            if (isDrawing)
            {
                DrawStroke(Background.ConvertScreenPosition(Input.mousePosition), Time.time);
            }

            if (playingStartTime > 0 && playingPointIndex >= 0)
            {
                // Debug.Log($"PlayingPosition:Update: playingPointIndex -> {playingPointIndex}");

                if (playingPointIndex < drawingPositions.Count - 1)
                {
                    float now = Time.time;
                    float gap = now - playingPointTime;

                    // Debug.Log($"PlayingPosition:Update: gap({gap}) -> duration({playingPointDuration})");
                    if (gap > 0.02 && gap < playingPointDuration)
                    {
                        float increase = gap / playingPointDuration;

                        Vector2 p = drawingPositions[playingPointIndex];
                        Vector2 p_1 = drawingPositions[playingPointIndex+1];

                        Vector2 position = new Vector2(p.x + (p_1.x - p.x) * increase, p.y + (p_1.y - p.y) * increase);

                        // Debug.Log($"PlayingPosition:Update: {position.y} -> {playingPointDuration} -> {p_1.y}-{p.y}={p_1.y - p.y} {increase}");
                        UpdateAudioSettingFromBackgroundPosition(position, false);
                    }
                }
            }
        }

        internal void DrawStroke(Vector2 newPosition, float time)
        {
            if (!gameObject.activeSelf) return;

            if (drawingPositions.Count > 0)
            {
                Vector2 last = drawingPositions[drawingPositions.Count - 1];
                if (Vector2.Distance(last, newPosition) < MinDistanceBetweenMovement)
                {
                    return;
                }
            }
            drawingPositions.Add(newPosition);
            drawingPositionTimes.Add(time);

            // GameObject go = PointDataManager.Singleton.createCurrentStepPoint(null);
            // if (go)
            // {
            //     go.transform.SetParent(transform);
            //     go.transform.localPosition = newPosition;
            //     go.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 20);
            //     go.GetComponent<Button>().interactable = false;
            //     go.GetComponent<Image>().raycastTarget = false;
            //     pointObjects.Add(go);
            // }
            DrawStroke();
        }

        private void DrawStroke()
        {
            if (drawingPositions.Count > 1)
            {
                strokeLine.DrawMesh(drawingPositions.ToArray(), StrokeWidth, SectionalSmooth);
                strokeLine.UpdatePercent(1);
                Debug.Log($"DrawStroke Count {StrokeWidth} {SectionalSmooth} " + drawingPositions.Count);
            }
        }

        public void CleanPoints()
        {
            foreach (var go in pointObjects)
            {
                Destroy(go);
            }
            pointObjects.Clear();

            drawingPositions.Clear();
            drawingPositionTimes.Clear();
        }

        internal void PlayPoints(bool play, PlayFinishedCallback callback = null)
        {
            if (!gameObject.activeSelf) return;

            isPlayingPoints = play;
            if (play)
            {
                mPlayFinishedCallback = callback;

                nextPlayingIndex = 0;
                playingPointIndex = -1;
                playingSequence = true;
                playingStartTime = Time.time;
                StartPlayAudio();
                StartCoroutine(AdjustByPointAtIndex());
            }
            else
            {
                StopPlayAudio();
                playingSequence = false;
                playingStartTime = 0;
                playingPointIndex = -1;

                if (mPlayFinishedCallback != null) {
                    mPlayFinishedCallback();
                    mPlayFinishedCallback = null;
                }
            }
        }

        private IEnumerator AdjustByPointAtIndex()
        {
            if (nextPlayingIndex >= drawingPositions.Count)
            {
                PlayPoints(false);
                yield break;
            }

            if (playingSequence)
            {
                if (nextPlayingIndex < drawingPositions.Count)
                {
                    float waitingNext = 0.01f;
                    if (nextPlayingIndex > 0)
                    {
                        Vector2 position = drawingPositions[nextPlayingIndex];
                        float ptime = drawingPositionTimes[nextPlayingIndex];
                        Vector2 position_1 = drawingPositions[nextPlayingIndex - 1];
                        float ptime_1 = drawingPositionTimes[nextPlayingIndex - 1];

                        float distance = Vector2.Distance(position_1, position);
                        float toffset = ptime - ptime_1;
                        waitingNext = TEMPO_LENGTH * (distance / TempoDrawingDistancePerSecond) * toffset;

                        Debug.Log($"PlayingPosition: {nextPlayingIndex} -> d{distance}({toffset}) -> {waitingNext}");
                    }

                    // StartCoroutine(PlayingPosition(nextPlayingIndex, waitingNext));
                    PlayingPosition(nextPlayingIndex, waitingNext);
                    nextPlayingIndex += 1;

                    if (waitingNext < 0.05)
                    {
                        yield return new WaitForEndOfFrame();
                    }
                    else
                    {
                        yield return new WaitForSeconds(waitingNext);
                    }
                    StartCoroutine(AdjustByPointAtIndex());
                } else {
                    PlayPoints(false);
                }
            }
        }

        private void PlayingPosition(int index, float length)
        {
            // Debug.Log($"PlayingPosition: {index} -> {length}");

            Vector2 position = drawingPositions[index];
            float ptime = drawingPositionTimes[index];

            strokeLine.gameObject.GetComponent<FreeStroke>().UpdateAudioSettingFromBackgroundPosition(position, false);

            playingPointIndex = index;
            playingPointTime = Time.time;
            playingPointDuration = length;
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

            // itemData.amplitude = amplitude;
        }

        public void PitchAudio(float pitchInOctave)
        {
            GetComponent<AudioSourcePlay>().PitchPlayingAudio(pitchInOctave);
            // itemData.pitchInOctave = pitchInOctave;
        }

        public void StartPlayAudio()
        {
            GetComponent<AudioSourcePlay>().StartPlayAudio();
        }

        public void StopPlayAudio()
        {
            GetComponent<AudioSourcePlay>().StopPlayAudio();
        }

        public void UpdateAudioSettingFromBackgroundPosition(Vector2 positionInBackground, bool round=true)
        {
            // itemData.numOfBeatTempo = Background.ConvertTempoFromPositionX(positionInBackground.x + Background.Size.x / 2);
            float pitch = Background.ConvertPitchInOctaveFromPositionY(positionInBackground.y, round);
            Debug.Log($"FreeStroke:UpdateSetting: {positionInBackground.y} -> {pitch}" );
            PitchAudio(pitch);
        }

        // public void UpdateAudioSetting()
        // {
        //     AmplyfyAudio(itemData.amplitude);
        //     PitchAudio(itemData.pitchInOctave);
        //     transform.localPosition = new Vector2(
        //         Background.ConvertTempoToPositionX(itemData.numOfBeatTempo) - Background.Size.x / 2,
        //         Background.ConvertPitchInOctaveToPositionY(itemData.pitchInOctave)
        //     );
        // }
    }
}