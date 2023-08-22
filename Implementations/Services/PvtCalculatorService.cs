using Abstractions;

namespace pvt_service.Services;

public class PvtCalculatorService : IPvtCalculationService
{
    private const double c1 = 1.9243101395421235 * 1e-6;
    private const double c2 = 1.2048192771084338;
    private const double c3 = 1.2254503;
    private const double c4 = 0.001638;
    private const double c5 = 1.76875;

    public MixProperties CalculateMixProperties(PvtParams pvtParams)
    {
        var Rs = GetGasSaturation(pvtParams);
        if (pvtParams.Rp < Rs)
        {
            var Rsb = pvtParams.Rp;
            var Pbp = Math.Pow(10, c3 + c4 * pvtParams.T - c5 / pvtParams.GammaOil) / c1 *
                      Math.Pow(Rsb / pvtParams.GammaGas, 1 / c2);
        }

        return new MixProperties();
    }

    private double TransformBarToPascal(double value) =>
        value * 1e5;

    private double GetGasSaturation(PvtParams pvtParams) =>
        pvtParams.GammaGas *
        Math.Pow(c1 * TransformBarToPascal(pvtParams.P) / Math.Pow(10, c3 + c4 * pvtParams.T - c5 / pvtParams.GammaOil),
            c2);

    private double GetNotSaturatedOil(PvtParams pvtParams)
    {
        var Rsb = pvtParams.Rp;
        return 0;
    }
}