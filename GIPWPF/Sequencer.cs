using System;
using System.Collections.Generic;
using GIP.Utils;
using NAudio.Dsp;

namespace GIP {

	class Sequencer {

		public static double[] FourierTransform (double[] samples, long start, long count, double sampleLength) {
			double baseFreq = 110; //A1
			double freqDiff = 1.0594630943592952645618252949463417007; //12th root of 2
			double fineFreqDiff = 1.005792941; // 120th root of 2

			double[] output = new double[12 * 4];

			long end = start + count;
			int noteCounter = 0;
			for (double freq = baseFreq; noteCounter < 12 * 4; freq *= freqDiff) {
				int freqCount = 0;
				double totalAmp = 0;
				for (double fineFreq = freq * Math.Pow (fineFreqDiff, -2); freqCount < 5; fineFreq *= fineFreqDiff) {
					double x = 0;
					double y = 0;
					double anglePart = 2 * Math.PI * fineFreq * sampleLength;
					for (long i = start; i < end; i++) {
						double angle = anglePart * i;
						double cos = Math.Cos (angle);
						x += cos * samples[i];
						y += Math.Sqrt (1 - (cos * cos)) * samples[i];
					}
					x /= count;
					y /= count;
					totalAmp += Math.Sqrt (x * x + y * y);
					freqCount++;
				}
				output[noteCounter] = totalAmp / 5;
				noteCounter++;

			}
			return output;
		}

		public static double[] FourierTransform (double[] samples, long start, long count, double sampleLength, double baseFreq, double freqDiff, double fineFreqDiff, int iterations, bool linear) {

			double[] output = new double[iterations];

			long end = start + count;
			int noteCounter = 0;
			for (double freq = baseFreq; noteCounter < iterations; freq = linear ? freq + freqDiff : freq * freqDiff) {
				int freqCount = 0;
				double totalAmp = 0;
				for (double fineFreq = freq * Math.Pow (fineFreqDiff, -2); freqCount < 5; fineFreq = linear ? fineFreq + fineFreqDiff : fineFreq * fineFreqDiff) {
					double x = 0;
					double y = 0;
					double anglePart = 2 * Math.PI * fineFreq * sampleLength;
					for (long i = start; i < end; i++) {
						double angle = anglePart * i;
						double cos = Math.Cos (angle);
						x += cos * samples[i];
						y += Math.Sqrt (1 - (cos * cos)) * samples[i];
					}
					x /= count;
					y /= count;
					totalAmp += Math.Sqrt (x * x + y * y);
					freqCount++;
				}
				output[noteCounter] = totalAmp / 5;
				noteCounter++;
				//Console.Write ("\r{0}%", noteCounter * 100 / iterations);
				Console.WriteLine ("Strength {0} for bpm: {1}", output[noteCounter - 1], freq * 60);
			}
			return output;
		}

	}
}
