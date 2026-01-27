using Xunit;
using ADWebApplication.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace ADWebApplication.Tests
{
    public class HomeControllerTests
    {
        [Fact]
        public void Index_ReturnsViewResult()
        {
            // Arrange
            var controller = new HomeController();

            // Act
            var result = controller.IndexAsync();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Null(viewResult.ViewName); // default view
        }
    }
}