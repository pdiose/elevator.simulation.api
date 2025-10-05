using Elevator.Simulation.Api.Models;
using Elevator.Simulation.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Elevator.Simulation.Tests
{
    public class ElevatorServiceTests
    {
        private ElevatorService CreateService()
        {
            var logger = NullLogger<ElevatorService>.Instance;
            return new ElevatorService(logger);
        }

        [Fact]
        public void GetState_ShouldReturnInitialState()
        {
            // Arrange
            var service = CreateService();

            // Act
            var state = service.GetState();

            // Assert
            Assert.NotNull(state);
            Assert.NotNull(state.Configuration);
            Assert.NotNull(state.Elevators);
        }

        [Fact]
        public void UpdateConfiguration_ShouldSetNumberOfElevators()
        {
            // Arrange
            var service = CreateService();
            var config = new ElevatorConfiguration
            {
                NumberOfFloors = 5,
                NumberOfElevators = 2,
                TravelTimePerFloor = 1,
                LoadingTime = 1
            };

            // Act
            service.UpdateConfiguration(config);
            var state = service.GetState();

            // Assert
            Assert.Equal(2, state.Elevators.Count);
        }

        [Fact]
        public void UpdateConfiguration_WithRandomStart_ShouldSetRandomElevatorFloors()
        {
            // Arrange
            var service = CreateService();
            var config = new ElevatorConfiguration
            {
                NumberOfFloors = 10,
                NumberOfElevators = 5,
                TravelTimePerFloor = 1,
                LoadingTime = 1,
                RandomElevatorStart = true
            };

            // Act
            service.UpdateConfiguration(config);
            var state = service.GetState();

            // Assert
            Assert.Equal(5, state.Elevators.Count);
            Assert.Contains(state.Elevators, e => e.CurrentFloor != 1);
            Assert.All(state.Elevators, e =>
                Assert.InRange(e.CurrentFloor, 1, config.NumberOfFloors));
        }

        [Fact]
        public void CallElevator_ShouldCreateCall()
        {
            // Arrange
            var service = CreateService();
            var config = new ElevatorConfiguration
            {
                NumberOfFloors = 5,
                NumberOfElevators = 1,
                TravelTimePerFloor = 1,
                LoadingTime = 1
            };
            service.UpdateConfiguration(config);

            // Act
            service.CallElevator(1, 3);
            var state = service.GetState();

            // Assert
            Assert.Single(state.Calls);
            Assert.Equal(1, state.Calls[0].FromFloor);
            Assert.Equal(3, state.Calls[0].ToFloor);
        }

        [Fact]
        public void GenerateRandomCalls_ShouldAddCalls()
        {
            // Arrange
            var service = CreateService();
            var config = new ElevatorConfiguration
            {
                NumberOfFloors = 5,
                NumberOfElevators = 1,
                TravelTimePerFloor = 1,
                LoadingTime = 1
            };
            service.UpdateConfiguration(config);

            // Act
            service.GenerateRandomCalls(3);
            var state = service.GetState();

            // Assert
            Assert.Equal(3, state.Calls.Count);
        }

        [Fact]
        public void ProcessNextStep_ShouldMoveElevator()
        {
            // Arrange
            var service = CreateService();
            var config = new ElevatorConfiguration
            {
                NumberOfFloors = 5,
                NumberOfElevators = 1,
                TravelTimePerFloor = 1,
                LoadingTime = 1,
                RandomElevatorStart = false
            };
            service.UpdateConfiguration(config);

            var state = service.GetState();
            var elevator = state.Elevators[0];
            elevator.CurrentFloor = 2;

            service.CallElevator(1, 3);
            int initialFloor = elevator.CurrentFloor;

            for (int i = 0; i < 5; i++)
            {
                service.ProcessNextStep();
                if (elevator.CurrentFloor != initialFloor)
                    break;
            }

            // Assert
            Assert.NotEqual(initialFloor, elevator.CurrentFloor);
        }
    }
}
