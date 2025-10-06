using Microsoft.AspNetCore.Mvc;
using Elevator.Simulation.Api.Services;
using Elevator.Simulation.Api.DTOs;
using Elevator.Simulation.Api.Models;

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

        [HttpGet("state")]
        public IActionResult GetState()
        {
            var state = _elevatorService.GetState();

            var dto = new
            {
                Elevators = state.Elevators.Select(e => new ElevatorInfoDto
                {
                    Id = e.Id,
                    CurrentFloor = e.CurrentFloor,
                    Status = e.Status,
                    DestinationFloors = e.DestinationFloors,
                    CurrentPassengerCount = e.CurrentPassengerCount,
                    TimeRemaining = e.TimeRemaining,
                    CurrentAction = e.CurrentAction
                }),
                Calls = state.Calls.Select(c => new ElevatorCallDto
                {
                    CallId = c.CallId,
                    FromFloor = c.FromFloor,
                    ToFloor = c.ToFloor,
                    CallTime = c.CallTime,
                    Status = c.Status,
                    AssignedElevator = c.AssignedElevator
                }),
                Configuration = state.Configuration ?? new ElevatorConfiguration()
            };

            return Ok(dto);
        }

        [HttpPost("configuration")]
        public IActionResult UpdateConfiguration([FromBody] ElevatorConfiguration config)
        {
            _elevatorService.UpdateConfiguration(config);
            return GetState();
        }

        [HttpPost("call")]
        public IActionResult CallElevator([FromBody] ElevatorCallRequest request)
        {
            _elevatorService.CallElevator(request.FromFloor, request.ToFloor);
            return GetState();
        }

        [HttpPost("random-calls")]
        public IActionResult GenerateRandomCalls([FromBody] RandomCallRequest request)
        {
            _elevatorService.GenerateRandomCalls(request.NumberOfCalls);
            return GetState();
        }

        [HttpPost("step")]
        public IActionResult ProcessStep()
        {
            _elevatorService.ProcessNextStep();
            return GetState();
        }
    }
}
