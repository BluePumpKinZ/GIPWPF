using System;
using System.Collections.Generic;
using GIP.Utils;
using NAudio;
using NAudio.Wave;

namespace GIP.IO {

	class GIPIO {

		public static string outputPath = "C:/Users/Jonas/OneDrive/GIP/Proef/Output/";

		public static Song LoadMP3 (string path) {

			Song song = new Song ();

			int readLength = 100000;

			using (Mp3FileReader reader = new Mp3FileReader (path)) {

				byte[] buffer = new byte[0];

				using (WaveStream pcm = WaveFormatConversionStream.CreatePcmStream (reader)) {

					using (WaveStream aligned = new BlockAlignReductionStream (pcm)) {

						buffer = new byte[aligned.Length];
						for (int i = 0; i < aligned.Length; i += readLength) {
							aligned.Read (buffer, i, readLength);
						}
						song.sampleRate = aligned.WaveFormat.SampleRate;
						song.channels = aligned.WaveFormat.Channels;
					}
				}

				song.samples = new double[buffer.Length / 2];
				for (int i = 0; i < song.samples.Length; i++) {
					song.samples[i] = BitConverter.ToInt16 (buffer, i * 2) / 32768.0f;
				}

			}

			return song;

		}

		public static void SaveSong (Song song, string path) {
			WaveFormat format = new WaveFormat (song.sampleRate, song.channels);
			WaveFileWriter waveFileWriter = new WaveFileWriter (path, format);
			float[] samples = new float[song.samples.LongLength];
			for (long i = 0; i < samples.LongLength; i++) {
				samples[i] = (float)song.samples[i];
			}
			waveFileWriter.WriteSamples (samples, 0, samples.Length);
			waveFileWriter.Close ();
		}
	}
}
