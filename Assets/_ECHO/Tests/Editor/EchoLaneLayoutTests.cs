using System.Collections.Generic;
using System.Linq;
using EchoGame.UI;
using NUnit.Framework;
namespace EchoGame.Tests
{
    public sealed class EchoLaneLayoutTests
    {
        static EchoUIData Echo(int id, float start, float end) => new EchoUIData { Id = id, Start = start, End = end };
        [Test] public void AdjacentHalfOpenIntervalsShareOneLane()
        {
            var result = EchoLaneLayout.Arrange(new[] { Echo(1,0,8), Echo(2,8,16), Echo(3,16,24), Echo(4,24,30) });
            Assert.That(result.Select(p => p.Lane), Is.All.EqualTo(0));
        }
        [Test] public void OverlapUsesTwoLanesAndEarliestFinishIsReused()
        {
            var result = EchoLaneLayout.Arrange(new[] { Echo(1,0,15), Echo(2,8,20), Echo(3,20,30) });
            Assert.That(result.Select(p => p.Lane), Is.EqualTo(new[] {0,1,0}));
        }
        [Test] public void EqualStartsSortByStableIdIndependentlyOfInputOrder()
        {
            var echoes = new[] { Echo(9,0,10), Echo(3,0,10), Echo(5,0,10) };
            var first = EchoLaneLayout.Arrange(echoes);
            System.Array.Reverse(echoes);
            var second = EchoLaneLayout.Arrange(echoes);
            Assert.That(first.Select(p => (p.Echo.Id,p.Lane)), Is.EqualTo(second.Select(p => (p.Echo.Id,p.Lane))));
            Assert.That(first.Select(p => p.Echo.Id), Is.EqualTo(new[] {3,5,9}));
        }
        [Test] public void EmptyAndInvalidIntervalsDoNotAllocateVisibleLanes()
        {
            Assert.That(EchoLaneLayout.Arrange(new EchoUIData[0]), Is.Empty);
            Assert.That(EchoLaneLayout.Arrange(new[] { Echo(1,2,2), Echo(2,8,4), Echo(3,float.NaN,10) }), Is.Empty);
        }
        [Test] public void ManySequentialRecordingsDoNotAllocateOneLanePerRecording()
        {
            var echoes = new List<EchoUIData>();
            for (int i=0;i<10000;i++) echoes.Add(Echo(i,i,i+1));
            Assert.That(EchoLaneLayout.Arrange(echoes).Select(p => p.Lane), Is.All.EqualTo(0));
        }
        [Test] public void RandomSchedulesUseExactlyPeakConcurrencyWithoutLaneCollisions()
        {
            var random = new System.Random(72);
            for (int trial=0;trial<30;trial++)
            {
                var echoes = new List<EchoUIData>();
                for (int i=0;i<100;i++) { int start=random.Next(30); echoes.Add(Echo(i,start,start+random.Next(1,12))); }
                var layout = EchoLaneLayout.Arrange(echoes);
                int peak = echoes.Max(e => echoes.Count(other => other.Start <= e.Start && e.Start < other.End));
                Assert.That(layout.Max(p => p.Lane)+1, Is.EqualTo(peak));
                foreach (var lane in layout.GroupBy(p => p.Lane))
                {
                    float end = float.NegativeInfinity;
                    foreach (var p in lane) { Assert.That(p.Echo.Start, Is.GreaterThanOrEqualTo(end)); end = p.Echo.End; }
                }
            }
        }
    }
}
