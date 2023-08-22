namespace Abstractions
{
    /// <summary>
    /// Параметры PVT для рассчета свойств смеси.
    /// </summary>
    public class PvtParams
    {
        /// <summary>
        /// Давление.
        /// </summary>
        public double P { get; set; }
        
        /// <summary>
        /// Температора.
        /// </summary>
        public double T { get; set; }
        
        /// <summary>
        /// Отн. плотность нефти.
        /// </summary>
        public double GammaOil { get; set; }
        
        /// <summary>
        /// Отн. плотность газа.
        /// </summary>
        public double GammaGas { get; set; }
        
        /// <summary>
        /// Отн. плотность воды.
        /// </summary>
        public double GammaWat { get; set; }
        
        /// <summary>
        /// Обводненность.
        /// </summary>
        public double Wct { get; set; }
        
        /// <summary>
        /// Газовый фактор.
        /// </summary>
        public double Rp { get; set; }
        
        /// <summary>
        /// Расход жидкости.
        /// </summary>
        public double QLiq { get; set; }
    }
}