using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ProceduralMusic : MonoBehaviour
{
    [Header("Wavetable")]
    public WavetableTxtLoader wavetableLoader;

    [Header("Musica")]
    [Range(60f, 130f)] public float tempo = 80f;
    [Range(1, 4)] public int beatsPerNote = 2;

    public float[] notes =
    {
        440.00f, 523.25f, 659.25f, 587.33f,   // A4 C5 E5 D5
        659.25f, 523.25f, 440.00f, 392.00f    // E5 C5 A4 G4
    };

    public float[] notesVariation =
    {
        440.00f, 659.25f, 587.33f, 523.25f,   // A4 E5 D5 C5
        587.33f, 523.25f, 440.00f, 329.63f    // D5 C5 A4 E4
    };

    [Header("Estructura de la cancion")]
    public bool useSongStructure = true;

    [Range(1, 8)] public int sectionBlocks = 4;

    [Header("Intro")]
    [Range(0f, 10f)] public float introSeconds = 2.5f;
    [Range(0f, 2f)] public float introVolume = 1.2f;
    [Range(0f, 2f)] public float introReverb = 0.4f;

    [Header("Forma de onda")]
    [Range(0, 3)] public int wavetableIndex = 0;
    [Range(1, 10)] public int harmonicCount = 6;
    [Range(0f, 1f)]
    public float[] harmonicLevels =
    {
        1f, 0.5f, 0.25f, 0.15f, 0.1f,
        0.08f, 0.05f, 0.04f, 0.03f, 0.02f
    };

    [Header("ADSR")]
    [Range(0.001f, 1f)] public float attack = 0.02f;
    [Range(0f, 1f)] public float decay = 0.20f;
    [Range(0f, 1f)] public float sustain = 0.45f;
    [Range(0f, 1f)] public float release = 0.25f;

    [Header("Vibrato / Tremolo / FM")]
    [Range(0f, 10f)] public float vibratoRate = 5f;
    [Range(0f, 20f)] public float vibratoDepth = 1.0f;
    [Range(0f, 10f)] public float tremoloRate = 2f;
    [Range(0f, 1f)] public float tremoloDepth = 0.10f;
    [Range(0f, 10f)] public float fmRate = 3f;
    [Range(0f, 500f)] public float fmDepth = 1.5f;

    [Header("Mezcla de capas")]
    [Range(0f, 1f)] public float melodyVolume = 0.25f;
    [Range(0f, 1f)] public float arpVolume = 0.12f;
    [Range(0f, 1f)] public float pulseBassVolume = 0.18f;
    [Range(0f, 1f)] public float sparkleVolume = 0.05f;

    [Header("Detalles de capas")]
    [Range(1, 4)] public int arpNotesPerBeat = 2;
    [Range(2, 16)] public int beatsBetweenSparkles = 4;
    [Range(0.1f, 2f)] public float sparkleDuration = 0.7f;

    [Header("Percusion")]
    [Range(0f, 1f)] public float kickVolume = 0.30f;
    [Range(0f, 1f)] public float hatVolume = 0.035f;

    private struct Plan
    {
        public float melody, bass, arp, sparkle, kick, hat;
        public int arpMul, hatMul;
        public float arpPitch;
        public float sparklePitch;
        public float sparkleEvery;
        public bool variation;

        public Plan(float melody, float bass, float arp, float sparkle, float kick, float hat,
                    int arpMul, int hatMul, float arpPitch, float sparklePitch,
                    float sparkleEvery, bool variation)
        {
            this.melody = melody; this.bass = bass; this.arp = arp;
            this.sparkle = sparkle; this.kick = kick; this.hat = hat;
            this.arpMul = arpMul; this.hatMul = hatMul;
            this.arpPitch = arpPitch; this.sparklePitch = sparklePitch;
            this.sparkleEvery = sparkleEvery; this.variation = variation;
        }
    }

    private static readonly Plan[] Plans =
    {
        new Plan(1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1.0f, 1, 1, 1.5f, 2f, 1.0f, false), 
        new Plan(1.0f, 1.0f, 1.0f, 0.8f, 1.0f, 1.0f, 2, 2, 1.5f, 2f, 1.0f, true ), 
        new Plan(0.8f, 0.6f, 0.8f, 1.0f, 0.0f, 0.0f, 1, 1, 1.5f, 2f, 1.0f, false),
        new Plan(0.8f, 1.0f, 1.0f, 0.5f, 1.0f, 1.0f, 2, 2, 1.5f, 2f, 1.0f, true ), 
        new Plan(1.0f, 1.0f, 1.2f, 2.5f, 1.0f, 1.0f, 2, 2, 2.0f, 4f, 0.5f, true )  
    };

    private static readonly float[][] Chords =
    {
        new[] { 220.00f, 261.63f, 329.63f, 440.00f }, 
        new[] { 174.61f, 220.00f, 261.63f, 349.23f }, 
        new[] { 196.00f, 261.63f, 329.63f, 392.00f }, 
        new[] { 196.00f, 246.94f, 293.66f, 392.00f }  
    };
    private static readonly float[] BassRoots = { 55.00f, 43.65f, 65.41f, 49.00f };
    private static readonly int[] ArpPattern = { 0, 1, 2, 3, 2, 1, 3, 2 };

    // Intro
    private const float IntroTempo = 140f;     
    private const int IntroNotesPerBeat = 4;  
    private const double IntroGridStart = -0.0741; 
    private const float IntroBaseFreq = 261.63f; 
    private const float IntroScale = 1.5f / 4000f; 
    private const float IntroFlutterRate = 28f; 
    private const float IntroFlutterDepth = 0.10f;
    private const double IntroFadeOut = 0.40; 
    private const double IntroTail = 8.0; 

    private static readonly byte[][] IntroLevels =
    {
        new byte[] { 10, 62, 32, 20, 30, 64, 87, 100, 88, 47, 21, 16, 32, 59, 59, 40, 20, 7, 3, 9, 26, 49, 50, 42, 32, 17, 9, 10, 22, 39, 40, 33, 23, 11, 5, 6, 13, 20, 18, 12 },   // armonico 1
        new byte[] { 18, 97, 37, 20, 17, 28, 55, 66, 64, 74, 38, 22, 12, 10, 24, 39, 50, 61, 33, 21, 17, 25, 44, 45, 39, 39, 17, 11, 8, 10, 17, 17, 14, 13, 5, 3, 3, 4, 6, 7 },   // armonico 2
        new byte[] { 22, 39, 53, 39, 22, 10, 19, 61, 118, 124, 125, 69, 34, 21, 8, 14, 18, 15, 21, 16, 8, 5, 6, 11, 15, 14, 15, 9, 5, 3, 3, 4, 5, 5, 5, 3, 1, 1, 0, 0 },   // armonico 3
        new byte[] { 32, 78, 98, 122, 92, 31, 18, 12, 16, 35, 26, 31, 41, 21, 6, 7, 6, 3, 2, 6, 5, 2, 1, 1, 1, 2, 2, 1, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0 },   // armonico 4
        new byte[] { 20, 35, 45, 36, 58, 48, 11, 8, 12, 13, 10, 15, 17, 10, 3, 2, 1, 2, 3, 2, 2, 2, 1, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },   // armonico 5
        new byte[] { 8, 18, 30, 39, 25, 12, 14, 8, 3, 3, 3, 4, 4, 3, 3, 1, 0, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },   // armonico 6
        new byte[] { 6, 20, 18, 15, 10, 7, 6, 10, 4, 4, 3, 2, 2, 1, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },   // armonico 7
        new byte[] { 4, 28, 6, 10, 4, 1, 2, 3, 3, 6, 3, 1, 1, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },   // armonico 8
    };
    private const int IntroSteps = 40;

    private static readonly float[] IntroGate =
        { 1.09f, 1.46f, 1.73f, 1.71f, 1.40f, 1.07f, 1.02f, 1.07f, 0.87f, 0.49f, 0.37f, 0.40f, 0.58f, 0.88f, 0.95f, 0.91f };

    private static readonly int[] CombBase = { 1116, 1188, 1277, 1356 };
    private static readonly int[] AllpassBase = { 556, 441 };
    private const float CombFeedback = 0.90f;
    private const float CombDamp = 0.3f;

    private const int BeatsPerChord = 8;
    private const int TableSize = 2048;
    private const int TableMask = TableSize - 1;
    private const double TwoPi = 2.0 * System.Math.PI;

    // Estado
    private float sampleRate;
    private double elapsedTime; 
    private double beatPos; 
    private double introElapsed; 
    private bool introWasActive;

    private double carrierPhase, bassPhase, sparklePhase, arpPhase, kickPhase;
    private uint noiseState = 22222;

    private float gMelody, gBass, gArp, gSparkle, gKick, gHat;

    // Reverb de la intro
    private float[][] combBuf, apBuf;
    private float[] combFilt;
    private int[] combIdx, apIdx;

    private int loadedWavetableIndex = -1;
    private float[] harmonicCos = new float[10];
    private float[] harmonicSin = new float[10];

    private volatile float[] waveTable;
    private int wavetableVersion;
    private int lastBuildHash = int.MinValue;

    private void Start()
    {
        sampleRate = AudioSettings.outputSampleRate;
        InitReverb();

        AudioSource source = GetComponent<AudioSource>();
        source.clip = null;
        source.loop = false;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = 1f;

        if (wavetableLoader == null)
            wavetableLoader = GetComponent<WavetableTxtLoader>();

        if (wavetableLoader != null)
        {
            wavetableLoader.LoadWavetable(wavetableIndex);
            AnalyzeWavetable();
        }
        else
        {
            harmonicSin[0] = 1f;
            wavetableVersion++;
        }

        loadedWavetableIndex = wavetableIndex;
        RebuildTableIfNeeded();
        source.Play();
    }

    private void InitReverb()
    {
        float scale = sampleRate / 44100f;

        combBuf = new float[CombBase.Length][];
        combIdx = new int[CombBase.Length];
        combFilt = new float[CombBase.Length];
        for (int c = 0; c < CombBase.Length; c++)
            combBuf[c] = new float[Mathf.Max(8, (int)(CombBase[c] * scale))];

        apBuf = new float[AllpassBase.Length][];
        apIdx = new int[AllpassBase.Length];
        for (int a = 0; a < AllpassBase.Length; a++)
            apBuf[a] = new float[Mathf.Max(8, (int)(AllpassBase[a] * scale))];
    }

    private float ProcessReverb(float input)
    {
        float x = input * 0.15f;
        float sum = 0f;

        for (int c = 0; c < combBuf.Length; c++)
        {
            float[] buf = combBuf[c];
            int idx = combIdx[c];
            float y = buf[idx];

            combFilt[c] = y * (1f - CombDamp) + combFilt[c] * CombDamp;
            buf[idx] = x + combFilt[c] * CombFeedback;

            if (++idx >= buf.Length) idx = 0;
            combIdx[c] = idx;
            sum += y;
        }

        sum *= 0.25f;

        for (int a = 0; a < apBuf.Length; a++)
        {
            float[] buf = apBuf[a];
            int idx = apIdx[a];
            float b = buf[idx];
            float o = -sum + b;
            buf[idx] = sum + b * 0.5f;

            if (++idx >= buf.Length) idx = 0;
            apIdx[a] = idx;
            sum = o;
        }

        return sum;
    }

    private void Update()
    {
        if (wavetableLoader != null && wavetableIndex != loadedWavetableIndex)
        {
            wavetableLoader.LoadWavetable(wavetableIndex);
            AnalyzeWavetable();
            loadedWavetableIndex = wavetableIndex;
        }

        RebuildTableIfNeeded();
    }

    private void AnalyzeWavetable()
    {
        float[] samples = wavetableLoader.samples;
        if (samples == null || samples.Length < 2) return;

        int count = samples.Length;
        int maxHarmonics = Mathf.Min(10, count / 2);

        float[] newCos = new float[10];
        float[] newSin = new float[10];

        for (int h = 1; h <= maxHarmonics; h++)
        {
            double a = 0.0, b = 0.0;

            for (int n = 0; n < count; n++)
            {
                double angle = TwoPi * h * n / count;
                a += samples[n] * System.Math.Cos(angle);
                b += samples[n] * System.Math.Sin(angle);
            }

            newCos[h - 1] = (float)(2.0 * a / count);
            newSin[h - 1] = (float)(2.0 * b / count);
        }

        harmonicCos = newCos;
        harmonicSin = newSin;
        wavetableVersion++;
    }

    private void RebuildTableIfNeeded()
    {
        int hash = wavetableVersion * 7919 + harmonicCount;
        if (harmonicLevels != null)
            for (int i = 0; i < harmonicLevels.Length; i++)
                hash = hash * 31 + (int)(harmonicLevels[i] * 1000f);

        if (hash == lastBuildHash && waveTable != null) return;
        lastBuildHash = hash;

        int count = Mathf.Clamp(harmonicCount, 1, 10);
        float[] t = new float[TableSize];
        float peak = 0f;

        for (int i = 0; i < TableSize; i++)
        {
            double theta = TwoPi * i / TableSize;
            double s = 0.0;

            for (int h = 0; h < count; h++)
            {
                float level = harmonicLevels != null && h < harmonicLevels.Length
                    ? Mathf.Clamp01(harmonicLevels[h]) : 1f;
                double a = theta * (h + 1);
                s += (harmonicCos[h] * System.Math.Cos(a) +
                      harmonicSin[h] * System.Math.Sin(a)) * level;
            }

            t[i] = (float)s;
            peak = Mathf.Max(peak, Mathf.Abs(t[i]));
        }

        if (peak < 1e-4f)
        {
            for (int i = 0; i < TableSize; i++)
                t[i] = (float)System.Math.Sin(TwoPi * i / TableSize);
        }
        else
        {
            float inv = 1f / peak;
            for (int i = 0; i < TableSize; i++) t[i] *= inv;
        }

        waveTable = t;
    }

    private static float Osc(double phase, float[] table)
    {
        if (table == null) return (float)System.Math.Sin(phase);

        double pos = phase * (TableSize / TwoPi);
        int i0 = (int)pos;
        float frac = (float)(pos - i0);
        i0 &= TableMask;
        return table[i0] * (1f - frac) + table[(i0 + 1) & TableMask] * frac;
    }

    private static float Adsr(double t, double dur, float a, float d, float s, float r)
    {
        if (t < 0.0 || t >= dur) return 0f;

        double total = a + d + r;
        double k = total > dur ? dur / total : 1.0;
        double aa = a * k, dd = d * k, rr = r * k;

        if (t < aa) return (float)(t / aa);
        t -= aa;

        if (t < dd) return Mathf.Lerp(1f, s, (float)(t / dd));
        t -= dd;

        double sustainLen = dur - aa - dd - rr;
        if (t < sustainLen) return s;
        t -= sustainLen;

        return rr > 0.0 ? s * (1f - (float)(t / rr)) : 0f;
    }

    private float Noise()
    {
        noiseState ^= noiseState << 13;
        noiseState ^= noiseState >> 17;
        noiseState ^= noiseState << 5;
        return (noiseState / (float)uint.MaxValue) * 2f - 1f;
    }

    private void OnAudioFilterRead(float[] data, int channels)
    {
        if (sampleRate <= 0f || channels <= 0 || combBuf == null) return;

        float[] baseNotes = notes;
        float[] varNotes = notesVariation;
        bool hasBase = baseNotes != null && baseNotes.Length > 0;
        bool hasVar = varNotes != null && varNotes.Length > 0;
        if (!hasBase && !hasVar)
        {
            System.Array.Clear(data, 0, data.Length);
            return;
        }

        float[] table = waveTable;

        double beatDuration = 60.0 / Mathf.Clamp(tempo, 60f, 130f);
        double dBeat = 1.0 / (sampleRate * beatDuration);
        double dt = 1.0 / sampleRate;
        double noteBeats = Mathf.Max(1, beatsPerNote);
        double noteDuration = noteBeats * beatDuration;
        double sparkleDur = Mathf.Max(0.1f, sparkleDuration);

        bool structure = useSongStructure;
        double introSecs = structure ? Mathf.Max(0f, introSeconds) : 0.0;
        double sectionBeats = Mathf.Max(1, sectionBlocks) * (double)BeatsPerChord;

        float introVol = Mathf.Clamp(introVolume, 0f, 2f);
        float introRev = Mathf.Max(0f, introReverb);

        float A = Mathf.Max(0.001f, attack);
        float D = Mathf.Max(0f, decay);
        float S = Mathf.Clamp01(sustain);
        float R = Mathf.Max(0f, release);

        float vibRate = Mathf.Max(0f, vibratoRate);
        float vibDepth = Mathf.Max(0f, vibratoDepth);
        float tremRate = Mathf.Max(0f, tremoloRate);
        float tremDepth = Mathf.Clamp01(tremoloDepth);
        float fmR = Mathf.Max(0f, fmRate);
        float fmD = Mathf.Max(0f, fmDepth);

        float k = 1f - Mathf.Exp(-1f / (sampleRate * 0.12f));

        for (int i = 0; i < data.Length; i += channels)
        {
            // Intro
            bool inIntro = structure && introElapsed < introSecs;

            double mb = beatPos;
            int sectionAbs = (int)(mb / sectionBeats);
            int section = structure ? sectionAbs % Plans.Length : 0;
            double sectionBeat = mb - sectionAbs * sectionBeats;
            Plan plan = Plans[section];

            int chordIndex = ((int)(mb / BeatsPerChord)) & 3;
            float[] chord = Chords[chordIndex];

            double beatTime = (beatPos - System.Math.Floor(beatPos)) * beatDuration;

            // Durante la intro no suena nada de la composicion
            float tMelody = inIntro ? 0f : plan.melody;
            float tBass = inIntro ? 0f : plan.bass;
            float tArp = inIntro ? 0f : plan.arp;
            float tSparkle = inIntro ? 0f : plan.sparkle;
            float tKick = inIntro ? 0f : plan.kick;
            float tHat = inIntro ? 0f : plan.hat;

            if (introWasActive && !inIntro)
            {
                // Al terminar la intro la composicion entra de golpe, justo en el pulso 0
                gMelody = tMelody; gBass = tBass; gArp = tArp;
                gSparkle = tSparkle; gKick = tKick; gHat = tHat;
            }
            else
            {
                gMelody += (tMelody - gMelody) * k;
                gBass += (tBass - gBass) * k;
                gArp += (tArp - gArp) * k;
                gSparkle += (tSparkle - gSparkle) * k;
                gKick += (tKick - gKick) * k;
                gHat += (tHat - gHat) * k;
            }
            introWasActive = inIntro;

            // Modulacion global
            float vibrato = (float)System.Math.Sin(TwoPi * vibRate * elapsedTime) * vibDepth;
            float fm = (float)System.Math.Sin(TwoPi * fmR * elapsedTime) * fmD;
            float pitchMul = Mathf.Max(0.01f, 1f + (vibrato + fm) / 440f);

            float tremolo = 1f - tremDepth * (0.5f + 0.5f *
                (float)System.Math.Sin(TwoPi * tremRate * elapsedTime));

            // Melodia 
            float[] phrase = (plan.variation && hasVar) || !hasBase ? varNotes : baseNotes;
            double noteFloor = System.Math.Floor(sectionBeat / noteBeats);
            int noteIndex = (int)noteFloor % phrase.Length;
            double noteTime = (sectionBeat - noteFloor * noteBeats) * beatDuration;

            float melodyFreq = Mathf.Max(1f, phrase[noteIndex]);
            float melody = Osc(carrierPhase, table) * melodyVolume *
                           Adsr(noteTime, noteDuration, A, D, S, R);

            // Bajo 
            float bass = Osc(bassPhase, table) * pulseBassVolume *
                         Adsr(beatTime, beatDuration, A, D, S, R);

            // Arpegio
            int arpPerBeat = Mathf.Clamp(arpNotesPerBeat * plan.arpMul, 1, 8);
            double arpPos = mb * arpPerBeat;
            int arpStep = (int)arpPos;
            double arpStepDur = beatDuration / arpPerBeat;
            double arpStepTime = (arpPos - arpStep) * arpStepDur;
            float arpFreq = chord[ArpPattern[arpStep % ArpPattern.Length]] * plan.arpPitch;
            float arp = Osc(arpPhase, table) * arpVolume *
                        Adsr(arpStepTime, arpStepDur, A, D, S, R);
            arpPhase += TwoPi * arpFreq * pitchMul / sampleRate;
            if (arpPhase > TwoPi) arpPhase -= TwoPi;

            // Destello
            double sparkleBeats = System.Math.Max(1.0, beatsBetweenSparkles * plan.sparkleEvery);
            double sparkleTime = (mb % sparkleBeats) * beatDuration;
            float sparkleFreq = Mathf.Max(1f, phrase[noteIndex] * plan.sparklePitch);
            float sparkle = Osc(sparklePhase, table) * sparkleVolume *
                            Adsr(sparkleTime, sparkleDur, A, D, S, R);

            float tonal = (melody * gMelody + bass * gBass + arp * gArp +
                           sparkle * gSparkle) * tremolo;

            // Percusión
            float kickFreq = 45f + 90f * Mathf.Exp(-(float)beatTime * 35f);
            float kickEnv = Mathf.Exp(-(float)beatTime * 9f);
            float kick = (float)System.Math.Sin(kickPhase) * kickEnv * kickVolume * gKick;
            kickPhase += TwoPi * kickFreq / sampleRate;
            if (kickPhase > TwoPi) kickPhase -= TwoPi;

            int hatRate = Mathf.Max(1, plan.hatMul);
            double hatPos = hatRate == 1
                ? (beatPos + 0.5) - System.Math.Floor(beatPos + 0.5)
                : (beatPos * hatRate) - System.Math.Floor(beatPos * hatRate);
            double hatTime = hatPos * beatDuration / hatRate;
            float hat = Noise() * Mathf.Exp(-(float)hatTime * 60f) * hatVolume * gHat;

            // Intro
            float introOut = 0f;
            if (structure && introElapsed < introSecs + IntroTail)
            {
                double ti = introElapsed;
                float bells = 0f;

                if (ti < introSecs)
                {
                    double stepDur = 60.0 / IntroTempo / IntroNotesPerBeat;
                    double pos = (ti - IntroGridStart) / stepDur;  
                    double cen = pos - 0.5;

                    // Niveles
                    int i0 = (int)System.Math.Floor(cen);
                    float fr = (float)(cen - i0);
                    int ia = Mathf.Clamp(i0, 0, IntroSteps - 1);
                    int ib = Mathf.Clamp(i0 + 1, 0, IntroSteps - 1);

                    // Pulso dentro del paso
                    int gl = IntroGate.Length;
                    double gp = (pos - System.Math.Floor(pos)) * gl - 0.5;
                    int gi = (int)System.Math.Floor(gp);
                    float gw = (float)(gp - gi);
                    float gate = Mathf.Lerp(IntroGate[(gi + gl) % gl], IntroGate[(gi + 1 + gl) % gl], gw);

                    float flutter = 1f + IntroFlutterDepth *
                        (float)System.Math.Sin(TwoPi * IntroFlutterRate * ti);

                    float sum = 0f;
                    for (int h = 0; h < IntroLevels.Length; h++)
                    {
                        float level = Mathf.Lerp(IntroLevels[h][ia], IntroLevels[h][ib], fr) * IntroScale;
                        if (level <= 0f) continue;
                        sum += level * (float)System.Math.Sin(TwoPi * IntroBaseFreq * (h + 1) * ti);
                    }

                    float fade = Mathf.Clamp01((float)((introSecs - ti) / IntroFadeOut));
                    bells = sum * gate * flutter * fade * introVol;
                }

                introOut = bells + ProcessReverb(bells) * introRev;
            }

            // Mezcla
            float output = (float)System.Math.Tanh(
                (tonal + kick + hat + introOut) * 1.5f);

            for (int c = 0; c < channels; c++)
                data[i + c] = output;

            // Avance de fases y tiempo
            carrierPhase += TwoPi * melodyFreq * pitchMul / sampleRate;
            if (carrierPhase > TwoPi) carrierPhase -= TwoPi;

            bassPhase += TwoPi * BassRoots[chordIndex] * pitchMul / sampleRate;
            if (bassPhase > TwoPi) bassPhase -= TwoPi;

            sparklePhase += TwoPi * sparkleFreq * pitchMul / sampleRate;
            if (sparklePhase > TwoPi) sparklePhase -= TwoPi;

            if (introElapsed < introSecs + IntroTail + 1.0) introElapsed += dt;
            if (!inIntro) beatPos += dBeat;
            elapsedTime += dt;
        }
    }
}