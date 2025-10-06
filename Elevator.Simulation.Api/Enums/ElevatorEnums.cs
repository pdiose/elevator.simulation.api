namespace Elevator.Simulation.Api.Enums
{
    public enum ElevatorCallStatus
    {
        Waiting = 1,
        Assigned = 2,
        InProgress = 3,
        Completed = 4
    }

    public enum ElevatorStatus
    {
        Idle = 1,
        Moving = 2,
        Loading = 3,
        Unloading = 4
    }
}
