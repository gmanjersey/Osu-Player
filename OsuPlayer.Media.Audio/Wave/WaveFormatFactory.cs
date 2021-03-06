﻿using Milky.OsuPlayer.Common;
using NAudio.Wave;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Milky.OsuPlayer.Media.Audio.Wave
{
    /// <summary>
    /// Audio file to wave stream
    /// </summary>
    internal static class WaveFormatFactory
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private static int _sampleRate = 44100;
        private static int _bits = 16;
        private static int _channels = 2;

        public struct ResamplerQuality
        {
            public int Quality { get; }

            public ResamplerQuality(int quality)
            {
                Quality = quality;
            }

            public static implicit operator int(ResamplerQuality quality)
            {
                return quality.Quality;
            }

            public static implicit operator ResamplerQuality(int quality)
            {
                return new ResamplerQuality(quality);
            }

            public static int Highest => 60;
            public static int Lowest => 1;
        }

        public static int SampleRate
        {
            get => _sampleRate;
            set
            {
                if (Equals(_sampleRate, value)) return;
                _sampleRate = value;
                CachedSound.ClearCacheSounds();
                CachedSound.ClearDefaultCacheSounds();
            }
        }

        public static int Bits
        {
            get => _bits;
            set
            {
                if (Equals(_bits, value)) return;
                _bits = value;
                CachedSound.ClearCacheSounds();
                CachedSound.ClearDefaultCacheSounds();
            }
        }

        public static int Channels
        {
            get => _channels;
            set
            {
                if (Equals(_channels, value)) return;
                _channels = value;
                CachedSound.ClearCacheSounds();
                CachedSound.ClearDefaultCacheSounds();
            }
        }

        public static WaveFormat IeeeWaveFormat => WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, Channels);

        public static WaveFormat PcmWaveFormat => new WaveFormat(SampleRate, Bits, Channels);

        public static async Task<MyAudioFileReader> GetResampledAudioFileReader(string path, MyAudioFileReader.WaveStreamType type)
        {
            var stream = await Resample(path, type).ConfigureAwait(false);
            return stream is MyAudioFileReader afr ? afr : new MyAudioFileReader(stream, type);
        }

        public static async Task<MyAudioFileReader> GetResampledAudioFileReader(string path)
        {
            var cache = Path.Combine(Domain.CachePath,
                $"{Guid.NewGuid().ToString().Replace("-", "")}.sound");
            var stream = await Resample(path, cache).ConfigureAwait(false);
            return stream is MyAudioFileReader afr ? afr : new MyAudioFileReader(cache);
        }

        private static async Task<Stream> Resample(string path, string targetPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var audioFileReader = new MyAudioFileReader(path);
                    if (CompareWaveFormat(audioFileReader.WaveFormat))
                    {
                        return audioFileReader;
                    }

                    using (audioFileReader)
                    using (var resampler = new MediaFoundationResampler(audioFileReader, PcmWaveFormat))
                    using (var stream = new FileStream(targetPath, FileMode.Create))
                    {
                        resampler.ResamplerQuality = ResamplerQuality.Highest;
                        WaveFileWriter.WriteWavFileToStream(stream, resampler);
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error while resampling audio file: {0}", path);
                    throw;
                }
            }).ConfigureAwait(false);
        }

        private static async Task<Stream> Resample(string path, MyAudioFileReader.WaveStreamType type)
        {
            if (!File.Exists(path))
            {
                path = Path.Combine(Domain.DefaultPath, "blank.wav");
            }

            return await Task.Run(() =>
            {
                MyAudioFileReader audioFileReader = null;
                try
                {
                    audioFileReader = new MyAudioFileReader(path);
                    if (CompareWaveFormat(audioFileReader.WaveFormat))
                    {
                        return (Stream)audioFileReader;
                    }

                    using (audioFileReader)
                    {
                        if (type == MyAudioFileReader.WaveStreamType.Wav)
                        {
                            using (var resampler = new MediaFoundationResampler(audioFileReader, PcmWaveFormat))
                            {
                                var stream = new MemoryStream();
                                resampler.ResamplerQuality = 60;
                                WaveFileWriter.WriteWavFileToStream(stream, resampler);
                                stream.Position = 0;
                                return stream;
                            }
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                }
                catch (Exception ex)
                {
                    audioFileReader?.Dispose();
                    Logger.Error(ex, "Error while resampling audio file: {0}", path);
                    throw;
                }
            }).ConfigureAwait(false);
        }

        private static bool CompareWaveFormat(WaveFormat waveFormat)
        {
            var pcmWaveFormat = PcmWaveFormat;
            if (pcmWaveFormat.Channels != waveFormat.Channels) return false;
            if (pcmWaveFormat.SampleRate != waveFormat.SampleRate) return false;
            return true;
        }
    }
}
