﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Smf.Interaction;

namespace Melanchall.DryWetMidi.Devices
{
    /// <summary>
    /// Provides a way to record MIDI data received by an input MIDI device.
    /// </summary>
    public sealed class Recording : IDisposable
    {
        #region Fields

        private readonly List<RecordingEvent> _events = new List<RecordingEvent>();
        private readonly Stopwatch _stopwatch = new Stopwatch();

        private bool _disposed = false;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Recording"/> with the specified
        /// tempo map and input MIDI device to capture MIDI data from.
        /// </summary>
        /// <param name="tempoMap">Tempo map used to calculate events times.</param>
        /// <param name="inputDevice">Input MIDI device to capture MIDI data from.</param>
        /// <exception cref="ArgumentNullException"><paramref name="tempoMap"/> is null. -or-
        /// <paramref name="inputDevice"/> is null.</exception>
        public Recording(TempoMap tempoMap, InputDevice inputDevice)
        {
            ThrowIfArgument.IsNull(nameof(tempoMap), tempoMap);
            ThrowIfArgument.IsNull(nameof(inputDevice), inputDevice);

            TempoMap = tempoMap;
            InputDevice = inputDevice;
            InputDevice.EventReceived += OnEventReceived;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the tempo map used to calculate recorded events times.
        /// </summary>
        public TempoMap TempoMap { get; }

        /// <summary>
        /// Gets the input MIDI device to record MIDI data from.
        /// </summary>
        public InputDevice InputDevice { get; }

        /// <summary>
        /// Gets a value indicating whether recording is currently running or not.
        /// </summary>
        public bool IsRunning { get; private set; }

        #endregion

        #region Methods

        // TODO: add methods to get recording length

        /// <summary>
        /// Gets MIDI events recorded by the current <see cref="Recording"/>.
        /// </summary>
        /// <returns>MIDI events recorded by the current <see cref="Recording"/>.</returns>
        public IReadOnlyList<TimedEvent> GetEvents()
        {
            return _events.Select(e => new TimedEvent(e.Event, TimeConverter.ConvertFrom((MetricTimeSpan)e.Time, TempoMap)))
                          .ToList()
                          .AsReadOnly();
        }

        /// <summary>
        /// Starts MIDI data recording.
        /// </summary>
        /// <exception cref="InvalidOperationException">Input device is not listening for MIDI events</exception>
        public void Start()
        {
            if (IsRunning)
                return;

            if (!InputDevice.IsListeningForEvents)
                throw new InvalidOperationException($"Input device is not listening for MIDI events. Call {nameof(InputDevice.StartEventsListening)} prior to recording start.");

            _stopwatch.Start();

            IsRunning = true;
        }

        /// <summary>
        /// Stops MIDI data recording. Note that this method doesn't reset the recording time. If you
        /// call <see cref="Start"/>, recording will be resumed from the point where <see cref="Stop"/> was called.
        /// </summary>
        public void Stop()
        {
            if (!IsRunning)
                return;

            _stopwatch.Stop();

            IsRunning = false;
        }

        private void OnEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            if (!IsRunning)
                return;

            _events.Add(new RecordingEvent(e.Event, _stopwatch.Elapsed));
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Releases all resources used by the current <see cref="Recording"/>.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                Stop();
                InputDevice.EventReceived -= OnEventReceived;
            }

            _disposed = true;
        }

        #endregion
    }
}