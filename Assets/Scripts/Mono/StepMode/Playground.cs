using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEditor;

namespace PointSoundGame.StepMode
{
    public class Playground : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool isPlaying {
            get { return isPlayingPoints; }
        }

        private GameObject creatingPointGo = null;
        private float creatingStartTime = 0;

        private ToolBar toolBar = null;

        private List<GameObject> pointObjects = new List<GameObject>();

        private int nextPlayingIndex = 0;
        private bool playingSequence = true;
        private bool isPlayingPoints = false;
        private float playingStartTime = 0;
        private List<GameObject> playingTempoSortedPoints;

        private static float TEMPO_LENGTH = 60.0f / 120.0f; // 一分钟90拍

        // Start is called before the first frame update
        void Start()
        {
            toolBar = GameObject.Find("ToolBar").GetComponent<ToolBar>();

            foreach (var item in PointDataManager.Singleton.stepPointData.pointList)
            {
                GameObject go = PointDataManager.Singleton.createStepPoint(item);
                go.transform.SetParent(transform);
                pointObjects.Add(go);

                StepPoint point = go.GetComponent<StepPoint>();
                point.UpdateAudioSetting();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (creatingPointGo != null)
            {
                creatingPointGo.GetComponent<StepPoint>().TransformPointByPositionInScreen(Input.mousePosition);
            }
        }

        private void AmplifyAudio(float amplitude)
        {
            creatingPointGo.GetComponent<StepPoint>().AmplyfyAudio(amplitude);
        }

        private void PitchAudio(float pitchInOctave)
        {
            creatingPointGo.GetComponent<StepPoint>().PitchAudio(pitchInOctave);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (creatingPointGo)
            {
                creatingPointGo.GetComponent<StepPoint>().StopPlayAudio();
                creatingStartTime = 0;
                creatingPointGo = null;
            }

            Debug.Log("StepMode.Playground:OnPointerDown: " + eventData.position + eventData.pointerCurrentRaycast.gameObject.name);

            GameObject go = PointDataManager.Singleton.createCurrentStepPoint(null);
            if (go)
            {
                go.transform.SetParent(transform);
                pointObjects.Add(go);
                PointDataManager.Singleton.stepPointData.Append(go.GetComponent<StepPoint>().itemData);

                creatingStartTime = Time.time;
                creatingPointGo = go;

                StepPoint point = creatingPointGo.GetComponent<StepPoint>();
                Debug.Log("OnPointerDown:creatingPoint " + creatingPointGo);
                point.TransformPointByPositionInScreen(eventData.position);

                point.StartPlayAudio();
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Debug.Log("StepMode.Playground:OnPointerUp: " + eventData.position + eventData.pointerCurrentRaycast.gameObject.name);

            if (creatingPointGo)
            {
                creatingPointGo.GetComponent<StepPoint>().StopPlayAudio();
                creatingStartTime = 0;
                creatingPointGo = null;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Debug.Log("StepMode.Playground:OnPointerExit: " + eventData.position);

            if (creatingPointGo)
            {
                creatingPointGo.GetComponent<StepPoint>().StopPlayAudio();
                creatingStartTime = 0;
                creatingPointGo = null;
            }
        }

        internal void CleanPoints()
        {
            foreach (var go in pointObjects)
            {
                Destroy(go);
            }
            pointObjects.Clear();
        }

        internal void PlayPoints(bool play)
        {
            isPlayingPoints = play;
            if (play)
            {
                playingTempoSortedPoints = new List<GameObject>(pointObjects);
                playingTempoSortedPoints.Sort((p1, p2) =>
                    p1.GetComponent<StepPoint>().itemData.numOfBeatTempo.CompareTo(p2.GetComponent<StepPoint>().itemData.numOfBeatTempo));

                nextPlayingIndex = 0;
                playingSequence = true;
                playingStartTime = Time.time;
                StartCoroutine(PlayPointAtIndex());
            }
            else
            {
                foreach (var go in pointObjects)
                {
                    go.GetComponent<StepPoint>().StopPlayAudio();
                }
                playingSequence = false;
            }
        }

        private IEnumerator PlayPointAtIndex()
        {
            if (nextPlayingIndex >= playingTempoSortedPoints.Count)
            {
                PlayPoints(false);
                yield break;
            }

            if (playingSequence)
            {
                if (nextPlayingIndex < playingTempoSortedPoints.Count)
                {
                    GameObject go = playingTempoSortedPoints[nextPlayingIndex];
                    go.GetComponent<StepPoint>().StartPlayAudio();
                    Debug.Log("PlayPointAtIndex: " + JsonUtility.ToJson(go.GetComponent<StepPoint>().itemData));

                    float waitingNext = 1.0f;
                    if (nextPlayingIndex + 1 < playingTempoSortedPoints.Count)
                    {
                        float tempo = go.GetComponent<StepPoint>().itemData.numOfBeatTempo;
                        float tempoNext = playingTempoSortedPoints[nextPlayingIndex + 1].GetComponent<StepPoint>().itemData.numOfBeatTempo;
                        waitingNext = TEMPO_LENGTH * (tempoNext - tempo);
                    }

                    StartCoroutine(PlayingPoint(go, nextPlayingIndex, waitingNext));
                    nextPlayingIndex += 1;

                    if (waitingNext < 0.05)
                    {
                        yield return new WaitForEndOfFrame();
                    }
                    else
                    {
                        yield return new WaitForSeconds(waitingNext);
                    }
                    StartCoroutine(PlayPointAtIndex());
                }
            }
        }

        private IEnumerator PlayingPoint(GameObject go, int index, float length)
        {
            yield return new WaitForSeconds(length);
            go.GetComponent<StepPoint>().Invoke("StopPlayAudio", 0.1f);

            if (index + 1 == playingTempoSortedPoints.Count)
            {
                yield return new WaitForSeconds(0.1f);
                PlayPoints(false);
            }
        }
    }
}