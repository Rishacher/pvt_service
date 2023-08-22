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
        public double QMix { get; set; }
        
        /// <summary>
        /// Плотность смеси.
        /// </summary>
        public double RhoMix { get; set; }
        
        /// <summary>
        /// Вязкость смеси.
        /// </summary>
        public double MuMix { get; set; }
    }
}