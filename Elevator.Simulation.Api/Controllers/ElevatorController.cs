using Microsoft.AspNetCore.Mvc;
using Elevator.Simulation.Api.Models;
using Elevator.Simulation.Api.Services;

namespace Elevator.Simulation.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ElevatorController : ControllerBase
    {
        private readonly IElevatorService _elevatorService;

        public ElevatorController(IElevatorService elevatorService)
        {
            _elevatorService = elevatorService;
        }

        [HttpPost("reset")]
        public IActionResult ResetElevators()
        {
            _elevatorService.ResetElevators();
            return Ok(_elevatorService.GetState());
        }

        [HttpGet("state")]
        public IActionResult GetState()
        {
            return Ok(_elevatorService.GetState());
        }

        [HttpPost("configuration")]
        public IActionResult UpdateConfiguration([FromBody] ElevatorConfiguration config)
        {
            _elevatorService.UpdateConfiguration(config);
            return Ok(_elevatorService.GetState());
        }

        [HttpPost("call")]
        public IActionResult CallElevator([FromBody] ElevatorCallRequest request)
        {
            _elevatorService.CallElevator(request.FromFloor, request.ToFloor);
            return Ok(_elevatorService.GetState());
        }

        [HttpPost("random-calls")]
        public IActionResult GenerateRandomCalls([FromBody] RandomCallRequest request)
        {
            _elevatorService.GenerateRandomCalls(request.NumberOfCalls);
            return Ok(_elevatorService.GetState());
        }

        [HttpPost("step")]
        public IActionResult ProcessStep()
        {
            _elevatorService.ProcessNextStep();
            return Ok(_elevatorService.GetState());
        }
    }
}
