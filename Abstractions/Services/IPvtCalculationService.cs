namespace Abstractions
{
    /// <summary>
    /// Сервис рассчета PVT
    /// </summary>
    public interface IPvtCalculationService
    {
        
        /// <summary>
        /// Рассчет свойств смеси
        /// </summary>
        /// <param name="pvtParams"></param>
        /// <returns></returns>
        public MixProperties CalculateMixProperties(PvtParams pvtParams);
    }
}