using System;
using System.Collections.Generic;

namespace GIP.Utils {

	class Song {

		public double[] samples;
		public int channels;
		public int sampleRate;
		public double sampleLength { get { return 1.0 / sampleRate; } }

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
			double[] newSamples = new double[samples.LongLength / channels];
			for (long i = 0; i < newSamples.LongLength; i += channels) {
				if (!average) {
					newSamples[i] = samples[i * channels];
					continue;
				}
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
			for (int i = 0; i < samples.LongLength; i++) {
				newSamples[i] = Math.Clamp (samples[i] * scale, -1, 1);
			}
			return new Song (newSamples, channels, sampleRate);
		}
	}

	public class Note {
		public int index;
		public double time;
		public double Frequency { get { return 110 * Math.Pow (1.0594630943592952645618252949463417007, index); } }

		public Note () {
			index = 0;
			time = 0;
		}

		public Note (int index, double time) {
			this.index = index;
			this.time = time;
		}
	}

	public static class Analysis {

		public static double AverageAmplitude (double[] samples, long start, long count) {
			if (count == 0)
				count = samples.LongLength;
			double total = 0;
			for (long i = start; i < start + count; i++) {
				total += Math.Abs (samples[i]);
			}
			total /= count;
			return total * 2;
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
