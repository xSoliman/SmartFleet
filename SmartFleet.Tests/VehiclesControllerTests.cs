using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SmartFleet.Controllers;
using SmartFleet.Data;
using SmartFleet.Models;
using SmartFleet.Services;
using SmartFleet.Services.Interfaces;
using Xunit;
using FluentAssertions;
using System.Security.Claims;

namespace SmartFleet.Tests
{
    public class VehiclesControllerTests : IDisposable
    {
        private readonly SmartFleetContext _context;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly Mock<IUserRoleService> _mockUserRoleService;
        private readonly Mock<IPaginationService> _mockPaginationService;
        private readonly Mock<ISearchService> _mockSearchService;
        private readonly Mock<INotificationService> _mockNotificationService;
        private readonly VehiclesController _controller;
        private readonly ApplicationUser _testUser;

        public VehiclesControllerTests()
        {
            var options = new DbContextOptionsBuilder<SmartFleetContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new SmartFleetContext(options);

            _mockUserManager = CreateMockUserManager();
            _mockUserRoleService = new Mock<IUserRoleService>();
            _mockPaginationService = new Mock<IPaginationService>();
            _mockSearchService = new Mock<ISearchService>();
            _mockNotificationService = new Mock<INotificationService>();

            _testUser = new ApplicationUser
            {
                Id = "test-user-id",
                UserName = "testuser@example.com",
                Email = "testuser@example.com"
            };

            _controller = new VehiclesController(
                _context,
                _mockUserManager.Object,
                _mockUserRoleService.Object,
                _mockPaginationService.Object,
                _mockSearchService.Object,
                _mockNotificationService.Object
            );

            SetupDefaultAuthorization();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private Mock<UserManager<ApplicationUser>> CreateMockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var options = new Mock<Microsoft.Extensions.Options.IOptions<IdentityOptions>>();
            var passwordHasher = new Mock<IPasswordHasher<ApplicationUser>>();
            var userValidators = new List<IUserValidator<ApplicationUser>> { new Mock<IUserValidator<ApplicationUser>>().Object };
            var passwordValidators = new List<IPasswordValidator<ApplicationUser>> { new Mock<IPasswordValidator<ApplicationUser>>().Object };
            var keyNormalizer = new Mock<ILookupNormalizer>();
            var errors = new Mock<IdentityErrorDescriber>();
            var services = new Mock<IServiceProvider>();
            var logger = new Mock<Microsoft.Extensions.Logging.ILogger<UserManager<ApplicationUser>>>();

            var mockUserManager = new Mock<UserManager<ApplicationUser>>(
                store.Object,
                options.Object,
                passwordHasher.Object,
                userValidators,
                passwordValidators,
                keyNormalizer.Object,
                errors.Object,
                services.Object,
                logger.Object
            );

            return mockUserManager;
        }

        private void SetupDefaultAuthorization()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _testUser.Id),
                new Claim(ClaimTypes.Name, _testUser.UserName ?? "testuser"),
                new Claim(ClaimTypes.Role, "FleetManager")
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

        [Fact]
        public async Task Details_WithValidId_ReturnsViewResult()
        {
            // Arrange
            var vehicle = new Vehicle { Id = 201, Model = "Toyota Camry", LicensePlate = "ABC123", Status = VehicleState.available, Type = VehicleType.Car, Capacity = 4 };
            _context.Vehicles.Add(vehicle);
            _context.SaveChanges();
            _mockUserRoleService.Setup(s => s.HasAccessToVehicles(It.IsAny<ApplicationUser>())).ReturnsAsync(true);
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(_testUser);

            // Act
            var result = await _controller.Details(201);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            viewResult.ViewData["PageTitle"].Should().Be("Vehicles");
        }

        [Fact]
        public async Task Details_WithNullId_ReturnsNotFound()
        {
            // Arrange
            _mockUserRoleService.Setup(s => s.HasAccessToVehicles(It.IsAny<ApplicationUser>())).ReturnsAsync(true);
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(_testUser);

            // Act
            var result = await _controller.Details(null);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Details_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            _mockUserRoleService.Setup(s => s.HasAccessToVehicles(It.IsAny<ApplicationUser>())).ReturnsAsync(true);
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(_testUser);

            // Act
            var result = await _controller.Details(9999);

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Create_Get_WithValidAccess_ReturnsViewResult()
        {
            // Arrange
            _mockUserRoleService.Setup(s => s.HasAccessToVehicles(It.IsAny<ApplicationUser>())).ReturnsAsync(true);
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(_testUser);

            // Act
            var result = await _controller.Create();

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            viewResult.ViewData["PageTitle"].Should().Be("Vehicles");
        }

        [Fact]
        public async Task Create_Post_WithValidModel_RedirectsToIndex()
        {
            // Arrange
            _mockUserRoleService.Setup(s => s.HasAccessToVehicles(It.IsAny<ApplicationUser>())).ReturnsAsync(true);
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(_testUser);
            var vehicle = new Vehicle
            {
                Id = 301,
                Model = "Toyota Camry",
                Type = VehicleType.Car,
                Capacity = 5,
                LicensePlate = "ABC123",
                Status = VehicleState.available,
                TotalDistanceTraveled = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            // Act
            var result = await _controller.Create(vehicle, null);

            // Assert
            var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirectResult.ActionName.Should().Be(nameof(VehiclesController.Index));
        }

        [Fact]
        public async Task Create_Post_WithInvalidModel_ReturnsView()
        {
            // Arrange
            _mockUserRoleService.Setup(s => s.HasAccessToVehicles(It.IsAny<ApplicationUser>())).ReturnsAsync(true);
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(_testUser);
            var vehicle = new Vehicle();
            _controller.ModelState.AddModelError("Model", "Model is required");

            // Act
            var result = await _controller.Create(vehicle, null);

            // Assert
            result.Should().BeOfType<ViewResult>();
        }

        [Fact]
        public async Task Edit_Get_WithValidId_ReturnsViewResult()
        {
            // Arrange
            var vehicle = new Vehicle { Id = 401, Model = "Toyota Camry", LicensePlate = "ABC123", Status = VehicleState.available, Type = VehicleType.Car, Capacity = 4 };
            _context.Vehicles.Add(vehicle);
            _context.SaveChanges();
            _mockUserRoleService.Setup(s => s.HasAccessToVehicles(It.IsAny<ApplicationUser>())).ReturnsAsync(true);
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(_testUser);

            // Act
            var result = await _controller.Edit(401);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            viewResult.ViewData["PageTitle"].Should().Be("Vehicles");
        }

        [Fact]
        public async Task Delete_Get_WithValidId_ReturnsViewResult()
        {
            // Arrange
            var vehicle = new Vehicle { Id = 501, Model = "Toyota Camry", LicensePlate = "ABC123", Status = VehicleState.available, Type = VehicleType.Car, Capacity = 4 };
            _context.Vehicles.Add(vehicle);
            _context.SaveChanges();
            _mockUserRoleService.Setup(s => s.HasAccessToVehicles(It.IsAny<ApplicationUser>())).ReturnsAsync(true);
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(_testUser);

            // Act
            var result = await _controller.Delete(501);

            // Assert
            result.Should().BeOfType<ViewResult>();
        }

        [Fact]
        public async Task DeleteConfirmed_WithValidId_RedirectsToIndex()
        {
            // Arrange
            var vehicle = new Vehicle { Id = 502, Model = "Toyota Camry", LicensePlate = "ABC123", Status = VehicleState.available, Type = VehicleType.Car, Capacity = 4 };
            _context.Vehicles.Add(vehicle);
            _context.SaveChanges();
            _mockUserRoleService.Setup(s => s.HasAccessToVehicles(It.IsAny<ApplicationUser>())).ReturnsAsync(true);
            _mockUserManager.Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(_testUser);

            // Act
            var result = await _controller.DeleteConfirmed(502);

            // Assert
            var redirectResult = result.Should().BeOfType<RedirectToActionResult>().Subject;
            redirectResult.ActionName.Should().Be(nameof(VehiclesController.Index));
        }
    }
} 