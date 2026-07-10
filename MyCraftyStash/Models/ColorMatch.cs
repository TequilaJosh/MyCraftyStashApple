using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyCraftyStash.Models;

/// <summary>A row in the TE color-match chart (DMC floss / OLO marker → a
/// Taylored Expressions color). Copied from the desktop's settings.db.</summary>
[Table("color_matches")]
public class ColorMatch
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")] public int Id { get; set; }

    [Column("system")] public string System { get; set; } = "";          // "DMC" or "OLO"
    [Column("external_code")] public string ExternalCode { get; set; } = "";
    [Column("te_color_name")] public string TeColorName { get; set; } = "";
    [Column("external_hex")] public string? ExternalHex { get; set; }
    [Column("te_color_hex")] public string? TeColorHex { get; set; }
    [Column("notes")] public string? Notes { get; set; }
}
