using Elevator.Simulation.Api.Enums;

namespace Elevator.Simulation.Api.Models
{
    public class ElevatorConfiguration
    {
        public int NumberOfFloors { get; set; } = 10;
        public int NumberOfElevators { get; set; } = 4;
        public int TravelTimePerFloor { get; set; } = 10;
        public int LoadingTime { get; set; } = 10;
        public bool RandomElevatorStart { get; set; } = false;
    }

    public class ElevatorCall
    {
        public int CallId { get; set; }
        public int FromFloor { get; set; }
        public int ToFloor { get; set; }
        public DateTime CallTime { get; set; }
        public ElevatorCallStatus Status { get; set; }
        public string StatusInfo => Status.ToString();
        public int? AssignedElevator { get; set; }
    }

    public class ElevatorInfo
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

    public class ElevatorCallRequest
    {
        public int FromFloor { get; set; }
        public int ToFloor { get; set; }
    }

    public class SimulationState
    {
        public List<ElevatorInfo> Elevators { get; set; } = new();
        public List<ElevatorCall> Calls { get; set; } = new();
        public ElevatorConfiguration Configuration { get; set; } = new();
    }

    public class RandomCallRequest
    {
        public int NumberOfCalls { get; set; }
    }
}
