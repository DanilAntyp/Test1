using Microsoft.AspNetCore.Mvc;
using Test1.Exceptions;
using Test1.Models;
using Test1.Services;

namespace Test1.Controllers         
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;

        public AppointmentsController(IAppointmentService appointmentService)
        {
            _appointmentService = appointmentService;
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetAppointmentById(int id)
        {
            var dto = await _appointmentService.GetAppointmentByIdAsync(id);

            if (dto is null)
                return NotFound($"Appointment {id} not found");

            return Ok(dto);
        }

        [HttpPost]
        public async Task<IActionResult> AddAppointment([FromBody] CreateAppointmentDTO body)
        {
            try
            {
                await _appointmentService.AddAppointmentAsync(body);
                return StatusCode(StatusCodes.Status201Created);   
            }
            catch (NotFoundException e)   
            {
                return NotFound(e.Message);        
            }
            catch (ArgumentException e)         
            {
                return BadRequest(e.Message);    
            }
        }
    }
}