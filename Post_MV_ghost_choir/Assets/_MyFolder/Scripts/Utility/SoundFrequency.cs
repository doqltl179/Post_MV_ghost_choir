using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Sound
{
    public class SoundFrequency
    {
        /// <summary>
        /// <br/> 0 : 도
        /// <br/> 1 : 도#
        /// <br/>   :
        /// <br/>   :
        /// <br/>   :
        /// <br/>10 : 라#
        /// <br/>11 : 시
        /// </summary>
        private static readonly string[] Notes = new string[] { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        /// <summary>
        /// note index : 0 ~ 11
        /// </summary>
        private const int BasicNoteIndex = 9;

        /// <summary>
        /// octave : 0 ~ ...
        /// </summary>
        private const int BasicOctave = 4;

        private const float BasicFrequency = 440f;

        /// <summary>
        /// <br/> x = Octave Index
        /// <br/> y = Note Index
        /// </summary>
        public static float[,] SavedFrequency { get; private set; }
        public static int OctaveLength { get { return SavedFrequency.GetLength(0); } }

        /// <summary>
        /// <br/> Octave Length is 10.
        /// <br/> This max values are checked Audio Samples that length 8192.
        /// </summary>
        public static float[] OctaveMaxSamples { get; private set; } = new float[]
        {
            0f,
            0.03330114f,
            0.06950111f,
            0.08514042f,
            0.08141362f,
            0.06620358f,
            0.02566053f,
            0.01066053f,
            0f,
            0f,
        };



        #region Utility
        /// <summary>
        /// 
        /// </summary>
        public static void SaveFrequency()
        {
            int lengthX = 10;
            int lengthY = Notes.Length;
            SavedFrequency = new float[lengthX, lengthY];

            int basicCount = BasicOctave * lengthY + BasicNoteIndex;
            int currentCount = 0;

            for (int x = 0; x < lengthX; x++)
            {
                for (int y = 0; y < lengthY; y++)
                {
                    //SavedFrequency[x, y] = BasicFrequency * Mathf.Pow(2, (float)(currentCount - basicCount) / lengthY);
                    SavedFrequency[x, y] = GetFrequency(basicCount, currentCount);

                    currentCount++;
                }
            }
        }

        public static int GetOctave(float frequency)
        {
            for (int i = 0; i < OctaveLength - 1; i++)
            {
                if (SavedFrequency[i, 0] <= frequency && frequency < SavedFrequency[i + 1, 0])
                {
                    return i;
                }
            }

            return -1;
        }

        public static float GetOctaveMaxSample(int octave)
        {
            return (0 <= octave && octave < OctaveMaxSamples.Length) ? OctaveMaxSamples[octave] : 0f;
        }

        public static float GetFrequency(float[] samples, float maxFrequency)
        {
            int maxSampleIndex = 0;
            for (int i = 1; i < samples.Length; i++)
            {
                if (samples[maxSampleIndex] < samples[i])
                    maxSampleIndex = i;
            }

            return ((float)maxSampleIndex / samples.Length) * maxFrequency;
        }

        public static float GetFrequency(float[] samples, float maxFrequency, out float usedSample)
        {
            int maxSampleIndex = 0;
            for (int i = 1; i < samples.Length; i++)
            {
                if (samples[maxSampleIndex] < samples[i])
                    maxSampleIndex = i;
            }

            usedSample = samples[maxSampleIndex];

            return ((float)maxSampleIndex / samples.Length) * maxFrequency;
        }

        public static float GetFrequency(float[] samples, Vector2Int samplesIndexRange, float maxFrequency, out float usedSample)
        {
            float[] copySamples = samples[samplesIndexRange.x..samplesIndexRange.y];
            if (copySamples.Length == 0)
            {
                usedSample = 0f;

                return 0f;
            }

            int maxSampleIndex = 0;
            for (int i = 1; i < copySamples.Length; i++)
            {
                if (copySamples[maxSampleIndex] < copySamples[i])
                    maxSampleIndex = i;
            }

            usedSample = copySamples[maxSampleIndex];

            return ((float)(maxSampleIndex + samplesIndexRange.x) / samples.Length) * maxFrequency;
        }

        public static float GetNormalizedFrequency(float frequency)
        {
            int octave = GetOctave(frequency);

            return GetNormalizedFrequency(octave, frequency);
        }

        public static float GetNormalizedFrequency(int octave, float frequency)
        {
            if(0 <= octave && octave < OctaveLength)
            {
                float startFrequency = SavedFrequency[octave, 0];

                float endFrequency = 0;
                if((octave + 1) >= OctaveLength)
                {
                    int basicCount = BasicOctave * Notes.Length + BasicNoteIndex;
                    int endFrequencyCount = (octave + 1) * Notes.Length + 0;
                    endFrequency = GetFrequency(basicCount, endFrequencyCount);
                }
                else
                {
                    endFrequency = SavedFrequency[octave + 1, 0];
                }

                return Mathf.InverseLerp(startFrequency, endFrequency, frequency);
            }

            return 0;
        }
        #endregion

        private static float GetFrequency(int basicCount, int currentCount)
        {
            return BasicFrequency * Mathf.Pow(2, (float)(currentCount - basicCount) / Notes.Length);
        }
    }
}