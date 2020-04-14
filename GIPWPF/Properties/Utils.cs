using System;
using System.Collections.Generic;

namespace GIP.Utils {

	public static class Extensions {
		public static T Clamp<T> (this T val, T min, T max) where T : IComparable<T> {
			if (val.CompareTo (min) < 0)
				return min;
			else if (val.CompareTo (max) > 0)
				return max;
			else
				return val;
		}

	}

	public static class Meth {
		public static double BellCurve (double value) {
			return 1.0 / Math.Sqrt (2.0 * Math.PI) * Math.Exp (-(value * value / 2.0));
		}
	}

	class Song {

		public double[] samples;
		public int channels;
		public int sampleRate;
		public double SampleLength { get { return 1.0 / sampleRate; } }

		public Song () {
			samples = new double[0];
			channels = 1;
			sampleRate = 44100;
		}

		public Song (double[] samples, int channels, int sampleRate) {
			this.samples = samples;
			this.channels = channels;
			this.sampleRate = sampleRate;
		}

		public Song ToMono (bool average = true) {
			if (channels == 1)
				return this;
			// Make a new list of samples
			double[] newSamples = new double[samples.LongLength / channels];
			// Get the average of every channel for this sample
			for (long i = 0; i < newSamples.LongLength; i += channels) {
				if (!average) {
					newSamples[i] = samples[i * channels];
					continue;
				}
				// Get the average
				double tot = 0;
				for (int j = 0; j < channels; j++) {
					tot += samples[i * channels + j];
				}
				tot /= channels;
				newSamples[i] = tot;
			}
			return new Song (newSamples, 1, sampleRate);
		}

		public Song LevelAudio () {
			double scale = 1.0 / Analysis.AverageAmplitude (samples, 0, samples.LongLength);
			double[] newSamples = new double[samples.LongLength];
			// Adjust volume and clamp to bounds
			for (int i = 0; i < samples.LongLength; i++) {
				newSamples[i] = (samples[i] * scale).Clamp (-1, 1);
			}
			return new Song (newSamples, channels, sampleRate);
		}
	}

	public static class Analysis {

		public static double AverageAmplitude (double[] samples, long start, long count) {
			if (count == 0)
				count = samples.LongLength - start;
			double total = 0;
			for (long i = start; i < start + count; i++) {
				total += Math.Abs (samples[i]);
			}
			total /= count;
			return total * Math.Sqrt(2);
		}

	}

	public static class Debugging {

		public static void LogArray<T> (T[] array) {
			for (int i = 0; i < array.Length; i++) {
				Console.WriteLine (array[i]);
			}
		}

	}

}
