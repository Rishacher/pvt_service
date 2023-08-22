using Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace pvt_service.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class PvtCalculatorController : ControllerBase
    {
        private readonly ILogger<PvtCalculatorController> _logger;
        private readonly IPvtCalculationService _pvtCalculationService;

        public PvtCalculatorController(ILogger<PvtCalculatorController> logger, IPvtCalculationService pvtCalculationService)
        {
            _logger = logger;
            _pvtCalculationService = pvtCalculationService;
        }

        /// <summary>
        /// Рассчет свойств смеси на основе параметров PVT.
        /// </summary>
        /// <param name="pvtParams">параметры PVT</param>
        /// <returns>рассчитанные свойства смеси</returns>
        [HttpPost(Name = "calculator")]
        public MixProperties Calculate([FromBody] PvtParams pvtParams)
        {
            return new MixProperties()
            {
                MuMix = 0.8,
                QMix = 0.8,
                RhoMix = 0.8,
            };
        }
            
            // _pvtCalculationService.CalculateMixProperties(pvtParams);
    }
}