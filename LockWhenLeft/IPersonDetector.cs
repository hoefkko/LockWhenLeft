using System;
using System.Drawing;

namespace LockWhenLeft;

public interface IPersonDetector
{
    bool Paused { get; set; }
    bool ForceCameraFeed { get; set; }
    float ConfidenceTreshold { get; set; }
    int Sensitivity { get; set; }
    event Action PersonDetected;
    event Action NoPersonDetected;
    event Action NoPersonButChairDetected;
    event Action<string> OnErrorOccurred;
    event Action<Bitmap> NewFrameAvailable;
    void Start();
    void Stop();
}