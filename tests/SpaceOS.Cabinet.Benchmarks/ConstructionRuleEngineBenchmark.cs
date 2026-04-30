using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using SpaceOS.Cabinet.Abstractions;
using SpaceOS.Cabinet.Construction;
using SpaceOS.Cabinet.Domain;
using SpaceOS.Cabinet.Domain.Skeleton;
using SpaceOS.Cabinet.Geometry;
using SpaceOS.Cabinet.Machining;

namespace SpaceOS.Cabinet.Benchmarks;

/// <summary>
/// Benchmarks comparing sequential <c>ApplyAll</c> against parallel <c>ApplyAllAsync</c>
/// on a 500-part skeleton with 10 rules.
///
/// Run with: <c>dotnet run -c Release</c>
/// Target: ApplyAllAsync ≥ 30% faster than ApplyAll on machines with ≥ 2 logical cores.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class ConstructionRuleEngineBenchmark
{
    private Skeleton _skeleton = null!;
    private IConstructionContext _context = null!;
    private ConstructionRuleEngine _engine = null!;

    /// <summary>
    /// Number of rules used in each benchmark run.
    /// BenchmarkDotNet will parameterise if needed; fixed at 10 for target measurements.
    /// </summary>
    [Params(10)]
    public int RuleCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var dim = AssemblyDimension.Create(600, 720, 560).Value;
        _skeleton = BuildLargeSkeleton(dim, partCount: 500);
        _context = new BenchmarkConstructionContext(dim);
        _engine = new ConstructionRuleEngine(
            Enumerable.Range(0, RuleCount).Select(i => new BenchmarkRule(i)));
    }

    /// <summary>Baseline: sequential <see cref="ConstructionRuleEngine.ApplyAll"/>.</summary>
    [Benchmark(Baseline = true)]
    [System.Obsolete]
    public void Sequential_ApplyAll()
    {
#pragma warning disable CS0618 // intentional: measuring baseline
        _engine.ApplyAll(_skeleton, _context);
#pragma warning restore CS0618
    }

    /// <summary>Subject: parallel <see cref="ConstructionRuleEngine.ApplyAllAsync"/> via Channel&lt;T&gt;.</summary>
    [Benchmark]
    public async Task Parallel_ApplyAllAsync()
    {
        await _engine.ApplyAllAsync(_skeleton, _context).ConfigureAwait(false);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a skeleton with <paramref name="partCount"/> additional parts beyond the base cuboid.
    /// </summary>
    private static Skeleton BuildLargeSkeleton(AssemblyDimension dim, int partCount)
    {
        var skeleton = Skeleton.Create(Guid.NewGuid(), dim).Value;

        var partDim = PartDimension.Create(564, 560, 18).Value;
        for (var i = 0; i < partCount; i++)
        {
            // Distribute parts at 1 mm steps along Z so they do not overlap the base cuboid exactly.
            var z = 10.0 + i * 1.2;
            var transform = AffineTransform.Translation(Vector3.Create(18, 0, z).Value).Value;
            var frame = PartFrame.Create(transform, partDim).Value;
            skeleton.AddPart(frame, "lamiboard-18mm");
        }

        return skeleton;
    }

    private sealed class BenchmarkRule(int index) : IConstructionRule
    {
        public string RuleId => $"Bench-{index}";
        public string Description => $"Benchmark rule {index}.";

        public ConstructionRuleResult Apply(Skeleton skeleton, IConstructionContext ctx, CancellationToken ct)
        {
            // Simulate a small amount of CPU work proportional to part count
            // to make parallelism measurable. Pure no-op rules are too fast.
            var count = 0;
            foreach (var _ in skeleton.Parts)
                count++;

            var advisories = count > 0
                ? Array.Empty<DesignAdvisory>()
                : [new DesignAdvisory(RuleId, AdvisorySeverity.Info, "Skeleton", "No parts.", null)];

            return new ConstructionRuleResult(Array.Empty<MachiningFeature>(), advisories);
        }
    }

    private sealed class BenchmarkConstructionContext(AssemblyDimension dim) : IConstructionContext
    {
        public ITenantStandardProvider TenantStandard { get; } = new BenchmarkTenantStandardProvider();
        public AssemblyDimension AssemblyDimension { get; } = dim;
    }

    private sealed class BenchmarkTenantStandardProvider : ITenantStandardProvider
    {
        public Guid TenantId { get; } = Guid.NewGuid();
        public string DefaultCarcassMaterial => "lamiboard-18mm";
        public double DefaultCarcassThickness => 18.0;
        public string DefaultBackPanelMaterial => "hdf-5mm";
        public double DefaultBackPanelThickness => 5.0;
        public BackPanelAttachmentDefault BackPanelAttachment => BackPanelAttachmentDefault.Groove;
        public TopType TopType => TopType.FullTop;
        public bool LineBoreEnabled => true;
        public double LineBoreFirstHoleOffset => 38.0;
        public double LineBoreSpacing => 32.0;
        public double LineBoreDiameter => 5.0;
        public double TallCabinetHeightThreshold => 2000.0;
        public double LongShelfThreshold => 800.0;
        public IReadOnlyDictionary<string, AdvisorySeverity> RuleSeverityOverrides { get; }
            = new Dictionary<string, AdvisorySeverity>();
    }
}
