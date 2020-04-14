using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using GIP.Utils;
using GIP.IO;
using GIP;
using MidiSharp;
using MidiSharp.CodeGeneration;
using MidiSharp.Events;
using MidiSharp.Events.Voice.Note;
using MidiSharp.Events.Meta;

namespace GIPWPF {


	public partial class MainWindow : Window {

		public MainWindow () {

			InitializeComponent ();
			//Main ();
			IO_ProgressLabel.Content = "";
		}

		static double sampleFrequency = 1.0 / 0.06;
		static Song rawAudio;
		static Song producedAudio;

		void Run (object sender, RoutedEventArgs e) {
			IO_Image.Stretch = Stretch.Fill;
			ThreadStart threadStart = new ThreadStart (delegate { ThreadRun (); });
			Thread thread = new Thread (threadStart);
			thread.Start ();
		}

		public void ThreadRun () {
			LoadAudio ();
			SequenceAudio ();
			AnalyseAudio ();
			// Output (OutputType.Wavelet);
			Output (OutputType.Audio);
			GIPIO.SaveSong (producedAudio, "C:/Users/Jonas/OneDrive/GIP/Proef/Output/audio.wav");
			// Output (OutputType.Midi);
		}

		void LoadAudio () {
			//"C:/Users/Jonas/OneDrive/GIP/Proef/RawData001.mp3"
			//"D:/Documenten/Music/Songs/MP3/Upcoming/Success.mp3"
			WriteToProgressLabel ("Reading Audio");
			rawAudio = GIPIO.LoadMP3 ("C:/Users/Jonas/OneDrive/GIP/Proef/RawData005.mp3");
			WriteToProgressLabel ("Converting to Mono");
			rawAudio = rawAudio.ToMono ();
			WriteToProgressLabel ("Leveling Audio");
			rawAudio = rawAudio.LevelAudio ();
		}

		List<double[]> allAmps = new List<double[]> ();
		void SequenceAudio () {
			allAmps.Clear ();
			// Calculating the step size
			int sampleStep = (int)Math.Round (rawAudio.sampleRate / sampleFrequency);
			for (long i = 0; i < rawAudio.samples.Length - sampleStep; i += sampleStep) {

				double progress = Math.Round (10000.0 * i / (rawAudio.samples.Length - sampleStep)) / 100;
				WriteToProgressLabel ("Transforming: " + progress + "%");
				double[] amps = Sequencer.FourierTransform (rawAudio.samples, i, sampleStep, rawAudio.SampleLength);
				allAmps.Add (amps);
			}
		}

		List<Note> notes;
		void AnalyseAudio () {
			WriteToProgressLabel ("Combining Notes");
			// Create an empty list of notes
			List<Note> allNotes = new List<Note> ();
			// The duration of a the smallest possible note
			double duration = 1 / (sampleFrequency - 1);
			for (int i = 0; i < allAmps.Count; i++) {
				// The time at which the note should start
				double startTime = (double)i / sampleFrequency;
				// The maximum value at this timestamp
				double maxValue = allAmps[i].Max ();
				for (int j = 0; j < allAmps[i].Length; j++) {
					if (allAmps[i][j] >= maxValue * 0.75) {
						// Add the new note to the list
						allNotes.Add (new Note (startTime, j, duration, allAmps[i][j]));
					}
				}
			}
			// string path = "C:/Users/Jonas/Desktop/result.txt";
			//File.AppendAllText (path, "StartCount: " + allNotes.Count + "\n");
			// allNotes.Add (new Note (startTime, noteIndex, duration, allAmps[i][noteIndex]));
			for (int i = 0; i < allNotes.Count; i++) {
				if (i < 0)
					continue;
				Note note1 = allNotes[i];
				for (int j = 0; j < allNotes.Count; j++) {
					Note note2 = allNotes[j];
					// Execute test to see if notes are eligable for combining
					if (i == j)
						continue;
					if (note1.noteNumber != note2.noteNumber)
						continue;
					if (!Note.TestOverlap (note1, note2))
						continue;
					// Combine the note1 and note2
					allNotes[i] = Note.Combine (note1, note2);
					// Remove old note
					allNotes.RemoveAt (j);
					// Move the pointer to adjust for index sliding
					i -= 2;
					break;
				}
			}

			double normalDuration = 1.0 / sampleFrequency;
			double actualDuration = 1.0 / (sampleFrequency - 1);

			for (int i = 0; i < allNotes.Count; i++) {
				allNotes[i].duration -= actualDuration - normalDuration;
			}
			//File.AppendAllText (path, "EndCount: " + allNotes.Count + "\n");
			notes = allNotes;
		}
		WriteableBitmap bitmap;
		uint[] pixels;
		public enum OutputType { Audio, Wavelet, Midi }
		void Output (OutputType type) {
			switch (type) {
			case OutputType.Midi:

				/*List<MidiEvent> midiEvents = new List<MidiEvent> ();

				double max = 0;
				for (int i = 0; i < notes.Count; i++) {
					double d = notes[i].duration * 4;
					double t1 = notes[i].time * 4;
					double t2 = t1 + d;
					max = t2 > max ? t2 : max;

					NoteOnEvent note = new NoteOnEvent (Convert.ToInt64 (t1), 1, notes[i].noteNumber + 40, 127, Convert.ToInt32(d));
					midiEvents.Add (note);
				}
				MidiEvent endMarker = new NoteEvent (Convert.ToInt64(max), 1, MidiCommandCode.StopSequence, 0, 0);
				midiEvents.Add (endMarker);

				MidiEventCollection collection = new MidiEventCollection (0, 1);
				collection.AddTrack (midiEvents);

				MidiFile.Export ("C:/Users/Jonas/OneDrive/GIP/Proef/Output/midi.mid", collection);*/

				MidiTrack track = new MidiTrack ();
				long maxTime = 0;
				//string path = "C:/Users/Jonas/Desktop/result.txt";
				double previousTime = 0;
				for (int i = 0; i < notes.Count; i++) {

					string path = "C:/Users/Jonas/Desktop/result.txt";
					File.AppendAllText (path, string.Format("{0} at {1}\n", DecodeNote (notes[i].noteNumber + 44, NoteNotation.Short), notes[i].time));

					double d = 1;
					double t1 = notes[i].time * 20 - previousTime;
					double t2 = t1 + d;
					if (t1 < 0)
						continue;
					previousTime = t2;

					

					MidiEvent onEvent = new OnNoteVoiceMidiEvent (Convert.ToInt64 (t1), 1, (byte)(44 + notes[i].noteNumber), 63);
					MidiEvent offEvent = new OffNoteVoiceMidiEvent (Convert.ToInt64 (t2), 1, (byte)(44 + notes[i].noteNumber), 0);
					long t2long = (long)t2;
					maxTime = (t2long > maxTime) ? t2long : maxTime;

					track.Events.Add (onEvent);
					track.Events.Add (offEvent);
				}
				MidiEvent endMarker = new EndOfTrackMetaMidiEvent (maxTime);
				track.Events.Add (endMarker);
				MidiSequence sequence = new MidiSequence (Format.Zero, 1000);
				sequence.Tracks.Add (track);

				

				FileStream stream = new FileStream ("C:/Users/Jonas/OneDrive/GIP/Proef/Output/midi.mid", FileMode.OpenOrCreate);
				sequence.Save (stream);
				stream.Close ();
				break;
			case OutputType.Audio:
				double longest = 0;
				for (int i = 0; i < notes.Count; i++) {
					double time = notes[i].time + notes[i].duration;
					longest = Math.Max (time, longest);
				}
				double[] newSamples = new double[(int)Math.Ceiling (longest * rawAudio.sampleRate)];

				for (int i = 0; i < notes.Count; i++) {
					int startIndex = (int)Math.Floor (notes[i].time * rawAudio.sampleRate);
					int endIndex = (int)Math.Floor ((notes[i].time + notes[i].duration) * rawAudio.sampleRate);
					double freq = 110 * Math.Pow (1.059463094359295, notes[i].noteNumber);
					double amp = notes[i].amplitude;
					for (int j = startIndex; j < endIndex; j++) {
						double time = j * rawAudio.SampleLength + notes[i].time;
						double value = Math.Sin (time * freq * 2 * Math.PI) * amp;
						//double maxAmp = Math.Min (1, 5 * Math.Min (Math.Abs (time - notes[i].time), Math.Abs (time - (notes[i].time + notes[i].duration))));
						newSamples[j] += value;
					}
				}
				for (int i = 0; i < newSamples.Length; i++) {
					newSamples[i] = Math.Max (Math.Min (1, newSamples[i]), -1);
				}
				int avgSpread = 35;
				double[] smoothSamples = new double[newSamples.Length - avgSpread];
				for (int i = 0; i < newSamples.Length - avgSpread; i++) {
					double tot = 0;
					for (int j = 0; j < avgSpread; j++) {
						tot += newSamples[i + j];
					}
					smoothSamples[i] = tot / avgSpread;
				}
				Song song = new Song (smoothSamples, rawAudio.channels, rawAudio.sampleRate);

				// GIPIO.SaveSong (song, GIPIO.outputPath + "test.wav");
				producedAudio = song;
				break;
			case OutputType.Wavelet:
				pixels = new uint[allAmps.Count * allAmps[0].Length];
				for (int x = 0; x < allAmps.Count; x++) {
					for (int y = 0; y < allAmps[0].Length; y++) {

						double i = allAmps[x][y];
						// i *= 2;
						i = Math.Min (1, i);
						// double r = -2 * (i - 1.0) * (2 * (i - 1.0)) + 1;
						// double g = -2 * (i - 0.5) * (2 * (i - 0.5)) + 1;
						// double b = -2 * (i - 0.0) * (2 * (i - 0.0)) + 1;

						double r, g, b;
						r = g = b = i;

						uint red = (uint)Math.Round (r * 255);
						uint green = (uint)Math.Round (g * 255);
						uint blue = (uint)Math.Round (b * 255);

						int index = (allAmps[0].Length - y - 1) * allAmps.Count + x;
						pixels[index] = (uint)((255 << 24) + (red << 16) + (green << 8) + blue);
					}
				}
				DrawWavelet ();

				break;
			}
		}

		void WriteToProgressLabel (string content) {
			Dispatcher.Invoke (DispatcherPriority.Normal, new Action (delegate () {
				IO_ProgressLabel.Content = content;
			}));
		}

		void DrawWavelet () {
			Dispatcher.Invoke (DispatcherPriority.Normal, new Action (delegate () {
				bitmap = new WriteableBitmap (allAmps.Count, allAmps[0].Length, 96, 96, PixelFormats.Bgra32, null);
				bitmap.WritePixels (new Int32Rect (0, 0, allAmps.Count, allAmps[0].Length), pixels, allAmps.Count * 4, 0);
				IO_Image.Source = bitmap;
				IO_ProgressLabel.Content = "Done";
				BitmapEncoder encoder = new PngBitmapEncoder ();
				encoder.Frames.Add (BitmapFrame.Create (bitmap));
				FileStream stream = new FileStream (GIPIO.outputPath + "Wavelet.png", FileMode.OpenOrCreate);
				encoder.Save (stream);
			}));
		}

		double GetTimingSimularity () {
			if (rawAudio.SampleLength != producedAudio.SampleLength)
				throw new ArgumentException ("Songs do not share the same samplelength");
			long minLength = Math.Min (rawAudio.samples.Length, producedAudio.samples.Length);
			double totalDiff = 0;
			int total = 0;
			int sampleStep = rawAudio.sampleRate / 50; // 20ms intervals
			for (long i = 0; i < minLength - sampleStep; i += sampleStep) {
				double amp1 = Analysis.AverageAmplitude (rawAudio.samples, i, sampleStep);
				double amp2 = Analysis.AverageAmplitude (producedAudio.samples, i, sampleStep);
				totalDiff += Math.Abs (amp1 - amp2);
				total++;
			}
			return totalDiff / total;
		}

		double GetFrequencySimularity () {
			if (rawAudio.SampleLength != producedAudio.SampleLength)
				throw new ArgumentException ("Songs do not share the same samplelength");
			long minLength = Math.Min (rawAudio.samples.Length, producedAudio.samples.Length);
			List<double[]> tempAmps = new List<double[]> ();
			int sampleStep = (int)Math.Round (rawAudio.sampleRate / sampleFrequency);
			for (long i = 0; i < minLength - sampleStep; i += sampleStep) {

				double progress = Math.Round (10000.0 * i / (minLength - sampleStep)) / 100;
				WriteToProgressLabel ("Transforming produced audio: " + progress + "%");
				double[] amps = Sequencer.FourierTransform (producedAudio.samples, i, sampleStep, rawAudio.SampleLength);
				tempAmps.Add (amps);
			}
			double totalDiff = 0;
			double tot = 0;
			for (int i = 0; i < tempAmps.Count; i++) {
				for (int j = 0; j < tempAmps[i].Length; j++) {
					totalDiff += Math.Abs (allAmps[i][j] - tempAmps[i][j]);
					tot++;
				}
			}
			totalDiff /= tot;
			return totalDiff;
		}

		string[] noteNotations = new string[] {
			"A","A#", "B", "C","C#","D","D#","E","F","F#","G", "G#"
		};

		public enum NoteNotation { Short, Long };
		string DecodeNote (int noteOrder, NoteNotation noteNotation) {
			int octave = (noteOrder - (noteOrder % 12)) / 12;
			int note = noteOrder % 12;
			string noteStr = noteNotations[note];
			if (note < 3)
				octave--;
			if (noteNotation == NoteNotation.Long) {
				return string.Format ("{0} in octave {1}", noteStr, octave);
			} else {
				return string.Format ("{0}{1}", noteStr, octave);
			}
		}

		public class Note {
			public double time;
			public int noteNumber;
			public double duration;
			public double amplitude;

			public Note (double time, int noteNumber, double duration, double amplitude) {
				this.time = time;
				this.noteNumber = noteNumber;
				this.duration = duration;
				this.amplitude = amplitude;
			}

			public static bool TestOverlap (Note a, Note b) {

				if (a.time < b.time) {

					if (a.time + a.duration >= b.time)
						//if (Math.Abs (a.amplitude - b.amplitude) < 0.1)
						return true;

				} else {

					if (b.time + b.duration >= a.time)
						//if (Math.Abs (a.amplitude - b.amplitude) < 0.1)
						return true;

				}
				return false;
			}

			public static Note Combine (Note a, Note b) {
				double begin = Math.Min (a.time, b.time);
				double end = Math.Max (a.time + a.duration, b.time + b.duration);
				double duration = end - begin;
				double amplitude = Math.Max (a.amplitude, b.amplitude);
				return new Note (begin, a.noteNumber, duration, amplitude);
			}

			public int CompareTo (object obj) {
				if (obj == null)
					return 1;
				return amplitude.CompareTo ((obj as Note).amplitude);
			}
		}
	}
}
