using Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace pvt_service.Controllers
{
    [ApiController]
    public class PvtController : ControllerBase
    {
        private readonly ILogger<PvtController> _logger;
        private readonly IPvtCalculationService _pvtCalculationService;

        public PvtController(ILogger<PvtController> logger, IPvtCalculationService pvtCalculationService)
        {
            _logger = logger;
            _pvtCalculationService = pvtCalculationService;
        }

        /// <summary>
        /// Рассчет свойств смеси на основе параметров PVT.
        /// </summary>
        /// <param name="pvtParams">параметры PVT</param>
        /// <returns>рассчитанные свойства смеси</returns>
        [Route("calculator")]
        [HttpPost]
        public MixProperties Calculator([FromBody] PvtParams pvtParams) =>
            _pvtCalculationService.CalculateMixProperties(pvtParams);
    }
}