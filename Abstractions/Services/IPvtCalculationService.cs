namespace Abstractions
{
    public interface IPvtCalculationService
    {
        public MixProperties CalculateMixProperties(PvtParams pvtParams);
    }
}