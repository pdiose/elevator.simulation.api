using Elevator.Simulation.Api.Data;
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
        private readonly ElevatorDbContext _dbContext;
        private readonly ILogger<ElevatorService> _logger;
        private int _nextCallId = 1;
        private readonly Random _random = new();

        public ElevatorService(ElevatorDbContext dbContext, ILogger<ElevatorService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
            _state = new SimulationState
            {
                Configuration = new ElevatorConfiguration(),
                Elevators = new List<ElevatorInfo>()
            };
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
            _nextCallId = 1;
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

            _dbContext.ElevatorCalls.Add(call);
            _dbContext.SaveChanges();
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
            // Ensure `call` is tracked by the context
            var trackedCall = _dbContext.ElevatorCalls
                .FirstOrDefault(c => c.Id == call.Id);

            if (trackedCall == null)
            {
                // Option: attach
                _dbContext.ElevatorCalls.Attach(call);
                trackedCall = call;
            }

            // 1. Fetch elevators from the database
            var elevatorEntities = _dbContext.ElevatorInfos
                .Where(e => e.Status == ElevatorStatus.Idle || e.Status == ElevatorStatus.Moving)
                .ToList(); // bring into memory

            // 2. Filter with IsOnTheWay
            var availableElevators = elevatorEntities
                .Where(e => IsOnTheWay(
                    elevator: e,
                    floor: call.FromFloor,
                    goingUp: call.ToFloor > call.FromFloor))
                .ToList();

            if (!availableElevators.Any()) return;

            // 3. Pick the best elevator
            var bestElevator = availableElevators
                .OrderBy(e => CalculateCost(e, call.FromFloor))
                .First();

            // 4. Update the elevator’s destination list
            bestElevator.DestinationFloors.Add(call.FromFloor);
            bestElevator.DestinationFloors.Add(call.ToFloor);
            bestElevator.DestinationFloors = bestElevator.DestinationFloors.Distinct().ToList();

            // 5. Update call assignment
            trackedCall.Status = ElevatorCallStatus.Assigned;
            trackedCall.AssignedElevator = bestElevator.Id;

            // 6. Save changes
            _dbContext.SaveChanges();
        }

        private bool IsOnTheWay(ElevatorInfo elevator, int floor, bool goingUp)
        {
            if (elevator.DestinationFloors == null || elevator.DestinationFloors.Count == 0)
                return false;

            var possible = elevator.DestinationFloors
                .Where(d => goingUp ? d >= elevator.CurrentFloor : d <= elevator.CurrentFloor)
                .ToList();
            if (!possible.Any())
                return false;

            int next = possible
                .OrderBy(d => Math.Abs(d - elevator.CurrentFloor))
                .First();

            bool currentDirection = next > elevator.CurrentFloor;

            if (currentDirection != goingUp)
                return false;

            if (goingUp)
            {
                return floor >= elevator.CurrentFloor && floor <= next;
            }
            else
            {
                return floor <= elevator.CurrentFloor && floor >= next;
            }
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

            var activeCalls = _state.Calls
                .Where(c => c.AssignedElevator == elevator.Id && c.Status != ElevatorCallStatus.Completed)
                .ToList();

            bool hasActiveCalls = activeCalls.Any();

            if (!elevator.DestinationFloors.Any() && !hasActiveCalls)
            {
                elevator.Status = ElevatorStatus.Idle;
                elevator.CurrentAction = $"Idle at floor {elevator.CurrentFloor}";
                return;
            }

            if (!elevator.DestinationFloors.Any() && hasActiveCalls)
            {
                elevator.DestinationFloors = activeCalls
                    .SelectMany(c => new[] { c.FromFloor, c.ToFloor })
                    .Distinct()
                    .ToList();
            }

            var nextFloor = elevator.DestinationFloors.First();

            if (elevator.CurrentFloor == nextFloor)
            {
                var callsAtThisFloor = _state.Calls
                    .Where(c => c.AssignedElevator == elevator.Id &&
                                (c.FromFloor == elevator.CurrentFloor || c.ToFloor == elevator.CurrentFloor) &&
                                c.Status != ElevatorCallStatus.Completed)
                    .ToList();

                bool hasUnloading = callsAtThisFloor.Any(c =>
                    c.ToFloor == elevator.CurrentFloor && c.Status == ElevatorCallStatus.InProgress);

                bool hasLoading = callsAtThisFloor.Any(c =>
                    c.FromFloor == elevator.CurrentFloor && c.Status == ElevatorCallStatus.Assigned);

                int fullTime = _state.Configuration.LoadingTime;
                int halfTime = fullTime / 2;

                if (hasUnloading && hasLoading)
                {
                    if (elevator.CurrentAction != "Unloading passengers (1/2)" &&
                        elevator.CurrentAction != "Loading passengers (2/2)")
                    {
                        foreach (var call in callsAtThisFloor.Where(c =>
                            c.ToFloor == elevator.CurrentFloor && c.Status == ElevatorCallStatus.InProgress))
                            call.Status = ElevatorCallStatus.Completed;

                        elevator.Status = ElevatorStatus.Unloading;
                        elevator.CurrentAction = "Unloading passengers (1/2)";
                        elevator.TimeRemaining = halfTime;
                        return;
                    }

                    if (elevator.CurrentAction == "Unloading passengers (1/2)")
                    {
                        foreach (var call in callsAtThisFloor.Where(c =>
                            c.FromFloor == elevator.CurrentFloor && c.Status == ElevatorCallStatus.Assigned))
                            call.Status = ElevatorCallStatus.InProgress;

                        elevator.Status = ElevatorStatus.Loading;
                        elevator.CurrentAction = "Loading passengers (2/2)";
                        elevator.TimeRemaining = halfTime;
                        return;
                    }
                }

                if (hasLoading)
                {
                    if (elevator.CurrentAction == "Unloading passengers (1/2)")
                    {
                        foreach (var call in callsAtThisFloor.Where(c =>
                            c.FromFloor == elevator.CurrentFloor && c.Status == ElevatorCallStatus.Assigned))
                            call.Status = ElevatorCallStatus.InProgress;

                        elevator.Status = ElevatorStatus.Loading;
                        elevator.CurrentAction = "Loading passengers (2/2)";
                        elevator.TimeRemaining = halfTime;
                        return;
                    }
                    else
                    {
                        foreach (var call in callsAtThisFloor.Where(c =>
                            c.FromFloor == elevator.CurrentFloor && c.Status == ElevatorCallStatus.Assigned))
                            call.Status = ElevatorCallStatus.InProgress;

                        elevator.Status = ElevatorStatus.Loading;
                        elevator.CurrentAction = "Loading passengers";
                        elevator.TimeRemaining = fullTime;
                        return;
                    }
                        
                }

                if (hasUnloading)
                {
                    foreach (var call in callsAtThisFloor.Where(c =>
                            c.ToFloor == elevator.CurrentFloor && c.Status == ElevatorCallStatus.InProgress))
                        call.Status = ElevatorCallStatus.Completed;

                    elevator.Status = ElevatorStatus.Unloading;
                    elevator.CurrentAction = "Unloading passengers";
                    elevator.TimeRemaining = fullTime;
                    return;
                }

                elevator.DestinationFloors.RemoveAt(0);
                return;
            }
            else
            {
                elevator.Status = ElevatorStatus.Moving;
                elevator.CurrentAction = $"Moving to floor {nextFloor}";
                elevator.TimeRemaining = _state.Configuration.TravelTimePerFloor;
                elevator.CurrentFloor += elevator.CurrentFloor < nextFloor ? 1 : -1;
                return;
            }
        }


        private void UpdateCallStatuses()
        {
            var unassignedCalls = _state.Calls.Where(c => c.Status == ElevatorCallStatus.Waiting).ToList();
            foreach (var call in unassignedCalls)
            {
                AssignElevatorToCall(call);
            }
        }
    }
}
