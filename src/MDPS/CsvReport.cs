using System.Globalization;
using System.Text;
using AionMeter.Core.Combat;

namespace MDPS;

public static class CsvReport
{
    public static string Create(EncounterSnapshot snapshot, bool simulated)
    {
        var pt = CultureInfo.GetCultureInfo("pt-BR");
        var output = new StringBuilder("Modo;Combate;Início;Duração (s);Jogador;Dano;DPS;Participação (%);Cura realizada;HPS\r\n");
        foreach (var p in snapshot.Combatants.OrderByDescending(c => c.Damage))
            output.AppendLine(string.Join(";", simulated ? "SIMULAÇÃO" : "REAL",
                Cell(snapshot.Title), Cell(snapshot.StartedAt.ToString("O")),
                (snapshot.CombatMs / 1000.0).ToString("0.000", pt), Cell(p.Name),
                p.Damage.ToString(pt), p.Dps.ToString("0.00", pt), (p.Share * 100).ToString("0.00", pt),
                p.Healing?.ToString(pt) ?? "", p.Hps?.ToString("0.00", pt) ?? ""));
        return output.ToString();
    }

    private static string Cell(string value)
    {
        // Names come from the network: spreadsheet software must not interpret them as formulas.
        if (value.TrimStart().FirstOrDefault() is '=' or '+' or '-' or '@' || value.StartsWith('\t') || value.StartsWith('\r'))
            value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
}
