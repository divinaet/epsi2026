namespace StarWarsApi.Models;

/// <summary>
/// DTO pour la mise à jour partielle d'un personnage (PATCH).
/// Les propriétés nulles ne seront pas modifiées.
/// </summary>
public class CharacterUpdateDto
{
    public string? Name { get; set; }
    public string? Planet { get; set; }
    public string? Affiliation { get; set; }
    public int? BirthYear { get; set; }
}
