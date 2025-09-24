public interface IAudioReactive
{
    void React(float[] spectrum, float[] waveform, bool beat, float level);
    void Activate();
    void Deactivate();
}
