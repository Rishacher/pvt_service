using System.Security.Cryptography.Xml;
using Abstractions;

namespace pvt_service.Services;

public class PvtCalculatorService : IPvtCalculationService
{
    private const double c1 = 1.9243101395421235 * 1e-6;
    private const double c2 = 1.2048192771084338;
    private const double c3 = 1.2254503;
    private const double c4 = 0.001638;
    private const double c5 = 1.76875;
    private const double c6 = 5.6145833333333333;
    private const double c7 = 574.5875;
    private const double c8 = 24.04220577350111;

    public MixProperties CalculateMixProperties(PvtParams pvtParams)
    {
        // Расчет газосодержания.
        var rs = GetGasSaturation(pvtParams);

        // Плотность, объем, вязкость нефти.
        var (rhoOil, vOil, muLive) = pvtParams.Rp < rs
            ? GetNotSaturatedOil(pvtParams)
            : GetSaturatedOil(pvtParams);

        // Объемный коэффициент газа.
        var bg = 350.958 * pvtParams.T / pvtParams.P;

        // Плотность газа;
        var rhoGas = 28.97 * pvtParams.GammaGas / (c8 * bg);

        // Вязкость газа.
        var b = 2.57 + 1914.5 / 1.8 * pvtParams.T + 0.275 * pvtParams.GammaGas;
        var muGas = 1e-4 * (7.77 + 0.183 * pvtParams.GammaGas)
                         * (Math.Pow(1.8 * pvtParams.T, 1.5)
                             / 122.4 + 373.6 * pvtParams.GammaGas + 1.8 * pvtParams.T
                         )
                         * Math.Exp(b * Math.Pow(rhoGas / 1000, 1.11 + 0.04 * b));

        // Объем газа.
        double vGas = 0;
        if (pvtParams.Rp < rs)
        {
            vGas = 0;
        }
        else
        {
            var vOilLiq = pvtParams.QLiq * (1 - pvtParams.Wct );
            vGas = bg * vOilLiq * (pvtParams.Rp - rs);
        }

        // Обводненность.
        var vWat = pvtParams.QLiq * pvtParams.Wct ;
        var WCT = vWat / (vOil + vWat);

        // Плотность жидкости.
        var rhoWat = 1000;
        var rhoLiq = rhoOil * (1 - WCT) + rhoWat * WCT;

        // Вязкость жидкости.
        var muWat = 1;
        var muLiq = muLive / 1000 * (1 - WCT) + muWat / 1000 * WCT;

        // Объем жидкости.
        var vLiq = vOil + vWat;

        // Газовая фракцию.
        var GF = vGas / (vLiq + vGas);

        // Плотность смеси.
        var rhoMix = rhoLiq * (1 - GF) + rhoGas * GF;

        // Вязкость смеси.
        var muMix = muLiq * (1 - GF) + muGas / 1000 * GF;

        // Расход смеси.
        var vMix = vLiq + vGas;

        return new MixProperties
        {
            QMix = vMix,
            RhoMix = rhoMix,
            MuMix = muMix,
        };
    }

    private double TransformCelsiusToFahrenheit(double c) =>
        1.8 * (c - 273.15) + 32;

    private double TranslatePascalToPsi(double pascal) =>
        1.4504 * 1e-4 * pascal;

    private double TransformGammaOilToDegrees(double gammaOil) =>
        141.5 / gammaOil - 131.5;

    private double TransformGasToCB(double gas) =>
        gas / 0.17810760667903522;

    private double GetPower(PvtParams pvtParams) =>
        c3 + c4 * pvtParams.T - c5 / pvtParams.GammaOil;

    private double GetGasSaturation(PvtParams pvtParams) =>
        pvtParams.GammaGas
        * Math.Pow(
            c1 * pvtParams.P
            / Math.Pow(10, GetPower(pvtParams)), c2
        );

    private (double, double, double) GetSaturatedOil(PvtParams pvtParams)
    {
        var tk = pvtParams.T;
        var tf = TransformCelsiusToFahrenheit(pvtParams.T);
        var p = pvtParams.P;
        var oilApi = TransformGammaOilToDegrees(pvtParams.GammaOil);

        // Газосодержание.
        var rs = pvtParams.GammaGas * Math.Pow(c1 * p / Math.Pow(10, GetPower(pvtParams)), c2);

        // Объемный коэффициент нефти в точке давления насыщения нефти. 
        var boil = 0.972 + 147 / 1e6
            * Math.Pow(c6 * rs * Math.Pow(pvtParams.GammaGas / pvtParams.GammaOil, 0.5) + 2.25 * tk - c7, 1.175);

        // Плотность нефти в рассматриваемых условиях.
        var rhoOil = (1000 * pvtParams.GammaOil + 1.2217 * rs * pvtParams.GammaGas) / boil;

        // Объем нефти в заданных условиях.
        var voilLiq = pvtParams.QLiq * (1 - pvtParams.Wct );
        var voil = voilLiq * boil;

        double muDead = 0;
        if (tf <= 295 && oilApi <= 58)
        {
            if (tf > 70)
            {
                var d = Math.Pow(tf, -1.163) * Math.Pow(10, 3.0324 - 0.02023 * oilApi);

                muDead = Math.Pow(10, d) - 1;
            }
            else
            {
                var d70 = Math.Pow(70, -1.163) * Math.Pow(10, 3.0324 - 0.02023 * oilApi);
                var muOil70 = Math.Pow(10, d70) - 1;

                var d80 = Math.Pow(80, -1.163) * Math.Pow(10, 3.0324 - 0.02023 * oilApi);
                var muOil80 = Math.Pow(10, d80) - 1;

                var lmu = Math.Log10(muOil70 / muOil80);
                var l78 = Math.Log10((double)8 / 7);

                var c = lmu / l78;
                var b = Math.Pow(70, c) * muOil70;
                var d = Math.Log10(b) - c * Math.Log10(tf);
                muDead = Math.Pow(10, d);
            }
        }

        // Вязкость насыщенной нефти.
        var muLive = 10.715 * Math.Pow(TransformGasToCB(rs) + 100, -0.515) *
                     Math.Pow(muDead, 5.44 * Math.Pow(TransformGasToCB(rs) + 150, -0.338));
        return (rhoOil, voil, muLive);
    }

    private (double, double, double) GetNotSaturatedOil(PvtParams pvtParams)
    {
        var tk = pvtParams.T;
        var tf = TransformCelsiusToFahrenheit(tk);
        var p = pvtParams.P;
        var oilApi = TransformGammaOilToDegrees(pvtParams.GammaOil);

        // Газосодержание.
        var rsb = pvtParams.Rp;

        // Давление насыщения.
        var pbp = Math.Pow(10, GetPower(pvtParams)) / c1 * Math.Pow(rsb / pvtParams.GammaGas, 1 / c2);

        // Объемный коэффициент нефти в точке давления насыщения нефти. 
        var bbpp = 0.972 + 147 / 1e6
            * Math.Pow(c6 * rsb * Math.Pow(pvtParams.GammaGas / pvtParams.GammaOil, 0.5) + 2.25 * tk - c7, 1.175);

        // Объемный коэффициент при заданном давлении.
        var a = (-1.433 / 1e5) + (5 / 1e5) * rsb + (17.2 / 1e5) * tf +
                (-1.180 / 1e5) * pvtParams.GammaGas + (12.61 / 1e5) * oilApi;
        var boil = bbpp * Math.Exp((a / TranslatePascalToPsi(pbp)) * (TranslatePascalToPsi(pbp) - TranslatePascalToPsi(p)));

        // Плотность нефти в рассматриваемых условиях.
        var rhoOil = (1000 * pvtParams.GammaOil + 1.2217 * rsb * pvtParams.GammaGas) / boil;

        // Объем ненеасыщенной нефти.
        var voilLiq = pvtParams.QLiq * (1 - pvtParams.Wct );
        var voil = voilLiq * boil;

        // Вязкость дегазированной нефти.
        double muDead = 0;
        if (tf <= 295 && oilApi <= 58)
        {
            if (tf > 70)
            {
                var d = Math.Pow(tf, -1.163) * Math.Pow(10, 3.0324 - 0.02023 * oilApi);

                muDead = Math.Pow(10, d) - 1;
            }
            else
            {
                var d70 = Math.Pow(70, -1.163) * Math.Pow(10, 3.0324 - 0.02023 * oilApi);
                var muOil70 = Math.Pow(10, d70) - 1;

                var d80 = Math.Pow(80, -1.163) * Math.Pow(10, 3.0324 - 0.02023 * oilApi);
                var muOil80 = Math.Pow(10, d80) - 1;

                var lmu = Math.Log10(muOil70 / muOil80);
                var l78 = Math.Log10((double)8 / 7);

                var c = lmu / l78;
                var b = Math.Pow(70, c) * muOil70;
                var d = Math.Log10(b) - c * Math.Log10(tf);
                muDead = Math.Pow(10, d);
            }
        }

        // Вязкость насыщенной нефти.
        var muLive = 10.715 * Math.Pow(TransformGasToCB(rsb) + 100, -0.515) *
                     Math.Pow(muDead, 5.44 * Math.Pow(rsb + 150, -0.338));
        return (rhoOil, voil, muLive);
    }
}