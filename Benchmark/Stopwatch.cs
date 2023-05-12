using System.Collections.Generic;
using System.Text;

namespace Benchmark;

public class Stopwatch
{
    private readonly double frequencyR;
    private System.Diagnostics.Stopwatch stopwatch;
    private long restartTicks;

    public List<Record> Records { get; }

    public Stopwatch()
    {
        this.stopwatch = new System.Diagnostics.Stopwatch();
        this.frequencyR = 1.0d / (double)System.Diagnostics.Stopwatch.Frequency;
        this.stopwatch.Start();

        this.Records = new List<Record>();

        this.Restart();
    }

    public void Restart()
    {
        this.restartTicks = this.stopwatch.ElapsedTicks;
    }

    public void Lap(string? comment = null)
    {
        var record = new Record()
        {
            Elapsed = this.GetElapsed(),
            Comment = comment,
        };

        this.Restart();

        this.Records.Add(record);
    }

    public void Split(string? comment = null)
    {
        var record = new Record()
        {
            Elapsed = this.GetElapsed(),
            Comment = comment,
        };

        this.Records.Add(record);
    }

    public string ToSimpleString()
    {
        var sb = new StringBuilder();
        int n;

        void AppendText(Record record)
        {
            sb.Append(record.Comment);
            sb.Append(": ");
            var s = string.Format("{0:F1}", record.Elapsed * 1000_000);
            sb.Append(s);
        }

        for (n = 0; n < (this.Records.Count - 1); n++)
        {
            AppendText(this.Records[n]);
            sb.Append("\r\n");
        }

        if (n < this.Records.Count)
        {
            AppendText(this.Records[n]);
        }

        return sb.ToString();
    }

    public double GetElapsed() => (double)(this.stopwatch.ElapsedTicks - this.restartTicks) * this.frequencyR;

    public class Record
    {
        public double Elapsed { get; set; }

        public string? Comment { get; set; }
    }
}
