using UnityEngine;
using CSCore.CoreAudioAPI;
using CSCore.SoundOut;
using CSCore;
using CSCore.Codecs;
using UnityEditor.Rendering.Universal;
using System;
using System.Linq;
using CSCore.DSP;
using CSCore.Utils;
using CSCore.Streams.SampleConverter;
using NUnit.Framework;
using System.Collections;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;



public class CSCoreTest : MonoBehaviour
{
    public static bool isIREnabled = false;
    public static float[] IR0 = new float[AudioRayTracer.IRLength];
    public static float[] IR1 = new float[AudioRayTracer.IRLength];
    public static float[] IR2 = new float[AudioRayTracer.IRLength];
    public static float[] IR3 = new float[AudioRayTracer.IRLength];
    public static Complex[][] IR0FFTLeft;
    public static Complex[][] IR0FFTRight;
    public const int IRChunkSize = 2048*2;

    public void SetIRFromHistogram(float[][] IRHistogramLeft, float[][] IRHistogramRight)
    {
        Array.Copy(IRHistogramLeft[0], IR0, IR0.Length);
        Array.Copy(IRHistogramLeft[1], IR1, IR1.Length);
        Array.Copy(IRHistogramLeft[2], IR2, IR2.Length);
        Array.Copy(IRHistogramLeft[3], IR3, IR3.Length);

        {
            // bands 0 and 1
            LowpassFilter lpf0 = new LowpassFilter(44100, 350);
            lpf0.Q = 2;
            HighpassFilter hpf1 = new HighpassFilter(44100, 350);
            hpf1.Q = 2;
            lpf0.Process(IR0);
            hpf1.Process(IR1);
            for (int i = 0; i < IR0.Length; i++)
            {
                IR0[i] = IR0[i] * 0.5f + IR1[i] * 0.5f;
            }
            // bands 2 and 3
            LowpassFilter lpf2 = new LowpassFilter(44100, 2500);
            lpf2.Q = 2;
            HighpassFilter hpf3 = new HighpassFilter(44100, 2500);
            hpf3.Q = 2;
            lpf2.Process(IR2);
            hpf3.Process(IR3);
            for (int i = 0; i < IR2.Length; i++)
            {
                IR2[i] = IR2[i] * 0.5f + IR3[i] * 0.5f;
            }

            // combine (0,1) and (2,3)
            LowpassFilter lpf01 = new LowpassFilter(44100, 750);
            lpf01.Q = 2;
            HighpassFilter hpf23 = new HighpassFilter(44100, 750);
            hpf23.Q = 2;
            lpf01.Process(IR0);
            hpf23.Process(IR2);
            for (int i = 0; i < IR0.Length; i++)
            {
                IR0[i] = IR0[i] * 0.5f + IR2[i] * 0.5f;
            }

            IR0FFTLeft = ConvertIRToFFTByChunks(IR0, 4410);
        }

        Array.Copy(IRHistogramRight[0], IR0, IR0.Length);
        Array.Copy(IRHistogramRight[1], IR1, IR1.Length);
        Array.Copy(IRHistogramRight[2], IR2, IR2.Length);
        Array.Copy(IRHistogramRight[3], IR3, IR3.Length);

        {
            // bands 0 and 1
            LowpassFilter lpf0 = new LowpassFilter(44100, 350);
            lpf0.Q = 2;
            HighpassFilter hpf1 = new HighpassFilter(44100, 350);
            hpf1.Q = 2;
            lpf0.Process(IR0);
            hpf1.Process(IR1);
            for (int i = 0; i < IR0.Length; i++)
            {
                IR0[i] = IR0[i] * 0.5f + IR1[i] * 0.5f;
            }
            // bands 2 and 3
            LowpassFilter lpf2 = new LowpassFilter(44100, 2500);
            lpf2.Q = 2;
            HighpassFilter hpf3 = new HighpassFilter(44100, 2500);
            hpf3.Q = 2;
            lpf2.Process(IR2);
            hpf3.Process(IR3);
            for (int i = 0; i < IR2.Length; i++)
            {
                IR2[i] = IR2[i] * 0.5f + IR3[i] * 0.5f;
            }

            // combine (0,1) and (2,3)
            LowpassFilter lpf01 = new LowpassFilter(44100, 750);
            lpf01.Q = 2;
            HighpassFilter hpf23 = new HighpassFilter(44100, 750);
            hpf23.Q = 2;
            lpf01.Process(IR0);
            hpf23.Process(IR2);
            for (int i = 0; i < IR0.Length; i++)
            {
                IR0[i] = IR0[i] * 0.5f + IR2[i] * 0.5f;
            }
            IR0FFTRight = ConvertIRToFFTByChunks(IR0, 4410);
        }
    }
    public static Complex[] GetFFTFromSignal(float[] signal, int exponent)
    {
        int n = (int)Math.Pow(2, exponent);
        Complex[] complexSignal = new Complex[n];
        for (int i = 0; i < signal.Length; i++)
        {
            complexSignal[i].Real = signal[i];
        }
        FastFourierTransformation.Fft(complexSignal, exponent, FftMode.Forward);
        return complexSignal;
    }

    public static Complex[][] ConvertIRToFFTByChunks(float[] impulse, int signalSize)
    {
        int M = IRChunkSize;             // chunk length
        int L = signalSize;             // block size for convolution
        int N = 1;
        int exponent = 0;

        // Correct FFT size: convolution of M and L
        while (N < M + L - 1)
        {
            N <<= 1;
            exponent++;
        }

        int chunkCount = (impulse.Length + M - 1) / M;
        Complex[][] chunkFFTs = new Complex[chunkCount][];

        float[] buffer = new float[N];     // full FFT input buffer (N size)

        for (int i = 0; i < chunkCount; i++)
        {
            Array.Clear(buffer, 0, buffer.Length);

            int copyCount = Math.Min(M, impulse.Length - i * M);
            Array.Copy(impulse, i * M, buffer, 0, copyCount);

            chunkFFTs[i] = GetFFTFromSignal(buffer, exponent);
        }

        return chunkFFTs;
    }

    public static float[] ConvolveByChunks(float[] signal, float[] impulse, out float counter)
    {
        int n = 1;
        int exponent = 0;
        while (n <= signal.Length + impulse.Length - 1)
        {
            n <<= 1;
            exponent++;
        }
        int chunkLength = signal.Length;
        float[] subImpulse = new float[chunkLength];
        float[] result = new float[n];
        float totalCounter = 0;
        for (int i = 0; i < Math.Ceiling(impulse.Length / (float)chunkLength); i++)
        {
            Array.Clear(subImpulse, 0, subImpulse.Length);
            Array.Copy(impulse, chunkLength * i, subImpulse, 0, Math.Min(chunkLength, impulse.Length - i*chunkLength));
            //Debug.Log(Math.Min(chunkLength, impulse.Length - i * chunkLength));
            float tempCounter = 0;
            float[] subResult = Convolve(signal, subImpulse, out tempCounter);
            totalCounter += tempCounter;
            for (int j = 0; j < subResult.Length; j++)
            {
                if (i * chunkLength + j < result.Length)
                {
                    result[i * chunkLength + j] += subResult[j];
                }
            }
        }
        counter = totalCounter;
        return result;
    }

    public static float[] ConvolveByChunks(float[] signal, Complex[][] impulseChunksFFTs, int fullImpulseLength, out float counter)
    {
        int n = 1;
        int exponent = 0;
        while (n < signal.Length + fullImpulseLength - 1)
        {
            n <<= 1;
            exponent++;
        }
        //Debug.Log("n:" + n);
        float[] result = new float[n];
        if (impulseChunksFFTs == null)
        {
            //Debug.Log("impulseChunksFFTs == null");
            counter = 0;
            return result;
        }
        int chunkLength = IRChunkSize;
        //Debug.Log("impuselChunksFFTs[0].Length: " + chunkLength);
        float totalCounter = 0;
        if (impulseChunksFFTs == null)
        {
            counter = 0;
            return result;
        }
        for (int i = 0; i < impulseChunksFFTs.Length; i++)
        {
            float tempCounter = 0;
            float[] subResult = Convolve(signal, impulseChunksFFTs[i], out tempCounter);
            //Debug.Log("subResult.Length: " + subResult.Length);
            totalCounter += tempCounter;
            //Debug.Log("Adding from " + i * chunkLength + " to " + (i * chunkLength + subResult.Length));
            for (int j = 0; j < subResult.Length; j++)
            {
                result[i * chunkLength + j] += subResult[j];
            }
        }
        counter = totalCounter;
        return result;
    }
    public static float[] Convolve(float[] signal, float[] impulse, out float counter)
    {
        // Choose FFT size as next power of two of combined length
        int n = 1;
        int exponent = 0;
        while (n <= signal.Length + impulse.Length - 1)
        {
            n <<= 1;
            exponent++;
        }

        var fftSignal = new Complex[n];
        var fftImpulse = new Complex[n];
        var fftResult = new Complex[n];

        // Copy data into complex buffers
        for (int i = 0; i < signal.Length; i++)
        {
            fftSignal[i].Real = signal[i];
        }
        float largestImpulse = 0;
        for (int i = 0; i < impulse.Length; i++)
        {
            fftImpulse[i].Real = impulse[i];
            /*if (largestImpulse < impulse[i])
            {
                largestImpulse = fftImpulse[i].Real;
            }*/
        }
        counter = largestImpulse;

        // Forward FFT
        FastFourierTransformation.Fft(fftSignal, exponent, FftMode.Forward);
        FastFourierTransformation.Fft(fftImpulse, exponent, FftMode.Forward);

        // Multiply spectra
        for (int i = 0; i < n; i++)
        {
            float real = fftSignal[i].Real * fftImpulse[i].Real - fftSignal[i].Imaginary * fftImpulse[i].Imaginary;
            float imag = fftSignal[i].Real * fftImpulse[i].Imaginary + fftSignal[i].Imaginary * fftImpulse[i].Real;
            fftResult[i].Real = real;
            fftResult[i].Imaginary = imag;
            
        }

        // Inverse FFT
        FastFourierTransformation.Fft(fftResult, exponent, FftMode.Backward);
        //Debug.Log(n);
        // Extract real part and normalize
        float[] result = new float[n];
        for (int i = 0; i < n; i++)
        {
            result[i] = fftResult[i].Real * n;
        }

        // Trim to linear convolution length
        Array.Resize(ref result, signal.Length + impulse.Length - 1);

        return result;
    }

    public static float[] Convolve(float[] signal, Complex[] impulse, out float counter)
    {
        // Choose FFT size as next power of two of combined length
        int n = impulse.Length;
        int exponent = ((int)(Math.Log(impulse.Length) / Math.Log(2)));

        var fftSignal = new Complex[n];
        var fftImpulse = impulse;
        var fftResult = new Complex[n];

        // Copy data into complex buffers
        for (int i = 0; i < signal.Length; i++)
        {
            fftSignal[i].Real = signal[i];
        }
        float largestImpulse = 0;
        for (int i = 0; i < impulse.Length; i++)
        {
            if (largestImpulse < impulse[i])
            {
                largestImpulse = fftImpulse[i].Real;
            }
        }
        counter = largestImpulse;

        // Forward FFT
        FastFourierTransformation.Fft(fftSignal, exponent, FftMode.Forward);

        // Multiply spectra
        for (int i = 0; i < n; i++)
        {
            float real = fftSignal[i].Real * fftImpulse[i].Real - fftSignal[i].Imaginary * fftImpulse[i].Imaginary;
            float imag = fftSignal[i].Real * fftImpulse[i].Imaginary + fftSignal[i].Imaginary * fftImpulse[i].Real;
            fftResult[i].Real = real;
            fftResult[i].Imaginary = imag;

        }

        // Inverse FFT
        FastFourierTransformation.Fft(fftResult, exponent, FftMode.Backward);
        //Debug.Log(n);
        // Extract real part and normalize
        float[] result = new float[n];
        for (int i = 0; i < n; i++)
        {
            result[i] = fftResult[i].Real * n;
            if (result[i] > 0)
            {
                //counter = result[i];
            }
        }

        // Trim to linear convolution length
        Array.Resize(ref result, impulse.Length);

        return result;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private ISoundOut _soundOut;
    private IWaveSource _waveSource;

    [SerializeField] private AudioClip audioClip;
    [SerializeField] private string audioFilename = "clap.wav";

    [SerializeField]
    private int usedDeviceId = 0;

    MMDevice usedDevice;


    void Start()
    {
        
        // choosing output device
        MMDeviceEnumerator deviceEnumerator = new MMDeviceEnumerator();
        MMDeviceCollection devices = deviceEnumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active);
        for(int i = 0; i < devices.Count; i++)
        {
            Debug.Log(i + ": " + devices[i].ToString());
        }
        usedDevice = devices[usedDeviceId];

        Debug.Log(Application.streamingAssetsPath);
        _waveSource = CodecFactory.Instance.GetCodec(Application.streamingAssetsPath + "/" + audioFilename)
            .ToSampleSource()
            .ToStereo()
            .ToWaveSource();
        Debug.Log(_waveSource.WaveFormat.BitsPerSample);
        Debug.Log(_waveSource.WaveFormat.Channels);
        Debug.Log(_waveSource.WaveFormat.SampleRate);
        Debug.Log(_waveSource.WaveFormat.ExtraSize);

        _soundOut = new WaveOut(100);
        ConvolveEffSource ces = new ConvolveEffSource(_waveSource.ToSampleSource(), 4800*2);
        

        IWaveSource wave = ces.ToWaveSource();
        // WaveFormat stereoFormat = new WaveFormat()
        // new WaveFormat(_source.WaveFormat.SampleRate, _source.WaveFormat.BitsPerSample

        //IWaveSource wave = _waveSource;
        if (_waveSource == null)
        {
            Debug.LogError("Wave source is null");
        }
        _soundOut.Initialize(wave);
        _soundOut.Play();

        
    }

    private void OnDestroy()
    {
        _soundOut?.Stop();
        _soundOut?.Dispose();
        _waveSource?.Dispose();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

public class ConvolveEffSource : ISampleSource
{
    private ISampleSource _source;
    private float[] _buffer;
    private float[] _overlapBuffer0;
    private float[] _overlapBuffer1;
    private float[] _ir;
    private float[] b0;
    private float[] b1;
    private int overlapAmount = 0;
    private WaveFormat finalFormat = new WaveFormat(44100, 32, 2, AudioEncoding.IeeeFloat, 0);
    private HighpassFilter hpf = new HighpassFilter(44100, 1000);

    public ConvolveEffSource(ISampleSource source, int bufferSize)
    {
        _source = source;
        _buffer = new float[bufferSize];
        _ir = new float[CSCoreTest.IR0.Length];
        _ir[0] = 1.0f;
        _overlapBuffer0 = new float[CSCoreTest.IR0.Length * 3];
        _overlapBuffer1 = new float[CSCoreTest.IR0.Length * 3];
        b0 = new float[4410];
        b1 = new float[4410];
    }
    public bool CanSeek => _source.CanSeek;

    public WaveFormat WaveFormat => finalFormat;

    public long Position { get => _source.Position; set => _source.Position = value; }

    public long Length => _source.Length;

    public void Dispose()
    {
        _source.Dispose();
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samples = _source.Read(buffer, offset, count);
        float counter = 0;
        //Debug.Log(samples);
        for (int i = 0; i < samples; i+=2)
        {
            b0[i/2] = buffer[offset + i];
            b1[i/2] = buffer[offset + i];
        }
        float[] result0;
        float[] result1;
        if (CSCoreTest.isIREnabled)
        {
            result0 = CSCoreTest.ConvolveByChunks(b0, CSCoreTest.IR0FFTLeft, AudioRayTracer.IRLength, out counter);
            result1 = CSCoreTest.ConvolveByChunks(b1, CSCoreTest.IR0FFTRight, AudioRayTracer.IRLength, out counter);
        }
        else
        {
            result0 = CSCoreTest.ConvolveByChunks(b0, _ir, out counter);
            result1 = CSCoreTest.ConvolveByChunks(b1, _ir, out counter);
        }
        //float[] result1 = CSCoreTest.ConvolveByChunks(b1, CSCoreTest.isIREnabled ? CSCoreTest.IR0 : _ir, out counter);

        overlapAmount = result0.Length - samples / 2;
        for (int i = 0; i < overlapAmount; i++)
        {
            result0[i] += _overlapBuffer0[i];
            result1[i] += _overlapBuffer1[i];
        }

        Array.Copy(result0, samples / 2, _overlapBuffer0, 0, result0.Length - samples / 2);
        Array.Copy(result1, samples / 2, _overlapBuffer1, 0, result1.Length - samples / 2);
        for (int i = 0; i < samples; i += 2)
        {
            buffer[offset + i] = result0[i / 2];
            //buffer[offset + i + 1] = result0[i / 2];
            buffer[offset + i + 1] = result1[i / 2];
        }

        return samples;
    }
}

public class ArrayWaveSource : ISampleSource
{
    private readonly float[] _data;
    private readonly WaveFormat _format;
    private int _pos = 0;

    public ArrayWaveSource(float[] data, int sampleRate)
    {
        _data = data;
        _format = new WaveFormat(sampleRate, 32, 1, AudioEncoding.IeeeFloat);
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samples = count;
        int available = Math.Min(samples, _data.Length - _pos);

        Array.Copy(_data, _pos, buffer, offset, available);
        _pos = (_pos + available) % _data.Length;

        return available;
    }

    public bool CanSeek => false;
    public WaveFormat WaveFormat => _format;
    public long Position { get => _pos; set => _pos = (int)value; }
    public long Length => _data.Length;
    public void Dispose() { }
}

public class DelayEffect : ISampleSource
{
    private float[] _delayBuffer;
    private int _delayBufferIndex = 0;
    private int _delayMs;
    private int _delaySamples;
    private ISampleSource _source;
    public DelayEffect(ISampleSource source, int delayMs = 2000)
    {
        _source = source;
        _delayMs = delayMs;
        _delaySamples = (int)((delayMs / 1000) * _source.WaveFormat.SampleRate);
        _delayBuffer = new float[_delaySamples * _source.WaveFormat.Channels];
    }
    public bool CanSeek => _source.CanSeek;

    public WaveFormat WaveFormat => _source.WaveFormat;

    public long Position { get => _source.Position; set => _source.Position = value; }

    public long Length => _source.Length;

    public void Dispose()
    {
        _source.Dispose();
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = _source.Read(buffer, offset, count);
        for (int i = 0; i < samplesRead; i++)
        {
            int delayIndex = (_delayBufferIndex + i) % _delayBuffer.Length;
            float drySample = buffer[offset + i];
            float wetSample = _delayBuffer[delayIndex];
            buffer[offset + i] = wetSample;
            _delayBuffer[delayIndex] = drySample;
        }
        _delayBufferIndex = (_delayBufferIndex + samplesRead) % _delayBuffer.Length;
        return samplesRead;
    }
}

public class EchoEffect : ISampleSource
{
    private readonly ISampleSource _source;
    private readonly float[] _delayBuffer;
    private int _bufferPos;
    private readonly int _delaySamples;
    private readonly float _decay;

    public EchoEffect(ISampleSource source, int delayMs = 500, float decay = 0.5f)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
        _decay = decay;

        // Convert delay (ms) to samples
        _delaySamples = (int)((delayMs / 1000f) * _source.WaveFormat.SampleRate);
        _delayBuffer = new float[_delaySamples * _source.WaveFormat.Channels];
        _bufferPos = 0;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = _source.Read(buffer, offset, count);

        for (int i = 0; i < samplesRead; i++)
        {
            int ch = i % _source.WaveFormat.Channels;
            int delayIndex = (_bufferPos + i) % _delayBuffer.Length;

            float delayedSample = _delayBuffer[delayIndex];
            float drySample = buffer[offset + i];
            float wetSample = drySample + delayedSample * _decay;

            buffer[offset + i] = wetSample;

            // store new sample (the wet version, or just dry if you prefer)
            _delayBuffer[delayIndex] = wetSample;
        }

        _bufferPos = (_bufferPos + samplesRead) % _delayBuffer.Length;
        return samplesRead;
    }

    public bool CanSeek => _source.CanSeek;
    public WaveFormat WaveFormat => _source.WaveFormat;
    public long Position { get => _source.Position; set => _source.Position = value; }
    public long Length => _source.Length;
    public void Dispose() => _source.Dispose();
}

public class FeedbackEchoEffect : ISampleSource
{
    private readonly ISampleSource _source;
    private readonly float[] _delayBuffer;
    private int _writePos;
    private readonly int _delaySamples;
    private readonly float _feedback;
    private readonly float _mix;
    private readonly int _channels;
    private bool _sourceEnded = false;
    private const float SilenceThreshold = 0.0001f;

    public FeedbackEchoEffect(ISampleSource source, int delayMs = 500, float feedback = 0.9f, float mix = 0.5f)
    {
        _source = source;
        _channels = source.WaveFormat.Channels;

        _delaySamples = (int)((delayMs / 1000f) * source.WaveFormat.SampleRate);
        _delayBuffer = new float[_delaySamples * _channels];
        _feedback = feedback;
        _mix = mix;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int samplesRead = 0;
        if (!_sourceEnded)
        {
            samplesRead = _source.Read(buffer, offset, count);
            if (samplesRead == 0)
                _sourceEnded = true;
        }

        // If source has ended, fill with zeros but continue to process feedback tail
        if (_sourceEnded && samplesRead == 0)
        {
            Array.Clear(buffer, offset, count);
            samplesRead = count;
        }

        bool anyNonSilent = false;

        for (int i = 0; i < samplesRead; i++)
        {
            int ch = i % _channels;
            int readIndex = (_writePos + ch) % _delayBuffer.Length;
            float delayed = _delayBuffer[readIndex];
            float dry = buffer[offset + i];
            float wet = dry + delayed * _mix;

            buffer[offset + i] = wet;

            float newVal = dry + delayed * _feedback;
            _delayBuffer[readIndex] = Math.Clamp(newVal, -1f, 1f);

            if (Math.Abs(_delayBuffer[readIndex]) > SilenceThreshold)
                anyNonSilent = true;

            if (ch == _channels - 1)
                _writePos = (_writePos + _channels) % _delayBuffer.Length;
        }

        // Stop returning samples once buffer is completely silent
        if (_sourceEnded && !anyNonSilent)
            return 0;

        return samplesRead;
    }

    public bool CanSeek => false;
    public WaveFormat WaveFormat => _source.WaveFormat;
    public long Position { get => _source.Position; set => _source.Position = value; }
    public long Length => 0; // unknown since it may extend infinitely
    public void Dispose() => _source.Dispose();
}

public class ConvolutionEffect : ISampleSource
{
    private readonly ISampleSource _source;
    private readonly float[] _impulse;
    private readonly float[] _buffer;
    private int _position = 0;

    public ConvolutionEffect(ISampleSource source, float[] impulse)
    {
        _source = source;
        _impulse = impulse;
        _buffer = new float[impulse.Length + 8192 * 16]; // adjust as needed
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int read = _source.Read(buffer, offset, count);
        if (read == 0) return 0;

        // Copy samples into buffer for convolution
        Array.Copy(buffer, offset, _buffer, _position, Math.Min(read, _buffer.Length - _position));
        if (_position + read > _buffer.Length)
        {
            Array.Copy(buffer, offset + Math.Min(read, _buffer.Length - _position), _buffer, 0, read - (_buffer.Length - _position));
        }

        for (int i = 0; i < read; i++)
        {
            float sum = 0;
            for (int j = 0; j < _impulse.Length; j++)
            {
                int idx = _position + i - j;
                if (idx >= 0 && idx < _buffer.Length)
                    sum += _buffer[idx] * _impulse[j];
            }
            buffer[offset + i] = sum;
        }

        _position += read;
        _position %= (_buffer.Length - _impulse.Length);

        return read;
    }

    public bool CanSeek => false;
    public WaveFormat WaveFormat => _source.WaveFormat;
    public long Position { get => _source.Position; set => _source.Position = value; }
    public long Length => _source.Length;
    public void Dispose() => _source.Dispose();
}