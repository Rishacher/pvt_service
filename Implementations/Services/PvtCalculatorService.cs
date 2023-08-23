using Abstractions;

namespace PvtService.Services;

public class PvtCalculationService : IPvtCalculationService
{
    double C1 = 1.9243101395421235 * 1e-6;
    double C2 = 1.2048192771084338;
    double C3 = 1.2254503;
    double C4 = 0.001638;
    double C5 = 1.76875;

    double KtoF(double temp_K)
    {
        double temp_F = (temp_K - 273.15) * 9 / 5 + 32;
        return temp_F;
    }

    double MMtoFB(double MM)
    {
        double FB = MM / 0.17810760667903522;
        return FB;
    }

    double PascaltoPSI(double pres)
    {
        double pressPSI = pres * 1.45 / 1e4;
        return pressPSI;
    }


    public MixProperties CalculateMixProperties(PvtParams pvtParams)
    {
        double rho_wat = 1000;
        double mu_wat = 1;
        double r_s = _calc_r_s(pvtParams.GammaGas, pvtParams.P, pvtParams.T, pvtParams.GammaOil);

        double rho_oil, v_oil, mu_oil, b_oil;
        if (pvtParams.Rp < r_s)
        {
            (rho_oil, v_oil, mu_oil, b_oil) = _calc_unsaturated(pvtParams.Rp, pvtParams.T, pvtParams.GammaOil,
                pvtParams.GammaGas, pvtParams.P, pvtParams.QLiq, pvtParams.Wct);
        }
        else
            (rho_oil, v_oil, mu_oil, b_oil) = _calc_saturated(r_s, pvtParams.GammaGas, pvtParams.GammaOil, pvtParams.T,
                pvtParams.QLiq, pvtParams.Wct);


        double b_g = _calc_b_g(pvtParams.T, pvtParams.P);

        double rho_gas = 28.97 * pvtParams.GammaGas / (24.0422057735011 * b_g);
        double scaled_temp = 1.8 * pvtParams.T;
        double b = 2.57 + 1914.5 / scaled_temp + 0.275 * pvtParams.GammaGas;
        double mu_gas = 1e-4 * (7.77 + 0.183 * pvtParams.GammaGas) *
                        Math.Pow(scaled_temp, 1.5)
                        / (122.4 + 373.6 * pvtParams.GammaGas + scaled_temp)
                        * Math.Exp(b * Math.Pow(rho_gas / 1e3, 1.11 + 0.04 * b));

        double v_oil_cons = pvtParams.QLiq * (1 - pvtParams.Wct);

        double v_gas = pvtParams.Rp < r_s ? 0 : b_g * v_oil_cons * (pvtParams.Rp - r_s);
        double v_wat = pvtParams.QLiq * pvtParams.Wct;

        double wct_context = v_wat / (v_oil + v_wat);
        double rho_liq = lerp(rho_wat, rho_oil, wct_context);
        double mu_liq = lerp(mu_wat, mu_oil, wct_context);
        double v_liq = v_oil + v_wat;

        double gas_fraction = v_gas / (v_liq + v_gas);
        double rho_mix = lerp(rho_gas, rho_liq, gas_fraction);
        double mu_mix = lerp(mu_gas, mu_liq, gas_fraction);
        double v_mix = v_liq + v_gas;

        return new MixProperties { MuMix = mu_mix, QMix = v_mix / 3600 / 24, RhoMix = rho_mix };
    }

    (double, double, double, double) _calc_saturated(double r_s, double gamma_gas, double gamma_oil, double temp,
        double q_liq, double wct)
    {
        double b_oil = _calc_b_bpp(r_s, gamma_gas, gamma_oil, temp); //check bpp

        double rho_oil = _calc_rho_oil(gamma_oil, r_s, gamma_gas, b_oil);
        double v_oil_cons = q_liq * (1 - wct); //# v
        double v_oil = v_oil_cons * b_oil;

        double gamma_oil_api = _calc_gamma_oil_api(gamma_oil);
        //# 295F
        if (KtoF(temp) <= 295 && gamma_oil_api <= 58)
        {
            double mu_live = _calc_mu_live(temp, gamma_oil, r_s);
            return (rho_oil, v_oil, mu_live, b_oil);
        }
        else
        {
            return (0, 0, 0, 0);
        }
    }

    (double, double, double, double) _calc_unsaturated(double gor, double temp, double gamma_oil, double gamma_gas,
        double pressure, double q_liq, double wct)
    {
        double r_sb = gor;
        double y_g = _calc_y_g(temp, gamma_oil);
        double p_bp_pa = (
            Math.Pow(10, y_g)
            / (1.9243101395421235 * 1e-6)
            * Math.Pow((r_sb / gamma_gas), (1 / 1.2048192771084338))
        );
        double b_bpp = _calc_b_bpp(r_sb, gamma_gas, gamma_oil, temp);
        double gamma_oil_api = _calc_gamma_oil_api(gamma_oil);
        double temp_F = KtoF(temp);
        double A = (
            -1.433
            + 5 * r_sb
            + 17.2 * temp_F
            - 1.180 * gamma_gas
            + 12.61 * gamma_oil_api
        ) / 1e+5;
        double p_bp = PascaltoPSI(p_bp_pa);
        double C = (A / p_bp);
        double pres_PSI = PascaltoPSI(pressure);
        double b_oil = b_bpp * Math.Exp(C * (p_bp - pres_PSI));

        double rho_oil = _calc_rho_oil(gamma_oil, gor, gamma_gas, b_oil);
        double v_oil_cons = q_liq * (1 - wct); //  # v
        double v_oil = v_oil_cons * b_oil;

        if (KtoF(temp) <= 295 && gamma_oil_api <= 58)
        {
            double mu_live = _calc_mu_live(temp, gamma_oil, gor);

            return (rho_oil, v_oil, mu_live, b_oil);
        }
        else
        {
            return (0, 0, 0, 0);
        }
    }

    double _calc_gamma_oil_api(double gamma_oil)
    {
        return (141.5 / gamma_oil) - 131.5;
    }

    double _calc_D(double temp, double gamma_oil)
    {
        return (Math.Pow(temp, -1.163)) * Math.Pow(10, (3.0324 - 0.02023 * _calc_gamma_oil_api(gamma_oil)));
    }

    double _calc_y_g(double temp, double gamma_oil)
    {
        return C3 + C4 * temp - C5 / gamma_oil;
    }

    double _calc_b_bpp(double gas_sat, double gamma_gas, double gamma_oil, double temp)
    {
        return (
            0.972
            + 147e-6
            * Math.Pow((
                    5053125 / 900000 * gas_sat * Math.Sqrt(gamma_gas / gamma_oil)
                    + 2.25 * temp
                    - 574.5875
                )
                , 1.175)
        );
    }

    double _calc_rho_oil(double gamma_oil, double gas_sat, double gamma_gas, double b_oil)
    {
        return 1e3 * ((gamma_oil + 1.2217e-3 * gas_sat * gamma_gas) / b_oil);
    }

    double _calc_mu_live(double temp, double gamma_oil, double gas_sat)
    {
        //# 70F
        double temp_F = KtoF(temp);
        double mu_dead;
        if (temp_F > 70)
        {
            double D = _calc_D(temp_F, gamma_oil);
            mu_dead = Math.Pow(10, D - 1);
        }
        else
        {
            double D_80 = _calc_D(80, gamma_oil);
            double D_70 = _calc_D(70, gamma_oil);

            double mu_oil_80 = Math.Pow(10, D_80 - 1);
            double mu_oil_70 = Math.Pow(10, D_70 - 1);

            double L_seven_eighth = Math.Log10(8 / 7);

            double L_mu = Math.Log10(mu_oil_70 / mu_oil_80);

            double c = L_mu / L_seven_eighth;

            double b = Math.Pow(70, c * mu_oil_70);

            double D = Math.Log10(b) - c * Math.Log10(temp_F);

            mu_dead = Math.Pow(10, D);
        }

        double gas_sat_fb = MMtoFB(gas_sat);
        double mu_live = (
            10.715
            * Math.Pow((gas_sat_fb + 100), -0.515)
            * Math.Pow(mu_dead, (5.44 * Math.Pow((gas_sat_fb + 150), -0.338)))
        );
        return mu_live;
    }

    double _calc_r_s(double gamma_gas, double pressure, double temp, double gamma_oil)
    {
        double y_g = _calc_y_g(temp, gamma_oil);

        return gamma_gas * Math.Pow((C1 * pressure / Math.Pow(10, y_g)), C2);
    }

    double _calc_b_g(double temp, double pressure, double z = 1)
    {
        return 359.958 * z * temp / pressure;
    }

    double lerp(double min, double max, double t)
    {
        return max * (1 - t) + min * t;
    }
}