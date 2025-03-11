using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NAudioExtend.Provider
{
    public class AudioFileReader(string fileName) : NAudio.Wave.AudioFileReader(fileName), IExtendProvider
    {
        public TimeSpan TotalDuration => base.TotalTime;

        public TimeSpan CurrentPosition { get => base.CurrentTime; set => base.CurrentTime = value; }
    }
}
