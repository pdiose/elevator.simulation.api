using Elevator.Simulation.Api.Enums;
using Elevator.Simulation.Api.Models;

namespace Elevator.Simulation.Api.Services
{
    public interface IElevatorService
    {
        SimulationState GetState();
        void UpdateConfiguration(ElevatorConfiguration config);
        void CallElevator(int fromFloor, int toFloor);
        void GenerateRandomCalls(int numberOfCalls);
        void ProcessNextStep();
    }

    public class ElevatorService : IElevatorService
    {
        private readonly SimulationState _state;
        private readonly ILogger<ElevatorService> _logger;
        private int _nextCallId = 1;
        private readonly Random _random = new();

        public ElevatorService(ILogger<ElevatorService> logger)
        {
            _logger = logger;
            _state = new SimulationState
            {
                Configuration = new ElevatorConfiguration(),
                Elevators = new List<ElevatorInfo>()
            };
            //InitializeElevators();
        }

        private void InitializeElevators()
        {
            _state.Elevators.Clear();

            int maxFloor = _state.Configuration.NumberOfFloors;

            for (int i = 0; i < _state.Configuration.NumberOfElevators; i++)
            {
                int startingFloor = 1;

                if (_state.Configuration.RandomElevatorStart)
                {
                    startingFloor = _random.Next(1, maxFloor + 1);
                }

                _state.Elevators.Add(new ElevatorInfo
                {
                    Id = i + 1,
                    CurrentFloor = startingFloor
                });
            }
        }

        public SimulationState GetState() => _state;

        public void UpdateConfiguration(ElevatorConfiguration config)
        {
            _state.Configuration = config;
            InitializeElevators();
            _state.Calls.Clear();
        }

        public void CallElevator(int fromFloor, int toFloor)
        {
            if (fromFloor == toFloor) return;

            var call = new ElevatorCall
            {
                CallId = _nextCallId++,
                FromFloor = fromFloor,
                ToFloor = toFloor,
                CallTime = DateTime.Now,
                Status = ElevatorCallStatus.Waiting
            };

            _state.Calls.Add(call);
            AssignElevatorToCall(call);
        }

        public void GenerateRandomCalls(int numberOfCalls)
        {
            for (int i = 0; i < numberOfCalls; i++)
            {
                var fromFloor = _random.Next(1, _state.Configuration.NumberOfFloors + 1);
                int toFloor;
                do
                {
                    toFloor = _random.Next(1, _state.Configuration.NumberOfFloors + 1);
                } while (toFloor == fromFloor);

                CallElevator(fromFloor, toFloor);
            }
        }

        private void AssignElevatorToCall(ElevatorCall call)
        {
            var availableElevators = _state.Elevators
                .Where(e => e.Status == ElevatorStatus.Idle ||
                           (e.Status == ElevatorStatus.Moving &&
                            IsOnTheWay(e, call.FromFloor, call.ToFloor > call.FromFloor)))
                .ToList();

            if (!availableElevators.Any()) return;

            var bestElevator = availableElevators
                .OrderBy(e => CalculateCost(e, call.FromFloor))
                .First();

            bestElevator.DestinationFloors.Add(call.FromFloor);
            bestElevator.DestinationFloors.Add(call.ToFloor);
            bestElevator.DestinationFloors = bestElevator.DestinationFloors.Distinct().ToList();

            call.Status = ElevatorCallStatus.Assigned;
            call.AssignedElevator = bestElevator.Id;
        }

        private bool IsOnTheWay(ElevatorInfo elevator, int floor, bool goingUp)
        {
            if (elevator.DestinationFloors.Count == 0) return false;

            var currentDirection = elevator.DestinationFloors[0] > elevator.CurrentFloor;
            return currentDirection == goingUp &&
                   ((goingUp && floor >= elevator.CurrentFloor) ||
                    (!goingUp && floor <= elevator.CurrentFloor));
        }

        private int CalculateCost(ElevatorInfo elevator, int callFloor)
        {
            if (elevator.Status == ElevatorStatus.Idle)
                return Math.Abs(elevator.CurrentFloor - callFloor);

            var direction = elevator.DestinationFloors[0] > elevator.CurrentFloor;
            var callDirection = callFloor > elevator.CurrentFloor;

            if (direction == callDirection)
            {
                if (direction && callFloor >= elevator.CurrentFloor)
                    return callFloor - elevator.CurrentFloor;
                if (!direction && callFloor <= elevator.CurrentFloor)
                    return elevator.CurrentFloor - callFloor;
            }

            return Math.Abs(elevator.DestinationFloors.Last() - elevator.CurrentFloor) +
                   Math.Abs(callFloor - elevator.DestinationFloors.Last());
        }

        public void ProcessNextStep()
        {
            foreach (var elevator in _state.Elevators)
            {
                ProcessElevatorMovement(elevator);
            }

            UpdateCallStatuses();
        }

        private void ProcessElevatorMovement(ElevatorInfo elevator)
        {
            if (elevator.TimeRemaining.HasValue && elevator.TimeRemaining > 0)
            {
                elevator.TimeRemaining--;
                return;
            }

            if (!elevator.DestinationFloors.Any())
            {
                elevator.Status = ElevatorStatus.Idle;
                elevator.CurrentAction = "Idle";
                return;
            }

            var nextFloor = elevator.DestinationFloors[0];

            if (elevator.CurrentFloor == nextFloor)
            {
                // At destination - handle loading/unloading
                elevator.DestinationFloors.RemoveAt(0);

                var callsAtThisFloor = _state.Calls.Where(c =>
                    c.AssignedElevator == elevator.Id &&
                    (c.FromFloor == elevator.CurrentFloor || c.ToFloor == elevator.CurrentFloor) &&
                    c.Status != ElevatorCallStatus.Completed).ToList();

                if (callsAtThisFloor.Any(c => c.FromFloor == elevator.CurrentFloor && c.Status == ElevatorCallStatus.Assigned))
                {
                    // Loading passengers
                    elevator.Status = ElevatorStatus.Loading;
                    elevator.CurrentAction = "Loading passengers";
                    elevator.TimeRemaining = _state.Configuration.LoadingTime;

                    foreach (var call in callsAtThisFloor.Where(c => c.FromFloor == elevator.CurrentFloor))
                    {
                        call.Status = ElevatorCallStatus.InProgress;
                    }
                }
                else if (callsAtThisFloor.Any(c => c.ToFloor == elevator.CurrentFloor && c.Status == ElevatorCallStatus.InProgress))
                {
                    // Unloading passengers
                    elevator.Status = ElevatorStatus.Unloading;
                    elevator.CurrentAction = "Unloading passengers";
                    elevator.TimeRemaining = _state.Configuration.LoadingTime;

                    var completedCalls = callsAtThisFloor.Where(c => c.ToFloor == elevator.CurrentFloor).ToList();
                    foreach (var call in completedCalls)
                    {
                        call.Status = ElevatorCallStatus.Completed;
                    }
                }
            }
            else
            {
                // Move towards next floor
                elevator.Status = ElevatorStatus.Moving;
                elevator.CurrentAction = $"Moving to floor {nextFloor}";
                elevator.TimeRemaining = _state.Configuration.TravelTimePerFloor;

                if (elevator.CurrentFloor < nextFloor)
                    elevator.CurrentFloor++;
                else
                    elevator.CurrentFloor--;
            }
        }

        private void UpdateCallStatuses()
        {
            // Reassign unassigned calls
            var unassignedCalls = _state.Calls.Where(c => c.Status == ElevatorCallStatus.Waiting).ToList();
            foreach (var call in unassignedCalls)
            {
                AssignElevatorToCall(call);
            }
        }
    }
}
