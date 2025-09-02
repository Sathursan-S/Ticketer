using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Text.Json;

namespace Gateway.Api.Controllers
{
    /// <summary>
    /// Provides discovery endpoints for Swagger documentation
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class SwaggerDiscoveryController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public SwaggerDiscoveryController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Gets all available Swagger endpoints for microservices
        /// </summary>
        /// <returns>A list of available Swagger endpoints</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SwaggerEndpoint>), StatusCodes.Status200OK)]
        public IActionResult GetSwaggerEndpoints()
        {
            var endpoints = new List<SwaggerEndpoint>
            {
                new SwaggerEndpoint 
                { 
                    Name = "Gateway API", 
                    Url = "/swagger/v1/swagger.json", 
                    Version = "v1"
                },
                new SwaggerEndpoint 
                { 
                    Name = "Payment Service", 
                    Url = "/api/payments/swagger/v1/swagger.json", 
                    Version = "v1" 
                },
                new SwaggerEndpoint 
                { 
                    Name = "Auth Service", 
                    Url = "/api/auth/v3/api-docs", 
                    Version = "v1" 
                },
                new SwaggerEndpoint 
                { 
                    Name = "Booking Service", 
                    Url = "/api/booking/swagger/v1/swagger.json", 
                    Version = "v1" 
                },
                new SwaggerEndpoint 
                { 
                    Name = "Ticket Service", 
                    Url = "/api/tickets/swagger/v1/swagger.json", 
                    Version = "v1" 
                },
                new SwaggerEndpoint 
                { 
                    Name = "Events Service", 
                    Url = "/api/events/swagger/v1/swagger.json", 
                    Version = "v1" 
                },
                new SwaggerEndpoint 
                { 
                    Name = "Notification Service", 
                    Url = "/api/notifications/swagger/v1/swagger.json", 
                    Version = "v1" 
                }
            };

            return Ok(endpoints);
        }

        /// <summary>
        /// Data class representing a Swagger endpoint
        /// </summary>
        public class SwaggerEndpoint
        {
            /// <summary>
            /// The name of the service
            /// </summary>
            public string Name { get; set; }
            
            /// <summary>
            /// The URL to the Swagger JSON
            /// </summary>
            public string Url { get; set; }
            
            /// <summary>
            /// The API version
            /// </summary>
            public string Version { get; set; }
        }
    }
}
