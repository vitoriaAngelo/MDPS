using AionMeter.Core.Combat;
using AionMeter.Core.Events;
using MDPS;

public class CsvTests
{
    [Fact]
    public void ExportEscapesUntrustedNamesAndLabelsSimulation()
    {
        var player = new CombatantSnapshot(1, "=1+1;\"nome\"", GameClass.Ranger, true, 15000, 1500, 1, 2, 0.5, 10000, 0);
        var snapshot = new EncounterSnapshot(Guid.NewGuid(), "Boss;teste", "DG", DateTimeOffset.UtcNow,
            10000, 10000, false, EncounterEndReason.Manual, 15000, 1500, null, [player]);
        var csv = CsvReport.Create(snapshot, true);
        Assert.Contains("SIMULAÇÃO;\"Boss;teste\"", csv);
        Assert.Contains("\"'=1+1;\"\"nome\"\"\"", csv);
        Assert.Contains(";15000;1500,00;100,00", csv);
    }
}
