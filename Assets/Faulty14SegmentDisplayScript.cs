using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using System;
using System.Text.RegularExpressions;

public class Faulty14SegmentDisplayScript : MonoBehaviour
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

    private static readonly bool[][] _segmentArragements = new bool[26][] {
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
    private bool _isAnimating;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        _currentRSequenceIx = Rnd.Range(0, 26);
        _currentGSequenceIx = Rnd.Range(0, 26);
        _currentBSequenceIx = Rnd.Range(0, 26);
        _rSegPositions = Enumerable.Range(0, 14).ToArray();
        _gSegPositions = Enumerable.Range(0, 14).ToArray();
        _bSegPositions = Enumerable.Range(0, 14).ToArray();

        _rSegPositions.Shuffle();
        _gSegPositions.Shuffle();
        _bSegPositions.Shuffle();

        _cycleSequence = StartCoroutine(CycleSequence());
        Debug.LogFormat("[Faulty 14 Segment Display #{0}] Shuffled red segment order: {1}", _moduleId, _rSegPositions.Select(i => i + 1).Join(" "));
        Debug.LogFormat("[Faulty 14 Segment Display #{0}] Shuffled green segment order: {1}", _moduleId, _gSegPositions.Select(i => i + 1).Join(" "));
        Debug.LogFormat("[Faulty 14 Segment Display #{0}] Shuffled blue segment order: {1}", _moduleId, _bSegPositions.Select(i => i + 1).Join(" "));

        PlayPauseSel.OnInteract += delegate ()
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
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
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
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

        RightSel.OnInteract += delegate ()
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
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

        for (int i = 0; i < ColorPickerSels.Length; i++)
            ColorPickerSels[i].OnInteract += ColorPickerPress(i);

        for (int i = 0; i < SegmentSels.Length; i++)
            SegmentSels[i].OnInteract += SegmentPress(i);

        SubmitSel.OnInteract += SubmitPress;
    }

    private bool SubmitPress()
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
        if (_isAnimating)
            return false;
        var correct = new int[14];
        for (int i = 0; i < 14; i++)
        {
            if (_rSegPositions[i] != i || _gSegPositions[i] != i || _bSegPositions[i] != i)
                correct[i] = 1;
        }
        Debug.LogFormat("[Faulty 14 Segment Display #{0}] Submitted red segments: {1}", _moduleId, _rSegPositions.Select(i => i + 1).Join(" "));
        Debug.LogFormat("[Faulty 14 Segment Display #{0}] Submitted green segments: {1}", _moduleId, _gSegPositions.Select(i => i + 1).Join(" "));
        Debug.LogFormat("[Faulty 14 Segment Display #{0}] Submitted blue segments: {1}", _moduleId, _bSegPositions.Select(i => i + 1).Join(" "));
        _isAnimating = true;
        if (correct.Contains(1))
        {
            if (_cycleSequence != null)
                StopCoroutine(_cycleSequence);
            for (int i = 0; i < SegmentObjs.Length; i++)
                SegmentObjs[i].GetComponent<MeshRenderer>().material = SegmentMats[correct[i] == 0 ? 2 : 4];
            Module.HandleStrike();
            StartCoroutine(StrikeAnimation());
            Debug.LogFormat("[Faulty 14 Segment Display #{0}] Not all color channels have been correctly swapped. Strike.", _moduleId);
        }
        else
        {
            ;
            StartCoroutine(SolveAnimation());
            Debug.LogFormat("[Faulty 14 Segment Display #{0}] All color channels have been correctly swapped. Module solved.", _moduleId);
        }
        return false;
    }

    private KMSelectable.OnInteractHandler ColorPickerPress(int color)
    {
        return delegate ()
        {
            var soundNames = new string[] { "SegSelect1", "SegSelect2", "SegSelect3", "SegSelect4", "SegSelect5" };
            Audio.PlaySoundAtTransform(soundNames[Rnd.Range(0, soundNames.Length)], transform);
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
            if (_isAnimating)
                return false;
            var soundNames = new string[] { "SegSelect1", "SegSelect2", "SegSelect3", "SegSelect4", "SegSelect5" };
            Audio.PlaySoundAtTransform(soundNames[Rnd.Range(0, soundNames.Length)], transform);
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

    private IEnumerator StrikeAnimation()
    {
        yield return new WaitForSeconds(3);
        _cycleSequence = StartCoroutine(CycleSequence());
        PlayPauseText.text = "PAUSE";
        _isCycling = true;
        _isAnimating = false;
    }

    private IEnumerator SolveAnimation()
    {
        Audio.PlaySoundAtTransform("InputCorrect", transform);
        if (_cycleSequence != null)
            StopCoroutine(_cycleSequence);
        for (int i = 0; i < SegmentObjs.Length; i++)
            SegmentObjs[i].GetComponent<MeshRenderer>().material = SegmentMats[0];
        var solveSegs = new bool[15][]
        {
            new bool[14] { true, true, false, false, false, false, false, false, true, false, false, false, false, true }, //C
            new bool[14] { true, true, false, false, false, true, false, false, true, false, false, false, true, true },   //O
            new bool[14] { false, true, true, false, false, true, false, false, true, false, false, true, true, false },   //N
            new bool[14] { true, true, false, false, false, false, false, true, true, false, false, false, true, true },   //G
            new bool[14] { true, true, false, false, false, true, true, true, true, false, false, true, false, false },    //R
            new bool[14] { true, true, false, false, false, true, true, true, true, false, false, false, true, false },    //A
            new bool[14] { true, false, false, true, false, false, false, false, false, false, true, false, false, false },//T
            new bool[14] { false, true, false, false, false, true, false, false, true, false, false, false, true, true },  //U
            new bool[14] { false, true, false, false, false, false, false, false, true, false, false, false, false, true },//L
            new bool[14] { true, true, false, false, false, true, true, true, true, false, false, false, true, false },    //A
            new bool[14] { true, false, false, true, false, false, false, false, false, false, true, false, false, false },//T
            new bool[14] { true, false, false, true, false, false, false, false, false, false, true, false, false, true }, //I
            new bool[14] { true, true, false, false, false, true, false, false, true, false, false, false, true, true },   //O
            new bool[14] { false, true, true, false, false, true, false, false, true, false, false, true, true, false },   //N
            new bool[14] { true, true, false, false, false, false, true, true, false, false, false, false, true, true },   //S
        };
        var dashSegs = new bool[14] { false, false, false, false, false, false, true, true, false, false, false, false, false, false };
        for (int j = 0; j < solveSegs.Length; j++)
        {
            for (int i = 0; i < SegmentObjs.Length; i++)
                SegmentObjs[i].GetComponent<MeshRenderer>().material = SegmentMats[solveSegs[j][i] ? 2 : 0];
            yield return new WaitForSeconds(0.15f);
        }
        _moduleSolved = true;
        Module.HandlePass();
        for (int i = 0; i < SegmentObjs.Length; i++)
            SegmentObjs[i].GetComponent<MeshRenderer>().material = SegmentMats[dashSegs[i] ? 2 : 0];
    }

#pragma warning disable 0414
    private readonly string TwitchHelpMessage = "!{0} swap 1 14 [Swap segments 1 and 14] | !{0} red [Pick colors red/green/blue] | !{0} toggle [Pauses/resumes the cycle] | !{0} left/right <#> [Cycle left/right in the sequence, optionally with amount] | !{0} submit [Submit the answer] | Commands can be chained with commas and semicolons.";
#pragma warning restore 0414

    private abstract class TpCommand { }
    private sealed class TpSwap : TpCommand { public int Segment1, Segment2; }
    private sealed class TpColor : TpCommand { public int Color; }
    private sealed class TpToggle : TpCommand { }
    private sealed class TpMove : TpCommand { public bool Right; public int Amount; }
    private sealed class TpSubmit : TpCommand { }

    private IEnumerator ProcessTwitchCommand(string command)
    {
        // First, parse all of the commands and represent them as TpCommand objects
        var commandPieces = command.ToLowerInvariant().Split(';', ',');
        var commands = new List<TpCommand>();
        Match m;

        foreach (var cmd in commandPieces)
        {
            if ((m = Regex.Match(cmd, @"^\s*swap\s*(\d+)\s*(\d+)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
            {
                int val1;
                int val2;
                if (!int.TryParse(m.Groups[1].Value, out val1) || !int.TryParse(m.Groups[2].Value, out val2))
                {
                    yield return "sendtochaterror Invalid segments! Must be in the range from 1 to 14";
                    yield break;
                }
                if (val1 > 14 || val2 > 14 || val1 < 1 || val2 < 1)
                {
                    yield return "sendtochaterror Invalid segments! Must be in the range from 1 to 14";
                    yield break;
                }
                commands.Add(new TpSwap { Segment1 = val1, Segment2 = val2 });
                continue;
            }

            if ((m = Regex.Match(cmd, @"^\s*((?<r>red)|(?<g>green)|blue)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
            {
                commands.Add(new TpColor { Color = m.Groups["r"].Success ? 0 : m.Groups["g"].Success ? 1 : 2 });
                continue;
            }

            if ((m = Regex.Match(cmd, @"^\s*(left|(?<r>right))(\s+(?<amt>\d+))?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
            {
                int amount;
                if (!int.TryParse(m.Groups["amt"].Value, out amount) || amount < 1 || amount > 26)
                {
                    yield return string.Format("sendtochaterror “{0}” is an invalid amount by which to move left or right (must be 1–26).", m.Groups["amt"].Value);
                    yield break;
                }
                commands.Add(new TpMove { Right = m.Groups["r"].Success, Amount = amount });
                continue;
            }

            if ((m = Regex.Match(cmd, @"^\s*(pause|play|resume|toggle)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
            {
                commands.Add(new TpToggle());
                continue;
            }

            if ((m = Regex.Match(cmd, @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
            {
                commands.Add(new TpSubmit());
                continue;
            }

            yield break;
        }

        // Next, execute the commands
        yield return null;
        foreach (var cmd in commands)
        {
            TpSwap swap;
            TpColor color;
            TpMove move;

            if ((swap = cmd as TpSwap) != null)
            {
                SegmentSels[swap.Segment1 - 1].OnInteract();
                yield return new WaitForSeconds(0.2f);
                SegmentSels[swap.Segment2 - 1].OnInteract();
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            if ((color = cmd as TpColor) != null)
            {
                ColorPickerSels[color.Color].OnInteract();
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            if ((move = cmd as TpMove) != null)
            {
                if (_isCycling)
                {
                    yield return "sendtochaterror You can't go left or right if the sequence is cycling!";
                    yield break;
                }
                (move.Right ? RightSel : LeftSel).OnInteract();
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            if (cmd is TpToggle)
            {
                PlayPauseSel.OnInteract();
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            if (cmd is TpSubmit)
            {
                SubmitSel.OnInteract();
                yield return new WaitForSeconds(0.1f);
                continue;
            }

            yield break;
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        ColorPickerSels[0].OnInteract();
        yield return new WaitForSeconds(0.1f);
        for (int red = 0; red < 14; red++)
        {
            if (_rSegPositions[red] == red)
                continue;
            SegmentSels[red].OnInteract();
            yield return new WaitForSeconds(0.1f);
            SegmentSels[Array.IndexOf(_rSegPositions, red)].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        ColorPickerSels[1].OnInteract();
        yield return new WaitForSeconds(0.1f);
        for (int green = 0; green < 14; green++)
        {
            if (_gSegPositions[green] == green)
                continue;
            SegmentSels[green].OnInteract();
            yield return new WaitForSeconds(0.1f);
            SegmentSels[Array.IndexOf(_gSegPositions, green)].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        ColorPickerSels[2].OnInteract();
        yield return new WaitForSeconds(0.1f);
        for (int blue = 0; blue < 14; blue++)
        {
            if (_bSegPositions[blue] == blue)
                continue;
            SegmentSels[blue].OnInteract();
            yield return new WaitForSeconds(0.1f);
            SegmentSels[Array.IndexOf(_bSegPositions, blue)].OnInteract();
            yield return new WaitForSeconds(0.1f);
        }
        SubmitSel.OnInteract();
        while (!_moduleSolved)
            yield return true;
    }
}
