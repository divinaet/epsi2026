using System;

namespace StarWarsApi.Models;

public class Character
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Planet { get; set; } = string.Empty;
    public string Affiliation { get; set; } = string.Empty;
    public int BirthYear { get; set; }
}
