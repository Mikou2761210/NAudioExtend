using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NAudioExtend.Provider
{
    public interface IExtendProvider : IWaveProvider, IDisposable
    {
        public TimeSpan TotalDuration { get; }

        public TimeSpan CurrentPosition { get; set; }
    }
}
