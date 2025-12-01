using System.ComponentModel.DataAnnotations;

namespace Nudges.Configuration;

public class OidcSettings {
    [Required]
    [MinLength(1)]
    public string ServerUrl { get; set; } = string.Empty;
    [Required]
    [MinLength(1)]
    public string Realm { get; set; } = string.Empty;
    [Required]
    [MinLength(1)]
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
}
