using Elevator.Simulation.Api.Enums;
using Elevator.Simulation.Api.Models;

namespace Elevator.Simulation.Api.DTOs
{

    public class ElevatorCallDto
    {
        public int CallId { get; set; }
        public int FromFloor { get; set; }
        public int ToFloor { get; set; }
        public DateTime CallTime { get; set; }
        public ElevatorCallStatus Status { get; set; }
        public string StatusInfo => Status.ToString();
        public int? AssignedElevator { get; set; }
    }

    public class ElevatorInfoDto
    {
        public int Id { get; set; }
        public int CurrentFloor { get; set; } = 1;
        public ElevatorStatus Status { get; set; } = ElevatorStatus.Idle;
        public string StatusInfo => Status.ToString();
        public List<int> DestinationFloors { get; set; } = new();
        public int? CurrentPassengerCount { get; set; }
        public int? TimeRemaining { get; set; }
        public string? CurrentAction { get; set; }
    }

    public class SimulationStateDto
    {
        public List<ElevatorInfoDto> Elevators { get; set; } = new();
        public List<ElevatorCallDto> Calls { get; set; } = new();
        public ElevatorConfiguration Configuration { get; set; } = new();
    }
}
