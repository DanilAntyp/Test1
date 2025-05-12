using Test1.Models;

namespace Test1.Services;

public interface IAppointmentService
{
   Task<AppointmentDTO> GetAppointmentByIdAsync(int appointment_id);
   Task AddAppointmentAsync(CreateAppointmentDTO dto);                  

    
}