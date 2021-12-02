using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using System;

public class Faulty14SegmentDisplaysScript : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombModule Module;
    public KMBombInfo BombInfo;

    public KMSelectable[] SegmentSels;
    public KMSelectable[] ColorSels;
    public KMSelectable PlayPauseSel;
    public KMSelectable LeftSel;
    public KMSelectable RightSel;
    public KMSelectable SubmitSel;
    public KMSelectable[] ColorPickerSels;
    public GameObject[] SegmentObjs;
    public GameObject[] SegmentBorderObjs;
    public GameObject PickerLight;
    public Material[] SegmentMats;
    public Material[] PickerMats;
    public Material SegmentBorderMat;
    public TextMesh PlayPauseText;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private bool[][] _segmentArragements = new bool[26][] {
        new bool[14] { true, true, false, false, false, true, true, true, true, false, false, false, true, false },    //A
        new bool[14] { true, false, false, true, false, true, false, true, false, false, true, false, true, true },    //B
        new bool[14] { true, true, false, false, false, false, false, false, true, false, false, false, false, true }, //C
        new bool[14] { true, false, false, true, false, true, false, false, false, false, true, false, true, true },   //D
        new bool[14] { true, true, false, false, false, false, true, true, true, false, false, false, false, true },   //E
        new bool[14] { true, true, false, false, false, false, true, true, true, false, false, false, false, false },  //F
        new bool[14] { true, true, false, false, false, false, false, true, true, false, false, false, true, true },   //G
        new bool[14] { false, true, false, false, false, true, true, true, true, false, false, false, true, false },   //H
        new bool[14] { true, false, false, true, false, false, false, false, false, false, true, false, false, true }, //I
        new bool[14] { false, false, false, false, false, true, false, false, true, false, false, false, true, true }, //J
        new bool[14] { false, true, false, false, true, false, true, false, true, false, false, true, false, false },  //K
        new bool[14] { false, true, false, false, false, false, false, false, true, false, false, false, false, true },//L
        new bool[14] { false, true, true, false, true, true, false, false, true, false, false, false, true, false },   //M
        new bool[14] { false, true, true, false, false, true, false, false, true, false, false, true, true, false },   //N
        new bool[14] { true, true, false, false, false, true, false, false, true, false, false, false, true, true },   //O
        new bool[14] { true, true, false, false, false, true, true, true, true, false, false, false, false, false },   //P
        new bool[14] { true, true, false, false, false, true, false, false, true, false, false, true, true, true },    //Q
        new bool[14] { true, true, false, false, false, true, true, true, true, false, false, true, false, false },    //R
        new bool[14] { true, true, false, false, false, false, true, true, false, false, false, false, true, true },   //S
        new bool[14] { true, false, false, true, false, false, false, false, false, false, true, false, false, false },//T
        new bool[14] { false, true, false, false, false, true, false, false, true, false, false, false, true, true },  //U
        new bool[14] { false, true, false, false, true, false, false, false, true, true, false, false, false, false }, //V
        new bool[14] { false, true, false, false, false, true, false, false, true, true, false, true, true, false },   //W
        new bool[14] { false, false, true, false, true, false, false, false, false, true, false, true, false, false }, //X
        new bool[14] { false, false, true, false, true, false, false, false, false, false, true, false, false, false },//Y
        new bool[14] { true, false, false, false, true, false, false, false, false, true, false, false, false, true } };//Z

    private int _currentRSequenceIx;
    private int _currentGSequenceIx;
    private int _currentBSequenceIx;
    private Coroutine _cycleSequence;
    private bool _isCycling = true;

    private int[] _rSegPositions = new int[14];
    private int[] _gSegPositions = new int[14];
    private int[] _bSegPositions = new int[14];

    private int _currentSelectedColor;
    private int _currentSelectedSegment = 99;
    private bool _segIsSelected;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        _currentRSequenceIx = Rnd.Range(0, 26);
        _currentGSequenceIx = Rnd.Range(0, 26);
        _currentBSequenceIx = Rnd.Range(0, 26);
        _rSegPositions = Enumerable.Range(0, 14).ToArray().Shuffle();
        Debug.LogFormat("[Faulty 14 Segment Displays #{0}] Shuffled red segment order: {1}", _moduleId, _rSegPositions.Select(i => i + 1).Join(" "));
        _gSegPositions = Enumerable.Range(0, 14).ToArray().Shuffle();
        Debug.LogFormat("[Faulty 14 Segment Displays #{0}] Shuffled green segment order: {1}", _moduleId, _gSegPositions.Select(i => i + 1).Join(" "));
        _bSegPositions = Enumerable.Range(0, 14).ToArray().Shuffle();
        Debug.LogFormat("[Faulty 14 Segment Displays #{0}] Shuffled green segment order: {1}", _moduleId, _bSegPositions.Select(i => i + 1).Join(" "));
        _cycleSequence = StartCoroutine(CycleSequence());

        PlayPauseSel.OnInteract += delegate ()
        {
            if (!_moduleSolved)
            {
                if (_isCycling)
                {
                    if (_cycleSequence != null)
                        StopCoroutine(_cycleSequence);
                    _isCycling = false;
                    PlayPauseText.text = "PLAY";
                }
                else
                {
                    _cycleSequence = StartCoroutine(CycleSequence());
                    _isCycling = true;
                    PlayPauseText.text = "PAUSE";
                }
            }
            return false;
        };

        LeftSel.OnInteract += delegate ()
        {
            if (!_moduleSolved)
            {
                if (!_isCycling)
                {
                    _currentRSequenceIx = (_currentRSequenceIx + 1) % 26;
                    _currentGSequenceIx = (_currentGSequenceIx + 1) % 26;
                    _currentBSequenceIx = (_currentBSequenceIx + 1) % 26;
                    for (int i = 0; i < SegmentObjs.Length; i++)
                        SegmentObjs[i].GetComponent<MeshRenderer>().material = SegmentMats[
                            (_segmentArragements[_currentRSequenceIx][_rSegPositions[i]] ? 4 : 0) +
                            (_segmentArragements[_currentGSequenceIx][_gSegPositions[i]] ? 2 : 0) +
                            (_segmentArragements[_currentBSequenceIx][_bSegPositions[i]] ? 1 : 0)];
                }
            }
            return false;
        };

        RightSel.OnInteract += delegate ()
        {
            if (!_moduleSolved)
            {
                if (!_isCycling)
                {
                    _currentRSequenceIx = (_currentRSequenceIx + 25) % 26;
                    _currentGSequenceIx = (_currentGSequenceIx + 25) % 26;
                    _currentBSequenceIx = (_currentBSequenceIx + 25) % 26;
                    for (int i = 0; i < SegmentObjs.Length; i++)
                        SegmentObjs[i].GetComponent<MeshRenderer>().material = SegmentMats[
                            (_segmentArragements[_currentRSequenceIx][_rSegPositions[i]] ? 4 : 0) +
                            (_segmentArragements[_currentGSequenceIx][_gSegPositions[i]] ? 2 : 0) +
                            (_segmentArragements[_currentBSequenceIx][_bSegPositions[i]] ? 1 : 0)];
                }
            }
            return false;
        };

        for (int i = 0; i < ColorPickerSels.Length; i++)
            ColorPickerSels[i].OnInteract += ColorPickerPress(i);

        for (int i = 0; i < SegmentSels.Length; i++)
            SegmentSels[i].OnInteract += SegmentPress(i);

        SubmitSel.OnInteract += SubmitPress;
    }

    private bool SubmitPress()
    {
        bool correct = true;
        for (int i = 0; i < 14; i++)
        {
            if (_rSegPositions[i] != i || _gSegPositions[i] != i || _bSegPositions[i] != i)
            {
                correct = false;
                break;
            }
        }
        if (correct)
            Module.HandlePass();
        else
            Module.HandleStrike();
        return false;
    }

    private KMSelectable.OnInteractHandler ColorPickerPress(int color)
    {
        return delegate ()
        {
            if (!_moduleSolved)
            {
                _currentSelectedColor = color;
                PickerLight.GetComponent<MeshRenderer>().material = PickerMats[_currentSelectedColor];
            }
            return false;
        };
    }

    private KMSelectable.OnInteractHandler SegmentPress(int seg)
    {
        return delegate ()
        {
            if (!_segIsSelected)
            {
                _segIsSelected = true;
                _currentSelectedSegment = seg;
                SegmentBorderObjs[seg].GetComponent<MeshRenderer>().material = SegmentBorderMat;
            }
            else if (seg == _currentSelectedSegment)
            {
                _currentSelectedSegment = 99;
                _segIsSelected = false;
                for (int i = 0; i < 14; i++)
                    SegmentBorderObjs[i].GetComponent<MeshRenderer>().material = SegmentMats[0];
            }
            else
            {
                var colorNames = new string[] { "red", "green", "blue" };
                if (_currentSelectedColor == 0)
                {
                    var temp = _rSegPositions[seg];
                    _rSegPositions[seg] = _rSegPositions[_currentSelectedSegment];
                    _rSegPositions[_currentSelectedSegment] = temp;
                }
                else if (_currentSelectedColor == 1)
                {
                    var temp = _gSegPositions[seg];
                    _gSegPositions[seg] = _gSegPositions[_currentSelectedSegment];
                    _gSegPositions[_currentSelectedSegment] = temp;
                }
                else
                {
                    var temp = _bSegPositions[seg];
                    _bSegPositions[seg] = _bSegPositions[_currentSelectedSegment];
                    _bSegPositions[_currentSelectedSegment] = temp;
                }
                var logSegs = new int[2] { seg, _currentSelectedSegment };
                Array.Sort(logSegs);
                Debug.LogFormat("[Faulty 14 Segment Displays #{0}] Swapped segments #{1} and #{2} on the {3} channel.", _moduleId, logSegs[0] + 1, logSegs[1] + 1, colorNames[_currentSelectedColor]);
                for (int i = 0; i < 14; i++)
                    SegmentBorderObjs[i].GetComponent<MeshRenderer>().material = SegmentMats[0];
                _segIsSelected = false;
            }
            for (int i = 0; i < SegmentObjs.Length; i++)
                SegmentObjs[i].GetComponent<MeshRenderer>().material = SegmentMats[
                    (_segmentArragements[_currentRSequenceIx][_rSegPositions[i]] ? 4 : 0) +
                    (_segmentArragements[_currentGSequenceIx][_gSegPositions[i]] ? 2 : 0) +
                    (_segmentArragements[_currentBSequenceIx][_bSegPositions[i]] ? 1 : 0)];
            return false;
        };
    }

    private IEnumerator CycleSequence()
    {
        while (!_moduleSolved)
        {
            for (int i = 0; i < SegmentObjs.Length; i++)
                SegmentObjs[i].GetComponent<MeshRenderer>().material = SegmentMats[
                    (_segmentArragements[_currentRSequenceIx][_rSegPositions[i]] ? 4 : 0) +
                    (_segmentArragements[_currentGSequenceIx][_gSegPositions[i]] ? 2 : 0) +
                    (_segmentArragements[_currentBSequenceIx][_bSegPositions[i]] ? 1 : 0)];
            yield return new WaitForSeconds(0.5f);
            _currentRSequenceIx = (_currentRSequenceIx + 1) % 26;
            _currentGSequenceIx = (_currentGSequenceIx + 1) % 26;
            _currentBSequenceIx = (_currentBSequenceIx + 1) % 26;
        }
    }
}
