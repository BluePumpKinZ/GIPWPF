using System;
using System.Collections.Generic;
using GIP.Utils;

namespace GIP {

	public class Sequencer {

		public static double[] cosines;
		public static double[] sines;
		public static double[] bellCurve;

		public static int detectionRange = 12 * 8;
		public static int anglePercision = 10000;

		public static void GenerateAngles () {
			if (cosines != null && sines != null && bellCurve != null)
				return;
			cosines = new double[anglePercision];
			sines = new double[anglePercision];

			for (int i = 0; i < anglePercision; i++) {
				double t = 2.0 * Math.PI * i / anglePercision;
				cosines[i] = Math.Cos (t);
				sines[i] = Math.Sin (t);
			}
			bellCurve = new double[3];
			bellCurve[0] = Meth.BellCurve (-1);
			bellCurve[1] = Meth.BellCurve (+0);
			bellCurve[2] = Meth.BellCurve (+1);

		}

		static double Lerp (double a, double b, double t) {
			return a * (1 - t) + b * t;
		}

		public static double GetAbsoluteCos (double angle) {
			double t = angle % 1 * anglePercision;
			int minIndex = (int)Math.Floor (t);
			int maxIndex = (minIndex + 1) % anglePercision;
			return Lerp (cosines[minIndex], cosines[maxIndex], t % 1);
		}

		public static double GetAbsoluteSin (double angle) {
			double t = angle % 1 * anglePercision;
			int minIndex = (int)Math.Floor (t);
			int maxIndex = (minIndex + 1) % anglePercision;
			return Lerp (sines[minIndex], sines[maxIndex], t % 1);
		}

		public static double[] FourierTransform (double[] samples, long start, long count, double sampleLength) {

			GenerateAngles ();

			// The lowest frequency that is recongnised
			double baseFreq = 110; //A2
								   // The multiplier applied to the frequency for the next note
			double freqDiff = 1.0594630943592952645618252949463417007; //12th root of 2
																	   // The multiplier applied to the frequency to broaden the frequency detection
			double fineFreqDiff = 1.005792941; // 120th root of 2

			double[] output = new double[detectionRange];

			long end = start + count;
			int noteCounter = 0;
			// Loop through all the frequencies
			for (double freq = baseFreq; noteCounter < detectionRange; freq *= freqDiff) {
				int freqCount = 0;
				double totalAmp = 0;
				double startFreq = freq / fineFreqDiff;
				// Loop through all the samples for every base frequency
				for (double fineFreq = startFreq; freqCount < 3; fineFreq *= fineFreqDiff) {
					double x = 0;
					double y = 0;
					// The angle increase for every sample
					double anglePart = fineFreq * sampleLength;
					// Loop through the finer frequencies for every base frequency
					for (long i = start; i < end; i++) {
						// Add to the total
						double angle = anglePart * i;
						double cos = GetAbsoluteCos (angle);
						double sin = GetAbsoluteSin (angle);
						double amp = bellCurve[freqCount];
						x += cos * samples[i] * amp;
						y += sin * samples[i] * amp;
					}
					// Divide by the total number of samples to get an average
					x /= count;
					y /= count;
					totalAmp += Math.Sqrt (x * x + y * y);
					freqCount++;
				}
				output[noteCounter] = totalAmp * 2.506628274631000502415765284811;
				noteCounter++;

			}
			return output;
		}

		public static double[] FourierTransform (double[] samples, long start, long count, double sampleLength, double baseFreq, double freqDiff, int iterations, bool linear) {

			double[] output = new double[iterations];

			long end = start + count;
			int noteCounter = 0;
			for (double freq = baseFreq; noteCounter < iterations; freq = linear ? freq + freqDiff : freq * freqDiff) {
				double x = 0;
				double y = 0;
				double anglePart = 2 * Math.PI * freq * sampleLength;
				for (long i = start; i < end; i++) {
					double angle = anglePart * i;
					double cos = Math.Cos (angle);
					double sin = Math.Sin (angle);
					x += cos * samples[i];
					y += sin * samples[i];
				}
				x /= count;
				y /= count;
				double totalAmp = Math.Sqrt (x * x + y * y);
				output[noteCounter] = totalAmp;
				noteCounter++;
				//Console.Write ("\r{0}%", noteCounter * 100 / iterations);
				//Console.WriteLine ("Strength {0} for bpm: {1}", output[noteCounter - 1], freq * 60);
			}
			return output;
		}

	}
}
