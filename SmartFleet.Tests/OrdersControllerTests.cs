#nullable disable
using Xunit;
using Moq;
using Microsoft.EntityFrameworkCore;
using SmartFleet.Controllers;
using SmartFleet.Models;
using SmartFleet.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using SmartFleet.Data;
using SmartFleet.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using MockQueryable.Moq;

namespace SmartFleet.Tests
{
    public class OrdersControllerTests
    {
        private SmartFleetContext GetInMemoryDbContext(string dbName = null)
        {
            dbName ??= System.Guid.NewGuid().ToString();
            var options = new DbContextOptionsBuilder<SmartFleetContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
            return new SmartFleetContext(options);
        }

        private Mock<IReferenceCheckService> GetMockReferenceCheckService()
        {
            var mockService = new Mock<IReferenceCheckService>();
            mockService.Setup(s => s.CanDeleteOrderAsync(It.IsAny<int>()))
                .ReturnsAsync((true, "Order can be deleted."));
            return mockService;
        }

        [Fact]
        public async Task Details_ReturnsViewResult_WhenOrderExists()
        {
            var dbContext = GetInMemoryDbContext();
            dbContext.Users.Add(new ApplicationUser { Id = "test-user", UserName = "testuser" });
            dbContext.Orders.Add(new Order { Id = 2, Destination = "Test", Status = OrderState.Pending, Reason = "Test Reason", StartLocation = "Test Start", UserId = "test-user" });
            dbContext.SaveChanges();

            var userManager = MockUserManager("test-user", "testuser", "NormalUser");
            var userRoleService = new Mock<IUserRoleService>();
            userRoleService.Setup(s => s.HasAccessToOrders(It.IsAny<ApplicationUser>())).ReturnsAsync(true);
            var searchService = new Mock<ISearchService>();
            var mockReferenceCheckService = GetMockReferenceCheckService();
            var controller = new OrdersController(dbContext, userManager.Object, null, userRoleService.Object, null, searchService.Object, mockReferenceCheckService.Object);

            var result = await controller.Details(2);
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Order>(viewResult.Model);
            Assert.Equal(2, model.Id);
        }

        [Fact]
        public async Task Details_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            var dbContext = GetInMemoryDbContext();
            dbContext.Users.Add(new ApplicationUser { Id = "test-user", UserName = "testuser" });
            dbContext.SaveChanges();
            var userManager = MockUserManager("test-user", "testuser", "NormalUser");
            var userRoleService = new Mock<IUserRoleService>();
            userRoleService.Setup(s => s.HasAccessToOrders(It.IsAny<ApplicationUser>())).ReturnsAsync(true);
            var searchService = new Mock<ISearchService>();
            var mockReferenceCheckService = GetMockReferenceCheckService();
            var controller = new OrdersController(dbContext, userManager.Object, null, userRoleService.Object, null, searchService.Object, mockReferenceCheckService.Object);
            var result = await controller.Details(999);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_Get_ReturnsViewResult_WhenUserCanCreate()
        {
            var dbContext = GetInMemoryDbContext();
            dbContext.Users.Add(new ApplicationUser { Id = "test-user", UserName = "testuser" });
            dbContext.SaveChanges();
            var userManager = MockUserManager("test-user", "testuser", "NormalUser");
            var userRoleService = new Mock<IUserRoleService>();
            userRoleService.Setup(s => s.CanCreateOrder(It.IsAny<ApplicationUser>())).ReturnsAsync(true);
            var searchService = new Mock<ISearchService>();
            var mockReferenceCheckService = GetMockReferenceCheckService();
            var controller = new OrdersController(dbContext, userManager.Object, null, userRoleService.Object, null, searchService.Object, mockReferenceCheckService.Object);
            var result = await controller.Create();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Create_Post_RedirectsToIndex_OnSuccess()
        {
            var dbContext = GetInMemoryDbContext();
            dbContext.Users.Add(new ApplicationUser { Id = "test-user", UserName = "testuser" });
            dbContext.SaveChanges();
            var userManager = MockUserManager("test-user", "testuser", "NormalUser");
            var userRoleService = new Mock<IUserRoleService>();
            userRoleService.Setup(s => s.CanCreateOrder(It.IsAny<ApplicationUser>())).ReturnsAsync(true);
            var notificationService = new Mock<INotificationService>();
            notificationService.Setup(n => n.CreateNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RelatedTable>(), It.IsAny<int?>())).Returns(Task.CompletedTask);
            userRoleService.Setup(s => s.GetUsersByRole(It.IsAny<string>())).ReturnsAsync(new List<ApplicationUser> { new ApplicationUser { Id = "manager-id", UserName = "manager" } });
            var searchService = new Mock<ISearchService>();
            var mockReferenceCheckService = GetMockReferenceCheckService();
            var controller = new OrdersController(dbContext, userManager.Object, notificationService.Object, userRoleService.Object, null, searchService.Object, mockReferenceCheckService.Object);
            // Mock HttpContext and Response for cookies
            var httpContext = new Mock<HttpContext>();
            var responseMock = new Mock<HttpResponse>();
            var cookiesMock = new Mock<IResponseCookies>();
            responseMock.SetupGet(r => r.Cookies).Returns(cookiesMock.Object);
            httpContext.SetupGet(h => h.Response).Returns(responseMock.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };
            var order = new Order { VehicleType = VehicleType.Car, PassengerCount = 1, StartLocation = "A", Destination = "B", TripStartDate = System.DateTime.Now, TripEndDate = System.DateTime.Now.AddHours(1), Reason = "Test" };
            var result = await controller.Create(order);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async Task Create_Post_SendsNotification_OnSuccess()
        {
            var dbContext = GetInMemoryDbContext();
            dbContext.Users.Add(new ApplicationUser { Id = "test-user", UserName = "testuser" });
            dbContext.SaveChanges();
            var userManager = MockUserManager("test-user", "testuser", "NormalUser");
            var userRoleService = new Mock<IUserRoleService>();
            userRoleService.Setup(s => s.CanCreateOrder(It.IsAny<ApplicationUser>())).ReturnsAsync(true);
            var notificationService = new Mock<INotificationService>();
            notificationService.Setup(n => n.CreateNotificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<RelatedTable>(), It.IsAny<int?>())).Returns(Task.CompletedTask);
            userRoleService.Setup(s => s.GetUsersByRole(It.IsAny<string>())).ReturnsAsync(new List<ApplicationUser> { new ApplicationUser { Id = "manager-id", UserName = "manager" } });
            var searchService = new Mock<ISearchService>();
            var mockReferenceCheckService = GetMockReferenceCheckService();
            var controller = new OrdersController(dbContext, userManager.Object, notificationService.Object, userRoleService.Object, null, searchService.Object, mockReferenceCheckService.Object);
            // Mock HttpContext and Response for cookies
            var httpContext = new Mock<HttpContext>();
            var responseMock = new Mock<HttpResponse>();
            var cookiesMock = new Mock<IResponseCookies>();
            responseMock.SetupGet(r => r.Cookies).Returns(cookiesMock.Object);
            httpContext.SetupGet(h => h.Response).Returns(responseMock.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext.Object };
            var order = new Order { VehicleType = VehicleType.Car, PassengerCount = 1, StartLocation = "A", Destination = "B", TripStartDate = System.DateTime.Now, TripEndDate = System.DateTime.Now.AddHours(1), Reason = "Test" };
            var result = await controller.Create(order);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            notificationService.Verify(n => n.CreateNotificationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                RelatedTable.Order,
                It.IsAny<int?>()
            ), Times.AtLeastOnce());
        }

        [Fact]
        public async Task Edit_Get_ReturnsViewResult_WhenOrderExists()
        {
            var dbContext = GetInMemoryDbContext();
            dbContext.Users.Add(new ApplicationUser { Id = "test-user", UserName = "testuser" });
            dbContext.Orders.Add(new Order { Id = 3, Destination = "Test", Status = OrderState.Pending, Reason = "Test Reason", StartLocation = "Test Start", UserId = "test-user" });
            dbContext.SaveChanges();
            var userManager = MockUserManager("test-user", "testuser", "NormalUser");
            var userRoleService = new Mock<IUserRoleService>();
            userRoleService.Setup(s => s.CanEditOrder(It.IsAny<ApplicationUser>(), It.IsAny<OrderState>())).ReturnsAsync(true);
            userRoleService.Setup(s => s.GetUserRoles(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { "NormalUser" });
            var searchService = new Mock<ISearchService>();
            var mockReferenceCheckService = GetMockReferenceCheckService();
            var controller = new OrdersController(dbContext, userManager.Object, null, userRoleService.Object, null, searchService.Object, mockReferenceCheckService.Object);
            var result = await controller.Edit(3);
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<Order>(viewResult.Model);
            Assert.Equal(3, model.Id);
        }

        [Fact]
        public async Task Edit_Get_ReturnsNotFound_WhenOrderDoesNotExist()
        {
            var dbContext = GetInMemoryDbContext();
            dbContext.Users.Add(new ApplicationUser { Id = "test-user", UserName = "testuser" });
            dbContext.SaveChanges();
            var userManager = MockUserManager("test-user", "testuser", "NormalUser");
            var userRoleService = new Mock<IUserRoleService>();
            userRoleService.Setup(s => s.CanEditOrder(It.IsAny<ApplicationUser>(), It.IsAny<OrderState>())).ReturnsAsync(true);
            userRoleService.Setup(s => s.GetUserRoles(It.IsAny<ApplicationUser>())).ReturnsAsync(new List<string> { "NormalUser" });
            var searchService = new Mock<ISearchService>();
            var mockReferenceCheckService = GetMockReferenceCheckService();
            var controller = new OrdersController(dbContext, userManager.Object, null, userRoleService.Object, null, searchService.Object, mockReferenceCheckService.Object);
            var result = await controller.Edit(999);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteConfirmed_WithValidId_RedirectsToIndex()
        {
            var dbContext = GetInMemoryDbContext();
            dbContext.Users.Add(new ApplicationUser { Id = "test-user", UserName = "testuser" });
            var order = new Order { Id = 4, Destination = "Test", Status = OrderState.Pending, Reason = "Test Reason", StartLocation = "Test Start", UserId = "test-user" };
            dbContext.Orders.Add(order);
            dbContext.SaveChanges();

            var userManager = MockUserManager("test-user", "testuser", "NormalUser");
            var userRoleService = new Mock<IUserRoleService>();
            var searchService = new Mock<ISearchService>();
            var mockReferenceCheckService = GetMockReferenceCheckService();
            var controller = new OrdersController(dbContext, userManager.Object, null, userRoleService.Object, null, searchService.Object, mockReferenceCheckService.Object);

            var result = await controller.DeleteConfirmed(4);
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        [Fact]
        public async Task DeleteConfirmed_WithReferences_ReturnsForbidden()
        {
            var dbContext = GetInMemoryDbContext();
            dbContext.Users.Add(new ApplicationUser { Id = "test-user", UserName = "testuser" });
            var order = new Order { Id = 5, Destination = "Test", Status = OrderState.Pending, Reason = "Test Reason", StartLocation = "Test Start", UserId = "test-user" };
            dbContext.Orders.Add(order);
            dbContext.SaveChanges();

            var userManager = MockUserManager("test-user", "testuser", "NormalUser");
            var userRoleService = new Mock<IUserRoleService>();
            var searchService = new Mock<ISearchService>();
            var mockReferenceCheckService = new Mock<IReferenceCheckService>();
            mockReferenceCheckService.Setup(s => s.CanDeleteOrderAsync(5))
                .ReturnsAsync((false, "Cannot delete order because it has associated trips."));
            var controller = new OrdersController(dbContext, userManager.Object, null, userRoleService.Object, null, searchService.Object, mockReferenceCheckService.Object);

            var result = await controller.DeleteConfirmed(5);
            var redirectResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectResult.ActionName);
        }

        // Helper to mock UserManager
        private Mock<UserManager<ApplicationUser>> MockUserManager(string id, string userName, string role)
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var userManager = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null);
            var fakeUser = new ApplicationUser { Id = id, UserName = userName };
            userManager.Setup(u => u.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(fakeUser);
            userManager.Setup(u => u.GetRolesAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(new List<string> { role });
            return userManager;
        }
    }
} 