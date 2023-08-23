using System.Text.Json.Serialization;

namespace Abstractions
{
    /// <summary>
    /// Свойства смеси.
    /// </summary>
    public class MixProperties
    {
        /// <summary>
        /// Расход смеси.
        /// </summary>
        [JsonPropertyName("qMix")]
        public double QMix { get; set; }
        
        /// <summary>
        /// Плотность смеси.
        /// </summary>
        [JsonPropertyName("rhoMix")]
        public double RhoMix { get; set; }
        
        /// <summary>
        /// Вязкость смеси.
        /// </summary>
        [JsonPropertyName("muMix")]
        public double MuMix { get; set; }
    }
}